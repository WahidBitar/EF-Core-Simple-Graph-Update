# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Project Is

A NuGet package (`Diwink.Extensions.EntityFrameworkCore`) that provides a single `DbContext.UpdateGraph()` extension method for EF Core. It diffs a detached entity graph against a tracked one and applies mutations using relationship-specific strategies, then the caller calls `SaveChangesAsync()`.

Published to NuGet as `Diwink.Extensions.EntityFrameworkCore` (current version 11.0.0).

## Commands

```bash
# Build (multi-TFM: net8.0, net9.0, net10.0, net11.0)
dotnet build src/EFCore.UpdateGraph.slnx

# Run all tests (unit + integration)
dotnet test src/EFCore.UpdateGraph.slnx

# Run only unit tests (in-memory, no Docker)
dotnet test src/Diwink.Extensions.EntityFrameworkCore.Tests.Unit

# Run only integration tests (requires Docker — uses Testcontainers SQL Server)
dotnet test src/Diwink.Extensions.EntityFrameworkCore.Tests.Integration

# Run a single test by name
dotnet test src/Diwink.Extensions.EntityFrameworkCore.Tests.Unit --filter "FullyQualifiedName~EntityKeyHelperTests.GetKeyValues"

# Pack the NuGet package
dotnet pack src/Diwink.Extensions.EntityFrameworkCore -c Release -o ./artifacts
```

## Architecture

### Two-Phase Graph Update

The core design is a **validate-then-apply** pipeline in `GraphUpdateOrchestrator`:

1. **Phase 1 — Validate**: Walk every loaded navigation on the tracked entity. Classify each navigation and collect all errors into an `OperationGuard`. If any navigation mutation is unsupported, the entire operation is rejected before any change tracker state is modified (all-or-nothing semantics). Uses try/finally recursion path (temporary membership) to allow revisiting entities via different validation paths.
2. **Phase 2 — Apply**: Update scalar properties via `SetValues()`, then delegate each navigation to its relationship strategy. All supported navigation types recursively apply nested navigations on child entities.

### Cycle Detection

Bidirectional navigations (e.g., `Course.Tags ↔ TopicTag.Courses`) can create cycles during recursive graph traversal. Two mechanisms prevent infinite recursion:

- **`IsNavigationBackToAggregateRoot`**: Skips any navigation whose target type equals the aggregate root type (e.g., `Course.Catalog` when root is `LearningCatalog`).
- **`visitedEntities` set**: A permanent `HashSet<object>` with `ReferenceEqualityComparer.Instance` tracks every entity processed during apply. If an entity is encountered again via a different path, `ApplyNavigations` returns immediately. First-wins semantics — an entity's navigations are processed exactly once.

### Navigation Classification

`ClassifyNavigation()` maps EF Core metadata to one of six buckets:

| Classification | EF Metadata Signal | Strategy |
|---|---|---|
| `PureManyToMany` | `ISkipNavigation` | `PureManyToManyStrategy` |
| `PayloadManyToMany` | Collection where target is a join entity with composite FK key + payload properties | `PayloadManyToManyStrategy` |
| `OneToMany` | `INavigation`, `IsCollection`, regular entity (not payload join) | `OneToManyStrategy` |
| `RequiredOneToOne` | `INavigation`, `IsUnique`, `IsRequired` | `RequiredOneToOneStrategy` |
| `OptionalOneToOne` | `INavigation`, `IsUnique`, `!IsRequired` | `OptionalOneToOneStrategy` |
| `Unsupported` | Many-to-one references (`!IsCollection`, `!IsUnique`) | Silently skipped if unchanged; throws if mutated |

### Key Components

- **`DbContextExtensions`** — Public API surface. Single method: `UpdateGraph<T>(this DbContext, T existing, T updated)`.
- **`GraphUpdateOrchestrator`** — Internal orchestrator. Owns validation + apply phases, recursion cycle detection, and navigation dispatch.
- **`OperationGuard`** — Collects `GraphUpdateException`s. Throws single error or wraps multiple in `PartialMutationNotAllowedException`.
- **`EntityKeyHelper`** — Reads primary keys from tracked entries and detached CLR objects via EF metadata. Handles composite keys and `byte[]` comparison.
- **`RelationshipStrategies/`** — One static class per supported pattern. Each has an `Apply()` or `RemoveDependent()`/`DetachDependent()` entry point. All strategies recursively call `ApplyNavigations` on child entities, threading `visitedEntities` for cycle detection. `OneToManyStrategy` branches on FK `IsRequired` for removal (delete vs null FK). `PureManyToManyStrategy` uses `ApplyValuesIfNotModified` (skips if entity state is Modified) to avoid overwriting scalars already set by a prior path.
- **`PropertyAccessorCache`** — Thread-safe `ConcurrentDictionary` cache for `PropertyInfo` lookups, avoiding repeated reflection.
- **`CollectionHelper`** — Shared Add/Remove for collection navigations. `IList` fast path with `ICollection<T>` reflection fallback.
- **`Exceptions/`** — Hierarchy rooted at `GraphUpdateException : InvalidOperationException`. Each carries a `RelationshipPath` for diagnostics.

### Test Model (TestModel project)

An education-domain model exercising all supported relationship types:

- `LearningCatalog` → `Course` (one-to-many, required FK, cascade delete)
- `Course` → `CourseReview` (one-to-many, optional FK, SetNull)
- `Course` → `TopicTag` (pure many-to-many via skip navigation)
- `Course` → `CourseMentorAssignment` (payload many-to-many — join entity with extra properties)
- `Course` → `CoursePolicy` (required one-to-one)
- `Mentor` → `MentorWorkspace` (optional one-to-one)

Entity configurations are in `TestModel/Configurations/` using `IEntityTypeConfiguration<T>`.

### Test Organization

- **Unit tests** use EF Core InMemory provider. No Docker needed.
- **Integration tests** use Testcontainers with SQL Server (`mcr.microsoft.com/mssql/server:2022-latest`). Override image with `SQL_SERVER_IMAGE` env var. Schema is reset per-test via `DatabaseBootstrap.ResetSchemaAsync()`. All integration tests share one container via xUnit collection fixture (`IntegrationTestCollection`).
- Integration tests are organized by contract area: `Contracts/OneToMany/`, `Contracts/ManyToMany/`, `Contracts/OneToOne/`, `Contracts/CycleDetection/`, `Contracts/Rejection/`.

## Multi-TFM Build

`Directory.Build.props` defines shared TFMs and version ranges:
- `net8.0` / `net9.0` → EF Core 9.x (`[9.0.14,10.0.0)`)
- `net10.0` → EF Core 10.x (`[10.0.5,11.0.0)`)
- `net11.0` → EF Core 11.x (`[11.0.0,12.0.0)`)

The library project uses `InternalsVisibleTo` for both test projects.

## CI

GitHub Actions (`.github/workflows/dotnet-core.yml`): build + test on push/PR to `master`, then pack + publish to NuGet on master push (requires `NUGET_API_KEY` secret).
