# Feature Specification: Graph-Update Package Rebuild

**Feature Branch**: `001-rebuild-graph-update`  
**Created**: 2026-03-31  
**Status**: Draft  
**Input**: User description: "this project represent an old package that targets EntityFrameworkCore v3 We need to REBUILD it from scratch to be compatible with EntityFrameworkCore v10+ we should cover the Many to Many ; one to one relationships"

## Clarifications

### Session 2026-03-31

- Q: For supported optional one-to-one removals, should the first rebuild support only `null`, only `detach`, both by ownership model, or reject them for now? → A: Support both `null` and `detach`, chosen by ownership model in this first rebuild.
- Q: Should only explicitly loaded relationships participate, and should missing or partially loaded navigations be rejected when a requested mutation depends on them? → A: Only explicitly loaded relationships participate; if a requested mutation depends on a missing or partially loaded navigation, reject it clearly.
- Q: Should many-to-many support in the first rebuild manage links only, or also create and update related entities through that relationship? → A: Many-to-many support manages link membership and may both create new related entities and update existing ones through that relationship.
- Q: If any requested mutation inside an otherwise supported graph is unsupported, should the package reject the entire operation or apply the supported parts only? → A: Reject the entire operation and apply none of the requested graph changes.
- Q: Should the first rebuild's many-to-many support include only pure association membership, or also association entities with payload? → A: Support pure many-to-many association membership and association entities with payload in the same first rebuild contract.
- Q: When a graph contains loaded navigations of currently-unsupported relationship types (e.g., one-to-many), should the package silently skip them, reject if changes are detected, or reject any graph containing them? → A: Detect changes in unsupported relationship types and reject only if mutations are found; unchanged unsupported navigations are silently skipped.
- Q: How is the "updated graph" provided to the package — tracked in-place modifications, a separate detached graph, or an explicit delta/command? → A: The caller provides a separate detached object graph representing the desired state; the package diffs it against the tracked original.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Modern Many-to-Many Updates (Priority: P1)

As an application developer upgrading from the legacy package, I want the rebuilt
package to update many-to-many relationships in a modern application model so
that association changes are applied safely without deleting related entities and
can also create or update related entities when that behavior is part of the
supported contract, whether the relationship is expressed as pure association
membership or through association entities with payload.

**Why this priority**: Many-to-many mutation is a core graph-update need and is
one of the explicit relationship patterns requested for the rebuild, including
both direct association membership and payload-carrying association entities.

**Independent Test**: Can be fully tested by updating an aggregate that adds and
removes many-to-many associations across both supported many-to-many shapes, then
confirming the aggregate reflects the requested links or association entities
while the related entities still exist.

**Acceptance Scenarios**:

1. **Given** an aggregate with existing many-to-many links and related entities
   that must remain in the system, **When** the caller removes one association in
   the updated graph, **Then** the package removes only the link and does not
   delete the related entity.
2. **Given** an aggregate with existing many-to-many links, **When** the caller
   adds a new valid association in the updated graph, **Then** the package creates
   the missing link and preserves the other existing related entities.
3. **Given** a supported many-to-many relationship containing related entities
   that are new or changed, **When** the caller submits the updated graph,
   **Then** the package may create new related entities, update existing related
   entities, and reconcile link membership according to the published contract.
4. **Given** a supported many-to-many relationship represented through
   association entities with payload, **When** the caller adds, updates, or
   removes those association entities in the updated graph, **Then** the package
   reconciles the association entities and preserves the related entities unless
   the published contract explicitly states otherwise.

---

### User Story 2 - Explicit One-to-One Semantics (Priority: P2)

As an application developer using reference navigations, I want the rebuilt
package to handle one-to-one relationships with explicit required and optional
semantics so that reference removals produce predictable delete, null, or detach
outcomes.

**Why this priority**: One-to-one support is the second explicit relationship
pattern requested for the rebuild, and its removal behavior must be contractually
clear to avoid destructive ambiguity.

**Independent Test**: Can be fully tested by updating one required one-to-one
relationship and one optional one-to-one relationship, then confirming the
package applies the documented removal behavior for each.

**Acceptance Scenarios**:

1. **Given** a required one-to-one dependent reference in a supported graph,
   **When** the caller removes that reference from the updated graph, **Then** the
   package deletes the removed dependent according to the published contract.
2. **Given** an optional one-to-one reference in a supported graph, **When** the
   caller removes that reference from the updated graph, **Then** the package
   nulls or detaches the reference according to the ownership model published in
   the contract and does not silently substitute a different behavior.

---

### User Story 3 - Clear Support Boundaries for the Rebuild (Priority: P3)

As a package maintainer and reviewer, I want the rebuilt package to document
exactly which graph shapes are supported and to reject unsupported mutation
scenarios clearly so that the package can evolve safely without carrying forward
legacy ambiguity.

**Why this priority**: A from-scratch rebuild should replace implicit legacy
behavior with a smaller, explicit, reviewable contract.

**Independent Test**: Can be fully tested by exercising one supported
many-to-many scenario, one supported one-to-one scenario, and one unsupported
scenario that must fail fast with a clear exception.

**Acceptance Scenarios**:

1. **Given** a graph mutation request outside the documented support boundary,
   **When** the caller invokes the package, **Then** the package rejects the
   request with a clear, specific exception instead of applying a best-effort
   mutation.
2. **Given** a documented supported relationship pattern, **When** the package is
   released, **Then** public documentation states whether the pattern updates,
   unlinks, nulls, deletes, or rejects on removal.
3. **Given** an otherwise supported graph containing one unsupported requested
   mutation, **When** the caller invokes the package, **Then** the package
   rejects the entire operation and applies none of the requested graph changes.

### Edge Cases

- A many-to-many removal attempts to drop the association and the related entity
  in a single implicit action.
- A many-to-many update includes both link membership changes and new or modified
  related entity state in the same operation.
- A many-to-many relationship is represented through association entities that
  carry payload and must be reconciled alongside the related entities.
- A one-to-one removal is requested but the package cannot determine whether the
  reference is required or optional from the supported contract.
- A navigation needed to apply deterministic graph mutation is missing or only
  partially available to the operation, and the requested mutation therefore
  must be rejected clearly.
- A caller submits a graph shape that is not part of the rebuilt package's
  documented support boundary.
- A graph contains both supported and unsupported requested mutations in the same
  operation.
- A legacy scenario appears to "work" in code but has no contract, tests, or
  documentation in the rebuilt package.
- A graph contains loaded navigations of currently-unsupported relationship types
  (e.g., one-to-many) that are unchanged; these must be silently skipped.
- A graph contains loaded navigations of currently-unsupported relationship types
  (e.g., one-to-many) that have been mutated; the entire operation must be
  rejected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The rebuilt package MUST serve as the supported replacement for the
  legacy package for modern EF Core consumers beginning with v10+.
- **FR-001a**: The package MUST accept a separate detached object graph
  representing the caller's desired state and MUST compare it against the
  tracked original graph to determine mutations.
- **FR-002**: The rebuilt package MUST provide a supported graph-update contract
  for many-to-many relationship mutations, including both pure association
  membership and association entities with payload.
- **FR-003**: For supported many-to-many removals, the package MUST unlink the
  relationship and MUST NOT delete the related entities solely because the
  association was removed.
- **FR-004**: For supported many-to-many relationship mutations, the package MAY
  create new related entities and update existing related entities through that
  relationship when the public contract explicitly allows it.
- **FR-005**: When many-to-many related entity creation or update is supported,
  the package MUST document how related entity state changes and link membership
  changes are reconciled in the same operation.
- **FR-006**: The package MUST publish separate behavior contracts for supported
  pure many-to-many association membership and supported association entities
  with payload.
- **FR-007**: The rebuilt package MUST provide a supported graph-update contract
  for one-to-one relationship mutations.
- **FR-008**: For supported required one-to-one removals, the package MUST treat
  removal as delete behavior for the removed dependent.
- **FR-009**: For supported optional one-to-one removals, the package MUST
  support both `null` and `detach` outcomes, chosen by the documented ownership
  model, and MUST NOT silently substitute delete behavior.
- **FR-010**: The package MUST explicitly state the mutation outcome for each
  supported relationship pattern using the terms `update`, `unlink`, `null`,
  `delete`, or `reject`.
- **FR-011**: Unsupported graph shapes, unsupported relationship scenarios, and
  undefined mutation cases MUST fail fast with a clear and specific exception.
- **FR-012**: The rebuilt package MUST define its support boundary as package
  contract and MUST NOT rely on undocumented legacy behavior.
- **FR-013**: Public documentation MUST describe the supported many-to-many and
  one-to-one behaviors, including add, update, and removal outcomes.
- **FR-014**: The rebuild MUST remain scoped to graph-update behavior for this
  package and MUST NOT expand into unrelated EF Core helper functionality.
- **FR-015**: Only explicitly loaded relationships MUST participate in supported
  graph mutation behavior.
- **FR-016**: If a requested mutation depends on a missing or partially loaded
  navigation, the package MUST reject the request with a clear and specific
  exception instead of inferring state or silently ignoring the mutation.
- **FR-017**: If any requested mutation within a graph operation is unsupported,
  the package MUST reject the entire operation and MUST NOT apply only the
  supported subset of requested graph changes.
- **FR-018**: The package MUST detect changes in loaded navigations of
  currently-unsupported relationship types (e.g., one-to-many) and MUST reject
  the entire operation if mutations are found in those navigations.
- **FR-019**: The package MUST silently skip loaded navigations of
  currently-unsupported relationship types when no mutations are detected in
  those navigations, allowing practical use of graphs that naturally contain
  mixed relationship types.

### Key Entities *(include if feature involves data)*

- **Aggregate Graph**: The loaded root object and its included relationships
  (tracked by the DbContext) that are compared against the caller's detached
  updated graph to determine mutations.
- **Association Link**: The many-to-many relationship connection whose presence or
  absence changes without deleting the related entity.
- **Association Entity with Payload**: A many-to-many association object that
  carries its own state and must be reconciled as part of the graph update.
- **Reference Dependent**: The one-to-one related object whose lifecycle depends
  on whether the relationship is required or optional.
- **Relationship Contract**: The public definition of whether a supported
  mutation updates, unlinks, nulls, deletes, or rejects.

### Relationship Behavior Contract *(mandatory for graph-mutation changes)*

| Pattern | Supported? | Add / Update Outcome | Removal Outcome | Ownership / Requiredness Notes |
|---------|------------|----------------------|-----------------|-------------------------------|
| Pure many-to-many association | Yes | Add missing links, create new related entities when supplied, and update existing related entities when supplied | Unlink the association only | Applies only when the relationship needed for mutation is explicitly loaded; related entity creation and update are part of the first rebuild contract |
| Many-to-many association entity with payload | Yes | Create, update, and reconcile association entities with payload, and create or update related entities when supplied according to the published contract | Remove the association entity while preserving the related entities unless the published contract explicitly states otherwise | Applies only when the relationship needed for mutation is explicitly loaded; payload behavior must be documented separately from pure membership |
| One-to-one required reference | Yes | Create, replace, or update the dependent according to the supported contract | Delete the removed dependent | Requiredness must be explicit in the published contract and the reference must be available to the operation |
| One-to-one optional reference | Yes | Create, attach, or update the reference according to the supported contract | Null or detach according to the ownership model published in the contract | The first rebuild supports both `null` and `detach`; the contract must state which applies, and the reference must be available to the operation |

### Unsupported or Rejected Scenarios *(mandatory for graph-mutation changes)*

- Any graph mutation where requiredness or ownership cannot be determined from the
  supported contract: MUST reject with a clear exception describing the ambiguity.
- Any relationship pattern outside the documented support boundary of this
  rebuild: MUST reject with a clear exception describing that the scenario is not
  supported.
- Any mutation attempt that depends on silent best-effort inference from legacy
  behavior: MUST reject with a clear exception describing the undefined behavior.
- Any mutation request that depends on a missing or partially loaded navigation:
  MUST reject with a clear exception describing that the required relationship
  state was not available.
- Any graph operation that contains both supported and unsupported requested
  mutations: MUST reject the entire operation with a clear exception describing
  the unsupported portion.
- Any graph containing loaded navigations of currently-unsupported relationship
  types where mutations are detected in those navigations: MUST reject the entire
  operation with a clear exception describing the unsupported mutation; unchanged
  unsupported navigations are silently skipped.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of supported relationship patterns in this feature have an
  explicit published contract for add, update, and removal behavior.
- **SC-002**: 100% of supported many-to-many removal scenarios in this feature
  unlink associations without deleting the related entities.
- **SC-003**: 100% of supported many-to-many scenarios in this feature that
  include related entity creation or update produce the documented related entity
  and link membership outcomes.
- **SC-004**: 100% of supported many-to-many association-entity-with-payload
  scenarios in this feature produce the documented association-entity and related
  entity outcomes.
- **SC-005**: 100% of supported one-to-one removal scenarios in this feature
  produce the documented delete, null, or detach outcome based on requiredness
  and ownership model.
- **SC-006**: 100% of documented unsupported scenarios exercised for this feature
  fail fast with clear, specific exceptions rather than silent or best-effort
  mutation.
- **SC-007**: Public documentation for the rebuilt package fully describes the
  supported many-to-many and one-to-one behaviors before release.
- **SC-008**: 100% of mutation requests in this feature that depend on missing or
  partially loaded navigations fail fast with clear, specific exceptions.
- **SC-009**: 100% of graph operations in this feature that include any
  unsupported requested mutation are rejected as a whole, with no partial graph
  mutation applied.

## Change Impact *(mandatory when behavior changes)*

- **Semantic Change**: This is a from-scratch replacement of the legacy package's
  graph-update behavior, with an explicit supported contract for many-to-many and
  one-to-one mutations instead of relying on legacy implicit behavior.
- **Versioning Impact**: Major, because the rebuild replaces a legacy behavior
  surface with a newly defined supported contract and does not promise backward
  compatibility with undocumented prior behavior.
- **Documentation Surface**: README, package documentation, and release notes for
  the rebuilt package.

## Assumptions

- The rebuild is allowed to prioritize modern semantic clarity over preserving
  undocumented or ambiguous legacy behavior.
- Initial supported scope for this feature is limited to many-to-many and
  one-to-one relationship handling; other relationship patterns may be rejected
  until specified separately.
- Callers provide a separate detached object graph representing the desired state,
  along with the tracked original graph loaded via the DbContext, supplying the
  relationship data needed for deterministic mutation of the supported scenarios.
- Supported many-to-many behavior in this feature includes link reconciliation
  plus related entity creation and update when the relationship is within the
  published contract, across both pure association membership and association
  entities with payload.
- The rebuilt package is intended for modern EF Core consumers beginning with
  v10+, replacing the current legacy package that only serves older consumers.
