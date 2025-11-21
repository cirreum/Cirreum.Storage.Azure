# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Build Commands
```bash
# Build the solution
dotnet build

# Build in Release mode
dotnet build -c Release

# Clean build artifacts
dotnet clean
```

### Package Commands
```bash
# Create NuGet package
dotnet pack

# Create package in Release mode
dotnet pack -c Release
```

## Project Architecture

This is a .NET 10.0 Azure Blob Storage client library that implements the Cirreum Storage abstraction pattern. The project follows Cirreum's service provider registration pattern for cloud storage implementations.

### Core Components

- **AzureBlobStorageClient**: Main implementation of `ICloudStorageClient` interface, wraps Azure's `BlobServiceClient`
- **AzureBlobStorageRegistrar**: Service provider registrar following Cirreum's auto-registration pattern
- **Configuration**: Settings classes for connection strings, URIs, and health check options
- **Extensions**: Registration and conversion utilities for dependency injection setup
- **Health Checks**: Azure-specific health monitoring implementation

### Key Patterns

- **Service Provider Pattern**: Uses `ServiceProviderRegistrar` base class with automatic configuration binding
- **Keyed Services**: Supports both keyed and default service registration for multi-tenant scenarios  
- **Configuration Flexibility**: Supports both connection strings and URI-based configuration with DefaultAzureCredential
- **Health Check Integration**: Built-in health monitoring with configurable options and caching
- **Extension Methods**: Azure SDK type conversions and DI registration helpers

### Dependencies

- Azure.Storage.Blobs (12.26.0)
- Azure.Identity (1.17.1) 
- Cirreum.Storage (1.0.102) - Core storage abstractions
- Cirreum.ServiceProvider (1.0.2) - Registration framework
- Microsoft.AspNetCore.App - Framework reference

### Build Configuration

- Targets .NET 10.0
- Uses build props files in `/build/` for shared configuration
- Supports CI/CD detection and local development versioning
- InternalsVisibleTo configured for test assemblies in local builds only