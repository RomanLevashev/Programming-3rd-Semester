import { Badge, Card, Table } from 'react-bootstrap';
import type { RunDetails } from '../types';
import { formatDate, formatDuration } from '../utils/format';
import { StatusBadge } from './StatusBadge';

type Props = {
  run: RunDetails | null;
};

export function RunDetailsPanel({ run }: Props) {
  if (!run) {
    return (
      <Card className="mt-3">
        <Card.Header>Детали запуска</Card.Header>
        <Card.Body className="details-panel">
          <div className="text-muted">Выберите запуск, чтобы увидеть детали</div>
        </Card.Body>
      </Card>
    );
  }

  return (
    <Card className="mt-3">
      <Card.Header>Детали запуска</Card.Header>
      <Card.Body className="details-panel">
        <div className="d-flex justify-content-between align-items-center mb-3">
          <div>
            <div className="fw-semibold">{formatDate(run.startedAt)}</div>
            <div className="text-muted">
              Всего {run.passed + run.failed + run.ignored} тестов
            </div>
          </div>
          <div>
            <Badge bg="success" className="me-1">
              {run.passed} ok
            </Badge>
            <Badge bg="danger" className="me-1">
              {run.failed} fail
            </Badge>
            <Badge bg="warning" text="dark">
              {run.ignored} skip
            </Badge>
          </div>
        </div>
        {run.assemblies.map((assembly) => (
          <Card key={assembly.id} className="mb-3">
            <Card.Header>
              <div className="d-flex justify-content-between">
                <span>{assembly.assemblyName}</span>
                <span className="text-muted">{formatDuration(assembly.durationMs)}</span>
              </div>
            </Card.Header>
            <Card.Body className="p-0">
              <Table bordered size="sm" className="mb-0">
                <thead>
                  <tr>
                    <th>Тест</th>
                    <th>Статус</th>
                    <th>Время</th>
                    <th>Детали</th>
                  </tr>
                </thead>
                <tbody>
                  {assembly.tests.map((test, index) => (
                    <tr key={`${assembly.id}-${index}`}>
                      <td>
                        <div className="fw-semibold">{test.methodName}</div>
                        <div className="text-muted small">{test.className}</div>
                      </td>
                      <td className="text-nowrap">
                        <StatusBadge status={test.status} />
                      </td>
                      <td>{formatDuration(test.durationMs)}</td>
                      <td className="small">{test.details ?? '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </Table>
            </Card.Body>
          </Card>
        ))}
      </Card.Body>
    </Card>
  );
}
