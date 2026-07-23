import { callApi, type PagedResult } from '$lib/api';

export interface SecurityEvent {
	id:        string;
	eventType: string;
	userId:    string | null;
	userName:  string | null;
	clientId:  string | null;
	ipAddress: string | null;
	at:        string;
	success:   boolean;
	details:   string | null;
}

export async function getSecurityEvents(page = 1, pageSize = 20): Promise<PagedResult<SecurityEvent>> {
	return callApi<PagedResult<SecurityEvent>>(`api/security/events?page=${page}&pageSize=${pageSize}`);
}
