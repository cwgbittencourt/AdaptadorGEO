# Performance Benchmark

Este guia descreve como comparar o custo de tradução e o custo de execução real entre providers.

## Fase 1: tradução

Esta fase mede apenas a tradução para `SqlFragment`.

Compare:

- `GeoDatabase.For(connection).Translate(expression)`
- `MySqlSpatialTranslator.Translate(expression)`
- `SqlServerSpatialTranslator.Translate(expression)`
- `PostgreSqlSpatialTranslator.Translate(expression)`

Use as mesmas expressões em todas as medições:

- `Intersects(Point)`
- `Contains(Polygon)`
- `Within(MultiPolygon)`
- `Distance(Point)`
- `Buffer(250)`

Registre:

- tempo médio por tradução
- alocações por operação
- desvio entre providers

## Fase 2: execução real

A segunda fase executa queries em bancos locais subidos com [benchmarks/docker-compose.yml](../benchmarks/docker-compose.yml).

O benchmark lê as strings de conexão em [benchmarks/AdaptadorGEO.Benchmarks/appsettings.json](../benchmarks/AdaptadorGEO.Benchmarks/appsettings.json).

Ele usa a fachada `GeoDatabase` para montar a expressão e depois executa o `SqlFragment` no banco ativo.

Como executar:

```powershell
docker compose -f benchmarks/docker-compose.yml up -d
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=execution --iterations=100 --warmup=10
```

Se um provider não estiver ativo ou a conexão não estiver configurada, o benchmark marca esse provider como `Skipped`.

## Comparação de frameworks

Existe também um modo de comparação em SQL Server que contrasta:

- `AdaptadorGEO`
- `Dapper`
- `EF Core + NetTopologySuite`

Como executar:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --iterations=100 --warmup=10
```

Para inspecionar o SQL emitido por cada framework:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --dump-sql --iterations=100 --warmup=10
```

Esse modo usa a mesma tabela seedada e os mesmos cenários espaciais suportados.
`Buffer(250)` continua como `Skipped`, porque o baseline atual foi desenhado em torno de predicados em `WHERE`, enquanto `Buffer` produz geometria.

## Interpretação do baseline

O resultado sugere que `AdaptadorGEO` está ganhando principalmente por ter menos overhead no lado da aplicação.

- `AdaptadorGEO` executa o `SqlFragment` direto com `DbCommand`.
- `Dapper` continua próximo porque também evita uma camada ORM pesada.
- `EF Core + NetTopologySuite` adiciona mais infraestrutura de consulta e materialização, então tende a custar mais.
- As diferenças são pequenas e não justificam concluir, sozinhas, que o banco executou uma query diferente.
- A confirmação mais forte vem de comparar o SQL emitido, o plano de execução e o `STATISTICS IO/TIME`.

O flag `--dump-sql` ajuda a verificar se os frameworks chegam ao banco com comandos equivalentes ou apenas semanticamente próximos.

## Interpretação

Resultados melhores na tradução não significam necessariamente consultas mais rápidas.

A performance real da query continua dependendo do banco, do plano de execução, dos índices espaciais e do volume de dados.

O benchmark de execução é útil para comparar o comportamento prático dos providers nas mesmas condições.

## Por que manter o AdaptadorGEO

Os benchmarks medem custo, mas o objetivo da biblioteca vai além de performance.

O `AdaptadorGEO` vale a pena quando a aplicação precisa de:

- uma camada espacial única para vários providers
- SQL explícito e previsível
- integração com ADO.NET, Dapper ou EF Core sem reescrever a lógica espacial
- menos acoplamento ao ORM
- centralização das expressões espaciais em um único modelo de domínio

Ele tende a trazer mais valor em sistemas que alternam entre bancos, montam consultas dinamicamente ou querem manter a responsabilidade espacial fora do ORM. Em projetos fechados em um único provider, o benefício pode ser menor.

## Validação final

A reexecução final dos benchmarks, com `Release` e documentação consolidada, está registrada em [docs/final-validation-report-2026-06-04.md](final-validation-report-2026-06-04.md).
