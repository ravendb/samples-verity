import { callApi } from '$lib/api';

export interface User {
    id:         string;
    subjectId:  string;
    companyIds: string[];
    name:       string;
    surname:    string;
    email:      string;
    role:       string;
}

export async function getUsersByCompany(companyId: string): Promise<User[]> {
    return callApi<User[]>(`api/users?companyId=${encodeURIComponent(companyId)}`);
}

export async function getAllUsers(): Promise<User[]> {
    return callApi<User[]>('api/manage/users');
}

export async function setUserRole(subjectId: string, role: string): Promise<User> {
    return callApi<User>(`api/manage/users/${encodeURIComponent(subjectId)}/role`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ role }),
    });
}

export async function setUserCompanies(subjectId: string, companyIds: string[]): Promise<User> {
    return callApi<User>(`api/manage/users/${encodeURIComponent(subjectId)}/companies`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ companyIds }),
    });
}
