# Uso de SQL Espacial

## Visão geral

`AdaptadorGEO` traduz expressões espaciais para SQL nativo. A aplicação consumidora deve começar pela fachada `GeoDatabase` ou por `AsGeoDatabase()` e então executar o SQL gerado no seu próprio provider.

Os tradutores diretos por provider continuam disponíveis para cenários avançados, testes ou composição explícita de SQL.

## Exemplo

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;

var geo = GeoDatabase.For(connection);

var fragment = geo.Translate(
    Geo.Column("boundary").Intersects(Geo.Point(-23.55052, -46.63331)));
```

## Contrato de execução

- `fragment.CommandText` contém o SQL específico do provider.
- `fragment.Parameters` contém os valores que precisam ser vinculados pela aplicação.
- A biblioteca não abre conexões, não executa comandos e não materializa linhas do banco.
