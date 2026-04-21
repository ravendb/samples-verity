import type { Handle } from '@sveltejs/kit';

// BFF origin for server-to-server user info lookup; overridden by Aspire via env
const BFF_ORIGIN = process.env.BFF_ORIGIN ?? 'https://localhost:7443';

export const handle: Handle = async ({ event, resolve }) => {
	const cookie = event.request.headers.get('cookie') ?? '';

	try {
		const res = await fetch(`${BFF_ORIGIN}/bff/user`, {
			headers: { cookie }
		});

		if (res.status === 200) {
			const claims = (await res.json()) as { type: string; value: string }[];
			event.locals.user = {
				sub:   claims.find(c => c.type === 'sub')?.value   ?? null,
				name:  claims.find(c => c.type === 'name')?.value  ?? null,
				email: claims.find(c => c.type === 'email')?.value ?? null,
				sid:   claims.find(c => c.type === 'sid')?.value   ?? null
			};
		} else {
			event.locals.user = null;
		}
	} catch {
		event.locals.user = null;
	}

	return resolve(event);
};
