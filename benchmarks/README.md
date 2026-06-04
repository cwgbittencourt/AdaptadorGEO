# Benchmarks

Este diretório contém os benchmarks da solução.

Há dois modos de execução:

- `translation` - mede apenas a tradução para `SqlFragment`
- `execution` - executa as queries em bancos locais subidos com `docker-compose`

## Projeto

- `AdaptadorGEO.Benchmarks`

## Modo de tradução

Esse é o benchmark padrão.

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --iterations=10000 --warmup=1000
```

Ou explicitamente:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=translation --iterations=10000 --warmup=1000
```

## Modo de execução

Esse modo usa os bancos locais definidos em [benchmarks/docker-compose.yml](./docker-compose.yml) e as strings de conexão em [benchmarks/AdaptadorGEO.Benchmarks/appsettings.json](./AdaptadorGEO.Benchmarks/appsettings.json).

```powershell
docker compose -f benchmarks/docker-compose.yml up -d
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=execution --iterations=100 --warmup=10
```

Se o banco não estiver disponível, o benchmark mostra `Skipped` para o provider afetado.

## Modo de comparação de frameworks

Esse modo mede um baseline de execução no SQL Server entre:

- `AdaptadorGEO`
- `Dapper`
- `EF Core + NetTopologySuite`

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --iterations=100 --warmup=10
```

Para inspecionar o SQL emitido por cada framework, adicione `--dump-sql`:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --dump-sql --iterations=100 --warmup=10
```

O foco aqui é comparar o custo total de acesso espacial no mesmo banco e na mesma tabela de benchmark.
`Buffer(250)` continua marcado como `Skipped`, porque o conjunto atual de cenários de execução usa filtros em `WHERE`, e `Buffer` produz geometria.

Resultado resumido do baseline em SQL Server:

| Cenário | AdaptadorGEO | Dapper | EF Core + NetTopologySuite |
| --- | ---: | ---: | ---: |
| `Intersects(Point)` | 0,90 ms/op | 0,90 ms/op | 0,94 ms/op |
| `Contains(Polygon)` | 0,83 ms/op | 0,82 ms/op | 0,87 ms/op |
| `Within(MultiPolygon)` | 0,84 ms/op | 0,83 ms/op | 0,87 ms/op |
| `Distance(Point)` | 1,10 ms/op | 1,10 ms/op | 1,18 ms/op |

Relatório final consolidado: [../docs/final-validation-report-2026-06-04.md](../docs/final-validation-report-2026-06-04.md)

Leitura técnica do resultado:

- `AdaptadorGEO` entra na execução com menos camadas entre a aplicação e o `DbCommand`.
- `Dapper` fica perto porque também é um caminho fino, com overhead pequeno de bind e extensão.
- `EF Core + NetTopologySuite` tende a pagar mais custo de infraestrutura porque a consulta passa pelo `DbContext`, pelo pipeline de LINQ e pelos serviços internos do EF.
- As diferenças observadas são pequenas; isso sugere que o ganho está mais no cliente do que no banco.
- Para saber se o SQL final é equivalente, compare os comandos gerados e os planos de execução no SQL Server.
- `--dump-sql` imprime o `CommandText` de `AdaptadorGEO` e `Dapper`, mais o SQL do `ToQueryString()` do EF Core, para facilitar essa comparação.
- `Buffer(250)` permanece `Skipped` no baseline atual porque o modo de execução usa predicados em `WHERE`, enquanto `Buffer` gera geometria.

## Por que usar o AdaptadorGEO

Os benchmarks existem para medir custo, mas o motivo de manter o `AdaptadorGEO` no projeto é mais amplo:

- manter a lógica espacial fora do ORM e do provider
- reutilizar a mesma modelagem espacial em vários bancos
- gerar SQL previsível, fácil de inspecionar e de comparar
- integrar com ADO.NET, Dapper ou EF Core sem duplicar a construção das expressões
- concentrar em um ponto só o uso de `Buffer`, `Contains`, `Intersects`, `Within` e `Distance`

O ganho prático é maior quando a aplicação precisa de portabilidade entre bancos, controle do SQL gerado e uma camada espacial comum para a solução. Se a aplicação usa apenas um banco e já está bem atendida por um ORM espacial único, o valor adicional tende a ser menor.

## O que os benchmarks comparam

- `GeoDatabase.For(connection).Translate(expression)` como caminho de fachada
- `MySqlSpatialTranslator`
- `SqlServerSpatialTranslator`
- `PostgreSqlSpatialTranslator`

No modo de execução, `Buffer(250)` pode aparecer como `Skipped` quando o provider ou o SRID local não suportam a operação de forma consistente para a tabela de benchmark.

## O que eles medem

- tempo médio por operação
- alocações por operação

## O que eles não medem no modo de tradução

- execução da query no banco
- latência de rede
- índices espaciais
- materialização de linhas

## O que eles não medem no modo de execução

- comportamento do ORM da aplicação
- concorrência entre múltiplas cargas
- tuning de índices espaciais além do que você configurar no banco
