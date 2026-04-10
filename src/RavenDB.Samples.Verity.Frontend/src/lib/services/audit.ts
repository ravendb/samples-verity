import { callApi } from '$lib/api';

export interface AuditRevision {
	data:         Audit;
	changeVector: string;
	lastModified: string;
}

export interface Audit {
	id:             string;
	reportId:       string;
	auditorName:    string;
	auditorSurname: string;
	auditorEmail:   string;
	auditString:    string;
	generatedByAi:  boolean;
}

export interface SaveAuditRequest {
	reportId:       string;
	auditorName:    string;
	auditorSurname: string;
	auditorEmail:   string;
	auditString:    string;
	generatedByAi:  boolean;
}

export async function getAudit(reportId: string): Promise<Audit | null> {
	try {
		return await callApi<Audit>(`api/audit?reportId=${encodeURIComponent(reportId)}`);
	} catch (e: unknown) {
		if (e instanceof Error && e.message.startsWith('HTTP 404')) return null;
		throw e;
	}
}

export async function saveAudit(data: SaveAuditRequest): Promise<Audit> {
	return callApi<Audit>('api/audit', {
		method:  'POST',
		headers: { 'Content-Type': 'application/json' },
		body:    JSON.stringify(data),
	});
}

export async function restoreAuditRevision(auditId: string, changeVector: string): Promise<void> {
	return callApi<void>('api/audit/restore', {
		method:  'POST',
		headers: { 'Content-Type': 'application/json' },
		body:    JSON.stringify({ auditId, changeVector }),
	});
}

export async function getAuditRevisions(reportId: string): Promise<AuditRevision[] | null> {
	try {
		return await callApi<AuditRevision[]>(
			`api/audit/revisions?reportId=${encodeURIComponent(reportId)}`
		);
	} catch (e: unknown) {
		if (e instanceof Error && e.message.startsWith('HTTP 404')) return null;
		throw e;
	}
}

export async function generateAuditDraft(reportId: string, userId: string): Promise<string> {
	const result = await callApi<{ notes: string }>('api/agent/audit/generate', {
		method:  'POST',
		headers: { 'Content-Type': 'application/json' },
		body:    JSON.stringify({ reportId, userId }),
	});
	return result.notes;
}
