# Data Model: Graph-Update Package Rebuild (v2)

## Overview

The v2 test model is designed to exercise every supported relationship contract
and every required rejection path. The model intentionally includes both pure
many-to-many and payload-based many-to-many forms, plus required and optional
one-to-one references. The model also includes one-to-many relationships
(`LearningCatalog` -> `Courses`) which are currently unsupported; these serve as
test coverage for FR-018/FR-019 (silently skip when unchanged, reject when
mutated).

## Entities

### LearningCatalog (aggregate root)

- Purpose: Root aggregate used to drive graph updates.
- Fields:
  - `Id` (Guid, immutable key)
  - `Name` (string, required, unique within test fixture seed)
- Relationships:
  - One-to-many `Courses`
  - Pure many-to-many `Tags` (skip navigation to `TopicTag`)

### Course

- Purpose: Primary child entity under `LearningCatalog`.
- Fields:
  - `Id` (Guid)
  - `CatalogId` (Guid, FK)
  - `Title` (string, required)
  - `Code` (string, required, unique within catalog)
- Relationships:
  - Required one-to-one `CoursePolicy`
  - Many-to-many with payload via `CourseMentorAssignments`
  - Pure many-to-many `Tags`

### TopicTag (pure many-to-many related entity)

- Purpose: Shared taxonomy entity linked to courses/catalogs.
- Fields:
  - `Id` (Guid)
  - `Label` (string, required, unique)
- Relationships:
  - Pure many-to-many with `Course`

### Mentor

- Purpose: Related entity used in payload many-to-many scenarios.
- Fields:
  - `Id` (Guid)
  - `DisplayName` (string, required)
  - `Status` (enum/string)
- Relationships:
  - Many-to-many with payload via `CourseMentorAssignment`
  - Optional one-to-one `MentorWorkspace`

### CourseMentorAssignment (association entity with payload)

- Purpose: Payload-bearing join entity between `Course` and `Mentor`.
- Fields:
  - `CourseId` (Guid, FK, key part)
  - `MentorId` (Guid, FK, key part)
  - `Role` (string, required)
  - `AssignedOnUtc` (DateTime, required)
  - `AllocationPercent` (decimal, 0-100 validation)
- Relationships:
  - Many-to-one to `Course`
  - Many-to-one to `Mentor`

### CoursePolicy (required one-to-one dependent)

- Purpose: Required dependent proving delete-on-remove behavior.
- Fields:
  - `CourseId` (Guid, PK/FK to `Course`)
  - `PolicyVersion` (string, required)
  - `IsMandatory` (bool)
- Relationships:
  - Required one-to-one with `Course`

### MentorWorkspace (optional one-to-one dependent/reference)

- Purpose: Optional one-to-one entity used for `null`/`detach` semantics.
- Fields:
  - `Id` (Guid)
  - `MentorId` (Guid, nullable or required per ownership mode under test)
  - `DeskCode` (string)
  - `Building` (string)
- Relationships:
  - Optional one-to-one with `Mentor`

## Relationship Contract Mapping

| Relationship Pattern | Supported | Mutation Contract |
|----------------------|-----------|-------------------|
| Pure many-to-many (`Course` <-> `TopicTag`) | Yes | Add/update/unlink associations; unlink removal MUST NOT delete related entities |
| Many-to-many with payload (`Course` <-> `Mentor` via `CourseMentorAssignment`) | Yes | Create/update/remove assignment entities and related entity updates per contract; remove assignment MUST preserve related entities unless explicitly documented otherwise |
| Required one-to-one (`Course` <-> `CoursePolicy`) | Yes | Removing required dependent from graph MUST result in delete behavior |
| Optional one-to-one (`Mentor` <-> `MentorWorkspace`) | Yes | Removing reference MUST use documented `null` or `detach` behavior by ownership model |
| One-to-many (`LearningCatalog` -> `Courses`) | No (unsupported in v2) | Silently skipped when loaded but unchanged; entire operation rejected if mutations detected (FR-018/FR-019) |

## Validation Rules

- Keys are immutable once persisted.
- Duplicate many-to-many links or duplicate payload association keys are rejected.
- Payload validation:
  - `Role` required
  - `AllocationPercent` must be between `0` and `100`
- Required one-to-one dependent cannot exist without principal.
- Optional one-to-one behavior must map to one documented ownership mode.

## State Transitions

- `update`: Existing entity state changes within a supported pattern.
- `unlink`: Relationship link removed while both entities remain.
- `delete`: Required dependent removed and deleted.
- `null`: Optional relationship FK/reference set to null.
- `detach`: Optional reference disconnected by ownership model contract.
- `reject`: Unsupported, ambiguous, or insufficiently loaded mutation request;
  entire graph operation rejected.

## Graph Input Model

- The caller provides a separate **detached** object graph representing the
  desired post-mutation state.
- The package compares the detached graph against the **tracked** original graph
  (loaded via `DbContext` with explicit `Include()` calls) to determine mutations.
- Entity matching uses EF Core metadata primary keys.

## Rejection Scenarios Covered by Model

- Mutation depends on unloaded navigation branch.
- Graph includes at least one unsupported mutation path.
- Ambiguous ownership/requiredness for one-to-one mutation.
- Unsupported relationship pattern not included in the published v2 contract.
- Loaded one-to-many navigation (`Courses`) has mutations detected (items added,
  removed, or modified) → reject entire operation.
- Loaded one-to-many navigation (`Courses`) is present but unchanged → silently
  skipped.
