// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}

	// Extend ImportMetaEnv interface for Vite environment variables
	interface ImportMetaEnv {
		readonly BASE_API_HTTP: string;
	}

	// Vite define constants
	const _BASE_API_HTTP_: string | undefined;

	// Web Component: samples-ui-wrapper
	namespace svelteHTML {
		interface IntrinsicElements {
			'samples-ui-wrapper': {
				sourceLink: string;
				theme?: 'light' | 'dark';
				children?: import('svelte').Snippet;
			};
		}
	}
}

export {};
