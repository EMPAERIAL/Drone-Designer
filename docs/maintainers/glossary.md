# Glossary

This glossary defines the preferred terminology for Drone Designer documentation. Use these terms consistently across user, maintainer, and reference docs unless a code identifier requires a different exact name.

## Core Terms

| Term | Canonical meaning | Use instead of |
|---|---|---|
| Drone Designer | The application and repository as a whole. | "the tool", "the system" when precision matters |
| mission inputs | The operator-provided requirements and constraints entered before a design run. | "requirements form", "input set" |
| mission specs | The structured in-memory representation of mission inputs, primarily `MissionSpecs`. | "mission object", "spec payload" |
| selection engine | The core component-selection logic that turns mission specs into a candidate design. | "solver" when referring to the full pipeline |
| selection pipeline | The ordered sequence of selection-engine steps used during a design run. | "algorithm" when the meaning is the whole end-to-end flow |
| design run | One execution of the selection pipeline for a specific set of mission inputs. | "calculation" as the primary user-facing term |
| design recommendation | The resulting candidate configuration produced by the engine for a design run. | "answer", "solution" in user or maintainer docs |
| selection result | The structured output object returned by the engine, including chosen components, warnings, and derived values. | "result blob", "output model" |
| component database | The JSON-backed catalogue of selectable components used by the app. | "catalog", "parts list" when referring to the full data source |
| schema | The stable shape and meaning of the component database fields and related reference data. | "format" when field-level precision matters |
| scenario | A named test or validation case with a defined set of mission inputs. | "sample", "case" in testing docs when precision matters |
| export | A generated artifact such as PDF, CSV, or other non-CAD output produced from the app. | "report generation" when the output is broader than reports alone |
| CAD generation | The SolidWorks-driven workflow that creates CAD outputs from a selected design. | "SolidWorks export" when the workflow includes more than file export |

## Boundary Terms

### Selection engine

Use **selection engine** for the code that performs component sizing and selection. This term owns the internal decision logic.

Do not use **UI** or **repository** as synonyms for the engine. They are adjacent subsystems, not part of the engine itself.

### Selection pipeline

Use **selection pipeline** when describing the sequence of engine stages from mission specs through component choices and derived outputs.

Use **heuristic** only for a specific rule or shortcut inside the pipeline, not for the whole pipeline.

### Design recommendation

Use **design recommendation** in user-facing prose when talking about what the app gives the operator after a run. It is easier to read than code-level names and does not promise mathematical optimality.

Use **selection result** when you mean the concrete program output type or the full structured result shown in logs or code.

### Component database and schema

Use **component database** for the actual stored catalogue content.

Use **schema** for the field definitions, constraints, and stable meaning of that content. A schema change may require a component-database change, but the terms are not interchangeable.

## Preferred Language For Common Areas

- **Mission inputs** for operator-entered requirements.
- **Selection engine** for the core sizing and choice logic.
- **Selection pipeline** for the ordered internal flow.
- **Design recommendation** for the user-visible candidate configuration.
- **Scenario** for validation or harness inputs.
- **CAD generation** for the SolidWorks-backed output flow.

## Terms To Avoid In New Docs

- Avoid **solver** unless you specifically mean a numeric solver or convergence routine inside the engine.
- Avoid **catalogue** unless you are quoting an external source that already uses that spelling.
- Avoid **workflow** when you really mean a single screen or a single file format.
- Avoid **output** by itself when the distinction between export, CAD generation, and design recommendation matters.

## Maintenance Note

If later documentation introduces a term that conflicts with this glossary, update the glossary first or as part of the same change. Canonical terminology should lead the docs, not trail them.
