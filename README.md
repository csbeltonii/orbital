# Orbital

A clean, extensible Cosmos DB repository and container accessor abstraction for .NET.

## Projects

- `Orbital`: Core interfaces and base abstractions
- `Orbital.Extensions.DependencyInjection`: DI helpers
- `Orbital.Sample.WebApi`: Example usage
- `Orbital.Tests`: Integration tests using the Cosmos DB Emulator

## 🚀 Cosmos Serializer Benchmark Results (Orbital)

Benchmarks measured Cosmos DB performance using **System.Text.Json (STJ)** and **Newtonsoft.Json (NSJ)** serializers with various document sizes and workloads.

---

### 🖥️ Benchmark Environment

BenchmarkDotNet v0.15.2
OS: Windows 11 (10.0.26100.4349) [24H2]
CPU: 11th Gen Intel Core i7-1165G7 2.80GHz (8 logical, 4 physical cores)
.NET SDK: 9.0.201
Runtime: .NET 9.0.3 (RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI)

---

### 📊 Results

| Method                             | Mean        | StdDev     | Allocated     | Notes                            |
|------------------------------------|-------------|------------|---------------|----------------------------------|
| `CreateAndReadBenchmark_STJ`       | 196.2 ms    | 4.16 ms    | 124.46 KB     | Small document, single ops       |
| `CreateAndReadBenchmark_NSJ`       | 195.9 ms    | 3.86 ms    | 135.42 KB     | ≈ STJ, slightly more memory      |
| `CreateLargeDocument_STJ`          | 1,297.9 ms  | 21.81 ms   | 1,689 KB      | Large array, STJ leaner          |
| `CreateLargeDocument_NSJ`          | 1,302.7 ms  | 23.22 ms   | 1,466.17 KB   | Slightly faster, less memory     |
| `BulkCreate_STJ`                   | 1,678.7 ms  | 12.62 ms   | 16,029.86 KB  | 100 large docs, parallel create  |
| `BulkCreate_NSJ`                   | 1,674.0 ms  | 49.54 ms   | 17,406.52 KB  | 1.4MB more allocated             |
| `BulkUpsert_STJ`                   | 1,677.8 ms  | 16.79 ms   | 16,066.16 KB  | Nearly same as bulk create       |
| `BulkUpsert_NSJ`                   | 1,673.2 ms  | 18.61 ms   | 17,445.33 KB  | Slightly faster, more memory     |

> All operations used the same Cosmos container with 100-document workloads in bulk cases. No ETag enforcement on upserts.