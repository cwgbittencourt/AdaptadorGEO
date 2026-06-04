# Final Validation Report

Data da validação: 2026-06-04

## Estado da solução

Validação estrutural concluída com sucesso:

- `dotnet build AdaptadorGEO.slnx -c Release`
- `dotnet test AdaptadorGEO.slnx -c Release`
- `dotnet list AdaptadorGEO.slnx package --vulnerable --include-transitive`

Resultado:

- `0` warnings
- `0` errors
- nenhum pacote vulnerável reportado

## Benchmark de tradução

Comando:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -c Release -- --mode=translation --iterations=10000 --warmup=1000
```

### MySQL

| Cenário | Facade (us/op) | Direct (us/op) | Alloc (B/op) |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,86 | 0,53 | 704,0 |
| `Contains(Polygon)` | 1,65 | 1,39 | 1528,0 |
| `Within(MultiPolygon)` | 1,63 | 1,49 | 1736,0 |
| `Distance(Point)` | 0,56 | 0,55 | 704,0 |
| `Buffer(250)` | 0,23 | 0,27 | 376,0 |

### SQL Server

| Cenário | Facade (us/op) | Direct (us/op) | Alloc (B/op) |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,61 | 0,46 | 736,0 |
| `Contains(Polygon)` | 1,53 | 1,50 | 1560,0 |
| `Within(MultiPolygon)` | 1,80 | 1,96 | 1768,0 |
| `Distance(Point)` | 0,48 | 0,59 | 736,0 |
| `Buffer(250)` | 0,16 | 0,16 | 368,0 |

### PostgreSQL

| Cenário | Facade (us/op) | Direct (us/op) | Alloc (B/op) |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,52 | 0,61 | 704,0 |
| `Contains(Polygon)` | 1,37 | 1,46 | 1528,0 |
| `Within(MultiPolygon)` | 1,83 | 1,72 | 1736,0 |
| `Distance(Point)` | 0,64 | 0,56 | 704,0 |
| `Buffer(250)` | 0,21 | 0,32 | 376,0 |

## Benchmark de execução

Comando:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -c Release -- --mode=execution --iterations=100 --warmup=10
```

### MySQL

| Cenário | Mean (ms/op) | Alloc (B/op) |
| --- | ---: | ---: |
| `Intersects(Point)` | 0,89 | 3136,3 |
| `Contains(Polygon)` | 1,14 | 3128,3 |
| `Within(MultiPolygon)` | 1,10 | 3128,3 |
| `Distance(Point)` | 1,03 | 3144,3 |
| `Buffer(250)` | Skipped | MySQL: not supported in live execution |

### SQL Server

| Cenário | Mean (ms/op) | Alloc (B/op) |
| --- | ---: | ---: |
| `Intersects(Point)` | 0,95 | 4245,0 |
| `Contains(Polygon)` | 0,86 | 4234,0 |
| `Within(MultiPolygon)` | 0,87 | 4226,7 |
| `Distance(Point)` | 1,09 | 4234,0 |
| `Buffer(250)` | Skipped | SQL Server: not supported in live execution |

### PostgreSQL

| Cenário | Mean (ms/op) | Alloc (B/op) |
| --- | ---: | ---: |
| `Intersects(Point)` | 0,36 | 1907,7 |
| `Contains(Polygon)` | 0,37 | 1891,7 |
| `Within(MultiPolygon)` | 0,37 | 1891,7 |
| `Distance(Point)` | 0,39 | 1923,7 |
| `Buffer(250)` | Skipped | PostgreSQL: not supported in live execution |

## Comparação de frameworks

Comando:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -c Release -- --mode=comparison --dump-sql --iterations=100 --warmup=10
```

### SQL Server

| Cenário | AdaptadorGEO | Dapper | EF Core + NetTopologySuite |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,90 ms/op | 0,90 ms/op | 0,94 ms/op |
| `Contains(Polygon)` | 0,83 ms/op | 0,82 ms/op | 0,87 ms/op |
| `Within(MultiPolygon)` | 0,84 ms/op | 0,83 ms/op | 0,87 ms/op |
| `Distance(Point)` | 1,10 ms/op | 1,10 ms/op | 1,18 ms/op |
| `Buffer(250)` | Skipped | Skipped | Skipped |

## Leitura final

- `AdaptadorGEO` permanece competitivo com `Dapper` no baseline SQL Server.
- `EF Core + NetTopologySuite` continua um pouco acima, principalmente por infraestrutura adicional no cliente.
- O `dump-sql` confirma que `AdaptadorGEO` e `Dapper` chegam ao banco com o mesmo SQL lógico no comparativo.
- `Buffer(250)` continua fora do fluxo de execução ao vivo e deve ser tratado em benchmark próprio se o objetivo for medir a geometria gerada.

## Relatórios relacionados

- [Benchmark Execution Report](benchmark-execution-report-2026-06-04.md)
- [Framework Comparison Report](framework-comparison-report-2026-06-04.md)
- [Performance Benchmark Guide](performance-benchmark.md)
