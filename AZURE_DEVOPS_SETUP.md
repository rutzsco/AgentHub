# Azure DevOps Pipeline Setup Guide

This guide explains how to set up the Azure DevOps pipeline for building and pushing your AgentHub container image.

## Prerequisites

1. **Azure DevOps Project**: You need an Azure DevOps project where you can create pipelines.
2. **Container Registry**: Access to a container registry (Azure Container Registry, Docker Hub, etc.).
3. **Service Connection**: A service connection in Azure DevOps to authenticate with your container registry.

## Setup Steps

### 1. Create Service Connection

Before running the pipeline, you need to create a service connection in Azure DevOps:

1. Go to your Azure DevOps project
2. Navigate to **Project Settings** → **Service connections**
3. Click **New service connection**
4. Choose your registry type:
   - **Azure Container Registry**: For ACR
   - **Docker Registry**: For Docker Hub or other registries
5. Fill in the connection details and name it (e.g., `container-registry-connection`)

### 2. Set Up Variables

In your Azure DevOps project, set up the following variables:

#### Pipeline Variables
Go to **Pipelines** → Your pipeline → **Edit** → **Variables** and add:

- `CONTAINER_REGISTRY_URL`: Your container registry URL
  - For ACR: `yourregistry.azurecr.io`
  - For Docker Hub: `docker.io` or leave empty
- `CONTAINER_REGISTRY_REPOSITORY_NAME`: Your repository name (e.g., `agenthub`)

#### Service Connection Variable
Update the pipeline YAML to reference your service connection name:
```yaml
containerRegistry: 'your-service-connection-name'
```

### 3. Pipeline Features

The pipeline includes:

- **Triggers**: Runs on pushes to `main` branch
- **Pull Request Validation**: Builds (but doesn't push) on PRs to `main`
- **Multi-tag Strategy**: 
  - Tags images with the Git commit SHA
  - Tags with `latest` only for main branch builds
- **Conditional Push**: Only pushes images on main branch (not PRs)
- **Build Context**: Uses the repository root as build context

### 4. Customization Options

#### Different Registry Types

**Azure Container Registry (ACR):**
```yaml
variables:
  containerRegistry: 'yourregistry.azurecr.io'
  repository: 'agenthub'
```

**Docker Hub:**
```yaml
variables:
  containerRegistry: 'docker.io'
  repository: 'yourusername/agenthub'
```

**GitHub Container Registry:**
```yaml
variables:
  containerRegistry: 'ghcr.io'
  repository: 'yourusername/agenthub'
```

#### Additional Build Arguments

To pass build arguments to Docker:
```yaml
- task: Docker@2
  displayName: 'Build Docker image'
  inputs:
    command: 'build'
    Dockerfile: '$(dockerfilePath)'
    buildContext: '$(buildContext)'
    repository: '$(containerRegistry)/$(repository)'
    tags: |
      $(imageTag)
      $(Build.SourceVersion)
    arguments: '--build-arg BUILD_CONFIGURATION=Release --no-cache'
```

#### Different Tagging Strategy

For semantic versioning or custom tags:
```yaml
variables:
  # Use semantic version if available, otherwise use commit SHA
  imageTag: $(if(variables['Build.SourceBranch'], 'v1.0.$(Build.BuildId)', '$(Build.SourceVersion)'))
```

### 5. Security Considerations

- **Service Connections**: Use service connections instead of storing credentials directly
- **Variable Groups**: Consider using variable groups for shared configuration
- **Secrets**: Store sensitive values as secret variables
- **Permissions**: Ensure the pipeline has minimal required permissions

### 6. Monitoring and Troubleshooting

#### Common Issues:

1. **Authentication Failures**: Check service connection configuration
2. **Build Failures**: Verify Dockerfile path and build context
3. **Push Failures**: Ensure proper permissions on the registry
4. **Tag Issues**: Verify repository name format matches registry requirements

#### Useful Commands for Debugging:

Add these steps to troubleshoot:
```yaml
- task: PowerShell@2
  displayName: 'Debug Information'
  inputs:
    targetType: 'inline'
    script: |
      Write-Host "Build.SourceBranch: $(Build.SourceBranch)"
      Write-Host "Build.SourceVersion: $(Build.SourceVersion)"
      Write-Host "System.PullRequest.IsFork: $(System.PullRequest.IsFork)"
      Write-Host "Agent.OS: $(Agent.OS)"
```

## Comparison with GitHub Actions

| Feature | GitHub Actions | Azure DevOps |
|---------|---------------|---------------|
| Checkout | `actions/checkout@v4` | `checkout: self` |
| Docker Login | `docker/login-action@v3` | `Docker@2` with `login` command |
| Build & Push | `docker/build-push-action@v5` | Separate `Docker@2` tasks |
| Metadata | `docker/metadata-action@v5` | Manual tag generation |
| Conditional Execution | `${{ github.event_name == 'push' }}` | `condition:` with branch check |

The Azure DevOps pipeline provides similar functionality to the GitHub Actions workflow while using Azure DevOps-specific syntax and features.
