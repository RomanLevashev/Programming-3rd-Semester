import { Button, Card, Form } from 'react-bootstrap';

type Props = {
  uploading: boolean;
  onUpload: (files: FileList | null) => void;
  onRefresh: () => void;
};

export function UploadCard({ uploading, onUpload, onRefresh }: Props) {
  return (
    <Card>
      <Card.Header>Загрузка сборок</Card.Header>
      <Card.Body>
        <Form.Group controlId="uploadInput" className="mb-3">
          <Form.Label>Добавьте .dll (тесты и тестируемый код)</Form.Label>
          <Form.Control
            type="file"
            multiple
            accept=".dll"
            onChange={(e) => onUpload((e.target as HTMLInputElement).files)}
            disabled={uploading}
          />
          <Form.Text muted>Можно загружать по одному или пачкой, хранится история.</Form.Text>
        </Form.Group>
        <div className="d-flex gap-2">
          <Button variant="secondary" onClick={onRefresh} disabled={uploading}>
            Обновить список
          </Button>
        </div>
      </Card.Body>
    </Card>
  );
}
