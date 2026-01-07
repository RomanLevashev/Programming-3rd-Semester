import { Button, Card, Form, Spinner } from 'react-bootstrap';
import type { AssemblyUploadDto } from '../types';

type Props = {
  assemblies: AssemblyUploadDto[];
  selectedIds: Set<string>;
  running: boolean;
  uploading: boolean;
  onToggle: (id: string, checked: boolean) => void;
  onSelectAll: () => void;
  onClear: () => void;
  onDelete: (id: string) => void;
  onDeleteAll: () => void;
  onRun: () => void;
};

export function AssemblySelector({
  assemblies,
  selectedIds,
  running,
  uploading,
  onToggle,
  onSelectAll,
  onClear,
  onDelete,
  onDeleteAll,
  onRun,
}: Props) {
  return (
    <Card className="mt-3">
      <Card.Header>Выбор сборок</Card.Header>
      <Card.Body className="assembly-list">
        {assemblies.length === 0 && <div className="text-muted">Нет загруженных сборок</div>}
        {assemblies.map((assembly) => (
          <div key={assembly.id} className="d-flex justify-content-between align-items-center mb-2">
            <Form.Check
              type="checkbox"
              label={`${assembly.fileName} — ${(assembly.sizeBytes / 1024).toFixed(1)} КБ`}
              checked={selectedIds.has(assembly.id)}
              onChange={(e) => onToggle(assembly.id, e.target.checked)}
            />
            <Button
              size="sm"
              variant="outline-danger"
              onClick={() => onDelete(assembly.id)}
              disabled={running || uploading}
            >
              Удалить
            </Button>
          </div>
        ))}
        {assemblies.length > 0 && (
          <div className="d-flex gap-2 mt-3 flex-wrap">
            <Button size="sm" variant="outline-primary" onClick={onSelectAll}>
              Выбрать все
            </Button>
            <Button size="sm" variant="outline-secondary" onClick={onClear}>
              Очистить выбор
            </Button>
            <Button
              size="sm"
              variant="outline-danger"
              onClick={onDeleteAll}
              disabled={running || uploading}
            >
              Удалить все сборки
            </Button>
          </div>
        )}
      </Card.Body>
      <Card.Footer className="d-flex justify-content-between align-items-center">
        <div>
          {running && (
            <Spinner animation="border" size="sm" role="status" className="me-2">
              <span className="visually-hidden">Running...</span>
            </Spinner>
          )}
          {selectedIds.size} выбрано
        </div>
        <Button
          variant="success"
          onClick={onRun}
          disabled={running || uploading || selectedIds.size === 0}
        >
          Начать тестирование
        </Button>
      </Card.Footer>
    </Card>
  );
}
