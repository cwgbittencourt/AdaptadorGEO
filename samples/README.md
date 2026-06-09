# Samples

Este diretório contém quatro projetos de exemplo separados da biblioteca principal.

Eles existem apenas para demonstrar o uso da DLL `AdaptadorGEO` e não fazem parte do pacote publicado.

## Conteúdo

- [Projetos](#projetos)
- [Como executar](#como-executar)
- [O que cada um mostra](#o-que-cada-um-mostra)
- [Observação](#observação)
- [WKT](#wkt)
- [GeoJSON](#geojson)

## Projetos

- `AdaptadorGEO.Samples.AdoNet`
- `AdaptadorGEO.Samples.Dapper`
- `AdaptadorGEO.Samples.EFCore`
- `AdaptadorGEO.Samples.GeoJson`

Detalhes do sample GeoJSON:

- [samples/AdaptadorGEO.Samples.GeoJson/README.md](AdaptadorGEO.Samples.GeoJson/README.md)

## Como executar

Execute cada projeto a partir da raiz do repositório:

```powershell
dotnet run --project samples/AdaptadorGEO.Samples.AdoNet/AdaptadorGEO.Samples.AdoNet.csproj
dotnet run --project samples/AdaptadorGEO.Samples.Dapper/AdaptadorGEO.Samples.Dapper.csproj
dotnet run --project samples/AdaptadorGEO.Samples.EFCore/AdaptadorGEO.Samples.EFCore.csproj
dotnet run --project samples/AdaptadorGEO.Samples.GeoJson/AdaptadorGEO.Samples.GeoJson.csproj
```

## O que cada um mostra

- **ADO.NET**: uso direto com `GeoDatabase.For(connection)` e montagem manual do `DbCommand`.
- **Dapper**: uso com `connection.AsGeoDatabase()` e `QuerySqlFragment`.
- **EF Core**: uso com `dbContext.Database.AsGeoDatabase()` e `ExecuteSqlFragmentAsync`.
- **GeoJSON**: uso com `AdaptadorGEO.Formats.GeoFormats.ToGeoJson()` e `FromGeoJson()`.

## Persistência de região

Quando a aplicação precisa gravar uma região no banco, o padrão continua sendo:

- montar a geometria em C#;
- traduzir a geometria com `GeoDatabase`;
- encaixar o `SqlFragment` no `INSERT` ou `UPDATE`;
- executar o comando com a tecnologia de acesso já usada pela aplicação.

Exemplo conceitual:

```csharp
var geo = GeoDatabase.For(connection);
var areaFragment = geo.Translate(
    Geo.Polygon(
        Geo.Point(-23.55, -46.63),
        Geo.Point(-23.56, -46.64),
        Geo.Point(-23.57, -46.65),
        Geo.Point(-23.55, -46.63)));

using var command = connection.CreateCommand();
command.CommandText = $@"
INSERT INTO regions (name, area)
VALUES (@name, {areaFragment.CommandText});";

foreach (var parameter in areaFragment.Parameters)
{
    command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
}
```

Esse fluxo mostra a fronteira da biblioteca:

- o `AdaptadorGEO` traduz a geometria para o SQL correto do provider;
- a aplicação monta o comando final e decide como persistir.

## Observação

Os exemplos usam strings de conexão ilustrativas. Ajuste os valores para o seu ambiente antes de executar contra um banco real.

## WKT

Se você quiser converter entre geometria interna e WKT, veja a documentação específica:

- [docs/wkt.md](../docs/wkt.md)

## GeoJSON

Se você quiser ver um exemplo executável de conversão entre geometria interna e GeoJSON, rode:

- `AdaptadorGEO.Samples.GeoJson`
- [docs/geojson.md](../docs/geojson.md)
