import { callApi } from '$lib/api';
// callApi accepts options?: RequestInit and automatically parses JSON or text

// ── Types ─────────────────────────────────────────────────────

export interface Report {
	id:                   string;
	company:              string;
	cik:                  string;
	accessionNumber:      string;
	filingDate:           string;
	reportDate:           string;
	daysBetween:          number;
	formType:             string;
	sourceUrl:            string;
	profitLoss?:          number | null;
	profitable?:          boolean | null;
	profitabilitySummary?: string | null;
}

// ── API Functions ─────────────────────────────────────────────

/**
 * Fetches the test string from the API.
 */
export async function getTest(): Promise<string> {
	return callApi<string>('api/test');
}

/**
 * Fetches the list of all reports from RavenDB.
 */
export async function getReports(): Promise<Report[]> {
	return callApi<Report[]>('api/reports');
}

export interface PdfPage {
	page: number;
	text: string;
	words: string[];
}

export interface PdfResult {
	fileName: string;
	totalPages: number;
	pages: PdfPage[];
}

export async function readPdf(file: File): Promise<PdfResult> {
	const form = new FormData();
	form.append('file', file);

	return callApi<PdfResult>('api/read-pdf', {
		method: 'POST',
		body: form,
	});
}

export async function fetch10Q(cik: string, max: number): Promise<void> {
	return callApi<void>(`api/fetch-10q?cik=${encodeURIComponent(cik)}&max=${max}`, {
		method: 'POST',
	});
}