import type { AssemblyUploadDto, RunDetails, RunSummary } from './types';

const API_BASE = (import.meta.env.VITE_API_BASE as string | undefined) ?? 'http://localhost:5091';

async function fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, init);
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || response.statusText);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const contentLength = response.headers.get('content-length');
  if (contentLength === '0') {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export async function getAssemblies() {
  return fetchJson<AssemblyUploadDto[]>('/api/assemblies');
}

export async function deleteAssembly(id: string) {
  return fetchJson<void>(`/api/assemblies/${id}`, { method: 'DELETE' });
}

export async function deleteAllAssemblies() {
  return fetchJson<void>('/api/assemblies', { method: 'DELETE' });
}

export async function uploadAssemblies(files: FileList) {
  const formData = new FormData();
  Array.from(files).forEach((file) => formData.append('files', file));
  return fetchJson<AssemblyUploadDto[]>('/api/assemblies', {
    method: 'POST',
    body: formData,
  });
}

export async function getRuns() {
  return fetchJson<RunSummary[]>('/api/runs');
}

export async function getRunDetails(id: string) {
  return fetchJson<RunDetails>(`/api/runs/${id}`);
}

export async function runTests(assemblyIds: string[]) {
  return fetchJson<RunDetails>('/api/runs', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ assemblyIds }),
  });
}

export async function clearRuns() {
  return fetchJson<void>('/api/runs', { method: 'DELETE' });
}
