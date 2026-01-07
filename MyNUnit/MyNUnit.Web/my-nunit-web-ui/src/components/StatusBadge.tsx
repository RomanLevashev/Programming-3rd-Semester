import { Badge } from 'react-bootstrap';

export function StatusBadge({ status }: { status: string }) {
  const variant =
    status === 'Passed' ? 'success' : status === 'Ignored' ? 'warning' : 'danger';

  return (
    <Badge bg={variant} className="text-uppercase">
      {status}
    </Badge>
  );
}
