# Benchmark Execution Report

Data da validação: 2026-06-04

## Resumo

O benchmark de execução foi validado com containers locais ativos para MySQL, SQL Server e PostgreSQL.
O fluxo agora compila sem erros e sem warnings de restore, e a execução completa produz resultados para todos os cenários suportados no modo `execution`.

## O que foi ajustado

- MySQL:
  - conexão local ajustada em `appsettings.json` com `SslMode=None` e `AllowPublicKeyRetrieval=True`
- SQL Server:
  - o benchmark passa a criar o banco de dados de execução antes de abrir a conexão principal
- Benchmark:
  - os cenários agora continuam mesmo se um provider ou expressão falhar
  - `Skipped` passou a mostrar provider, cenário e motivo
  - a medição de alocação passou a usar `GC.GetTotalAllocatedBytes(precise: true)`
- Feed local:
  - pacotes exatos necessários para o restore foram adicionados ao `nuget-feed`

## Validação

Comandos executados com sucesso:

```powershell
dotnet restore benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj --ignore-failed-sources
dotnet build benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj --no-restore
```

Execução validada:

```powershell
benchmarks/AdaptadorGEO.Benchmarks/bin/Debug/net10.0/AdaptadorGEO.Benchmarks.exe --mode=execution --iterations=100 --warmup=10
```

Revalidação final executada em `Release`:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -c Release -- --mode=execution --iterations=100 --warmup=10
```

## Resultado final

### MySQL

- `Intersects(Point)` 0,89 ms/op
- `Contains(Polygon)` 1,14 ms/op
- `Within(MultiPolygon)` 1,10 ms/op
- `Distance(Point)` 1,03 ms/op
- `Buffer(250)` `Skipped`

### SQL Server

- `Intersects(Point)` 0,95 ms/op
- `Contains(Polygon)` 0,87 ms/op
- `Within(MultiPolygon)` 0,87 ms/op
- `Distance(Point)` 1,09 ms/op
- `Buffer(250)` `Skipped`

### PostgreSQL

- `Intersects(Point)` 0,36 ms/op
- `Contains(Polygon)` 0,37 ms/op
- `Within(MultiPolygon)` 0,36 ms/op
- `Distance(Point)` 0,39 ms/op
- `Buffer(250)` `Skipped`

## Cobertura de cenários

### Modo `translation`

Todos os cenários planejados seguem sendo testados:

- `Intersects(Point)`
- `Contains(Polygon)`
- `Within(MultiPolygon)`
- `Distance(Point)`
- `Buffer(250)`

### Modo `execution`

O benchmark executa os cenários suportados e registra `Skipped` quando a operação não é apropriada para execução ao vivo na tabela de benchmark.

Neste estado:

- `Intersects(Point)` executa
- `Contains(Polygon)` executa
- `Within(MultiPolygon)` executa
- `Distance(Point)` executa
- `Buffer(250)` aparece como `Skipped`

## Limitação atual

`Buffer(250)` foi mantido fora do fluxo de execução ao vivo porque o benchmark atual usa predicados em `WHERE`, e `Buffer` produz geometria, não booleano. Isso faz dele um caso melhor para um benchmark próprio de execução scalar, separado do conjunto atual.

## Observação sobre warnings

Os warnings `NU1603` do restore foram eliminados ao incluir no feed local as versões exatas que o grafo do benchmark pede.
