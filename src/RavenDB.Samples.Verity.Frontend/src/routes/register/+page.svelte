<script lang="ts">
	import { browser } from '$app/environment';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	const returnUrl = $page.url.searchParams.get('returnUrl') ?? '/';

	if (browser) goto(`/login?tab=register&returnUrl=${encodeURIComponent(returnUrl)}`, { replaceState: true });
</script>
