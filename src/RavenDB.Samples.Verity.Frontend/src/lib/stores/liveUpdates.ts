import { writable } from 'svelte/store';

export const lastUpdatedReportId = writable<string | null>(null);
