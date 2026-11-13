# Diwink.Extensions.EntityFrameworkCore

[![NuGet](https://img.shields.io/nuget/v/Diwink.Extensions.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Diwink.Extensions.EntityFrameworkCore/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Diwink.Extensions.EntityFrameworkCore.svg)](https://www.nuget.org/packages/Diwink.Extensions.EntityFrameworkCore/)
[![.NET CI](https://github.com/WahidBitar/EF-Core-Simple-Graph-Update/actions/workflows/dotnet-core.yml/badge.svg)](https://github.com/WahidBitar/EF-Core-Simple-Graph-Update/actions/workflows/dotnet-core.yml)
[![License](https://img.shields.io/github/license/WahidBitar/EF-Core-Simple-Graph-Update.svg)](https://github.com/WahidBitar/EF-Core-Simple-Graph-Update/blob/master/LICENSE)

A single `DbContext.UpdateGraph()` extension method for EF Core that diffs a detached entity graph against a tracked one and applies the correct add, update, and remove operations for every relationship type — then you call `SaveChangesAsync()`.

## Why?

EF Core tracks individual entities well, but updating a full aggregate graph (root + children + many-to-many links + nested navigations) requires tedious manual diffing. `UpdateGraph` handles this automatically:

- **One method call** replaces manual add/update/remove loops
- **Relationship-aware** — each navigation type gets the correct semantics
- **Recursive** — nested navigations at any depth are processed automatically
- **Safe** — validates the entire graph before applying any changes (all-or-nothing)

## Installation

```shell
dotnet add package Diwink.Extensions.EntityFrameworkCore
```

**Supported platforms:** .NET 8, .NET 9, .NET 10, and .NET 11 with EF Core 9.x, 10.x, or 11.x

## Quick Start

```csharp
// 1. Load the tracked entity with all navigations you want to update
var existing = await dbContext.Courses
    .Include(c => c.Tags)
    .Include(c => c.Policy)
    .Include(c => c.MentorAssignments)
    .Include(c => c.Reviews)
    .FirstAsync(c => c.Id == courseId);

// 2. Build the desired state (detached graph — from API, DTO mapping, etc.)
var updated = new Course
{
    Id = courseId,
    Title = "Updated Title",
    Code = "CS-101",
    Tags = [ new TopicTag { Id = existingTagId, Label = "Architecture" } ],
    Policy = new CoursePolicy { CourseId = courseId, PolicyVersion = "2.0", IsMandatory = true },
    Reviews = [ new CourseReview { Id = reviewId, Rating = 5, Comment = "Updated" } ]
};

// 3. Diff and apply — one call handles everything
dbContext.UpdateGraph(existing, updated);
await dbContext.SaveChangesAsync();
```

**Key rule:** Only navigations that are `.Include()`-loaded on the tracked entity will be processed. Unloaded navigations are left untouched. If the detached graph attempts to mutate an unloaded navigation, the operation is rejected.

## Supported Relationship Patterns

| Pattern | Add | Update | Remove | Behavior |
|---------|:---:|:------:|:------:|----------|
| **One-to-many** (required FK) | Child inserted | Scalars + nested navs updated | Child **deleted** | Cascade — child can't exist without parent |
| **One-to-many** (optional FK) | Child inserted | Scalars + nested navs updated | FK **nulled** | Child preserved, association cleared |
| **Pure many-to-many** (skip nav) | Link created | Related entity properties updated | Link **removed** | Related entity preserved in database |
| **Payload many-to-many** (join entity) | Association inserted | Payload + nested navs updated | Association **deleted** | Related entities preserved |
| **Required one-to-one** | Dependent inserted | Scalars + nested navs updated | Dependent **deleted** | Cascade delete |
| **Optional one-to-one** | Dependent inserted | Scalars + nested navs updated | FK **nulled** | Dependent preserved |

All supported navigation types recursively process nested navigations on child entities.

### Unsupported

**Many-to-one references** (dependent-side back-references like `Course.Catalog`) are not supported as update targets. They are silently skipped when unchanged, or rejected if mutations are detected.

## How It Works

### Two-Phase Pipeline

`UpdateGraph` uses a **validate-then-apply** pipeline:

1. **Validate** — Walk every loaded navigation on the tracked entity. Classify each relationship and collect all errors. If any navigation mutation is unsupported, the **entire operation is rejected** before any change tracker state is modified (all-or-nothing semantics).

2. **Apply** — Update scalar properties, then delegate each navigation to its relationship strategy. All strategies recursively process nested navigations.

### Cycle Detection

Bidirectional navigations (e.g., `Course.Tags` / `TopicTag.Courses`) can create cycles during recursive graph traversal. Two mechanisms prevent infinite recursion:

- **Aggregate root filter** — Navigations whose target type equals the aggregate root type are skipped (e.g., `Course.Catalog` when root is `LearningCatalog`).
- **Visited set** — A `HashSet<object>` with reference equality tracks processed entities. If an entity is encountered again via a different path, it's skipped. First-visit-wins semantics.

### Navigation Classification

The engine inspects EF Core metadata to classify each navigation:

| EF Core Metadata Signal | Classification |
|---|---|
| `ISkipNavigation` | Pure many-to-many |
| `INavigation` + `IsCollection` + payload join entity | Payload many-to-many |
| `INavigation` + `IsCollection` | One-to-many |
| `INavigation` + `!IsCollection` + `IsUnique` + `IsRequired` | Required one-to-one |
| `INavigation` + `!IsCollection` + `IsUnique` + `!IsRequired` | Optional one-to-one |
| `INavigation` + `!IsCollection` + `!IsUnique` | Unsupported (many-to-one) |

## Error Handling

All exceptions inherit from `GraphUpdateException` and include a `RelationshipPath` property for diagnostics.

| Exception | When |
|-----------|------|
| `UnsupportedNavigationMutatedException` | A many-to-one reference was mutated in the detached graph |
| `UnloadedNavigationMutationException` | The detached graph contains data for a navigation that wasn't `.Include()`-loaded |
| `PartialMutationNotAllowedException` | Multiple navigation violations detected (wraps individual errors) |

```csharp
try
{
    dbContext.UpdateGraph(existing, updated);
    await dbContext.SaveChangesAsync();
}
catch (GraphUpdateException ex)
{
    // ex.RelationshipPath tells you which navigation failed, e.g. "Course.Catalog"
    logger.LogError("Graph update failed at {Path}: {Message}", ex.RelationshipPath, ex.Message);
}
```

## Advanced Usage

### Deep Graphs with Multiple Relationship Types

```csharp
// Load the full aggregate with all navigations to update
var existing = await dbContext.LearningCatalogs
    .Include(c => c.Courses)
        .ThenInclude(c => c.Tags)
    .Include(c => c.Courses)
        .ThenInclude(c => c.Policy)
    .Include(c => c.Courses)
        .ThenInclude(c => c.Reviews)
    .Include(c => c.Courses)
        .ThenInclude(c => c.MentorAssignments)
    .FirstAsync(c => c.Id == catalogId);

// UpdateGraph recursively handles all levels:
// LearningCatalog → Courses (one-to-many)
//   → Tags (pure M:M), Policy (one-to-one),
//     Reviews (one-to-many), MentorAssignments (payload M:M)
dbContext.UpdateGraph(existing, updatedCatalog);
await dbContext.SaveChangesAsync();
```

### Selective Updates

Only included navigations are processed. This lets you update specific parts of an aggregate:

```csharp
// Only update tags — Policy, Reviews, MentorAssignments are untouched
var existing = await dbContext.Courses
    .Include(c => c.Tags)
    .FirstAsync(c => c.Id == courseId);

dbContext.UpdateGraph(existing, updated);
```

### One-to-Many Removal Semantics

Removal behavior depends on the FK constraint:

```csharp
// Required FK (e.g., LearningCatalog → Course):
// Removing a Course from the collection DELETES the Course row

// Optional FK (e.g., Course → CourseReview):
// Removing a Review from the collection NULLS the FK — the Review row is preserved
```

## Requirements

- **.NET 8.0**, **.NET 9.0**, **.NET 10.0**, or **.NET 11.0**
- **EF Core 9.x** (net8.0/net9.0), **EF Core 10.x** (net10.0), or **EF Core 11.x** (net11.0)
- No additional dependencies beyond `Microsoft.EntityFrameworkCore`


## Contributing
Please don't hesitate to contribute or give us your feedback and advice 🌹 🌹

## License

[Apache License 2.0](LICENSE)
