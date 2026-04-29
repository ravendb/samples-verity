<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	const returnUrl = $page.url.searchParams.get('returnUrl') ?? '/';

	goto(`/login?tab=register&returnUrl=${encodeURIComponent(returnUrl)}`, { replaceState: true });
</script>
