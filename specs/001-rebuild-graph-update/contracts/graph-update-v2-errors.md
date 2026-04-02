# Graph Update v2 Error Contract

## Purpose

Define rejection categories for unsupported or unsafe mutation requests.

## Error Categories

### `UnsupportedRelationshipPattern`

- Trigger: Requested mutation uses a relationship pattern not in v2 contract.
- Status: **Reserved** — not currently thrown. Unsupported navigations with
  mutations throw `UnsupportedNavigationMutatedException` instead. This
  exception exists for future patterns that fall outside even the
  "unsupported but skippable" category.
- Required error content:
  - Relationship path
  - Unsupported pattern identifier
  - Guidance to contract documentation

### `UnloadedNavigationMutation`

- Trigger: Requested mutation depends on navigation branch not explicitly loaded.
- Required error content:
  - Aggregate path requiring load
  - Missing navigation identifier
  - Statement that mutation was rejected without partial apply

### `AmbiguousOwnershipSemantics`

- Trigger: Required vs optional ownership semantics cannot be resolved for a
  one-to-one mutation path.
- Status: **Reserved** — not currently thrown. EF Core's `IForeignKey.IsRequired`
  always resolves deterministically based on FK nullability. This exception
  exists as a safety net for edge cases where metadata is genuinely ambiguous
  (e.g., custom model conventions or future EF Core changes).
- Required error content:
  - Relationship path
  - Missing semantic detail
  - Expected contract requirement

### `PartialMutationNotAllowed`

- Trigger: Graph contains both supported and unsupported requested mutations.
- Required error content:
  - First unsupported branch detected
  - Statement that entire operation was rejected

### `UnsupportedNavigationMutated`

- Trigger: Loaded navigation of a currently-unsupported relationship type (e.g.,
  one-to-many) has mutations detected during graph diff.
- Required error content:
  - Navigation path and relationship type
  - Statement that the relationship type is not in the v2 supported contract
  - Statement that the entire operation was rejected (unchanged unsupported
    navigations would have been silently skipped)
- Note: Distinct from `UnsupportedRelationshipPattern` which covers attempts to
  use a relationship pattern that the package explicitly rejects. This category
  covers a relationship type that is simply outside the current rebuild scope.

## Error Behavior Rules

- Errors must be specific and actionable; no generic "invalid graph" message.
- Errors must preserve all-or-nothing behavior.
- Error handling must be covered by integration contract tests.

## Implementation Details

### Validation Ordering

The orchestrator validates in two passes before any mutation:

1. **Loaded navigation validation**: Iterates all `IsLoaded` navigations.
   Unsupported navigations (e.g., one-to-many) are checked for mutations via
   key-based comparison. Mutations trigger `UnsupportedNavigationMutatedException`.
   Unchanged unsupported navigations are silently skipped.

2. **Unloaded navigation validation**: Iterates all `!IsLoaded` navigations.
   If the updated entity provides a non-null reference or non-empty collection
   for an unloaded navigation, this is treated as an attempted mutation on
   something the caller didn't load — triggers `UnloadedNavigationMutationException`.

### All-or-Nothing Guarantee

`OperationGuard` collects all validation errors before any changes. If one error
exists, it throws directly. If multiple errors exist, it wraps them in
`PartialMutationNotAllowedException`. No scalar or navigation mutations are
applied to the change tracker until validation passes.

### Exception Hierarchy

```
InvalidOperationException
  └── GraphUpdateException (abstract base, RelationshipPath)
        ├── UnsupportedRelationshipPatternException (PatternIdentifier)
        ├── UnloadedNavigationMutationException (NavigationName)
        ├── AmbiguousOwnershipSemanticsException (MissingDetail)
        ├── PartialMutationNotAllowedException (UnsupportedBranch)
        └── UnsupportedNavigationMutatedException (RelationshipType)
```
