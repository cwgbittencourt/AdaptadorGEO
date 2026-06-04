# Performance Section and Translation Benchmark Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** add a clear Performance section to the README and prepare a benchmark plan that compares translation cost across `GeoDatabase` and the direct provider translators.

**Architecture:** the documentation will distinguish between translation cost and database execution cost. The benchmark will measure only translation to `SqlFragment`, using the same spatial expressions across all supported providers. This keeps the comparison fair and avoids mixing query execution, indexes, and network latency into the result.

**Tech Stack:** Markdown documentation, .NET benchmarking guidance, BenchmarkDotNet as the recommended harness.

---

## File Structure

- `README.md` - add a `Performance` section explaining what is and is not covered by performance claims.
- `docs/performance-benchmark.md` - new benchmark guide describing scope, scenarios, and interpretation.
- `docs/superpowers/specs/2026-06-04-performance-e-benchmark-design.md` - design record for the work.

## Task 1: Add a Performance section to the README

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add the section heading and summary**

Insert a section with a short, direct summary:

```markdown
## Performance

`AdaptadorGEO` tem baixo overhead de tradução. O custo principal das consultas espaciais continua no banco de dados, porque a biblioteca apenas monta a expressão e gera o `SqlFragment`.
```

- [ ] **Step 2: Explain what should be benchmarked**

Include these points:

- benchmark translation only, not database execution
- compare `GeoDatabase` with direct translators
- focus on allocations and translation time

- [ ] **Step 3: Explain what affects real-world performance**

Mention:

- spatial indexes
- query shape
- database engine behavior
- `Buffer`, `Distance` and other expensive operators

## Task 2: Create a benchmark guide

**Files:**
- Create: `docs/performance-benchmark.md`

- [ ] **Step 1: Explain the benchmark scope**

The guide should clearly say:

- no network
- no database execution
- no result materialization
- translation only

- [ ] **Step 2: Define the benchmark scenarios**

Use the same expressions for each provider:

- `Intersects(Point)`
- `Contains(Polygon)`
- `Within(MultiPolygon)`
- `Distance(Point)`
- `Buffer(250)`

- [ ] **Step 3: Define the metrics**

Measure:

- average time per translation
- allocations per translation
- relative comparison between providers

- [ ] **Step 4: Add interpretation notes**

Explain that:

- faster translation does not necessarily mean faster query execution
- database indexes and engine optimizers dominate real query time
- the benchmark is for translation overhead, not database throughput

## Task 3: Validate the benchmark narrative

**Files:**
- Modify: `README.md`
- Modify: `docs/performance-benchmark.md`

- [ ] **Step 1: Check for ambiguity**

Verify that the README and benchmark guide both say the benchmark is translation-only.

- [ ] **Step 2: Check for provider bias**

Make sure the text does not imply one provider is universally faster overall.

- [ ] **Step 3: Commit**

```bash
git add README.md docs/performance-benchmark.md docs/superpowers/specs/2026-06-04-performance-e-benchmark-design.md docs/superpowers/plans/2026-06-04-performance-e-benchmark.md
git commit -m "docs: add performance guidance and benchmark plan"
```

---

## Self-Review

### 1. Spec coverage

The work covers the requested deliverables:

- a Performance section in the README
- a benchmark plan comparing providers
- a focus on translation-only measurement

### 2. Placeholder scan

No placeholders or vague instructions remain.

### 3. Type consistency

The benchmark targets the existing API surface:

- `GeoDatabase`
- `MySqlSpatialTranslator`
- `SqlServerSpatialTranslator`
- `PostgreSqlSpatialTranslator`

