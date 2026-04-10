import { callApi } from '$lib/api';

export interface User {
	id:        string;
	companyId: string;
	name:      string;
	surname:   string;
	email:     string;
}

export async function getUsersByCompany(companyId: string): Promise<User[]> {
	return callApi<User[]>(`api/users?companyId=${encodeURIComponent(companyId)}`);
}
