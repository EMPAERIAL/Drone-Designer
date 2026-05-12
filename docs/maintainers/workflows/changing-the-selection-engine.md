# Changing The Selection Engine

Use this workflow when you are changing the selection engine itself rather than only the UI around it or the component data it consumes.

This page is the sanctioned maintainer path for changes in the design-recommendation logic. It assumes the conceptual reading already lives elsewhere:

- [Selection Engine Overview](../concepts/selection-engine-overview.md)
- [Selection Engine Pipeline And Rationale](../concepts/selection-engine-pipeline-and-rationale.md)
- [Testing And Validation](../../reference/testing-and-validation.md)

## Read First

Before editing code, read in this order:

1. [`docs/maintainers/concepts/selection-engine-overview.md`](../concepts/selection-engine-overview.md) to confirm what the engine owns versus UI, repository, export, and CAD code.
2. [`docs/maintainers/concepts/selection-engine-pipeline-and-rationale.md`](../concepts/selection-engine-pipeline-and-rationale.md) to see the live step order, heuristics, and known divergences from the literature.
3. [`Core/Services/ComponentSelectionEngine.vb`](../../Core/Services/ComponentSelectionEngine.vb) to locate the exact stage you are changing.
4. [`docs/reference/component-database-and-schema.md`](../../reference/component-database-and-schema.md) if the change depends on fields or derived values from `components.json`.

Do not start from the UI files unless the change is explicitly about how mission inputs are mapped into engine-facing fields.

## Typical Change Coupling

Selection-engine edits usually move with one or more of these adjacent updates:

- `MissionSpecs` mapping or enum changes in [`UI/Forms/MainForm.Logic.vb`](../../UI/Forms/MainForm.Logic.vb) and [`Core/Models/MissionSpecs.vb`](../../Core/Models/MissionSpecs.vb)
- component-model or schema changes in [`Core/Models/ComponentSpecs.vb`](../../Core/Models/ComponentSpecs.vb) or [`Resources/AppData/components.json`](../../Resources/AppData/components.json)
- documentation changes in the concept and reference docs when the engine's actual behavior changed
- validation updates when a previous checklist assumption is no longer sufficient

If the change alters:

- selector ordering
- rejection gates
- torque, `Ct`, or `Cp` usage
- mission-energy assumptions
- warnings or result fields

then update the relevant concept or reference docs in the same vertical slice instead of leaving them stale.

## Implementation Workflow

Use this order for engine work:

1. Identify the exact pipeline stage that owns the behavior.
2. Confirm the upstream inputs and downstream outputs for that stage.
3. Check whether the change is code-only or also requires data/schema changes.
4. Make the smallest coherent code change that preserves the rest of the pipeline contract.
5. Re-read the surrounding comments before finalizing. The engine relies heavily on embedded design notes and named constants.
6. Update the concept/reference docs if the current-behavior explanation changed.

Practical rule: avoid "mystery fixes" in `ComponentSelectionEngine.vb`. If a maintainer cannot tell which stage changed and why, the next maintainer will struggle to validate the result.

## High-Risk Areas

Treat these areas as the highest-risk engine edits:

- `BuildMissionSpecs()` to `MissionSpecs` mapping mismatches
- `BuildPropCandidates()` filtering and ordering
- `SelectMotorForCandidate()` torque, voltage, and fit gates
- `CalculatePowerBudget()` and battery-feasibility logic
- any change that alters warnings without changing the underlying selection behavior

These areas can make the app look plausible while silently shifting the recommendation logic.

## Validation Path

Minimum validation for a real engine change:

1. Build the app.
2. Launch it and confirm startup still loads the component database.
3. Run at least one manual design flow through `Select Components`.
4. Run the in-app scenario harness described in [Testing And Validation](../../reference/testing-and-validation.md).
5. Check warnings, propulsion pairing, and battery output for obvious regressions.

Add Excel export validation when the change affects `SelectionResult` shape or display values. Add CAD validation only when the change can plausibly affect downstream geometry or manifest content.

## When To Update Other Docs

Update these docs in the same change when applicable:

- [Selection Engine Pipeline And Rationale](../concepts/selection-engine-pipeline-and-rationale.md): when the live stage order, heuristics, or literature-divergence explanation changed
- [Component Database And Schema](../../reference/component-database-and-schema.md): when new or renamed fields become engine-coupled
- [Testing And Validation](../../reference/testing-and-validation.md): when the minimum safe validation path changed
- [Changing The Component Database And Schema](changing-the-component-database-and-schema.md): when the engine change depends on a sanctioned data-edit path

## Primary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Models/MissionSpecs.vb`
- `Core/Models/ComponentSpecs.vb`
- `Core/Models/SizingPolicy.vb`
- `UI/Forms/MainForm.Logic.vb`

## Secondary Related Files

- `Core/Data/ComponentRepository.vb`
- `UI/Forms/MainForm.Tests.vb`
- `Resources/AppData/components.json`
- `docs/maintainers/concepts/selection-engine-overview.md`
- `docs/maintainers/concepts/selection-engine-pipeline-and-rationale.md`
- `docs/reference/component-database-and-schema.md`
- `docs/reference/testing-and-validation.md`
