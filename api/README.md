# AgentHub.Api

A .NET minimal API for the AgentHub project.

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later

### Running the API

1. Navigate to the api directory:
   ```bash
   cd api
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. Run the API:
   ```bash
   dotnet run
   ```

The API will start on `http://localhost:5000` by default.

## Endpoints

### Status Endpoint
- **GET** `/status`
- Returns the current status of the API
- Response:
  ```json
  {
    "status": "OK",
    "timestamp": "2025-06-20T13:29:32.080944Z"
  }
  ```

## Development

### Swagger Documentation
When running in development mode, Swagger UI is available at:
- `http://localhost:5000/swagger`

### Building
```bash
dotnet build
```

### Testing
```bash
# Test the status endpoint
curl http://localhost:5000/status
```
