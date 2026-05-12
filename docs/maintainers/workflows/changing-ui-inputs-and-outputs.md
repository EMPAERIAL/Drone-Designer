# Changing UI Inputs And Outputs

Use this workflow when you are changing what operators can enter, what the app shows back to them, or how the form-level interaction flow behaves.

This is not a pure styling page. In Drone Designer, UI changes are usually coupled to `MissionSpecs`, `SelectionResult`, or both.

## Read First

Before editing code, read in this order:

1. [`UI/Forms/MainForm.vb`](../../UI/Forms/MainForm.vb) for control layout and naming.
2. [`UI/Forms/MainForm.Logic.vb`](../../UI/Forms/MainForm.Logic.vb) for input validation, mission-spec mapping, and result binding.
3. [`docs/maintainers/architecture-overview.md`](../architecture-overview.md) to confirm which subsystem actually owns the behavior you are touching.
4. [`docs/reference/testing-and-validation.md`](../../reference/testing-and-validation.md) for the minimum UI validation path.

If the change affects export or CAD actions on the output tab, also read:

- [`UI/Forms/MainForm.Export.vb`](../../UI/Forms/MainForm.Export.vb)
- [`UI/Forms/MainForm.CAD.vb`](../../UI/Forms/MainForm.CAD.vb)

## Common Change Shapes

Most UI-facing changes fall into one of these buckets:

- add, remove, or rename a mission input control
- change validation or default values for an existing input
- change how `MissionSpecs` is built from the form
- change how the selected-components table or summary labels are populated
- change the operator flow between input, result, export, and CAD actions

Treat those as data-flow changes, not isolated control tweaks.

## Implementation Workflow

Use this order for UI changes:

1. Change the control surface in `MainForm.vb` if layout or control presence changed.
2. Update the validation and mapping logic in `MainForm.Logic.vb`.
3. Update any related model fields in `MissionSpecs` or `ComponentDisplayRow` if the UI contract changed.
4. Update output binding, export shaping, or CAD gating if the UI now exposes or depends on new state.
5. Update user or maintainer docs if the operator-visible flow changed.

Practical rule: keep layout changes and logic changes in sync. A visible control that is not mapped into `MissionSpecs`, or a mapped field that is no longer represented in the UI, creates misleading behavior quickly.

## Files That Usually Change Together

Typical pairings:

- input control plus mapping logic:
  `UI/Forms/MainForm.vb` + `UI/Forms/MainForm.Logic.vb`
- mission-input contract changes:
  `UI/Forms/MainForm.Logic.vb` + `Core/Models/MissionSpecs.vb`
- result-table presentation changes:
  `UI/Forms/MainForm.Logic.vb` + `Core/Models/ComponentDisplayRow.vb`
- output-tab action changes:
  `UI/Forms/MainForm.Export.vb` or `UI/Forms/MainForm.CAD.vb` plus the related user docs

If a UI label or flow change alters what the operator is expected to do, update the matching user page in `docs/user/`.

## Validation Path

Minimum validation for a UI-facing change:

1. Build the app.
2. Launch it and exercise the touched input or output path.
3. Run one successful design flow through `Select Components`.
4. Verify the changed field or result area behaves as intended.

Add these checks when relevant:

- run the in-app scenario harness if the change touched `MissionSpecs` mapping or validation
- run Excel export if the output tab or result table changed
- run CAD generation only if the change touched the CAD action path or its gating logic

Use the fuller checklist in [Testing And Validation](../../reference/testing-and-validation.md) instead of duplicating it in PR notes.

## Common Failure Modes

Watch for these regressions:

- combo-box index changes that silently remap to the wrong enum value
- validation rules updated in the UI but not reflected in deeper assumptions
- new output labels that are not backed by live data
- export or CAD buttons becoming reachable before `_lastResult` is valid
- layout changes that hide controls on the scrollable input tab

These are usually cheaper to catch with one manual design flow than by staring only at the code diff.

## Primary Related Files

- `UI/Forms/MainForm.vb`
- `UI/Forms/MainForm.Logic.vb`
- `Core/Models/MissionSpecs.vb`
- `Core/Models/ComponentDisplayRow.vb`
- `docs/reference/testing-and-validation.md`

## Secondary Related Files

- `UI/Forms/MainForm.Export.vb`
- `UI/Forms/MainForm.CAD.vb`
- `UI/Forms/MainForm.Tests.vb`
- `docs/user/workflow-design-a-uav-from-requirements.md`
- `docs/user/workflow-export-and-generate-outputs.md`
- `docs/user/troubleshooting-and-faq.md`
