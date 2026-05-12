# Drone Designer Documentation Plan

## Purpose

This plan defines the MVP documentation program for Drone Designer.

It is a documentation plan, not a product roadmap.
It describes:

- the target documentation structure
- the phases to build it
- the issues inside each phase
- issue dependencies
- the recommended execution order

The resulting documentation set is intended to support:

- users operating the released application from a ZIP-based binary package
- AI agents maintaining and extending the codebase, then reporting back to humans

---

## Agreed Documentation Principles

- Root `README.md` stays short and outward-facing.
- The real documentation home lives at `docs/index.md`.
- Documentation is split into `user`, `maintainers`, `reference`, and `assets`.
- User docs are workflow-oriented and screenshot-driven.
- Maintainer docs are agent-first and optimized for code changes.
- SolidWorks setup is not part of user installation documentation.
- User installation assumes a ZIP-based binary release for MVP.
- Maintainer documentation documents current reality, not future plans.
- Existing docs are raw material to absorb or delete, not the final structure.
- Documentation updates happen at phase completion, not for every small issue.
- No archive area will be maintained for old documentation.
- Maintainer docs should include `Primary Related Files` and `Secondary Related Files`.

---

## Target Documentation Tree

```text
docs/
  index.md

  user/
    index.md
    installation-and-prerequisites.md
    workflow-design-a-uav-from-requirements.md
    workflow-export-and-generate-outputs.md
    troubleshooting-and-faq.md

  maintainers/
    index.md
    agent-onboarding.md
    glossary.md
    architecture-overview.md
    known-limitations-and-risks.md

    concepts/
      selection-engine-overview.md
      selection-engine-pipeline-and-rationale.md

    workflows/
      changing-the-selection-engine.md
      changing-ui-inputs-and-outputs.md
      changing-the-component-database-and-schema.md
      changing-export-and-report-generation.md
      changing-cad-generation.md
      adding-or-updating-tests.md
      preparing-a-release-package.md

  reference/
    index.md
    component-database-and-schema.md
    configuration-and-runtime-dependencies.md
    testing-and-validation.md

  assets/
    user/
      installation/
      workflow-design-a-uav/
      workflow-export-and-generate-outputs/
      troubleshooting/
    maintainers/
      diagrams/
```

---

## Phase Overview

```text
Phase 1 -> Phase 2 -> Phase 3 -> Phase 4 -> Phase 5 -> Phase 6

Phase 1 creates the documentation system.
Phases 2, 3, and 4 depend on Phase 1.
Phase 5 depends on Phases 3 and 4.
Phase 6 depends on all prior phases.
```

---

## Phase 1 - Establish The Documentation System

**Goal:** create the structure, rules, and destinations before writing the real content.

### Issues

**DOC-001 - Create the new documentation tree**

- Create the directory structure under `docs/`.
- Create placeholder markdown files for the agreed target docs.
- Create `docs/assets/` subfolders for user screenshots and maintainer diagrams.

Dependencies: none

**DOC-002 - Write `docs/index.md`**

- Explain what Drone Designer does in a few sentences.
- Route readers into `user` vs `maintainers`.
- Provide a short documentation map.

Dependencies: `DOC-001`

**DOC-003 - Write `docs/user/index.md`**

- Define the user docs entry point.
- Link to installation, workflows, and troubleshooting.

Dependencies: `DOC-001`

**DOC-004 - Write `docs/maintainers/index.md`**

- Define the maintainer docs entry point.
- Link to onboarding, glossary, concepts, workflows, and reference docs.

Dependencies: `DOC-001`

**DOC-005 - Write `docs/maintainers/agent-onboarding.md`**

- Explain the docs structure.
- Define the documentation writing style for agents.
- Define the phase-level documentation update policy.
- Define when to update an existing doc versus create a new one.
- Define the requirement to use `Primary Related Files` and `Secondary Related Files`.

Dependencies: `DOC-004`

**DOC-006 - Write `docs/maintainers/glossary.md`**

- Define canonical terms used across the repo and docs.
- Stabilize terminology around engine, pipeline, design recommendation, component database, scenario, CAD generation, and mission inputs.

Dependencies: `DOC-004`

### Recommended execution order

1. `DOC-001`
2. `DOC-002`
3. `DOC-003`
4. `DOC-004`
5. `DOC-005`
6. `DOC-006`

### Exit criteria

- The full directory structure exists.
- The docs home and both audience entry pages exist.
- Maintainer onboarding and glossary exist.
- The documentation system is ready to receive migrated content.

---

## Phase 2 - User Documentation

**Goal:** produce an MVP user documentation set for real operators of the released app.

### Issues

**DOC-101 - Write `installation-and-prerequisites.md`**

- Document ZIP-based release usage.
- Document prerequisites.
- Mention SolidWorks only as an optional prerequisite for CAD generation.
- Keep configuration editing out of the main path.
- Assume the app should work out of the box.

Dependencies: `DOC-003`, `DOC-005`

**DOC-102 - Write `workflow-design-a-uav-from-requirements.md`**

- Document the end-to-end design workflow.
- Cover entering mission requirements, running the design, reading outputs, and iterating on inputs.
- Keep it use-case based, not screen-by-screen.

Dependencies: `DOC-003`, `DOC-005`, `DOC-006`

**DOC-103 - Write `workflow-export-and-generate-outputs.md`**

- Document export actions.
- Document CAD generation as the advanced final step.
- Keep the focus on the user workflow, not SolidWorks internals.

Dependencies: `DOC-003`, `DOC-005`, `DOC-006`

**DOC-104 - Write `troubleshooting-and-faq.md`**

- Create a dedicated troubleshooting and FAQ document.
- Seed it with currently known issues and known operator-facing failure modes.
- Leave room for future real-user questions.

Dependencies: `DOC-003`, `DOC-005`

**DOC-105 - Capture and organize user screenshots**

- Capture full-window screenshots.
- Add red-box callouts where attention needs to be directed.
- Store screenshots in the agreed `docs/assets/user/...` folders.

Dependencies: `DOC-101`, `DOC-102`, `DOC-103`, `DOC-104`

**DOC-106 - Review the user doc set for workflow coherence**

- Check that installation leads cleanly into use-case docs.
- Check that workflows are readable without maintainer knowledge.
- Check that screenshot references are consistent.

Dependencies: `DOC-101`, `DOC-102`, `DOC-103`, `DOC-104`, `DOC-105`

### Recommended execution order

1. `DOC-101`
2. `DOC-102`
3. `DOC-103`
4. `DOC-104`
5. `DOC-105`
6. `DOC-106`

### Exit criteria

- User installation is documented.
- Both major user workflows are documented.
- Troubleshooting and FAQ exists.
- Screenshots are stored and referenced correctly.

---

## Phase 3 - Maintainer Core Concepts

**Goal:** document the app as an agent-operable engineering system.

### Issues

**DOC-201 - Write `architecture-overview.md`**

- Explain the major subsystems and their boundaries.
- Show how UI, engine, repository, exports, configuration, and CAD generation fit together.

Dependencies: `DOC-004`, `DOC-005`, `DOC-006`

**DOC-202 - Write `known-limitations-and-risks.md`**

- Document maintainers-only limitations and sharp edges.
- Cover heuristics, data quality limits, SolidWorks fragility, obsolete paths still present, and weak validation areas.

Dependencies: `DOC-004`, `DOC-005`, `DOC-006`

**DOC-203 - Write `concepts/selection-engine-overview.md`**

- Explain what the selection engine owns.
- Explain the boundaries between engine, UI, repository, and outputs.

Dependencies: `DOC-201`

**DOC-204 - Write `concepts/selection-engine-pipeline-and-rationale.md`**

- Explain the actual pipeline in detail.
- Document key heuristics and constraints.
- Include inline references to the articles and research papers that informed the implementation.
- Explain where implementation behavior intentionally diverges from literature.

Dependencies: `DOC-203`, `DOC-006`

**DOC-205 - Extract useful content from current deep-dive docs**

- Reuse useful material from existing audit, onboarding, trace, schema, and SolidWorks notes.
- Migrate content into the new conceptual destinations.
- Do not preserve old structure for its own sake.

Dependencies: `DOC-201`, `DOC-202`, `DOC-203`, `DOC-204`

### Recommended execution order

1. `DOC-201`
2. `DOC-202`
3. `DOC-203`
4. `DOC-204`
5. `DOC-205`

### Exit criteria

- Architecture is documented.
- The selection engine has dedicated conceptual docs.
- Limitations are captured for maintainers.
- Existing deep-dive content has destinations in the new structure.

---

## Phase 4 - Reference Documentation

**Goal:** centralize stable facts and repeatable validation references.

### Issues

**DOC-301 - Write `reference/component-database-and-schema.md`**

- Document the component database structure.
- Document the schema and stable reference facts agents need.
- Replace the old monolithic schema reference over time.

Dependencies: `DOC-004`, `DOC-005`, `DOC-006`

**DOC-302 - Write `reference/configuration-and-runtime-dependencies.md`**

- Document config behavior.
- Document default paths and required runtime assets.
- Document optional dependencies like SolidWorks.
- Distinguish source checkout assumptions from release package assumptions.

Dependencies: `DOC-004`, `DOC-005`

**DOC-303 - Write `reference/testing-and-validation.md`**

- Define the central manual validation checklist.
- Include build, launch, scenario/test harness, export, and CAD checks when relevant.
- Keep it as one central checklist for the app.

Dependencies: `DOC-004`, `DOC-005`

**DOC-304 - Normalize terminology and file references across reference docs**

- Align all reference docs with the glossary.
- Add repo-relative links and exact related file references where needed.

Dependencies: `DOC-301`, `DOC-302`, `DOC-303`, `DOC-006`

### Recommended execution order

1. `DOC-301`
2. `DOC-302`
3. `DOC-303`
4. `DOC-304`

### Exit criteria

- Schema/reference facts are centralized.
- Runtime dependencies and configuration are documented.
- A central validation checklist exists.

---

## Phase 5 - Maintainer Workflow Documentation

**Goal:** document the sanctioned change paths for agents making code changes.

### Issues

**DOC-401 - Write `changing-the-selection-engine.md`**

- Explain what to read first.
- Explain which docs and files usually change together.
- Focus on the engine maintenance workflow.

Dependencies: `DOC-203`, `DOC-204`, `DOC-303`

**DOC-402 - Write `changing-ui-inputs-and-outputs.md`**

- Document the common path for UI-facing changes.
- Explain interactions with `MissionSpecs`, UI forms, and result presentation.

Dependencies: `DOC-201`, `DOC-303`

**DOC-403 - Write `changing-the-component-database-and-schema.md`**

- Document how to change the component database safely.
- Explain schema-impact awareness and validation expectations.

Dependencies: `DOC-301`, `DOC-303`

**DOC-404 - Write `changing-export-and-report-generation.md`**

- Document the export/report maintenance path.
- Explain what code and docs usually matter for export changes.

Dependencies: `DOC-201`, `DOC-303`

**DOC-405 - Write `changing-cad-generation.md`**

- Document the CAD generation maintenance path.
- Reuse useful content from the SolidWorks macro pipeline notes.
- Keep the audience as maintainers, not end users.

Dependencies: `DOC-201`, `DOC-202`, `DOC-302`, `DOC-303`

**DOC-406 - Write `adding-or-updating-tests.md`**

- Document how to add, update, or run relevant validation scenarios and harness checks.

Dependencies: `DOC-303`

**DOC-407 - Write `preparing-a-release-package.md`**

- Document how to assemble the ZIP-based MVP release artifact.
- Keep it as a separate workflow from general testing and validation.

Dependencies: `DOC-302`, `DOC-303`

### Recommended execution order

1. `DOC-401`
2. `DOC-402`
3. `DOC-403`
4. `DOC-404`
5. `DOC-405`
6. `DOC-406`
7. `DOC-407`

### Exit criteria

- Common change paths are documented.
- Release packaging is documented separately.
- Agents have practical workflow guidance for the highest-value maintenance tasks.

---

## Phase 6 - Migration And Cleanup

**Goal:** finish the transition from the old docs set to the new one.

### Issues

**DOC-501 - Map existing docs to keep/rewrite/delete**

- Evaluate each current doc.
- Decide whether it should be absorbed, rewritten into a new destination, or deleted.

Dependencies: `DOC-106`, `DOC-205`, `DOC-304`, `DOC-407`

**DOC-502 - Migrate surviving content**

- Move useful content into the new structure.
- Rewrite where necessary so the new docs read as a coherent set.

Dependencies: `DOC-501`

**DOC-503 - Delete outdated and redundant docs**

- Remove outdated docs once their useful content has been absorbed.
- Do not create an archive.

Dependencies: `DOC-502`

**DOC-504 - Repair stale links and path references**

- Check markdown links.
- Check doc-to-file references.
- Check screenshot asset paths.

Dependencies: `DOC-503`

**DOC-505 - Final consistency pass**

- Check structure, tone, terminology, and doc cross-links.
- Check that important maintainer docs include `Primary Related Files` and `Secondary Related Files`.

Dependencies: `DOC-504`

### Recommended execution order

1. `DOC-501`
2. `DOC-502`
3. `DOC-503`
4. `DOC-504`
5. `DOC-505`

### Exit criteria

- The old ad hoc docs have either been absorbed or removed.
- The new documentation tree is the single source of truth.
- Cross-links and references are consistent.

---

## Cross-Phase Dependency Summary

### Foundation dependencies

- Phase 1 is required before writing the rest of the new docs in a structured way.

### Main content dependencies

- Phase 2 depends on Phase 1.
- Phase 3 depends on Phase 1.
- Phase 4 depends on Phase 1.
- Phase 5 depends heavily on Phases 3 and 4.
- Phase 6 depends on all prior phases.

### Recommended global execution order

1. Phase 1 - Establish The Documentation System
2. Phase 2 - User Documentation
3. Phase 3 - Maintainer Core Concepts
4. Phase 4 - Reference Documentation
5. Phase 5 - Maintainer Workflow Documentation
6. Phase 6 - Migration And Cleanup

---

## Existing Docs As Raw Material

These current docs are likely source material, not final destinations:

- `docs/ONBOARDING.md`
- `docs/SCHEMA.md`
- `docs/solidworks-macro-pipeline.md`
- `docs/calibrated-survey-quad-selection-trace.md`
- `docs/codebase_audit_2026-04-26.md`
- `docs/parts-parameter-reference.md`

Suggested handling:

- absorb useful content into the new structure
- rewrite where the shape is wrong
- delete outdated or redundant originals once migration is complete

---

## Definition Of Done

The documentation initiative is complete when:

- the agreed docs tree exists
- users can install and use the app from documentation alone
- agents can navigate the codebase and standard change paths from documentation alone
- stable schema/config/validation references exist
- outdated ad hoc docs have been absorbed or deleted
- the new docs tree is the only documentation system that needs to be maintained
