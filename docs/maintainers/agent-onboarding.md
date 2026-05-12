# Agent Onboarding

This guide explains how AI agents and human maintainers should use the Drone Designer documentation set when making code or documentation changes. It is focused on the current repository state, not an idealized future process.

## Documentation Structure

The documentation system is divided by audience and by document role:

- [`docs/index.md`](../index.md): top-level entry point that routes readers into the correct area.
- [`docs/user/`](../user/index.md): operator-facing installation, workflows, and troubleshooting.
- [`docs/maintainers/`](index.md): onboarding, glossary, architecture, concepts, and sanctioned maintainer workflows.
- [`docs/reference/`](../reference/index.md): stable facts that other docs should link to instead of repeating.
- [`docs/assets/`](../assets/): screenshots, diagrams, and other documentation support assets.

Use the smallest destination that matches the reader need. Do not put user guidance into maintainer docs, and do not bury stable reference facts inside workflow narratives.

## Writing Style For Agents

Write documentation to reduce the number of repo reads needed for the next maintainer.

- Prefer current behavior over planned behavior.
- Use repository terminology consistently with the glossary.
- Link to adjacent docs instead of repeating the same explanation in multiple places.
- Keep user docs task-oriented and implementation-light.
- Keep maintainer docs change-oriented and explicit about where behavior lives.
- Prefer concise prose over long historical narratives.

When a maintainer doc describes a code area, state the code reality clearly enough that another agent can navigate to the relevant files without rediscovering the structure from scratch.

## When To Update An Existing Doc

Update an existing doc when the change:

- changes behavior already covered by that document
- sharpens or corrects an explanation the document already owns
- adds details that naturally extend the document's current scope

Examples:

- A selection-engine rule changes: update the relevant concept, workflow, or reference doc that already explains it.
- A release step changes: update the release workflow doc instead of creating a new note elsewhere.

## When To Create A New Doc

Create a new document only when:

- the topic does not have a clear destination in the current structure
- the content serves a distinct reader need that would overload an existing page
- the documentation plan already names a dedicated destination for that topic

If the structure does not clearly support a new topic, prefer updating the nearest existing document and leave a short note for later restructuring instead of creating ad hoc one-off docs.

## Phase-Level Update Policy

Documentation updates happen at meaningful phase boundaries, not on every small edit.

- During active implementation, capture code and decision changes in the codebase first.
- When a planned phase or vertical slice completes, update the affected docs so they match the new reality.
- Avoid creating churn by rewriting docs repeatedly while an area is still moving.
- If a change creates immediate user or maintainer risk, update the relevant documentation right away even if the wider phase is still in progress.

This policy keeps the docs coherent while still allowing urgent corrections when the current docs would otherwise become misleading.

## Required Related File Sections

Maintainer-oriented documents should include these sections when they describe a subsystem or workflow in enough detail that file navigation matters:

## Primary Related Files

List the core files a maintainer will almost certainly need to read or edit first.

## Secondary Related Files

List nearby supporting files that often matter during the same kind of change, but are not always the first stop.

Keep these file lists repo-relative and selective. The purpose is to shorten navigation time, not to inventory every file in the area.

## Working Rules

Before making a documentation change:

1. Identify the intended reader and their task.
2. Check whether the destination already exists in `user`, `maintainers`, or `reference`.
3. Update the existing doc if the scope already matches.
4. Create a new doc only when the current structure cannot carry the topic cleanly.
5. Add or refresh `Primary Related Files` and `Secondary Related Files` in maintainer docs when navigation support is part of the doc's job.

## Current Constraint

The repository still contains older documentation outside the new tree. Treat those files as source material during migration, not as the long-term home for new documentation work.

## Legacy Doc Transition Map

Phase 6 treats the new tree under `docs/user/`, `docs/maintainers/`, `docs/reference/`, and `docs/assets/` as the long-term system of record.

The remaining out-of-tree docs have these dispositions:

| Legacy path | Decision | New system-of-record destination |
|---|---|---|
| `docs/ONBOARDING.md` | delete after migration | this page plus `docs/maintainers/architecture-overview.md` |
| `docs/SCHEMA.md` | delete after migration | `docs/reference/component-database-and-schema.md` |
| `docs/PLAN.md` | delete after migration | `docs/maintainers/concepts/selection-engine-pipeline-and-rationale.md` |
| `docs/solidworks-macro-pipeline.md` | delete after migration | `docs/maintainers/workflows/changing-cad-generation.md` |
| `docs/calibrated-survey-quad-selection-trace.md` | delete after migration | `docs/maintainers/concepts/selection-engine-pipeline-and-rationale.md` |
| `docs/codebase_audit_2026-04-26.md` | delete after migration | `docs/maintainers/architecture-overview.md` and `docs/maintainers/known-limitations-and-risks.md` |
| `docs/parts-parameter-reference.md` | delete after migration | `docs/maintainers/workflows/changing-cad-generation.md` |
| `docs/mechanical-team-briefing.md` | delete after migration | `docs/maintainers/workflows/changing-cad-generation.md` |
| `docs/mechanical-team-briefing.pdf` | delete after migration | none; superseded by maintainer workflow docs |
| `docs/okay-now-what-i-drifting-dahl.md` | delete | none; not part of the maintained documentation system |

Keep these as active docs:

- `docs/DOCUMENTATION_PLAN.md`
- `docs/index.md`
- the new `docs/user/`, `docs/maintainers/`, `docs/reference/`, and `docs/assets/` trees
