# Changing Export And Report Generation

Use this workflow when you are changing the operator-facing export path, the exported workbook shape, or the result data that feeds those outputs.

This page is about Excel and report-style export behavior. CAD generation has its own workflow because it adds SolidWorks, COM, and template/macro dependencies that do not belong in the normal export path.

## Read First

Before editing export behavior, read in this order:

1. [`UI/Forms/MainForm.Export.vb`](../../UI/Forms/MainForm.Export.vb) for the live export path and workbook writer.
2. [`UI/Forms/MainForm.Logic.vb`](../../UI/Forms/MainForm.Logic.vb) to see how `_lastResult` and the output grid are populated before export.
3. [`docs/reference/testing-and-validation.md`](../../reference/testing-and-validation.md) for the minimum export validation path.
4. [`docs/user/workflow-export-and-generate-outputs.md`](../../user/workflow-export-and-generate-outputs.md) for the current operator-facing story.

If the export change depends on new result fields, also read the relevant concept/reference docs that describe those fields.

## Common Change Shapes

Most export work falls into one of these buckets:

- change workbook content or sheet structure
- change which result fields are exported
- change the save dialog or post-export open behavior
- change how recommended versus alternative rows are represented
- change the coupling between displayed results and exported results

Treat those as data-flow changes, not isolated file-format tweaks.

## Implementation Workflow

Use this order:

1. Confirm the source of the exported data.
2. Change the workbook-building or file-writing logic in `MainForm.Export.vb`.
3. Update any dependent result-shaping code if the export now needs fields that were not previously surfaced.
4. Update the user doc if the operator-visible export behavior changed.
5. Run the export validation path before merging.

Practical rule: keep the output-tab display and the workbook content aligned. If the workbook starts carrying values the UI never shows, or drops values the UI claims are present, operators will assume the export is wrong.

## Files That Usually Change Together

Typical pairings:

- workbook structure or styling:
  `UI/Forms/MainForm.Export.vb` + the user export workflow doc
- result-field changes:
  `UI/Forms/MainForm.Export.vb` + `UI/Forms/MainForm.Logic.vb` + the model or engine code that owns the field
- output-tab interaction changes:
  `UI/Forms/MainForm.Export.vb` + `UI/Forms/MainForm.vb`

If the change affects CAD behavior rather than workbook/report export, move to [`changing-cad-generation.md`](changing-cad-generation.md) instead of stretching this workflow.

## Validation Path

Minimum validation for an export change:

1. Build the app.
2. Launch it and complete one successful design flow.
3. Run `Export to Excel`.
4. Confirm the `.xlsx` file is created in a writable folder.
5. Open the workbook if practical and verify the changed sheet, fields, or formatting at a sanity-check level.

Add a manual design-run check first if the export change depends on `SelectionResult` data shape changes. Use the fuller checklist in [Testing And Validation](../../reference/testing-and-validation.md) instead of duplicating it in PR notes.

## Common Failure Modes

Watch for:

- export code reading stale UI state instead of the live `SelectionResult`
- workbook content drifting from the output-tab table
- formatting logic that still works structurally but drops recommended-versus-alternative distinctions
- save-dialog success with a malformed or incomplete workbook
- result-field additions that were not plumbed through the UI and export path consistently

These regressions are often visible only after opening the generated file, not from the app status label alone.

## Primary Related Files

- `UI/Forms/MainForm.Export.vb`
- `UI/Forms/MainForm.Logic.vb`
- `UI/Forms/MainForm.vb`
- `docs/reference/testing-and-validation.md`
- `docs/user/workflow-export-and-generate-outputs.md`

## Secondary Related Files

- `Core/Models/ComponentDisplayRow.vb`
- `Core/Services/ComponentSelectionEngine.vb`
- `UI/Forms/MainForm.CAD.vb`
- `docs/user/troubleshooting-and-faq.md`
- `docs/maintainers/workflows/changing-cad-generation.md`
