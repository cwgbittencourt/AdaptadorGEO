# README e Docs da Fachada Principal - Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** tornar a documentação inequívoca sobre a fachada principal `GeoDatabase`, deixando claro que ela é a forma recomendada de uso e que a instanciação direta de providers é apenas uso avançado.

**Architecture:** a solução já possui a fachada `GeoDatabase` e as extensões `AsGeoDatabase()`, então esta entrega é exclusivamente documental. O README e os arquivos de `docs/` devem passar a apresentar primeiro o caminho único de consumo, depois a integração com Dapper e EF Core, e só então, em uma seção secundária, a tradução direta por provider para cenários avançados.

**Tech Stack:** Markdown, documentação técnica do repositório, exemplos C#.

---

## File Structure

- `README.md` - documento principal da solução; deve abrir com a fachada única, explicar o contrato e reduzir a presença de providers diretos na rota principal.
- `docs/integration-helpers.md` - deve mostrar `AsGeoDatabase()` como caminho preferencial para Dapper e EF Core.
- `docs/spatial-sql-usage.md` - deve reforçar `GeoDatabase.For(connection)` como fluxo principal e posicionar tradutores específicos como alternativa avançada.

## Task 1: Reorganizar o README para colocar a fachada como entrada principal

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Write the documentation intent**

Inserir no topo do README uma frase de posicionamento semelhante a esta:

```markdown
`GeoDatabase` é a fachada principal da solução. A aplicação deve partir dela ou de `AsGeoDatabase()` para resolver automaticamente o provider ativo sem alterar o código de domínio.
```

- [ ] **Step 2: Move the facade examples before the provider-specific examples**

Manter logo no começo:

```csharp
using AdaptadorGEO;
using AdaptadorGEO.Integration;

using var connection = /* sua DbConnection */;

var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(
    Geo.Column("area").Intersects(Geo.Point(-23.55052, -46.63331)));
```

e:

```csharp
var geo = connection.AsGeoDatabase();
var geoFromEf = dbContext.Database.AsGeoDatabase();
```

- [ ] **Step 3: Rebaixar os exemplos diretos de providers para uma seção de uso avançado**

Transformar a seção `Tradução para SQL` em algo como `Tradução direta por provider (avançado)` e manter ali apenas um exemplo por provider, com um aviso explícito de que esse caminho é opcional.

Exemplo de texto:

```markdown
Se você precisar montar o tradutor manualmente, os providers específicos continuam disponíveis para cenários avançados, testes ou composição explícita de SQL.
```

- [ ] **Step 4: Diferenciar contrato principal e uso avançado**

Adicionar uma subseção curta:

```markdown
Uso recomendado:
- `GeoDatabase.For(connection)`
- `connection.AsGeoDatabase()`
- `dbContext.Database.AsGeoDatabase()`

Uso avançado:
- `new MySqlSpatialTranslator()`
- `new SqlServerSpatialTranslator()`
- `new PostgreSqlSpatialTranslator()`
```

- [ ] **Step 5: Validar que o sumário reflete a nova hierarquia**

O sumário deve deixar a prioridade visível:

1. Fachada principal
2. Helpers de integração
3. Tradução direta por provider

## Task 2: Ajustar `docs/spatial-sql-usage.md` para falar da fachada antes do tradutor

**Files:**
- Modify: `docs/spatial-sql-usage.md`

- [ ] **Step 1: Substituir a abertura genérica por um fluxo de consumo com fachada**

O texto de abertura deve dizer que a forma recomendada é:

```csharp
var geo = GeoDatabase.For(connection);
var fragment = geo.Translate(expression);
```

- [ ] **Step 2: Adicionar uma nota curta sobre tradutores diretos**

O arquivo deve conter uma nota curta do tipo:

```markdown
Os tradutores específicos por provider continuam existindo, mas são um caminho avançado para quando a aplicação precisa controlar explicitamente o dialeto.
```

- [ ] **Step 3: Manter o contrato de execução inalterado**

O documento deve continuar explicando o mesmo contrato:

- `CommandText`
- `Parameters`
- sem abrir conexão
- sem executar comandos

## Task 3: Atualizar `docs/integration-helpers.md` para tornar `AsGeoDatabase()` o caminho padrão

**Files:**
- Modify: `docs/integration-helpers.md`

- [ ] **Step 1: Priorizar os exemplos com `AsGeoDatabase()`**

Garantir que Dapper e EF Core mostrem primeiro:

```csharp
var geo = connection.AsGeoDatabase();
```

e:

```csharp
var geo = dbContext.Database.AsGeoDatabase();
```

- [ ] **Step 2: Explicar a ponte entre fachada e execução**

O documento deve deixar claro que os helpers não escolhem provider manualmente; eles só adaptam o `SqlFragment` produzido pela fachada.

- [ ] **Step 3: Remover qualquer sensação de obrigatoriedade de provider direto**

Se houver texto dando a entender que a aplicação precisa instanciar `MySqlSpatialTranslator`, `SqlServerSpatialTranslator` ou `PostgreSqlSpatialTranslator` no caminho normal, esse texto deve ser reescrito para uso avançado.

---

## Self-Review

### 1. Spec coverage

O plano cobre a queixa do usuário:

- a documentação atual ainda mostra providers diretos no fluxo principal;
- a fachada existe, mas não está sendo comunicada como padrão;
- os docs precisam mostrar uma única porta de entrada para a aplicação;
- os providers diretos devem ficar como opção avançada, não como requisito.

### 2. Placeholder scan

Não há `TODO`, `TBD` ou instruções vagas. Os passos usam conteúdo concreto e exemplos reais.

### 3. Type consistency

O plano usa consistentemente os nomes já existentes:

- `GeoDatabase`
- `AsGeoDatabase()`
- `MySqlSpatialTranslator`
- `SqlServerSpatialTranslator`
- `PostgreSqlSpatialTranslator`
- `SqlFragment`

