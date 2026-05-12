# Adding Or Updating Tests

Use this workflow when you are adding or updating any of the repository's validation paths: in-app scenarios, manual-check guidance, export checks, or CAD checks.

This page is about workflow and intent. The stable checklist lives in [Testing And Validation](../../reference/testing-and-validation.md).

## Read First

Before changing tests or validation scenarios, read:

1. [`docs/reference/testing-and-validation.md`](../../reference/testing-and-validation.md) for the current validation matrix.
2. [`UI/Forms/MainForm.Tests.vb`](../../UI/Forms/MainForm.Tests.vb) for the in-app scenario harness and keyboard entry points.
3. [`Test/README.md`](../../Test/README.md) for the current scenario and log conventions.

Then read the workflow doc for the subsystem you are changing if the validation is tied to one area:

- [Changing The Selection Engine](changing-the-selection-engine.md)
- [Changing UI Inputs And Outputs](changing-ui-inputs-and-outputs.md)
- [Changing The Component Database And Schema](changing-the-component-database-and-schema.md)
- [Changing CAD Generation](changing-cad-generation.md)

## What The Current Validation Paths Are Proving

The repo uses multiple validation paths because no single suite covers the whole app.

- build and startup checks prove the runtime still launches and loads configuration/resources
- one manual design flow proves the operator path still reaches a design recommendation
- the in-app scenario harness proves broader selector behavior across multiple cases
- Excel export checks prove output shaping still matches the result contract
- CAD checks prove the optional SolidWorks path still works on a capable machine

Add or update the smallest validation path that actually covers the risk of your change.

## Safe Test-Change Workflow

Use this order:

1. Decide which class of validation should catch the change.
2. Update the relevant harness, scenario, or checklist notes.
3. Run the changed validation path once to confirm it still works.
4. Update the reference checklist only if the stable recommended validation depth changed.

Practical rule: avoid adding ad hoc validation notes in random docs when the check belongs in the central testing reference or the in-app scenario harness.

## Working With The In-App Scenario Harness

The in-app harness lives in [`UI/Forms/MainForm.Tests.vb`](../../UI/Forms/MainForm.Tests.vb).

Current entry points:

- `Ctrl+Shift+T` runs the built-in scenario set
- `Ctrl+Shift+L` loads scenario definitions from CSV

Use the harness when:

- you changed selection logic
- you changed mission-input mapping
- you changed component data in ways that affect broad recommendation behavior

When updating the harness:

1. keep scenario names explicit
2. keep the scenario values realistic enough to expose regressions
3. prefer additive scenario changes over rewriting old cases without reason

## When Manual Validation Is Still The Right Tool

Do not force everything into the harness.

Manual validation is still the right tool for:

- output-tab interaction changes
- save dialogs and export flows
- CAD progress and COM-dependent behavior
- packaging and runtime-asset checks

Use the central testing reference to describe those checks once, then link to it from workflow docs and PRs.

## Validation Notes In PRs

When a PR changes tests or validation scenarios, note:

- which validation path you changed
- what that path is intended to prove
- whether you ran it in this PR

Good examples:

- `Docs only; not run.`
- `Updated the in-app scenario harness for a new propulsion edge case; built Debug and ran Ctrl+Shift+T.`
- `Updated export validation guidance; built Debug, ran one design flow, and verified Excel export.`

## Primary Related Files

- `docs/reference/testing-and-validation.md`
- `UI/Forms/MainForm.Tests.vb`
- `Test/README.md`

## Secondary Related Files

- `Test/Scenarios/`
- `Test/Logs/`
- `UI/Forms/MainForm.Logic.vb`
- `Core/Services/ComponentSelectionEngine.vb`
- `docs/maintainers/workflows/changing-the-selection-engine.md`
- `docs/maintainers/workflows/changing-cad-generation.md`
