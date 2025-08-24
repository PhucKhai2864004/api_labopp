# RAG Service for LabAssistant OPP

RAG (Retrieval-Augmented Generation) service for processing PDF assignments and providing AI-powered code review.

## Features

- PDF text extraction and chunking
- Vector embeddings generation using Ollama
- Vector storage using Qdrant
- Similarity search for context retrieval
- AI-powered code review with assignment context

## Prerequisites

- Node.js 18+
- Docker (optional)
- Ollama running on localhost:11434
- Qdrant running on localhost:6333

## Installation

### Local Development

1. Install dependencies:
```bash
npm install
```

2. Start the service:
```bash
npm start
```

### Docker

1. Build the image:
```bash
docker build -t rag-service .
```

2. Run the container:
```bash
docker run -p 3001:3001 rag-service
```

## API Endpoints

### Health Check
```
GET /health
```

### PDF Ingestion
```
POST /ingest
Content-Type: multipart/form-data

Form data:
- pdfFile: PDF file
- assignmentId: Assignment identifier
```

### Test PDF Ingestion
```
POST /test-ingest
Content-Type: multipart/form-data

Form data:
- pdfFile: PDF file
- assignmentId: Assignment identifier (optional)
```

### Code Review
```
POST /review
Content-Type: application/json

Body:
{
  "assignmentId": "string",
  "studentCode": "string",
  "algorithmType": "string (optional)",
  "language": "string (optional)"
}
```

### Suggest Test Cases
```
POST /suggest-testcases
Content-Type: application/json

Body:
{
  "assignmentId": "string",
  "studentCode": "string"
}

Response:
{
  "success": true,
  "assignmentId": "string",
  "testCases": [
    {
      "input": "input values",
      "expectedOutput": "expected result"
    }
  ],
  "suggestions": "general testing advice"
}
```

### Get Assignment Context
```
GET /context/:assignmentId
```

## Configuration

The service uses the following default configuration:

- **Port**: 3001
- **Chunk Size**: 2000 characters
- **Chunk Overlap**: 100 characters
- **File Size Limit**: 10MB
- **Ollama Model**: nomic-embed-text (embeddings), llama3.2:3b (LLM)
- **Qdrant Collection**: assignments

## Environment Variables

- `PORT`: Service port (default: 3001)
- `OLLAMA_URL`: Ollama service URL (default: http://localhost:11434)
- `QDRANT_URL`: Qdrant service URL (default: http://localhost:6333)

## Usage Examples

### Test with curl

1. Health check:
```bash
curl http://localhost:3001/health
```

2. Ingest PDF:
```bash
curl -X POST http://localhost:3001/ingest \
  -F "pdfFile=@assignment.pdf" \
  -F "assignmentId=assignment_001"
```

3. Review code:
```bash
curl -X POST http://localhost:3001/review \
  -H "Content-Type: application/json" \
  -d '{
    "assignmentId": "assignment_001",
    "studentCode": "public class Main { public static void main(String[] args) { System.out.println(\"Hello World\"); } }"
  }'
```

## Troubleshooting

1. **Service not starting**: Check if Ollama and Qdrant are running
2. **PDF processing errors**: Ensure PDF is not corrupted and under 10MB
3. **Embedding generation fails**: Verify Ollama is running and nomic-embed-text model is available
4. **Vector storage errors**: Check Qdrant connection and collection exists

## Development

For development with auto-restart:
```bash
npm run dev
```

## License

MIT
