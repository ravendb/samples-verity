import { page } from '@vitest/browser/context';
import { describe, expect, it } from 'vitest';
import { render } from 'vitest-browser-svelte';
import TipBox from './TipBox.svelte';

describe('TipBox.svelte', () => {
	it('should render both sections when both texts are provided', async () => {
		render(TipBox, {
			props: {
				contextText: 'This is context text',
				ravendbText: 'This is RavenDB text'
			}
		});

		const contextHeading = page.getByRole('heading', { name: 'Library Tip' });
		const ravendbHeading = page.getByRole('heading', { name: 'RavenDB Tip' });

		await expect.element(contextHeading).toBeInTheDocument();
		await expect.element(ravendbHeading).toBeInTheDocument();
	});

	it('should render only context section when only contextText is provided', async () => {
		render(TipBox, {
			props: {
				contextText: 'This is context text'
			}
		});

		const contextHeading = page.getByRole('heading', { name: 'Library Tip' });
		await expect.element(contextHeading).toBeInTheDocument();

		const ravendbHeading = page.getByRole('heading', { name: 'RavenDB Tip', exact: false });
		await expect.element(ravendbHeading).not.toBeInTheDocument();
	});

	it('should render only RavenDB section when only ravendbText is provided', async () => {
		render(TipBox, {
			props: {
				ravendbText: 'This is RavenDB text'
			}
		});

		const ravendbHeading = page.getByRole('heading', { name: 'RavenDB Tip' });
		await expect.element(ravendbHeading).toBeInTheDocument();

		const contextHeading = page.getByRole('heading', { name: 'Library Tip', exact: false });
		await expect.element(contextHeading).not.toBeInTheDocument();
	});

	it('should not render anything when both texts are missing', async () => {
		const { container } = render(TipBox, {
			props: {}
		});

		expect(container.innerHTML).toBe('<!---->');
	});

	it('should render inline code with backticks', async () => {
		const { container } = render(TipBox, {
			props: {
				contextText: 'Use the `Book` model to represent books'
			}
		});

		const codeElement = container.querySelector('code');
		expect(codeElement).toBeTruthy();
		expect(codeElement?.textContent).toBe('Book');
	});

	it('should render links with markdown syntax', async () => {
		const { container } = render(TipBox, {
			props: {
				ravendbText: 'Check out [RavenDB documentation](https://ravendb.net/docs) for more info'
			}
		});

		const linkElement = container.querySelector('a');
		expect(linkElement).toBeTruthy();
		expect(linkElement?.href).toBe('https://ravendb.net/docs');
		expect(linkElement?.textContent).toBe('RavenDB documentation');
	});

	it('should render both inline code and links in same text', async () => {
		const { container } = render(TipBox, {
			props: {
				contextText:
					'Use the `Session.Load()` method. Learn more at [docs](https://ravendb.net/docs)'
			}
		});

		const codeElement = container.querySelector('code');
		const linkElement = container.querySelector('a');

		expect(codeElement).toBeTruthy();
		expect(codeElement?.textContent).toBe('Session.Load()');
		expect(linkElement).toBeTruthy();
		expect(linkElement?.textContent).toBe('docs');
	});

	it('should sanitize potentially harmful HTML', async () => {
		const { container } = render(TipBox, {
			props: {
				contextText: 'This is <script>alert("xss")</script> safe text'
			}
		});

		const scriptElements = container.querySelectorAll('script');
		expect(scriptElements.length).toBe(0);
	});

	it('should render plain text without markdown', async () => {
		const { container } = render(TipBox, {
			props: {
				contextText: 'This is plain text'
			}
		});

		const tipText = container.querySelector('.tip-text');
		expect(tipText?.textContent).toContain('This is plain text');
	});
});
