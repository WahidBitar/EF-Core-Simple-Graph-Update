# Quickstart: Graph-Update v2 Planning Validation

## Goal

Validate the planned v2 workflow for `src-v2` with real-provider integration
tests running in SQL Server containers.

## Prerequisites

- .NET 10 SDK installed
- Docker engine running
- Repository checked out on branch `001-rebuild-graph-update`

## Proposed v2 Project Layout

- `src-v2/Diwink.Extensions.EntityFrameworkCore.V2/`
- `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.TestModel/`
- `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit/`
- `src-v2/Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration/`

## Exact Test Commands

```bash
# From repository root
cd src-v2

# 1. Restore all projects
dotnet restore EFCore.UpdateGraph.V2.slnx

# 2. Build entire solution
dotnet build EFCore.UpdateGraph.V2.slnx

# 3. Run unit tests (no Docker required)
dotnet test Diwink.Extensions.EntityFrameworkCore.V2.Tests.Unit \
  --no-restore -v n

# 4. Run integration tests (requires Docker)
dotnet test Diwink.Extensions.EntityFrameworkCore.V2.Tests.Integration \
  --no-restore -v n

# 5. Run specific test categories
dotnet test --filter "ManyToManyDiffStrategyTests"       # M2M unit tests
dotnet test --filter "OneToOneOwnershipResolverTests"     # 1:1 unit tests
dotnet test --filter "OperationGuardTests"                # rejection unit tests
dotnet test --filter "PureManyToManyContractTests"        # M2M integration
dotnet test --filter "RequiredOneToOneContractTests"      # 1:1 required integration
dotnet test --filter "OptionalOneToOneContractTests"      # 1:1 optional integration
dotnet test --filter "UnsupportedRelationshipPatternTests" # rejection integration
dotnet test --filter "UnloadedNavigationMutationTests"    # unloaded nav rejection
dotnet test --filter "PartialMutationNotAllowedTests"     # all-or-nothing rejection
```

## Validation Flow

1. Restore dependencies for v2 projects.
2. Execute unit tests for graph diff/traversal logic (12 tests).
3. Execute integration tests that start SQL Server via Testcontainers.
4. Apply test schema bootstrap/migrations inside containerized database.
5. Run behavior-focused contract suites by outcome category:
   - `update` — scalar property updates, add new entities
   - `unlink` — pure M2M remove association, payload M2M remove
   - `null` — optional 1:1 FK nulling, replace dependent
   - `delete` — required 1:1 dependent deletion, payload association removal
   - `reject` — unsupported mutations, unloaded navigations, mixed graphs
6. Confirm rejection tests enforce all-or-nothing semantics.
7. Confirm documentation artifacts match observed contract outcomes.

## Test Coverage Summary

| Suite | Tests | Provider | Category |
|-------|-------|----------|----------|
| ManyToManyDiffStrategyTests | 3 | InMemory | M2M add/remove/payload |
| OneToOneOwnershipResolverTests | 3 | InMemory | 1:1 required delete, optional null, update |
| OperationGuardTests | 6 | Pure unit | Guard behavior, error collection |
| PureManyToManyContractTests | 4 | SQL Server | Skip nav add/remove/update/new entity |
| PayloadAssociationContractTests | 3 | SQL Server | Payload add/update/remove |
| ManyToManySafetyContractTests | 2 | SQL Server | Remove-all preserves entities |
| RequiredOneToOneContractTests | 3 | SQL Server | Required 1:1 delete/update/add |
| OptionalOneToOneContractTests | 4 | SQL Server | Optional 1:1 null/update/add/replace |
| UnsupportedRelationshipPatternTests | 3 | SQL Server | 1:M skip/reject add/reject remove |
| UnloadedNavigationMutationTests | 3 | SQL Server | Unloaded reject/skip |
| PartialMutationNotAllowedTests | 1 | SQL Server | Mixed graph all-or-nothing |

## Expected Integration Assertions

- Caller provides a detached graph; package diffs against tracked original.
- Pure many-to-many removals unlink only; related rows remain.
- Payload association entity updates mutate payload without deleting related
  entities unintentionally.
- Required one-to-one removal deletes dependent.
- Optional one-to-one removal follows documented `null` or `detach` path.
- Any unsupported branch rejects the full mutation operation.
- Missing/partial navigation load requirements trigger explicit rejection.
- Loaded one-to-many navigation with no changes is silently skipped.
- Loaded one-to-many navigation with mutations triggers full rejection.

## Troubleshooting Signals

- Container startup failures indicate local Docker/runtime prerequisites issue.
- Provider mismatch failures indicate incorrect test bootstrap configuration.
- Behavioral mismatch indicates contract/implementation drift and must block
  release until reconciled in code, tests, and docs.
