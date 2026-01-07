import { useEffect, useState } from 'react';
import { Alert, Col, Container, Row } from 'react-bootstrap';
import './App.css';
import { AssemblySelector } from './components/AssemblySelector';
import { RunDetailsPanel } from './components/RunDetailsPanel';
import { RunHistory } from './components/RunHistory';
import { UploadCard } from './components/UploadCard';
import {
  clearRuns,
  deleteAllAssemblies,
  deleteAssembly,
  getAssemblies,
  getRunDetails,
  getRuns,
  runTests,
  uploadAssemblies,
} from './api';
import type { AssemblyUploadDto, RunDetails, RunSummary } from './types';

function App() {
  const [assemblies, setAssemblies] = useState<AssemblyUploadDto[]>([]);
  const [selectedAssemblies, setSelectedAssemblies] = useState<Set<string>>(new Set());
  const [runs, setRuns] = useState<RunSummary[]>([]);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [runDetails, setRunDetails] = useState<RunDetails | null>(null);
  const [uploading, setUploading] = useState(false);
  const [running, setRunning] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [info, setInfo] = useState<string | null>(null);

  useEffect(() => {
    refreshAssemblies();
    refreshRuns();
  }, []);

  useEffect(() => {
    if (selectedRunId) {
      loadRunDetails(selectedRunId);
    }
  }, [selectedRunId]);

  const refreshAssemblies = async () => {
    try {
      const data = await getAssemblies();
      setAssemblies(data);
      if (data.length > 0 && selectedAssemblies.size === 0) {
        setSelectedAssemblies(new Set(data.map((x) => x.id)));
      }
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const refreshRuns = async () => {
    try {
      const data = await getRuns();
      setRuns(data);
      if (data.length > 0 && !selectedRunId) {
        setSelectedRunId(data[0].id);
      }
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const loadRunDetails = async (id: string) => {
    try {
      const detail = await getRunDetails(id);
      setRunDetails(detail);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleUpload = async (files: FileList | null) => {
    if (!files || files.length === 0) return;
    setError(null);
    setInfo(null);
    setUploading(true);
    try {
      const uploaded = await uploadAssemblies(files);
      setInfo(`Загружено файлов: ${uploaded.length}`);
      await refreshAssemblies();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setUploading(false);
    }
  };

  const toggleAssembly = (id: string, checked: boolean) => {
    setSelectedAssemblies((prev) => {
      const next = new Set(prev);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  };

  const selectAllAssemblies = () => setSelectedAssemblies(new Set(assemblies.map((a) => a.id)));

  const clearAssemblies = () => setSelectedAssemblies(new Set());

  const handleDeleteAssembly = async (id: string) => {
    setError(null);
    try {
      await deleteAssembly(id);
      setSelectedAssemblies((prev) => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
      await refreshAssemblies();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleDeleteAllAssemblies = async () => {
    setError(null);
    try {
      await deleteAllAssemblies();
      setSelectedAssemblies(new Set());
      await refreshAssemblies();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleClearRuns = async () => {
    setError(null);
    setInfo(null);
    try {
      await clearRuns();
      setRuns([]);
      setSelectedRunId(null);
      setRunDetails(null);
    } catch (err) {
      setError((err as Error).message);
    }
  };

  const handleRun = async () => {
    if (selectedAssemblies.size === 0) {
      setError('Выберите хотя бы одну сборку для запуска.');
      return;
    }

    setRunning(true);
    setError(null);
    setInfo('Запуск тестов...');
    try {
      const result = await runTests(Array.from(selectedAssemblies));
      setRunDetails(result);
      setSelectedRunId(result.id);
      await refreshRuns();
      setInfo('Тесты завершены');
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setRunning(false);
    }
  };

  return (
    <Container fluid className="py-4 app-background">
      <h2 className="mb-4 text-light">MyNUnit Web Runner</h2>
      <Row xs={1} md={2} className="g-3">
        <Col>
          <UploadCard uploading={uploading} onUpload={handleUpload} onRefresh={refreshAssemblies} />
          <AssemblySelector
            assemblies={assemblies}
            selectedIds={selectedAssemblies}
            running={running}
            uploading={uploading}
            onToggle={toggleAssembly}
            onSelectAll={selectAllAssemblies}
            onClear={clearAssemblies}
            onDelete={handleDeleteAssembly}
            onDeleteAll={handleDeleteAllAssemblies}
            onRun={handleRun}
          />
        </Col>

        <Col>
          <RunHistory
            runs={runs}
            selectedRunId={selectedRunId}
            running={running}
            onSelectRun={setSelectedRunId}
            onRefresh={refreshRuns}
            onClear={handleClearRuns}
          />
          <RunDetailsPanel run={runDetails} />
        </Col>
      </Row>

      <div className="alerts mt-3">
        {error && (
          <Alert variant="danger" onClose={() => setError(null)} dismissible>
            {error}
          </Alert>
        )}
        {info && (
          <Alert variant="info" onClose={() => setInfo(null)} dismissible>
            {info}
          </Alert>
        )}
      </div>
    </Container>
  );
}

export default App;
