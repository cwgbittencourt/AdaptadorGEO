# Samples

Este diretório contém três projetos de exemplo separados da biblioteca principal.

Eles existem apenas para demonstrar o uso da DLL `AdaptadorGEO` e não fazem parte do pacote publicado.

## Conteúdo

- [Projetos](#projetos)
- [Como executar](#como-executar)
- [O que cada um mostra](#o-que-cada-um-mostra)
- [Observação](#observação)

## Projetos

- `AdaptadorGEO.Samples.AdoNet`
- `AdaptadorGEO.Samples.Dapper`
- `AdaptadorGEO.Samples.EFCore`

## Como executar

Execute cada projeto a partir da raiz do repositório:

```powershell
dotnet run --project samples/AdaptadorGEO.Samples.AdoNet/AdaptadorGEO.Samples.AdoNet.csproj
dotnet run --project samples/AdaptadorGEO.Samples.Dapper/AdaptadorGEO.Samples.Dapper.csproj
dotnet run --project samples/AdaptadorGEO.Samples.EFCore/AdaptadorGEO.Samples.EFCore.csproj
```

## O que cada um mostra

- **ADO.NET**: uso direto com `GeoDatabase.For(connection)` e montagem manual do `DbCommand`.
- **Dapper**: uso com `connection.AsGeoDatabase()` e `QuerySqlFragment`.
- **EF Core**: uso com `dbContext.Database.AsGeoDatabase()` e `ExecuteSqlFragmentAsync`.

## Observação

Os exemplos usam strings de conexão ilustrativas. Ajuste os valores para o seu ambiente antes de executar contra um banco real.
