# Graph Update v2 Contract

## Purpose

Define the public, behavior-level contract for graph mutation in package v2.
This contract governs supported relationship semantics and required outcomes.

## Public Entry Contract

- Operation: Graph mutation for a loaded aggregate and an incoming detached target
  graph.
- Input model: The caller provides a separate **detached** object graph
  representing the desired post-mutation state. The package compares it against
  the **tracked** original graph (loaded via `DbContext`) to determine mutations.
  The package owns the diff logic; the caller does not interact with the change
  tracker directly.
- Required inputs:
  - Existing aggregate graph tracked by the `DbContext`, with explicitly loaded
    navigations that are intended to participate in mutation.
  - Detached incoming graph representing desired post-mutation state.
- Required guarantees:
  - Deterministic behavior for supported relationship patterns.
  - Fail-fast rejection for unsupported/ambiguous/unloaded-dependent requests.
  - All-or-nothing behavior when rejection occurs.

## Supported Relationship Patterns

### Pure Many-to-Many Association (Skip Navigation)

- Add link to existing entity (tracked or in store): `update` — entity properties
  are updated via `SetValues`, then the entity is added to the skip navigation
  collection creating the join table row.
- Add link to new entity (not in tracker or store): `insert` — entity is
  explicitly tracked as `Added` via `context.Add()`, then added to the skip
  navigation collection. Both the entity and join table row are inserted.
- Update existing related entity properties through association: `update` —
  matched by primary key, scalar properties updated via `SetValues`.
- Remove association: `unlink` — entity is removed from the skip navigation
  collection; EF Core deletes the join table row. The related entity itself
  is preserved.
- Related entity deletion due solely to unlink: `reject`

### Many-to-Many Association Entity with Payload

- Add assignment entity: `insert` — new association entity added to the
  collection navigation; EF Core inserts the join row with payload fields.
- Update assignment payload: `update` — matched by composite primary key,
  payload scalar properties updated via `SetValues`.
- Remove assignment entity: `delete` — association entity removed via
  `context.Remove()`; the join row is deleted. Related entities on both
  sides of the relationship are preserved.
- Related entity deletion due solely to assignment removal: `reject`

### Required One-to-One

- Update existing dependent scalar properties: `update` — matched by key,
  scalar properties updated via `SetValues`. Nested navigations processed
  recursively.
- Add dependent when none exists: `insert` — the detached dependent is
  assigned to the tracked navigation; EF Core inserts it on `SaveChanges`.
- Remove required dependent (updated graph sets reference to `null`): `delete`
  — the existing dependent is marked for deletion via `context.Remove()`.
  The dependent row is deleted on `SaveChanges`.
- Ownership resolution: determined by `IForeignKey.IsRequired == true` in
  EF Core metadata. The FK is non-nullable (e.g., `CoursePolicy.CourseId`).
- Missing ownership/requiredness metadata: `reject`

### Optional One-to-One

- Update existing dependent scalar properties: `update` — matched by key,
  scalar properties updated via `SetValues`. Nested navigations processed
  recursively.
- Add dependent when none exists: `insert` — the detached dependent is
  assigned to the tracked navigation; EF Core inserts it on `SaveChanges`.
- Remove optional reference (updated graph sets reference to `null`):
  `null/detach` — the navigation is cleared (`CurrentValue = null`); EF Core
  nulls the FK on the dependent via `DeleteBehavior.SetNull`. The dependent
  entity is preserved in the database.
- Replace dependent: the old dependent is detached (FK nulled), and the new
  dependent is inserted. Both the old (preserved) and new entities exist
  after `SaveChanges`.
- Ownership resolution: determined by `IForeignKey.IsRequired == false` in
  EF Core metadata. The FK is nullable (e.g., `MentorWorkspace.MentorId`).
- Silent fallback from optional behavior to delete: `reject`

## Unsupported Relationship Types in Mixed Graphs

- Loaded navigations of currently-unsupported relationship types (e.g.,
  one-to-many) are **silently skipped** when no mutations are detected.
- If mutations are detected in loaded navigations of unsupported types, the
  **entire operation is rejected** with a clear exception.
- This allows practical use of real-world graphs that naturally contain mixed
  relationship types without forcing the caller to strip unsupported navigations.

## Cross-Cutting Contract Rules

- The caller provides a detached graph; the package diffs against the tracked
  original using EF Core metadata for key matching.
- Only explicitly loaded navigations participate in supported mutation.
- Loaded navigations of unsupported types are inspected for changes and rejected
  only if mutated; otherwise silently skipped.
- Unsupported mutation in any branch causes full operation rejection.
- Partial application of only supported branches is not allowed.
- Every supported relationship behavior must be covered by integration contract
  tests against a real provider.
- Public docs must match shipped behavior outcomes.
