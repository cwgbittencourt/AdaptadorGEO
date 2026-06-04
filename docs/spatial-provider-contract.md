# Spatial Provider Contract

## Purpose

The provider contract defines how `AdaptadorGEO` maps a common spatial expression tree into native SQL for each supported database.

## Supported Providers

- MySQL: `ST_Buffer`, `ST_Intersects`, `ST_Contains`, `ST_Distance`, `ST_Within`
- SQL Server: `geography::STGeomFromText`, `STBuffer`, `STIntersects`, `STContains`, `STDistance`, `STWithin`
- PostgreSQL/PostGIS: `ST_Buffer`, `ST_Intersects`, `ST_Contains`, `ST_Distance`, `ST_Within`

## Rules

- Geometry literals are translated using SRID `4326`.
- Coordinates are emitted as `POINT(longitude latitude)` in WKT form.
- Column names are escaped using the target database conventions.
- Distance values are parameterized.

## Consumer Responsibility

The consuming application is responsible for:

- selecting the provider translator
- binding `SqlParameter` values
- executing the generated command text
- mapping result sets back into its own domain objects
