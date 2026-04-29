/**
 * BFF auth client — wraps /bff/user and provides typed session info.
 * The /bff/user endpoint returns an array of claims when authenticated,
 * or HTTP 401 when the session cookie is absent/expired.
 */

export interface UserInfo {
    sub:        string;
    name:       string;
    givenName:  string;
    familyName: string;
    email:      string;
    role:       string;
    companyId:  string | null;
    logoutUrl:  string;
}

interface BffClaim {
    type:  string;
    value: string;
}

export async function getUser(): Promise<UserInfo | null> {
    try {
        // X-CSRF: 1 is required by Duende BFF on all management endpoints.
        const res = await fetch('/bff/user', { headers: { 'X-CSRF': '1' } });
        if (!res.ok) return null;

        const claims: BffClaim[] = await res.json();
        const get = (type: string) => claims.find(c => c.type === type)?.value ?? '';

        return {
            sub:        get('sub'),
            name:       get('name') || get('preferred_username') || get('email') || get('sub'),
            givenName:  get('given_name'),
            familyName: get('family_name'),
            email:      get('email'),
            role:       get('role') || 'User',
            companyId:  claims.find(c => c.type === 'company_id')?.value ?? null,
            logoutUrl:  get('bff:logout_url'),
        };
    } catch {
        return null;
    }
}

export interface RegisterData {
    username:    string;
    password:    string;
    displayName: string;
    email:       string;
    role:        'User' | 'Employee';
    companyId:   string | null;
}

export async function register(data: RegisterData): Promise<{ success: boolean; error?: string }> {
    try {
        const res = await fetch('/bff/register', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data),
        });
        if (res.ok) return { success: true };
        const body = await res.json().catch(() => ({}));
        return { success: false, error: body.error ?? 'Registration failed.' };
    } catch {
        return { success: false, error: 'Network error. Please try again.' };
    }
}

export function loginUrl(returnUrl = '/') {
    return `/bff/login?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export function registerUrl(returnUrl = '/') {
    return `/login?tab=register&returnUrl=${encodeURIComponent(returnUrl)}`;
}
