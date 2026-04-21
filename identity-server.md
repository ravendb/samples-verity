# Logging in to Verity

Verity requires you to sign in before you can use it. The sign-in screen is handled by a separate service (IdentityServer) that runs automatically alongside the rest of the app. You do not need to configure anything to get started.

## Running the app

```bash
cd src/RavenDB.Samples.Verity.AppHost
dotnet run
```

Once everything is up, open the **Aspire dashboard** at http://localhost:15000. You will see several services listed there.

**Before you can log in for the first time**, you need to create the database and seed the initial user accounts. Click the **Migrate DB** button next to the `app` service in the dashboard. Wait for it to complete (a few seconds).

## Logging in

Open the Verity app in your browser. The address is shown in the Aspire dashboard next to the `bff` service — it will be something like `https://localhost:7443`. You will be redirected to the login page automatically.

Two accounts are created by the migration:

| Email | Password |
|---|---|
| admin@verity.local | admin |
| auditor@verity.local | auditor |

Type one of those and click **Sign in**. You will be taken back to the app.

## Adding more user accounts

User accounts are stored as documents in RavenDB. To add a new account:

1. Open RavenDB Studio — the address is shown next to the `RavenDB` service in the Aspire dashboard.
2. Go to **Documents → IdentityUsers**.
3. Click **New document**.
4. Paste this, fill in the fields, and save:

```json
{
    "SubjectId": "choose-any-unique-id",
    "Username": "the-email-you-will-type-at-login",
    "Email": "the-email-you-will-type-at-login",
    "DisplayName": "Full Name Shown in the App",
    "PasswordHash": "paste-the-hash-generated-below",
    "IsActive": true,
    "Claims": [],
    "@metadata": { "@collection": "IdentityUsers" }
}
```

Set the document ID to `IdentityUsers/` followed by the same value you chose for `SubjectId` (for example `IdentityUsers/alice`).

**Generating a password hash**

Passwords are not stored as plain text. Run this one-liner to get the value to paste into `PasswordHash`:

```bash
dotnet script -e "using Microsoft.AspNetCore.Identity; Console.WriteLine(new PasswordHasher<object>().HashPassword(null!, args[0]));" -- "the-plain-text-password"
```

If you don't have `dotnet-script`, install it once with:

```bash
dotnet tool install -g dotnet-script
```

## Production deployments

When running in production, replace the built-in dev secrets with strong values. Set these once with the .NET user-secrets tool (run from anywhere — the `--id` flag targets the AppHost):

```bash
dotnet user-secrets set --id 8a63def4-dd24-4ac6-9074-5602c497b6cc "InternalJwtKey"         "<at least 32 random characters>"
dotnet user-secrets set --id 8a63def4-dd24-4ac6-9074-5602c497b6cc "OidcClientSecret"       "<random string>"
dotnet user-secrets set --id 8a63def4-dd24-4ac6-9074-5602c497b6cc "IdentityAdmin:Email"    "your-admin@yourdomain.com"
dotnet user-secrets set --id 8a63def4-dd24-4ac6-9074-5602c497b6cc "IdentityAdmin:Password" "<strong password>"
```

Do this **before** running Migrate DB for the first time. The migration reads these values and stores a hashed version — they cannot be changed by re-running the migration afterward. If you need to change them after the first run, delete the documents `IdentityClients/verity-bff` and `IdentityUsers/admin` in RavenDB Studio and click Migrate DB again.
