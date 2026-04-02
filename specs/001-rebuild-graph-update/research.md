# Phase 0 Research: Graph-Update Package Rebuild

## Decision 1: Target EF Core 10 on .NET 10 for v2

- Decision: Build `src-v2` against EF Core 10.x and .NET 10.
- Rationale:
  - Feature spec explicitly defines EF Core 10+ compatibility as a primary goal.
  - EF Core stable release planning aligns EF Core 10 with .NET 10.
  - A clean major-line rebuild avoids back-compat constraints from legacy 3.1-era
    behavior.
- Alternatives considered:
  - EF Core 9 / .NET 8 baseline: rejected because it does not satisfy the stated
    compatibility target.
  - Multi-targeting old/new EF Core in one line: rejected due to semantic drift
    and increased maintenance complexity.

## Decision 2: Use an isolated `src-v2` source tree

- Decision: Place v2 library and tests under a new `src-v2/` root.
- Rationale:
  - Enables parallel maintenance during migration.
  - Keeps legacy source as historical and behavioral reference.
  - Reduces accidental coupling between old and new package semantics.
- Alternatives considered:
  - In-place rewrite under existing `src/`: rejected because it increases
    migration risk and makes review boundaries less clear.

## Decision 3: Validate semantics with containerized real SQL Server

- Decision: Run integration contract tests against SQL Server in Docker using
  `Testcontainers.MsSql`.
- Rationale:
  - Constitution requires real-provider integration verification for mutation
    behavior.
  - Existing package context and legacy tests already align with SQL Server.
  - Container lifecycle automation makes local and CI execution deterministic.
- Alternatives considered:
  - InMemory provider-only tests: rejected because provider-specific relational
    behavior is not exercised.
  - Local developer SQL Server dependency only: rejected due to inconsistent
    setup and poor CI portability.
  - SQLite-only strategy: rejected because the current package context is SQL
    Server-centric and some semantics differ across providers.

## Decision 4: Support two many-to-many forms in first v2 contract

- Decision: Support both pure many-to-many membership and association entities
  with payload in this feature.
- Rationale:
  - Clarifications explicitly chose both forms for first release scope.
  - Payload association is required to represent realistic enterprise graphs.
- Alternatives considered:
  - Pure many-to-many only: rejected by clarification outcome.
  - Payload-only support: rejected because it excludes a common supported pattern.

## Decision 5: Enforce explicit loading and all-or-nothing rejection

- Decision:
  - Only explicitly loaded navigations participate.
  - If any requested mutation is unsupported, reject the entire operation.
- Rationale:
  - Prevents ambiguous partial updates.
  - Aligns with fail-fast and safety-focused constitutional rules.
- Alternatives considered:
  - Best-effort partial mutation: rejected due to unpredictable outcomes.
  - Silent ignore of unloaded branches: rejected because it hides semantic gaps.

## Decision 6: Design a richer domain model for contract coverage

- Decision: Introduce a v2 test model with:
  - Pure many-to-many relationship
  - Many-to-many relationship via payload association entity
  - Required one-to-one dependent reference
  - Optional one-to-one reference supporting `null` and `detach` outcomes
- Rationale:
  - Directly maps to supported relationship contracts in the feature spec.
  - Improves test readability and scenario isolation.
- Alternatives considered:
  - Reuse old fake model unchanged: rejected due to legacy shape limitations and
    reduced semantic clarity for v2 contracts.

## Decision 7: Detached graph as input model

- Decision: The caller provides a separate detached object graph representing the
  desired post-mutation state; the package diffs it against the tracked original.
- Rationale:
  - Cleanest contract: the package owns the diff logic entirely.
  - Caller does not need to understand change-tracker internals.
  - Aligns naturally with API/DTO patterns where the updated state arrives from
    outside the process boundary.
  - Matches the legacy package's `InsertUpdateOrDeleteGraph(newEntity, existingEntity)`
    two-graph signature pattern.
- Alternatives considered:
  - In-place modification of tracked entities: rejected because it couples caller
    to change-tracker internals and obscures the diff boundary.
  - Explicit delta/command objects: rejected because it adds API complexity without
    proportional benefit for graph-shaped mutations.

## Decision 8: Loaded unsupported relationship types are skipped unless mutated

- Decision: Loaded navigations of currently-unsupported relationship types (e.g.,
  one-to-many) are silently skipped when unchanged; if mutations are detected in
  those navigations, the entire operation is rejected.
- Rationale:
  - Real-world entity graphs almost always contain one-to-many navigations alongside
    M2M and 1:1 patterns.
  - Rejecting all graphs containing unsupported types would make the package
    impractical.
  - Silently ignoring all changes in unsupported types could lead to data loss.
  - Detect-and-reject-if-mutated is the safest middle ground.
- Alternatives considered:
  - Skip entirely (never inspect): rejected because silent mutation loss is unsafe.
  - Reject any graph containing unsupported types: rejected because it makes the
    package unusable with realistic graphs.

## Decision 9: Testing split by contract outcome

- Decision: Organize integration tests by semantic outcome families:
  `update`, `unlink`, `null`, `delete`, `reject`.
- Rationale:
  - Mirrors contract language from constitution/spec.
  - Simplifies reviewer validation and regression diagnosis.
- Alternatives considered:
  - Group tests by entity type only: rejected because contract outcome coverage
    becomes harder to audit.
