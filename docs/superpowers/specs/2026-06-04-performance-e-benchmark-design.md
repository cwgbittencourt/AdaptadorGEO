# Performance and Benchmark Design

**Goal:** documentar a expectativa de performance do AdaptadorGEO e definir um benchmark focado na tradução de expressões espaciais para `SqlFragment` em MySQL, SQL Server e PostgreSQL.

**Architecture:** a biblioteca deve ser avaliada como uma camada de tradução leve, com custo principal concentrado no SQL gerado e no banco de dados. O benchmark não mede execução de query nem tempo de rede; ele compara o custo de tradução entre a fachada `GeoDatabase` e os tradutores diretos por provider. O resultado esperado é uma base objetiva para comparar overhead de tradução, alocação e estabilidade entre os providers suportados.

**Tech Stack:** .NET 10, BenchmarkDotNet, AdaptadorGEO, providers suportados pela biblioteca.

---

## Scope

This design covers:

- a new `Performance` section in `README.md`
- a benchmark plan focused on translation only
- comparison across the supported providers
- guidance on how to interpret results

It does not cover:

- end-to-end database execution benchmarks
- network latency
- index tuning or database schema tuning

## Performance Section Content

The README section should explain:

- the library itself has low expected overhead
- the expensive work happens in the database
- the translation path should be benchmarked separately from query execution
- `GeoDatabase` is the recommended entrypoint for normal usage
- direct provider translators are advanced usage and can also be benchmarked

## Benchmark Plan

The benchmark should compare:

- `GeoDatabase.For(connection).Translate(expression)`
- `MySqlSpatialTranslator.Translate(expression)`
- `SqlServerSpatialTranslator.Translate(expression)`
- `PostgreSqlSpatialTranslator.Translate(expression)`

Use the same expressions for all providers:

- `Intersects(Point)`
- `Contains(Polygon)`
- `Within(MultiPolygon)`
- `Distance(Point)`
- `Buffer(250)`

Measure:

- average translation time
- allocations per operation
- relative consistency across providers

## Reporting

The benchmark report should clearly state:

- this is translation-only
- it does not represent database query performance
- differences reflect translation cost and provider-specific rendering behavior

