![Build](https://github.com/csbeltonii/orbital/actions/workflows/build.yml/badge.svg)


# Orbital

Orbital is a lightweight repository abstraction layer for Azure Cosmos DB. It supports repository patterns for scalable and testable access to Cosmos containers, including optional durability via Polly.

## Projects

- `Orbital`: Core interfaces and base abstractions
- `Orbital.Extensions.DependencyInjection`: DI helpers
- `Orbital.Sample.WebApi`: Example usage
- `Orbital.Tests`: Integration tests using Testcontainers
- `Orbital.Benchmarks`: Benchmark tests using the Cosmos DB Emulator

---

## Getting Started

This guide shows how to configure Orbital for a **simple microservice** that uses a **single Cosmos DB container**.

---

### 1. Add Configuration

Add your container configuration to `appsettings.json`:

```json
"RaceEventContainerSettings": {
  "DatabaseName": "trakr-data",
  "ContainerName": "race-events"
}
```

### 2. Register Services in DI

```csharp
// Register Cosmos Client
services.AddCosmosDb(orbitalCosmosOptions =>
{
    orbitalCosmosOptions.Configuration = configuration;
    orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;
});

// Register the container
services.AddCosmosContainer<RaceEventContainer, RaceEventContainerSettings>(configuration, "RaceEventContainerSettings");

// Register the container accessor
services.AddSingleton<ICosmosContainerAccessor, RaceEventContainer>();
```

ℹ️ Note: You can customize the JSON serializer (see[ Customizing the Serializer](#customizing-the-serializer)).

### 3. Register the repository

You have two options:

For a single type:

```csharp
services.AddSingleton<IRepository<RaceEvent, ICosmosContainerAccessor>, Repository<RaceEvent, ICosmosContainerAccessor>>();
```

For multiple document types in a single container:

```csharp
services.AddSingleton<IRepository<RaceEvent, ICosmosContainerAccessor>, Repository<RaceEvent, ICosmosContainerAccessor>>();
```

### 4. Define your document

Only the `IEntity` interface is required to use repositories:

```csharp
public interface IEntity
{
    string Id { get; set; }
    string? Etag { get; set; }
}
```

Orbital provides an abstract `Entity` base class with auditing support via SystemInformation.

```csharp
public abstract class Entity(string userId) : SystemInformation(userId), IEntity
{
    public abstract string DocumentType { get; }

    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("_etag")]
    [JsonProperty("_etag")]
    public string? Etag { get; set; }
}
```

```csharp
public class SystemInformation : IAudit
{
    public SystemInformation() => CreatedBy = string.Empty;
    public SystemInformation(string userId) => CreatedBy = userId;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; }
    public DateTime LastUpdated { get; set; }
    public string? UpdatedBy { get; set; }
    public int SchemaVersion { get; set; }

    public void UpdateSystemInformation(string updatedBy)
    {
        UpdatedBy = updatedBy;
        LastUpdated = DateTime.UtcNow;
    }
}
```

### 5. Use the repository in your services

Follow standard DI practices:

```csharp
public class EnterRaceEventService(IRepository<RaceEvent, IRaceEventContainer> raceEventRepository)
{
    // Your logic here
}
```

## Multi-Container Setup (Centralized Configuration)

Use this for larger systems with multiple Cosmos containers.

### 1. Define the centralized App Configuration

```csharp
"DatabaseSettings": {
  "DatabaseName": "Orbital Test",
  "Containers": {
    "SimpleContainer": "simple-container",
    "HierarchicalContainer": "hierarchical-container"
  }
}
```

### 2. Register Cosmos and Database Settings

```csharp
services.AddCosmosDb(orbitalCosmosOptions =>
{
    orbitalCosmosOptions.Configuration = configuration;
    orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;
});

services.AddOrbitalDatabaseSettings(configuration, "DatabaseSettings");
```

### 3. Define interfaces for containers

```csharp
public interface ISimpleContainer : ICosmosContainerAccessor;
```

### 4. Define configuration for your containers

```csharp
public class SimpleContainerConfiguration(IOptions<OrbitalDatabaseConfiguration> orbitalDatabaseConfiguration)
    : IOrbitalContainerConfiguration, ISimpleContainer
{
    public string DatabaseName { get; set; } = orbitalDatabaseConfiguration.Value.DatabaseName
        ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));

    public string ContainerName { get; set; } = orbitalDatabaseConfiguration.Value.Containers["SimpleContainer"]
        ?? throw new ArgumentNullException(nameof(orbitalDatabaseConfiguration));
}
```

### 5. Register repositories

```csharp
services.AddCosmosRepositories();
```

### 6. Use the repository in your services

```csharp
public class SampleItemService(IRepository<SampleItem, ISimpleContainer> repository)
{
    // Your logic here
}
```

## Customizing the Serializer

When using the configuration action, you can use the SystemTextJsonOptions and NewtonsoftJsonSettings properties to configure the serializer.

```csharp
services.AddCosmosDb(orbitalCosmosOptions =>
{
    orbitalCosmosOptions.Configuration = configuration;
    orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;
    orbitalCosmosOptions.SystemTextJsonOptions = new
    {
        orbitalCosmosOptions.Configuration = configuration;
        orbitalCosmosOptions.SerializerType = OrbitalSerializerType.SystemTextJson;

        orbitalCosmosOptions.SystemTextJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    };
});
```

ℹ️ Note:
If you provide serializer options (System.Text.Json or Newtonsoft), then use the Converters property there to register custom converters.
If you want to use the default options alongside your custom converters, use the properties exposed on the OrbitalCosmosOptions object.

## 🚀 Cosmos Serializer Benchmark Results (Orbital)

Benchmarks measured Cosmos DB performance using **System.Text.Json (STJ)** and **Newtonsoft.Json (NSJ)** serializers with various document sizes and workloads.

---

## 📊 Benchmark Environment

**BenchmarkDotNet** `v0.15.2`  
**OS**: Windows 11 (`10.0.26100.4349`) [24H2]  
**CPU**: 11th Gen Intel Core i7-1165G7 @ 2.80GHz (8 logical, 4 physical cores)  
**.NET SDK**: `9.0.201`  
**Runtime**: .NET 9.0.3 (RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI)

---

### 📊 Results

| Method                       | Mean       | StdDev   | Allocated    | Notes                           |
| ---------------------------- | ---------- | -------- | ------------ | ------------------------------- |
| `CreateAndReadBenchmark_STJ` | 196.2 ms   | 4.16 ms  | 124.46 KB    | Small document, single ops      |
| `CreateAndReadBenchmark_NSJ` | 195.9 ms   | 3.86 ms  | 135.42 KB    | ≈ STJ, slightly more memory     |
| `CreateLargeDocument_STJ`    | 1,297.9 ms | 21.81 ms | 1,689 KB     | Large array, STJ leaner         |
| `CreateLargeDocument_NSJ`    | 1,302.7 ms | 23.22 ms | 1,466.17 KB  | Slightly faster, less memory    |
| `BulkCreate_STJ`             | 1,678.7 ms | 12.62 ms | 16,029.86 KB | 100 large docs, parallel create |
| `BulkCreate_NSJ`             | 1,674.0 ms | 49.54 ms | 17,406.52 KB | 1.4MB more allocated            |
| `BulkUpsert_STJ`             | 1,677.8 ms | 16.79 ms | 16,066.16 KB | Nearly same as bulk create      |
| `BulkUpsert_NSJ`             | 1,673.2 ms | 18.61 ms | 17,445.33 KB | Slightly faster, more memory    |

> All operations used the same Cosmos container with 100-document workloads in bulk cases. No ETag enforcement on upserts.
