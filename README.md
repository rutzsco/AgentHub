# AgentHub.Api

A .NET minimal API for the AgentHub project.

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later

### Running the API

1. Navigate to the api directory:cd api
2. Restore dependencies:dotnet restore
3. Build the project:dotnet build
4. Run the API:dotnet run
The API will start on `http://localhost:5000` by default.

## Endpoints

### Status Endpoint
- **GET** `/status`
- Returns the current status of the API
- Response:{
  "status": "OK",
  "timestamp": "2025-06-20T13:29:32.080944Z"
}
### Knowledge Endpoints
- **POST** `/knowledge`
- Index knowledge content to Azure AI Search
- **POST** `/knowledge/search`
- Search knowledge using hybrid vector search

### Agent Endpoints

#### Image Analysis Agent
- **POST** `/agents/image-analysis`
- Analyze images using Azure OpenAI (complete response)
- Request body:{
  "message": "Analyze this image",
  "threadId": "optional-thread-id",
  "files": [
    {
      "name": "image.jpg",
      "dataUrl": "data:image/jpeg;base64,..."
      }
    ]
  }- Returns complete response after processing

- **POST** `/agents/image-analysis/streaming`
- Analyze images using Azure OpenAI (streaming response)
- Same request body as above
- Returns streaming NDJSON responses

## Development

### Swagger Documentation
When running in development mode, Swagger UI is available at:
- `http://localhost:5000/swagger`

### HTTP Test Files
The project includes HTTP test files for easy endpoint testing:
- `knowledge-tests.http` - Knowledge endpoint tests
- `agent-tests.http` - Agent endpoint tests

These files can be used with VS Code REST Client extension or similar tools.

### Buildingdotnet build
### Testing# Test the status endpoint
curl http://localhost:5000/status

# Test image analysis (complete response)
curl -X POST http://localhost:5000/agents/image-analysis \
  -H "Content-Type: application/json" \
  -d '{
    "message": "What do you see in this image?",
    "files": [
      {
        "name": "test.jpg",
        "dataUrl": "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD..."
      }
    ]
  }'
