# Próximos Passos do Benchmark

Este arquivo registra o ponto de continuidade para a próxima sessão.

## Estado atual

- O benchmark de `translation` já compila e executa.
- O modo `execution` já está implementado.
- O `docker-compose` com os 3 bancos já está criado.
- O `appsettings.json` do benchmark já está pronto para conexões locais.

## O que fazer na próxima sessão

1. Subir os containers com:
   - `docker compose -f benchmarks/docker-compose.yml up -d`
2. Validar se as portas locais estão disponíveis:
   - MySQL `3306`
   - SQL Server `1433`
   - PostgreSQL `5432`
3. Rodar o benchmark de execução:
   - `dotnet run --project benchmarks/AdaptadorGEO.Benchmarks/AdaptadorGEO.Benchmarks.csproj -- --mode=execution --iterations=100 --warmup=10`
4. Se algum provider falhar, ajustar:
   - connection string em `benchmarks/AdaptadorGEO.Benchmarks/appsettings.json`
   - credenciais do `docker-compose`
   - tempo de timeout
5. Comparar os resultados entre:
   - `MySQL`
   - `SQL Server`
   - `PostgreSQL`

## Possíveis melhorias depois da primeira medição

- aumentar o volume de seed para simular carga maior
- adicionar índices espaciais específicos por provider
- medir cenários separados por operação
- exportar o resultado para CSV ou Markdown
- incluir um modo de repetição para estabilizar a leitura

