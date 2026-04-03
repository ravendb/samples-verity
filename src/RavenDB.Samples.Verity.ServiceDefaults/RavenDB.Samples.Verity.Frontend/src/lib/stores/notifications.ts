import { writable } from 'svelte/store';
import { getNotificationCount } from '$lib/services/user';

/**
 * Store for managing notification count state
 */
export const notificationCount = writable<number>(0);

let pollInterval: ReturnType<typeof setInterval> | null = null;

/**
 * Fetches and updates the notification count
 */
export async function updateNotificationCount(): Promise<void> {
	try {
		const result = await getNotificationCount();
		notificationCount.set(result.count);
	} catch (error) {
		console.error('Failed to fetch notification count from API:', error);
		// Don't update the count on error to keep the last known state
	}
}

/**
 * Starts polling for notification count updates
 * @param intervalMs - Polling interval in milliseconds (default: 60000ms = 1 minute)
 */
export function startNotificationPolling(intervalMs: number = 60000): void {
	// Stop any existing polling
	stopNotificationPolling();

	// Initial fetch
	updateNotificationCount();

	// Set up periodic polling
	pollInterval = setInterval(updateNotificationCount, intervalMs);
}

/**
 * Stops the notification polling
 */
export function stopNotificationPolling(): void {
	if (pollInterval) {
		clearInterval(pollInterval);
		pollInterval = null;
	}
}
