# Framework Comparison Report

Data da validação: 2026-06-04

## Resumo

Foi adicionado um modo de benchmark comparativo em SQL Server para medir o custo de acesso espacial entre:

- `AdaptadorGEO`
- `Dapper`
- `EF Core + NetTopologySuite`

O comparativo usa a mesma tabela seedada e os mesmos cenários espaciais suportados no baseline atual.

## Como executar

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=comparison --iterations=100 --warmup=10
```

Revalidação final executada em `Release`:

```powershell
dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -c Release -- --mode=comparison --dump-sql --iterations=100 --warmup=10
```

## Resultado final

### SQL Server

- `Intersects(Point)`
  - `AdaptadorGEO` 0,90 ms/op
  - `Dapper` 0,90 ms/op
  - `EF Core` 0,94 ms/op
- `Contains(Polygon)`
  - `AdaptadorGEO` 0,83 ms/op
  - `Dapper` 0,82 ms/op
  - `EF Core` 0,87 ms/op
- `Within(MultiPolygon)`
  - `AdaptadorGEO` 0,84 ms/op
  - `Dapper` 0,83 ms/op
  - `EF Core` 0,87 ms/op
- `Distance(Point)`
  - `AdaptadorGEO` 1,10 ms/op
  - `Dapper` 1,10 ms/op
  - `EF Core` 1,18 ms/op
- `Buffer(250)`
  - `AdaptadorGEO` `Skipped`
  - `Dapper` `Skipped`
  - `EF Core` `Skipped`

## Interpretação

O comparativo mostra o custo de acesso espacial no mesmo banco e com a mesma massa de dados.

- `AdaptadorGEO` ficou levemente mais barato que `Dapper` nas consultas medidas.
- `EF Core + NetTopologySuite` ficou acima do baseline nos cenários mais simples, o que é esperado por adicionar a camada de materialização, tracking e tradução de LINQ.
- `Buffer(250)` segue fora do baseline atual porque ele produz geometria e não um predicado booleano de `WHERE`.

### Leitura técnica

O mais provável é que a vantagem do `AdaptadorGEO` venha do caminho de execução mais curto no lado da aplicação:

- `AdaptadorGEO` manda o `SqlFragment` direto para `DbCommand`.
- `Dapper` fica muito perto porque também é uma abstração fina.
- `EF Core + NetTopologySuite` percorre mais infraestrutura antes de executar a consulta.

Como as diferenças são pequenas, o benchmark mede principalmente overhead de cliente e não prova, sozinho, que o SQL final ou o plano de execução sejam melhores.
Para confirmar a hipótese, vale comparar:

- o SQL emitido por cada framework
- o plano de execução do SQL Server
- `SET STATISTICS IO ON`
- `SET STATISTICS TIME ON`

## Observação

Esse é um baseline inicial, não uma verdade geral de performance.
O custo real em produção continua dependendo de índices espaciais, plano de execução, volume de dados, timeouts e comportamento do motor do banco.

## Inspeção de SQL

O modo `--dump-sql` do benchmark de comparação permite inspecionar os comandos emitidos por cada framework.

Na prática, o comparativo mostra:

- `AdaptadorGEO` e `Dapper` chegam ao SQL Server com o mesmo `COUNT_BIG(*)` e a mesma cláusula `WHERE`, mudando apenas o mecanismo de bind de parâmetro.
- `EF Core + NetTopologySuite` gera um `SELECT` com a expressão espacial equivalente dentro do pipeline do EF, o que confirma que há mais infraestrutura no lado do cliente.
- Isso reforça a leitura de que a vantagem observada vem principalmente do caminho de execução mais curto, e não de um SQL radicalmente diferente.

## Resultado final consolidado

O rerun em `Release` confirma o baseline comparativo com o mesmo desenho:

- `AdaptadorGEO` continua próximo de `Dapper`
- `EF Core + NetTopologySuite` continua um pouco acima nas consultas simples
- o `dump-sql` continua mostrando comandos equivalentes entre `AdaptadorGEO` e `Dapper`

## Valor do AdaptadorGEO

O resultado de benchmark não é o único argumento para manter a biblioteca.

O `AdaptadorGEO` é útil porque:

- separa a lógica espacial do provider e da infraestrutura de acesso
- permite reutilizar expressões espaciais entre MySQL, SQL Server e PostgreSQL/PostGIS
- mantém o SQL gerado explícito e previsível
- integra com ADO.NET, Dapper e EF Core sem duplicar a montagem da expressão
- centraliza as operações espaciais em um modelo de domínio único

A conclusão prática é que o ganho não é só de performance. O valor principal está em portabilidade, previsibilidade e desacoplamento da camada espacial.
