import { callApi, type PagedResult } from '$lib/api';

export interface Company {
	id: string;
	name: string;
	cik: string;
	sic?: string | null;
	sicDescription?: string | null;
	fiscalYearEnd?: string | null;
}

export async function getCompanies(page = 1, pageSize = 20): Promise<PagedResult<Company>> {
	return callApi<PagedResult<Company>>(`api/companies?page=${page}&pageSize=${pageSize}`);
}

export { type PagedResult };

export async function getCompany(cik: string): Promise<Company> {
	return callApi<Company>(`api/company?cik=${encodeURIComponent(cik)}`);
}

export async function saveCompany(cik: string): Promise<Company> {
	return callApi<Company>(`api/company?cik=${encodeURIComponent(cik)}`, {
		method: 'POST',
	});
}