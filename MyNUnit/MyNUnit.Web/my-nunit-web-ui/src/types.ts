export type AssemblyUploadDto = {
  id: string;
  fileName: string;
  sizeBytes: number;
  uploadedAt: string;
};

export type RunSummary = {
  id: string;
  startedAt: string;
  completedAt: string;
  passed: number;
  failed: number;
  ignored: number;
  totalDurationMs: number;
};

export type TestResultDto = {
  className: string;
  methodName: string;
  status: string;
  durationMs: number;
  details?: string | null;
};

export type AssemblyRunDto = {
  id: number;
  assemblyUploadId?: string;
  assemblyName: string;
  passed: number;
  failed: number;
  ignored: number;
  durationMs: number;
  tests: TestResultDto[];
};

export type RunDetails = {
  id: string;
  startedAt: string;
  completedAt: string;
  passed: number;
  failed: number;
  ignored: number;
  totalDurationMs: number;
  assemblies: AssemblyRunDto[];
};
