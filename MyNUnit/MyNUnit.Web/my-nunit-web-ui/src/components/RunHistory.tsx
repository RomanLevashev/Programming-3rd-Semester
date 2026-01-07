import { Badge, Button, Card, Table } from 'react-bootstrap';
import type { RunSummary } from '../types';
import { formatDuration, formatDate } from '../utils/format';

type Props = {
  runs: RunSummary[];
  selectedRunId: string | null;
  running: boolean;
  onSelectRun: (id: string) => void;
  onRefresh: () => void;
  onClear: () => void;
};

export function RunHistory({ runs, selectedRunId, running, onSelectRun, onRefresh, onClear }: Props) {
  return (
    <Card>
      <Card.Header className="d-flex justify-content-between align-items-center">
        <span>История запусков</span>
        <div className="d-flex gap-2">
          <Button size="sm" variant="outline-secondary" onClick={onRefresh} disabled={running}>
            Обновить
          </Button>
          <Button size="sm" variant="outline-danger" onClick={onClear} disabled={running || runs.length === 0}>
            Очистить
          </Button>
        </div>
      </Card.Header>
      <Card.Body className="history-table">
        {runs.length === 0 ? (
          <div className="text-muted">Запусков пока не было</div>
        ) : (
          <Table bordered hover size="sm" responsive>
            <thead>
              <tr>
                <th>Начало</th>
                <th>Итог</th>
                <th>Длительность</th>
              </tr>
            </thead>
            <tbody>
              {runs.map((run) => (
                <tr
                  key={run.id}
                  className={selectedRunId === run.id ? 'table-active' : ''}
                  onClick={() => onSelectRun(run.id)}
                  role="button"
                >
                  <td>{formatDate(run.startedAt)}</td>
                  <td>
                    <Badge bg="success" className="me-1">
                      {run.passed}
                    </Badge>
                    <Badge bg="danger" className="me-1">
                      {run.failed}
                    </Badge>
                    <Badge bg="warning" text="dark">
                      {run.ignored}
                    </Badge>
                  </td>
                  <td>{formatDuration(run.totalDurationMs)}</td>
                </tr>
              ))}
            </tbody>
          </Table>
        )}
      </Card.Body>
    </Card>
  );
}
