# AgentHub API - Container Setup

This document provides instructions for building and deploying the AgentHub API as a Docker container.

## Prerequisites

- Docker installed on your local machine
- Access to a container registry (Azure Container Registry, Docker Hub, etc.)
- GitHub repository with the following secrets and variables configured

## GitHub Configuration

### Required Secrets
Add these secrets to your GitHub repository (Settings > Secrets and variables > Actions):

- `DOCKER_USERNAME`: Username for your container registry
- `DOCKER_PASSWORD`: Password/access token for your container registry

### Required Variables
Add these variables to your GitHub repository (Settings > Secrets and variables > Actions):

- `CONTAINER_REGISTRY_URL`: Your container registry URL (e.g., `myregistry.azurecr.io`)
- `CONTAINER_REGISTRY_REPOSITORY_NAME`: Your repository name (e.g., `agenthub-api`)

## Local Development

### Building the Docker Image
```bash
docker build -t agenthub-api:latest .
```

### Running the Container Locally
```bash
docker run -p 8080:8080 -p 8081:8081 agenthub-api:latest
```

The API will be available at:
- HTTP: http://localhost:8080
- HTTPS: http://localhost:8081

### Environment Variables
You may need to configure environment variables for Azure services:

```bash
docker run -p 8080:8080 \
  -e AzureOpenAI__Endpoint="your-endpoint" \
  -e AzureOpenAI__ApiKey="your-api-key" \
  -e AzureSearch__Endpoint="your-search-endpoint" \
  -e AzureSearch__ApiKey="your-search-key" \
  agenthub-api:latest
```

## Automated Deployment

The GitHub Action workflow (`.github/workflows/build-and-deploy.yml`) will automatically:

1. Build the Docker image
2. Tag it with a timestamp
3. Push it to your configured container registry

This happens automatically when you push to the `main` branch.

## Docker Image Details

- Base Image: `mcr.microsoft.com/dotnet/aspnet:8.0`
- Framework: .NET 8.0
- Ports: 8080 (HTTP), 8081 (HTTPS)
- User: Non-root user for security

## Security Considerations

- The container runs as a non-root user
- Only necessary files are copied (see `.dockerignore`)
- Uses official Microsoft base images
- Secrets should be managed through environment variables or Azure Key Vault
