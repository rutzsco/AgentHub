# Knowledge Endpoint Documentation

## Overview
The knowledge endpoint allows you to index content to Azure AI Search with security filters and metadata.

## Configuration

### Azure Search Settings
Update your `appsettings.json` or `appsettings.Development.json` with your Azure Search credentials:

```json
{
  "AzureSearch": {
    "Endpoint": "https://your-search-service.search.windows.net",
    "ApiKey": "your-api-key-here"
  }
}
```

### Azure OpenAI Settings
Configure Azure OpenAI for generating vector embeddings:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-service.openai.azure.com",
    "ApiKey": "your-openai-api-key-here",
    "EmbeddingDeploymentName": "text-embedding-ada-002"
  }
}
```

You can also set these as environment variables:
- `AzureSearch__Endpoint`
- `AzureSearch__ApiKey`
- `AzureOpenAI__Endpoint`
- `AzureOpenAI__ApiKey`
- `AzureOpenAI__EmbeddingDeploymentName`

## API Endpoints

### POST /knowledge

Index knowledge content to Azure AI Search.

**Request Body:**
```json
{
  "content": "Your knowledge content here",
  "indexName": "your-index-name",
  "securityFilters": {
    "department": "engineering",
    "clearanceLevel": "confidential"
  },
  "title": "Optional title",
  "category": "Optional category",
  "metadata": {
    "author": "John Doe",
    "version": "1.0",
    "tags": ["important", "documentation"]
  }
}
```

**Required Fields:**
- `content`: The main text content to be indexed
- `indexName`: The name of the Azure Search index to use

**Optional Fields:**
- `securityFilters`: Key-value pairs for access control
- `title`: Document title
- `category`: Document category
- `metadata`: Additional metadata as key-value pairs

**Response:**
```json
{
  "id": "generated-document-id",
  "status": "Success",
  "message": "Knowledge successfully indexed",
  "timestamp": "2025-06-20T10:00:00Z"
}
```

### POST /knowledge/search

Perform vector search on indexed knowledge content using semantic search capabilities.

**Request Body:**
```json
{
  "query": "What are the new product features?",
  "indexName": "company-knowledge",
  "top": 5,
  "securityFilters": {
    "department": "engineering"
  },
  "categories": ["Architecture", "Documentation"],
  "includeContent": true
}
```

**Required Fields:**
- `query`: The search query text
- `indexName`: The name of the Azure Search index to search

**Optional Fields:**
- `top`: Number of results to return (1-100, default: 5)
- `securityFilters`: Key-value pairs to filter results based on security attributes
- `categories`: Array of categories to filter by
- `includeContent`: Whether to include full content in results (default: true)

**Response:**
```json
{
  "results": [
    {
      "id": "doc-123",
      "content": "This is important company documentation...",
      "title": "Product Features Documentation",
      "category": "Documentation",
      "score": 0.95,
      "createdAt": "2025-06-20T09:00:00Z",
      "updatedAt": "2025-06-20T09:00:00Z",
      "metadata": {
        "author": "John Doe",
        "version": "1.0"
      }
    }
  ],
  "totalCount": 1,
  "status": "Success",
  "message": "Found 1 results",
  "timestamp": "2025-06-20T10:00:00Z",
  "query": "What are the new product features?"
}
```
- `content`: The main text content to be indexed
- `indexName`: The name of the Azure Search index to use

**Optional Fields:**
- `securityFilters`: Key-value pairs for access control
- `title`: Document title
- `category`: Document category
- `metadata`: Additional metadata as key-value pairs

**Response:**
```json
{
  "id": "generated-document-id",
  "status": "Success",
  "message": "Knowledge successfully indexed",
  "timestamp": "2025-06-20T10:00:00Z"
}
```

## Features

### Vector Search with Azure OpenAI Embeddings
The search endpoint provides:
- **Vector Embeddings**: Automatic generation of vector embeddings using Azure OpenAI text-embedding-ada-002
- **Hybrid Search**: Combines traditional category search with vector similarity calculation
- **Cosine Similarity**: Calculates similarity between query and document embeddings for ranking
- **Relevance Scoring**: Returns results ranked by vector similarity scores
- **Future-Ready**: Stores embeddings for when full vector search becomes available in Azure Search SDK

### Enhanced RBAC Security Filtering
- **Role-Based Access Control**: Advanced security filtering based on user roles and permissions
- **Multi-Value Filters**: Support for array-based security attributes
- **Granular Permissions**: Filter by department, clearance level, project access, etc.
- **Dynamic Filtering**: Security filters applied at query time for real-time access control

### Automatic Index Creation
Both endpoints automatically create indexes if they don't exist:
- **Indexing Endpoint**: Creates index when adding first document
- **Search Endpoint**: Creates empty index when searching (prevents "index not found" errors)

If the specified index doesn't exist, it will be automatically created with the following schema:
- `id`: Unique document identifier (key, filterable)
- `content`: Document content (not searchable, for display only)
- `title`: Document title (not searchable, filterable, facetable)
- `category`: Document category (searchable, filterable, facetable)
- `text_vector`: 1536-dimensional vector embeddings (stored for similarity calculation)
- `createdAt`: Creation timestamp (filterable, sortable)
- `updatedAt`: Update timestamp (filterable, sortable)
- `securityFilters`: Array of security attributes (filterable for RBAC)
- `metadata`: Additional metadata (filterable)

### Hybrid Search Implementation
Current implementation uses a hybrid approach:
- **Category Search**: Primary search performed on the category field
- **Vector Similarity**: Embeddings are stored and used for post-search similarity ranking
- **Cosine Similarity**: Calculates similarity between query and document embeddings
- **Relevance Ranking**: Results are re-ranked by vector similarity scores

### Security Filters
Security filters are stored as JSON and can be used for:
- Access control based on user attributes
- Content filtering based on organizational structure
- Multi-tenant data isolation

### Error Handling
The service provides detailed error messages for:
- Missing configuration
- Invalid requests
- Azure Search service errors
- Network connectivity issues

## Example Usage

### Basic Knowledge Indexing
```bash
curl -X POST "https://localhost:5001/knowledge" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "This is important company documentation about our new product features.",
    "indexName": "company-knowledge",
    "title": "Product Features Documentation"
  }'
```

### Knowledge with Enhanced RBAC Security Filters
```bash
curl -X POST "https://localhost:5001/knowledge" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Confidential engineering specifications for the new system architecture.",
    "indexName": "engineering-docs",
    "title": "System Architecture Specifications",
    "category": "Architecture",
    "securityFilters": {
      "department": "engineering",
      "clearanceLevel": "confidential",
      "project": ["project-alpha", "project-beta"],
      "role": "senior-engineer"
    },
    "metadata": {
      "author": "Jane Smith",
      "version": "2.1",
      "reviewDate": "2025-07-01"
    }
  }'
```

### Basic Vector Search
```bash
curl -X POST "https://localhost:5001/knowledge/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "product features documentation",
    "indexName": "company-knowledge",
    "top": 5
  }'
```

### Advanced Vector Search with RBAC Filters
```bash
curl -X POST "https://localhost:5001/knowledge/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "system architecture",
    "indexName": "engineering-docs",
    "top": 10,
    "categories": ["Architecture", "Engineering"],
    "securityFilters": {
      "department": "engineering",
      "clearanceLevel": "confidential",
      "role": ["senior-engineer", "architect"]
    },
    "includeContent": false
  }'
```

### Vector Search with Complex Security Context
```bash
curl -X POST "https://localhost:5001/knowledge/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "confidential project specifications",
    "indexName": "engineering-docs",
    "top": 3,
    "securityFilters": {
      "department": "engineering",
      "clearanceLevel": "confidential",
      "project": ["project-alpha", "project-beta"],
      "role": "senior-engineer"
    }
  }'
```

## Monitoring and Logging

The service logs the following events:
- Knowledge indexing attempts
- Knowledge search operations
- Index creation events
- Search query performance
- Error conditions
- Performance metrics

Check the application logs for detailed information about indexing and search operations.
