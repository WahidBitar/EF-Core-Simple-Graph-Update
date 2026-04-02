<!--
Sync Impact Report
Version change: template -> 1.0.0
Modified principles:
- Template Principle Slot 1 -> I. Package Scope and Identity
- Template Principle Slot 2 -> II. Relationship-Aware Mutation Contract
- Template Principle Slot 3 -> III. Explicit Support Boundaries
- Template Principle Slot 4 -> IV. Semantic Verification over Implementation Coverage
- Template Principle Slot 5 -> V. Documentation, Review, and Release Discipline
Added sections:
- Compliance and Release Review
- Constitutional Boundary
Removed sections:
- None
Templates requiring updates:
- ✅ .specify/templates/plan-template.md
- ✅ .specify/templates/spec-template.md
- ✅ .specify/templates/tasks-template.md
Runtime guidance reviewed:
- Reviewed README.md; no principle reference update was required for this constitution ratification.
Follow-up TODOs:
- None
-->
# Diwink.Extensions.EntityFrameworkCore Graph-Update Package Constitution

## Core Principles

### I. Package Scope and Identity
- This package MUST remain focused on EF Core graph-update behavior exposed by this
  package's public API.
- Every proposed change MUST be justified in terms of graph-update semantics,
  correctness, safety, or semantic clarity for this package.
- Contributors MUST NOT expand the package into a general EF Core helper library,
  generic data-access toolkit, repository abstraction, or unrelated utility surface
  unless the constitution is formally amended first.

Rationale: A narrow package identity keeps the contract coherent and prevents
scope creep from weakening graph-update behavior.

### II. Relationship-Aware Mutation Contract
- Supported graph-update behavior MUST be relation-aware, explicit, and published as
  package contract rather than left as incidental implementation behavior.
- For many-to-many removals, the package MUST unlink the relationship and MUST NOT
  delete the related entities merely because the association was removed.
- For one-to-many removals, the package MUST delete removed dependents unless a
  supported and explicitly documented exception applies.
- For one-to-one or reference navigations, removing a required owned or dependent
  reference MUST be treated as delete behavior.
- For one-to-one or reference navigations, removing an optional reference MUST null
  or detach according to the supported ownership model, and the package contract
  MUST state which outcome applies.
- Supported behavior MUST always be described in terms of update, unlink, null,
  delete, or reject outcomes.

Rationale: Graph mutation is only safe when relationship semantics are intentional,
reviewable, and visible to users of the package.

### III. Explicit Support Boundaries
- Every supported graph shape and relationship pattern MUST have an explicit,
  documented behavior contract.
- Unsupported graph shapes, unsupported relationship scenarios, or undefined
  mutation cases MUST fail fast with a clear, specific exception that explains why
  the scenario is rejected.
- The package MUST NOT perform silent, ambiguous, or best-effort mutations for
  unsupported cases.
- A scenario is not supported merely because current implementation happens to
  mutate it without throwing; support exists only when behavior, tests, and
  documentation all define the contract.

Rationale: Rejection is safer than ambiguity in persistence behavior.

### IV. Semantic Verification over Implementation Coverage
- No graph-mutation behavior may ship without integration tests against a real EF
  Core provider.
- No supported relationship type may ship without behavior-focused contract tests
  that verify the expected update, unlink, null, delete, and reject outcomes as
  applicable.
- Verification MUST be expressed at the observable semantic level and MUST NOT rely
  solely on unit-level implementation checks, branch coverage, or internal method
  assertions.
- Tests SHOULD cover both successful mutations and documented rejection paths when
  unsupported scenarios are part of the package contract.

Rationale: The package promise is behavioral, so verification must prove externally
visible outcomes rather than internal mechanics.

### V. Documentation, Review, and Release Discipline
- Public documentation MUST explicitly describe the behavior of each supported
  relationship pattern.
- Documentation MUST make clear whether a scenario is updated, unlinked, nulled,
  deleted, or rejected.
- Documentation drift MUST be treated as a governance failure whenever behavior
  changes.
- Any pull request that adds support for a new relationship shape or changes
  mutation semantics MUST include implementation, behavior-focused tests, and
  public documentation in the same pull request.
- Reviewers MUST reject behavior-changing pull requests that are missing any of the
  required implementation, test, or documentation updates.
- Any semantic breaking change in graph-update behavior MUST be released as a major
  version change, with no bypasses.
- Behavior changes MUST go through the normal pull request path with implementation,
  tests, documentation, and correct semantic versioning.

Rationale: Code, tests, documentation, and versioning together form the package
contract.

## Compliance and Release Review

- Pull requests MUST identify whether they change graph-update semantics, add or
  remove supported relationship patterns, modify unsupported-case handling, or
  leave package contract unchanged.
- Pull request review MUST check compliance against each applicable constitutional
  principle before approval.
- Pull requests that change behavior MUST show, in the same reviewable change set,
  the implementation, semantic test coverage, documentation update, and declared
  semantic-version impact.
- Release readiness MUST be evaluated at the semantic level: reviewers and
  maintainers MUST confirm that supported behaviors are explicitly contracted,
  unsupported behaviors fail fast, and breaking semantic changes are versioned as
  major releases.
- A release MUST NOT proceed on the basis of "known but undocumented" behavior.

## Constitutional Boundary

- This constitution is principle-based governance for the package and MUST NOT be
  used to encode stack targets, EF Core version support, CI choices, provider
  selection, packaging mechanics, release mechanics, or migration tactics.
- Implementation detail that becomes important during governance discussion MUST be
  captured in planning artifacts or a plan parking lot rather than embedded here as
  constitutional text.
- Exceptions are not ad hoc. Contributors and reviewers MUST NOT bypass this
  constitution through informal agreement, release pressure, or undocumented
  one-off waivers.
- Any durable exception or policy change MUST be handled only through formal
  constitution amendment before the changed behavior is approved or released.

## Governance

- This constitution applies only to the graph-update package in this repository and
  supersedes conflicting local habits or preferences for package evolution, review,
  and release.
- Compliance is checked during pull request review. Authors MUST state the affected
  relationship patterns, supported and unsupported outcomes, documentation impact,
  test evidence, and semantic-version impact. Reviewers MUST withhold approval when
  any applicable constitutional requirement is missing or contradicted.
- Amendments MUST be proposed through a pull request that changes the constitution
  text directly and explains the rationale, the governance impact, and any required
  downstream template or documentation updates.
- Amendments MUST be approved by the maintainers responsible for this package's
  governance before merge. Approval MUST be explicit in review, and unresolved
  objections MUST be addressed before ratification.
- Constitution versioning follows semantic versioning for governance changes:
  MAJOR for backward-incompatible removals or redefinitions of principles, MINOR
  for new principles or materially expanded governance, and PATCH for clarifications
  that do not change governance meaning.
- Ratified constitutions and amendments take effect on merge. No ad hoc exception,
  side agreement, or release shortcut overrides this rule.

**Version**: 1.0.0 | **Ratified**: 2026-03-31 | **Last Amended**: 2026-03-31
