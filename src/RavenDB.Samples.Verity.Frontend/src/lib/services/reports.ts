import { callApi } from '$lib/api';

export interface Report {
	id:              string;
	companyId:       string;
	accessionNumber: string;
	filingDate:      string;
	reportDate:      string;
	daysBetween:     number;
	formType:        string;
	sourceUrl:       string;
	quarter?:        number | null;
	year?:           number | null;
	abbreviation?:   string | null;
	revenues?:       number | null;
	expenses?:       number | null;
	assetsVal?:		 number | null;
	profitLoss?:     number | null;
	profitable?:     boolean | null;
	summary?:        string | null;
}

export async function getReportsByCik(cik: string): Promise<Report[]> {
	return callApi<Report[]>(`api/reports?cik=${encodeURIComponent(cik)}`);
}

export async function getReport(accession: string): Promise<Report> {
	return callApi<Report>(`api/report?accession=${encodeURIComponent(accession)}`);
}

export async function fetch10Q(cik: string, max: number): Promise<void> {
	return callApi<void>(`api/fetch-10q?cik=${encodeURIComponent(cik)}&max=${max}`, {
		method: 'POST',
	});
}
