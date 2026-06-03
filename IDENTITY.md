# Identity & Security in Verity

Verity is a financial audit platform — auditors review SEC filings, generate AI-assisted reports, and sign off on findings. That context shapes every identity decision made here.

## Why Duende IdentityServer

Verity has multiple companies, multiple auditors per company, and two distinct roles: regular users and company employees. A single shared login or ad-hoc JWT generation would not scale here. Duende IdentityServer provides a dedicated OAuth 2.0 / OpenID Connect authority that:

- Owns all user credentials and claims (name, email, `role`, `company_id`)
- Issues short-lived access tokens scoped to the `verity-api` resource
- Manages refresh tokens with one-time-use rotation, so a stolen refresh token is immediately invalidated on use
- Keeps auth logic out of the application — the Azure Functions backend only validates tokens, never issues them

For a financial platform with auditor accountability, having a central, auditable identity authority is not optional. It is the foundation that makes the audit trail meaningful.

## Why the BFF Pattern

The Verity frontend is a SvelteKit SPA. SPAs that store OAuth tokens in `localStorage` or JavaScript memory are vulnerable to XSS — any injected script can exfiltrate tokens silently.

The Backend-for-Frontend (BFF) pattern solves this by keeping tokens on the server:

```
Browser ──── cookie ────► BFF ──── Bearer token ────► Azure Functions
                           │
                           └──── OIDC ────────────────► IdentityServer
```

The browser never sees an access token. It authenticates using an `httpOnly` session cookie managed by `Duende.BFF`. Every `/api/*` call is forwarded by the BFF, which injects the current access token transparently. Token refresh happens automatically in the background via `Duende.AccessTokenManagement` — the frontend never has to think about token expiry.

The BFF also acts as the single external entry point: it proxies both the API and the Vite frontend dev server, so the browser always talks to one origin.

## Why FAPI 2.0

FAPI 2.0 (Financial-grade API Security Profile) is the security baseline required by Open Banking standards worldwide (UK, EU PSD2, AU CDR). For a platform that handles financial filings, applying FAPI 2.0 is the right posture — not because it is required here, but because it demonstrates what a real production deployment would need.

Two mechanisms are enabled on the `verity-bff` client:

**Pushed Authorization Requests (PAR)**
The browser never carries authorization parameters in the URL. Instead, the BFF pushes the full authorization request to IdentityServer's PAR endpoint first, receives a `request_uri`, and only that opaque reference appears in the browser redirect. This prevents parameter tampering and leakage via the referrer header or browser history.

**Demonstrating Proof-of-Possession (DPoP)**
Access tokens are bound to the BFF's RSA key pair. If an access token is intercepted in transit, it cannot be replayed by a different client — the recipient must prove possession of the private key that matches the public key in the token's `cnf` claim. The BFF generates a fresh RSA-PSS-SHA256 key pair on every startup.

## Identity Events in RavenDB

Every significant authentication action — login, logout, token issuance, client authentication failure — is written to the `SecurityEvents` collection in RavenDB by a custom `IEventSink` (`RavenEventSink`).

| Event | What it records |
|---|---|
| `UserLoginSuccess` | who logged in, from which IP, via which client |
| `UserLoginFailure` | attempted username, failure reason |
| `UserLogoutSuccess` | who logged out |
| `TokenIssuedSuccess` | subject, client, grant type |
| `TokenIssuedFailure` | client, error reason |
| `ClientAuthenticationFailure` | client ID, error (potential brute-force signal) |

Documents expire automatically after 90 days via RavenDB's built-in `@expires` metadata.

This matters for financial applications because compliance frameworks (SOC 2, ISO 27001, FAPI 2.0 itself) require evidence of *who authenticated and when*, not just *who changed what*. Storing these events in the same database as the audit records — queryable with RQL, visible in RavenDB Studio — creates a unified compliance picture: financial operations and the access history surrounding them, in one place.

## Architecture Overview

```
Browser
  │
  ▼
BFF  (Duende.BFF + YARP)
  │  httpOnly session cookie
  │  DPoP-bound access token forwarded to API
  ├──── /bff/login  → IdentityServer /authorize  (PAR + PKCE + DPoP)
  ├──── /bff/user   → session claims
  ├──── /api/*      → Azure Functions  (Bearer token injected)
  └──── /*          → Vite dev server  (frontend assets)

IdentityServer  (Duende IdentityServer 7.x)
  │  RavenDB user store
  │  In-memory clients & scopes
  └──── SecurityEvents → RavenDB  (RavenEventSink)

Azure Functions  (backend API)
  │  JWT Bearer validation (Authority = IdentityServer)
  │  [Authorize] on write endpoints
  └──── RavenDB  (Verity database)
```

## Setup

In addition to the prerequisites listed in the main README, Verity's identity layer requires a **Duende license key**.

Duende offers a free [Community Edition](https://duendesoftware.com/products/communityedition) for qualifying open-source projects. Once you have a key, store it as a user secret in the AppHost project:

```bash
cd src/RavenDB.Samples.Verity.AppHost
dotnet user-secrets set "Parameters:duende-license" "<your-license-key>"
```

The same key is forwarded to both the IdentityServer and BFF projects by Aspire at startup.

## Demo Credentials

After running the Setup migrations, the following accounts are available:

| Username | Password | Role | Company |
|---|---|---|---|
| `alice` | `Demo1234!` | Employee | Apple |
| `bob` | `Demo1234!` | Employee | Apple |
| `carol` | `Demo1234!` | Employee | Microsoft |
| `dave` | `Demo1234!` | Employee | Microsoft |
| `eve` | `Demo1234!` | User | — |

To create additional accounts, use the **Register** link in the top-right corner of the application.
