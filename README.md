# Cirreum.Storage.Azure

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Storage.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Storage.Azure/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Storage.Azure.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Storage.Azure/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Storage.Azure?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Storage.Azure/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Storage.Azure?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Storage.Azure/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**Azure Blob Storage implementation for the Cirreum Storage abstraction**

## Overview

**Cirreum.Storage.Azure** provides a robust Azure Blob Storage implementation of the Cirreum Storage abstraction, enabling seamless cloud storage operations with automatic service registration, health monitoring, and flexible configuration options.

## Features

- **Unified Storage Interface**: Implements `ICloudStorageClient` for consistent storage operations across cloud providers
- **Automatic Registration**: Service provider pattern with configuration-driven auto-registration
- **Flexible Authentication**: Supports both connection strings and URI-based configuration with DefaultAzureCredential
- **Health Monitoring**: Built-in health checks with configurable options and memory caching
- **Multi-tenant Support**: Keyed service registration for multiple storage accounts
- **Comprehensive Operations**: Full blob lifecycle management including upload, download, metadata, tags, and leasing

## Installation

```bash
dotnet add package Cirreum.Storage.Azure
```

## Quick Start

### Configuration

Add Azure Blob Storage configuration to your `appsettings.json`:

```json
{
  "ServiceProviders": {
    "Storage": {
      "Azure": {
        "Default": {
          "ConnectionString": "your-connection-string-or-uri"
        }
      }
    }
  }
}
```

### Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register all configured service providers
builder.Services.AddServiceProviders(builder.Configuration);

var app = builder.Build();
```

### Usage

```csharp
public class FileService
{
    private readonly ICloudStorageClient _storageClient;

    public FileService(ICloudStorageClient storageClient)
    {
        _storageClient = storageClient;
    }

    public async Task<string?> UploadFileAsync(string containerId, string fileName, string filePath)
    {
        await _storageClient.CreateIfNotExistsAsync(containerId);
        return await _storageClient.UploadFileAsync(containerId, fileName, filePath, overwrite: true);
    }

    public async Task<bool> FileExistsAsync(string containerId, string fileName)
    {
        return await _storageClient.ExistsAsync(containerId, fileName);
    }
}

## Advanced Configuration

### Multiple Storage Accounts

```json
{
  "ServiceProviders": {
    "Storage": {
      "Azure": {
        "Primary": {
          "ConnectionString": "primary-storage-connection-string"
        },
        "Backup": {
          "ConnectionString": "backup-storage-connection-string"
        }
      }
    }
  }
}
```

```csharp
// Inject specific storage client
public class FileService
{
    private readonly ICloudStorageClient _primaryStorage;
    private readonly ICloudStorageClient _backupStorage;

    public FileService(
        [FromKeyedServices("Primary")] ICloudStorageClient primaryStorage,
        [FromKeyedServices("Backup")] ICloudStorageClient backupStorage)
    {
        _primaryStorage = primaryStorage;
        _backupStorage = backupStorage;
    }
}
```

### URI-based Configuration with Managed Identity

```json
{
  "ServiceProviders": {
    "Storage": {
      "Azure": {
        "Default": {
          "ConnectionString": "https://yourstorageaccount.blob.core.windows.net"
        }
      }
    }
  }
}
```

### Health Check Configuration

```json
{
  "ServiceProviders": {
    "Storage": {
      "Azure": {
        "Default": {
          "ConnectionString": "your-connection-string",
          "HealthOptions": {
            "ContainerName": "health-check-container",
            "BlobName": "health-check.txt",
            "Content": "health check content"
          }
        }
      }
    }
  }
}
```

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

{REPO-NAME} follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*