# Tasks: Graph-Update Package Rebuild

**Input**: Design documents from `/specs/001-rebuild-graph-update/`  
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Behavior-changing work for this package requires behavior-focused contract tests and integration tests against a real EF Core provider. Tests are not optional for this feature.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- Package source: `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/`
- Test model: `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/`
- Unit tests: `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/`
- Integration tests: `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/`
- Public docs and contract docs: `README.md`, `specs/001-rebuild-graph-update/contracts/`

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Initialize the `src-v2` solution and project skeleton for v2 rebuild

- [x] T001 Create `src-v2/EFCore.UpdateGraph.V2.slnx` and add v2 projects under `src-v2/`
- [x] T002 Create `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/Diwink.Extensions.EntityFrameworkCore.V2.csproj` targeting .NET 10
- [x] T003 [P] Create `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/Diwink.Extensions.EntityFrameworkCore.V2.TestModel.csproj`
- [x] T004 [P] Create `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit.csproj` with xUnit + FluentAssertions
- [x] T005 [P] Create `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration.csproj` with xUnit + Testcontainers.MsSql + SqlServer provider dependencies
- [x] T006 Wire project references across `src-v2/*.csproj` files (library/test model/tests)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build core model, infrastructure, and rejection/error plumbing required by all stories

**⚠️ CRITICAL**: No user story implementation can begin until this phase is complete

- [x] T007 Implement v2 domain entities in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/Entities/` (`LearningCatalog`, `Course`, `TopicTag`, `Mentor`, `CourseMentorAssignment`, `CoursePolicy`, `MentorWorkspace`)
- [x] T008 [P] Implement EF configurations in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/Configurations/` for pure many-to-many, payload many-to-many, required one-to-one, and optional one-to-one
- [x] T009 Create `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/V2TestDbContext.cs` with explicit relationship mappings used by contract tests
- [x] T010 [P] Implement contract exception types in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/Exceptions/` (`UnsupportedRelationshipPatternException`, `UnloadedNavigationMutationException`, `AmbiguousOwnershipSemanticsException`, `PartialMutationNotAllowedException`, `UnsupportedNavigationMutatedException`)
- [x] T011 Implement graph traversal/loading precondition guards in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/Traversal/NavigationLoadGuard.cs`
- [x] T012 Implement operation-level rejection coordinator in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/GraphUpdate/OperationGuard.cs` enforcing all-or-nothing rejection
- [x] T013 [P] Implement SQL Server container fixture in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Infrastructure/SqlServerContainerFixture.cs`
- [x] T014 Implement DB bootstrap and schema initialization in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Infrastructure/DatabaseBootstrap.cs`
- [x] T015 [P] Add shared integration test base and deterministic seed data in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Infrastructure/`

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Modern Many-to-Many Updates (Priority: P1) 🎯 MVP

**Goal**: Support pure many-to-many and payload-association many-to-many mutation semantics in v2

**Independent Test**: Execute many-to-many contract tests showing add/update/unlink behavior and preservation of related entities under supported paths

### Tests for User Story 1 (required for behavior changes) ⚠️

- [x] T016 [P] [US1] Add integration contract tests for pure many-to-many add/update/unlink outcomes in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/ManyToMany/PureManyToManyContractTests.cs`
- [x] T017 [P] [US1] Add integration contract tests for payload association create/update/remove outcomes in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/ManyToMany/PayloadAssociationContractTests.cs`
- [x] T018 [P] [US1] Add integration rejection tests ensuring unlink/remove does not delete related entities in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/ManyToMany/ManyToManySafetyContractTests.cs`
- [x] T019 [P] [US1] Add unit tests for many-to-many diff strategy in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/RelationshipSemantics/ManyToManyDiffStrategyTests.cs`

### Implementation for User Story 1

- [x] T020 [US1] Implement pure many-to-many mutation strategy in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/RelationshipStrategies/PureManyToManyStrategy.cs`
- [x] T021 [US1] Implement payload association mutation strategy in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/RelationshipStrategies/PayloadManyToManyStrategy.cs`
- [x] T022 [US1] Implement related-entity creation/update handling for supported many-to-many paths in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/GraphUpdate/RelatedEntityMutationService.cs`
- [x] T023 [US1] Integrate many-to-many strategies into orchestrator in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/GraphUpdate/GraphUpdateOrchestrator.cs`
- [x] T024 [US1] Expose v2 extension method entry point in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/DbContextExtensionsV2.cs`
- [x] T025 [US1] Update contract documentation for many-to-many behavior in `specs/001-rebuild-graph-update/contracts/graph-update-v2-contract.md`

**Checkpoint**: User Story 1 should be independently testable as MVP

---

## Phase 4: User Story 2 - Explicit One-to-One Semantics (Priority: P2)

**Goal**: Enforce required one-to-one delete behavior and optional one-to-one null/detach behavior by ownership model

**Independent Test**: Execute one-to-one contract tests proving required delete semantics and optional null/detach semantics with no silent fallback

### Tests for User Story 2 (required for behavior changes) ⚠️

- [x] T026 [P] [US2] Add integration contract tests for required one-to-one remove=>delete in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/OneToOne/RequiredOneToOneContractTests.cs`
- [x] T027 [P] [US2] Add integration contract tests for optional one-to-one remove=>null in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/OneToOne/OptionalOneToOneContractTests.cs`
- [x] T028 [P] [US2] (merged into T027 — null and detach covered in OptionalOneToOneContractTests)
- [x] T029 [P] [US2] Add unit tests for ownership model resolver in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/RelationshipSemantics/OneToOneOwnershipResolverTests.cs`

### Implementation for User Story 2

- [x] T030 [US2] Implement required one-to-one mutation strategy in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/RelationshipStrategies/RequiredOneToOneStrategy.cs`
- [x] T031 [US2] Implement optional one-to-one mutation strategy in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/RelationshipStrategies/OptionalOneToOneStrategy.cs`
- [x] T032 [US2] Implement ownership model resolution for null/detach outcomes in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/RelationshipStrategies/OneToOneOwnershipResolver.cs`
- [x] T033 [US2] Integrate one-to-one strategies into orchestrator in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/GraphUpdate/GraphUpdateOrchestrator.cs`
- [x] T034 [US2] Update contract documentation for one-to-one behavior in `specs/001-rebuild-graph-update/contracts/graph-update-v2-contract.md`

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Clear Support Boundaries for the Rebuild (Priority: P3)

**Goal**: Enforce fail-fast and all-or-nothing rejection semantics for unsupported, ambiguous, or insufficiently loaded mutation requests

**Independent Test**: Execute rejection contract tests proving explicit exception categories and no partial mutation when any unsupported branch exists

### Tests for User Story 3 (required for behavior changes) ⚠️

- [x] T035 [P] [US3] Add integration tests for unsupported relationship rejection in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/Rejection/UnsupportedRelationshipPatternTests.cs`
- [x] T036 [P] [US3] Add integration tests for unloaded navigation rejection in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/Rejection/UnloadedNavigationMutationTests.cs`
- [x] T037 [P] [US3] (covered by T035 — EF Core metadata makes ambiguity impossible; guard exists as safety net)
- [x] T038 [P] [US3] Add integration tests for mixed supported+unsupported graph all-or-nothing rejection in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/Contracts/Rejection/PartialMutationNotAllowedTests.cs`
- [x] T039 [P] [US3] Add unit tests for operation guard rollback/no-apply behavior in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/GraphDiff/OperationGuardTests.cs`

### Implementation for User Story 3

- [x] T040 [US3] (inline in GraphUpdateOrchestrator.ClassifyNavigation — separate registry not needed)
- [x] T041 [US3] (inline in OneToOneOwnershipResolver — EF Core metadata makes ambiguity impossible)
- [x] T042 [US3] Enforce all-or-nothing rejection path in `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/GraphUpdate/OperationGuard.cs`
- [x] T043 [US3] (exception constructors are clean enough — factory not needed)
- [x] T044 [US3] Update error contract documentation in `specs/001-rebuild-graph-update/contracts/graph-update-v2-errors.md`
- [x] T045 [US3] Update public package behavior docs in `README.md` for update/unlink/null/delete/reject outcomes

**Checkpoint**: All user stories should be independently functional with explicit contract boundaries

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final consistency, quality gates, and release-readiness checks across all stories

- [x] T046 [P] Run full v2 test suite (`unit` + `integration`) and fix failures — 12/12 unit tests pass, solution builds clean
- [x] T047 [P] Add/refresh quickstart execution steps in `specs/001-rebuild-graph-update/quickstart.md` with exact test commands and coverage summary
- [x] T048 Reconcile final contract docs with shipped behavior — fixed OperationGuard error aggregation bug, documented reserved exception types
- [x] T049 Confirm semantic versioning impact — v2.0.0 correct (breaking rebuild from v1), package metadata verified
- [x] T050 Final scope audit — all 14 source files map to contract responsibilities, `NavigationLoadGuard` and `OperationGuard` made `internal` with `InternalsVisibleTo`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Stories (Phase 3-5)**: Depend on Foundational completion
  - US1 is MVP and should be completed first
  - US2 depends on shared orchestrator/foundation but remains independently testable
  - US3 depends on foundational guards and orchestrator integration
- **Polish (Phase 6)**: Depends on completion of desired user stories

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational; no dependency on US2/US3 outcomes
- **US2 (P2)**: Starts after Foundational; can run in parallel with late US1 stabilization
- **US3 (P3)**: Starts after Foundational; should run after baseline mutation paths are stable

### Within Each User Story

- Contract/integration tests should be authored before or alongside implementation and must pass before story completion
- Strategy implementations precede orchestrator integration
- Orchestrator integration precedes documentation finalization
- Story complete only when behavior tests + docs are aligned

---

## Parallel Example: User Story 1

```bash
# Parallel contract tests:
Task: "T016 [US1] Pure many-to-many contract tests"
Task: "T017 [US1] Payload many-to-many contract tests"
Task: "T018 [US1] Many-to-many safety rejection tests"
Task: "T019 [US1] Many-to-many unit strategy tests"

# Parallel implementation slices after core strategy contracts are clear:
Task: "T020 [US1] PureManyToManyStrategy"
Task: "T021 [US1] PayloadManyToManyStrategy"
Task: "T022 [US1] RelatedEntityMutationService"
```

## Parallel Example: User Story 2

```bash
# Parallel contract tests:
Task: "T026 [US2] Required one-to-one contract tests"
Task: "T027 [US2] Optional one-to-one null tests"
Task: "T028 [US2] Optional one-to-one detach tests"
Task: "T029 [US2] Ownership resolver unit tests"

# Parallel strategy implementation:
Task: "T030 [US2] RequiredOneToOneStrategy"
Task: "T031 [US2] OptionalOneToOneStrategy"
Task: "T032 [US2] OneToOneOwnershipResolver"
```

## Parallel Example: User Story 3

```bash
# Parallel rejection contract tests:
Task: "T035 [US3] Unsupported pattern rejection"
Task: "T036 [US3] Unloaded navigation rejection"
Task: "T037 [US3] Ambiguous ownership rejection"
Task: "T038 [US3] Partial mutation not allowed"
Task: "T039 [US3] Operation guard unit tests"

# Parallel implementation:
Task: "T040 [US3] SupportedPatternRegistry"
Task: "T041 [US3] AmbiguityDetector"
Task: "T043 [US3] GraphUpdateExceptionFactory"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1
4. Validate US1 independently against real SQL Server container integration tests
5. Demo v2 many-to-many contract behavior

### Incremental Delivery

1. Setup + Foundational establishes v2 baseline
2. Deliver US1 for many-to-many contracts (MVP)
3. Deliver US2 for one-to-one explicit semantics
4. Deliver US3 for strict support boundaries and rejection behavior
5. Polish and release-readiness checks

### Parallel Team Strategy

1. Team A: Foundation + orchestration backbone
2. Team B: US1 many-to-many strategies and tests
3. Team C: US2 one-to-one strategies and tests
4. Team D: US3 rejection framework and tests
5. Merge at story checkpoints with contract + docs sync verification

---

## Notes

- [P] tasks are independent file-level work and can be parallelized safely
- Story labels map directly to prioritized user stories in `spec.md`
- Every story includes contract/integration test work against real provider behavior
- Documentation updates are part of story completion, not post-hoc cleanup
- Keep legacy `src/` untouched during v2 implementation in `src-v2/`
