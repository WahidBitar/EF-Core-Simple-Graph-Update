# Implementation Plan: Graph-Update Package Rebuild

**Branch**: `001-rebuild-graph-update` | **Date**: 2026-03-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-rebuild-graph-update/spec.md`

## Summary

Rebuild the `Diwink.Extensions.EntityFrameworkCore` graph-update package from
scratch targeting EF Core 10+ on .NET 10. The new v2 package accepts a detached
object graph representing the caller's desired state and diffs it against the
tracked original to determine mutations. Supported relationship patterns: pure
many-to-many, many-to-many with payload association entities, required one-to-one,
and optional one-to-one (null/detach by ownership model). Unsupported patterns
fail fast; loaded-but-unchanged unsupported navigations are silently skipped.
All-or-nothing rejection semantics apply.

## Technical Context

**Language/Version**: C# on .NET 10  
**Primary Dependencies**: `Microsoft.EntityFrameworkCore` 10.x, `Microsoft.EntityFrameworkCore.SqlServer` (tests)  
**Storage**: SQL Server via Testcontainers (integration tests); no runtime storage dependency (library)  
**Testing**: xUnit, FluentAssertions, Testcontainers.MsSql  
**Target Platform**: .NET 10 (library targeting `net10.0`)  
**Project Type**: NuGet library  
**Performance Goals**: N/A (library; performance is bounded by EF Core change tracker)  
**Constraints**: Must not expand scope beyond graph-update behavior (constitution I); all supported behaviors must have contract tests against a real provider (constitution IV)  
**Scale/Scope**: Single library package, 4 v2 projects (library, test model, unit tests, integration tests)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Pre-Phase 0 | Post-Phase 1 | Notes |
|-----------|-------------|--------------|-------|
| I. Package Scope and Identity | PASS | PASS | FR-014 scopes to graph-update only; no helper expansion |
| II. Relationship-Aware Mutation Contract | PASS | PASS | Spec defines explicit contract per pattern (FR-002..FR-010); detached-graph input model clarified (FR-001a) |
| III. Explicit Support Boundaries | PASS | PASS | FR-011, FR-017, FR-018, FR-019 define rejection and skip rules; error contract covers all rejection categories |
| IV. Semantic Verification over Implementation Coverage | PASS | PASS | Decision 3 mandates real-provider integration; test split by outcome family (Decision 7) |
| V. Documentation, Review, and Release Discipline | PASS | PASS | SC-007 requires docs before release; major version change acknowledged |

No gate violations. No complexity justifications needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-rebuild-graph-update/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   ├── graph-update-v2-contract.md
│   └── graph-update-v2-errors.md
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src-v2/
├── Diwink.Extensions.EntityFrameworkCore.V2/
│   ├── Diwink.Extensions.EntityFrameworkCore.V2.csproj
│   └── (library source: graph diff, mutation engine, error types)
├── Diwink.Extensions.EntityFrameworkCore.V2.TestModel/
│   ├── Diwink.Extensions.EntityFrameworkCore.V2.TestModel.csproj
│   └── (entity classes, DbContext, configuration)
├── Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/
│   ├── Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit.csproj
│   └── (graph diff logic, validation, error generation)
└── Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/
    ├── Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration.csproj
    └── (contract tests by outcome: update, unlink, null, delete, reject)
```

**Structure Decision**: Isolated `src-v2/` tree alongside legacy `src/` to enable
parallel maintenance and clear review boundaries (Research Decision 2). Legacy
source remains as behavioral reference only.

## Complexity Tracking

No constitution violations to justify. All principles pass cleanly.
