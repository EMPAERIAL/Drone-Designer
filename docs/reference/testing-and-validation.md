# Testing And Validation

This page is the central validation checklist for Drone Designer. Maintainer workflow docs should link here instead of re-describing build, launch, selection, export, and CAD checks separately.

The repository does not currently expose one large external automated suite. Validation is a mix of:

- source-build checks
- launch smoke tests
- manual selection-flow checks
- the in-app scenario harness
- output-path checks for Excel and optional CAD generation

Use the other reference pages alongside this one when a validation run depends on data or runtime-path assumptions:

- [Component Database And Schema](component-database-and-schema.md) for schema-sensitive component-data changes
- [Configuration And Runtime Dependencies](configuration-and-runtime-dependencies.md) for output-folder, config, and optional SolidWorks dependency checks

## Pick The Right Validation Depth

Use the smallest checklist that still matches the risk of the change.

| Change type | Minimum validation |
|---|---|
| docs-only change | verify links and affected facts against source |
| UI text or layout change | build, launch, and exercise the touched screen |
| selection-engine or schema change | build, launch, run at least one manual design flow, and run the scenario harness |
| export change | build, launch, run one design flow, and verify Excel export |
| CAD-path change | build, launch, run one design flow, and verify the CAD path on a machine with SolidWorks |
| config or packaging change | build, launch from output or package, and verify resource-path assumptions |

## Preconditions

Before running the checklist, decide which environment you are validating:

- source build from `bin\Debug` or `bin\Release`
- packaged ZIP-style runtime
- CAD-capable machine with SolidWorks installed

Not every environment can run every check. The CAD checks are optional and only apply when the machine is expected to support that workflow.

## Core Checklist

### 1. Build Or Package Sanity

Use this when the change affects code, resources, or packaging.

Check:

- the project builds successfully for the intended configuration
- the output contains `Drone Designer.exe`
- `Resources\AppData\components.json` exists at the runtime path
- any required runtime assets touched by the change were copied into the actual output folder, not just left in the source tree

For source builds, pay extra attention to SolidWorks assets because the current project file explicitly copies only a subset of the source-tree macro and template files.

### 2. Startup Smoke Test

Run this after any change that could affect application startup, config, or repository loading.

Check:

- the app launches
- the main form opens
- no startup error appears for config creation or component-database loading
- the mission input controls are usable

A passing startup smoke test is the minimum bar before deeper workflow checks.

### 3. Manual Design-Run Check

Run at least one successful end-to-end design recommendation flow whenever the selection path, UI input mapping, schema, or config behavior may have changed.

Suggested steps:

1. launch the app
2. enter a realistic multirotor mission
3. click `Select Components`
4. confirm the selected-components table populates
5. review warnings and summary values for obvious breakage

Minimum expectations:

- no validation loop or unexpected exception
- at least one motor, propeller, battery, and ESC recommendation appears
- warnings, if any, look plausible rather than obviously corrupted

### 4. In-App Scenario Harness

Use this after selection-engine, schema, or mission-input changes.

The built-in harness lives in [`UI/Forms/MainForm.Tests.vb`](../../UI/Forms/MainForm.Tests.vb).

Current entry points:

- `Ctrl+Shift+T`: run the built-in scenario set
- `Ctrl+Shift+L`: load scenarios from a CSV file and run them

Supporting assets:

- maintained scenario notes: [`Test/README.md`](../../Test/README.md)
- generated logs: `Test/Logs/`
- maintained CSV scenarios: `Test/Scenarios/`

Track policy:

- `Test/Scenarios/multirotor_regression.csv` is the blocking regression track.
- `Test/Scenarios/fixedwing_exploratory.csv` is exploratory and non-blocking until a dedicated fixed-wing selection path is implemented.
- Harness output labels scenarios as `blocking` or `exploratory` and reports per-track pass/fail counts.

What to check:

- the harness starts
- scenarios complete without unexpected crashes
- passes and failures look plausible for the changed area
- a log file is written when expected

Use the harness to catch broad selector regressions faster than one-off manual clicking.

### 5. Excel Export Check

Run this after export-path changes and after any change that could corrupt `SelectionResult` display or export data shaping.

Suggested steps:

1. complete a successful design run
2. click `Export to Excel`
3. save to a writable folder
4. confirm the `.xlsx` file is created
5. open the workbook if practical and check the mission sheet and component sheet for obvious breakage

Minimum expectations:

- export does not fail with `Nothing to Export` after a successful run
- the output file exists
- mission inputs, summary values, and selected components are present

### 6. Optional CAD Generation Check

Run this only when:

- the change affects the CAD path
- the release claims to support CAD generation
- you are on a machine with SolidWorks available

Suggested steps:

1. complete a successful design run
2. click `Generate CAD`
3. choose an output folder
4. let the pipeline complete
5. inspect the output folder and the manifest

Minimum expectations:

- the progress flow starts
- SolidWorks connection succeeds
- expected output files or partial-failure records are produced
- the app returns a comprehensible success or failure result rather than hanging or crashing

Because this path depends on COM, templates, and `.swp` macros, diagnose CAD failures separately from main-app failures.

## Change-Focused Checklists

### Selection Engine Or Mission Model Changes

Run:

- build or package sanity
- startup smoke test
- manual design-run check
- in-app scenario harness

Also review:

- warnings returned by `SelectionResult`
- whether the chosen propulsion and battery results still look internally consistent

### Component Database Or Schema Changes

Run:

- build or package sanity
- startup smoke test
- manual design-run check
- in-app scenario harness

Also check:

- the repository still loads the edited records
- no category rename or field rename silently dropped components from the result set

### Config Or Runtime Dependency Changes

Run:

- build or package sanity
- startup smoke test
- manual design-run check

Also check:

- `DroneDesigner.config.json` creation and loading
- resolved component, template, output, and log paths if the change touched them

### Export Changes

Run:

- build or package sanity
- startup smoke test
- manual design-run check
- Excel export check

### CAD Changes

Run:

- build or package sanity
- startup smoke test
- manual design-run check
- optional CAD generation check on a SolidWorks-capable machine

Also confirm:

- required templates and `.swp` macros exist in the actual runtime output
- STA-thread and COM assumptions still hold if async flow changed

## Validation Notes To Capture In PRs

When writing testing notes for a PR, keep them short and explicit.

Good examples:

- `Docs only; not run.`
- `Built Debug, launched app, ran one manual selection flow, and verified Excel export.`
- `Built Debug, ran Ctrl+Shift+T scenario harness, and checked CAD generation on a SolidWorks 2026 machine.`

Avoid vague notes like `tested locally` when you can name the actual checks.

## Known Gaps In The Current Validation Story

The current repo state still has these limits:

- no obvious external CI-style regression suite for the whole app
- scenario coverage is useful but in-app and manual
- CAD validation is environment-dependent and cannot be assumed on every machine
- packaging validation still needs explicit attention because source assets and copied runtime assets are not identical by default

Plan validation depth accordingly.

## Primary Related Files

- `UI/Forms/MainForm.Tests.vb`
- `Core/Services/ComponentSelectionEngine.vb`
- `UI/Forms/MainForm.Logic.vb`
- `UI/Forms/MainForm.Export.vb`
- `UI/Forms/MainForm.CAD.vb`
- `docs/maintainers/workflows/changing-the-selection-engine.md`

## Secondary Related Files

- `Test/README.md`
- `Test/Scenarios/`
- `Test/Logs/`
- `Core/Data/ComponentRepository.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Resources/AppData/components.json`
- `docs/reference/component-database-and-schema.md`
- `docs/reference/configuration-and-runtime-dependencies.md`
