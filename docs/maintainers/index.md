# Maintainer Documentation

This section is for AI agents and human maintainers changing Drone Designer itself. It organizes the documentation set by the kind of repository work you need to do: onboarding, terminology, system understanding, sanctioned change paths, and stable reference facts.

## Read This First

- [Agent Onboarding](agent-onboarding.md): how this documentation system is structured, how agents should write docs, and when documentation updates belong in the work.
- [Glossary](glossary.md): canonical project terminology that later docs should reuse consistently.

## Understand The System

- [Architecture Overview](architecture-overview.md): major subsystems, boundaries, and how the app fits together.
- [Selection Engine Overview](concepts/selection-engine-overview.md): the engine's responsibility and its boundaries with UI, repository, and outputs.
- [Selection Engine Pipeline And Rationale](concepts/selection-engine-pipeline-and-rationale.md): detailed pipeline behavior, heuristics, and implementation rationale.
- [Known Limitations And Risks](known-limitations-and-risks.md): maintainer-facing sharp edges and known weak spots.

## Follow A Change Path

- [Changing The Selection Engine](workflows/changing-the-selection-engine.md)
- [Changing UI Inputs And Outputs](workflows/changing-ui-inputs-and-outputs.md)
- [Changing The Component Database And Schema](workflows/changing-the-component-database-and-schema.md)
- [Changing Export And Report Generation](workflows/changing-export-and-report-generation.md)
- [Changing CAD Generation](workflows/changing-cad-generation.md)
- [Adding Or Updating Tests](workflows/adding-or-updating-tests.md)
- [Preparing A Release Package](workflows/preparing-a-release-package.md)

## Stable Reference Facts

- [Reference Documentation](../reference/index.md): schema, runtime dependency, and validation facts that other maintainer docs can link back to.

## Suggested Reading Order

Use this sequence when you are new to the repo or starting work in an unfamiliar area:

1. Read the onboarding guide.
2. Check the glossary for canonical language.
3. Read architecture or concept docs for the subsystem you are changing.
4. Follow the matching workflow doc.
5. Use the reference docs for facts that need to stay precise and stable.
