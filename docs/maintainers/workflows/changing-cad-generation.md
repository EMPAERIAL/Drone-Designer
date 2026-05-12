# Changing CAD Generation

Use this workflow when you are changing the optional SolidWorks-backed CAD path rather than the main selection or Excel-export path.

This is one of the highest-fragility areas in the repository. It combines:

- UI gating in the output tab
- pipeline orchestration
- runtime path resolution
- SolidWorks COM automation
- compiled `.swp` macros
- template part assets

Read this page together with:

- [Configuration And Runtime Dependencies](../../reference/configuration-and-runtime-dependencies.md)
- [Testing And Validation](../../reference/testing-and-validation.md)
- [`docs/solidworks-macro-pipeline.md`](../../solidworks-macro-pipeline.md)

## Read First

Before editing code or macros, read in this order:

1. [`UI/Forms/MainForm.CAD.vb`](../../UI/Forms/MainForm.CAD.vb) for the UI entry point and state checks.
2. [`Core/Services/PipelineOrchestrator.vb`](../../Core/Services/PipelineOrchestrator.vb) for the end-to-end CAD workflow and path building.
3. [`Solidworks/SolidWorksAutomation.vb`](../../Solidworks/SolidWorksAutomation.vb) for COM connection rules and install/version checks.
4. [`Solidworks/MacroRunner.vb`](../../Solidworks/MacroRunner.vb) for macro invocation and error handling.
5. [`docs/solidworks-macro-pipeline.md`](../../solidworks-macro-pipeline.md) for the repo's hard-won notes about STA threading, `.swp` requirements, and module naming.

Do not start by editing only the macro files. In this repo, CAD failures are often in the orchestration or runtime-asset path rather than the macro body itself.

## Safe CAD-Change Workflow

Use this order:

1. Confirm whether the change belongs to UI gating, orchestration, COM connection, macro execution, or template assets.
2. Update the smallest owning layer first.
3. If the change affects a macro or template, verify that the built runtime output still contains the required asset.
4. Reconcile the change against the existing SolidWorks note instead of assuming the old note is fully current.
5. Run the CAD validation path on a SolidWorks-capable machine before merging.

Practical rule: a CAD change is incomplete until you verify the runtime output folder, not just the source tree.

## Current Repo Realities To Preserve

Useful current signal from the existing SolidWorks pipeline notes:

- `RunMacro2` expects compiled `.swp` files, not only `.swb` source files.
- COM calls must stay on one STA-thread block; scattering them across async continuations is a known failure mode.
- module names inside the VBA project matter and can differ from the macro filename.
- a source asset being present under `Resources\SolidWorks\` does not guarantee that it is copied into `bin\Debug` or `bin\Release`.

Those are not historical trivia. They are active maintenance constraints.

## Files That Usually Change Together

Typical pairings:

- UI action or button-state changes:
  `UI/Forms/MainForm.CAD.vb` + `UI/Forms/MainForm.Logic.vb`
- pipeline-stage or output-path changes:
  `Core/Services/PipelineOrchestrator.vb` + `Utilities/ConfigManager.vb`
- COM connection or macro-runner changes:
  `Solidworks/SolidWorksAutomation.vb` + `Solidworks/MacroRunner.vb`
- macro or template asset changes:
  `Resources/SolidWorks/` + `Drone Designer.vbproj` + runtime output verification

Update [Configuration And Runtime Dependencies](../../reference/configuration-and-runtime-dependencies.md) if the runtime assumptions changed. Update the user export workflow only if the operator-facing CAD behavior changed.

## Validation Path

Minimum validation for a real CAD change:

1. Build the app.
2. Launch it and complete one successful design flow.
3. Start `Generate CAD` on a machine with SolidWorks available.
4. Confirm the progress flow runs and the output folder or manifest is produced.
5. Verify the expected macros and templates existed in the actual runtime output.

Add these checks when relevant:

- export validation if the change touched result shaping used by both Excel and CAD
- release-output validation if the change depends on newly copied assets

Use [Testing And Validation](../../reference/testing-and-validation.md) as the central checklist. The important point here is that CAD validation is environment-dependent and cannot be inferred from a normal launch smoke test.

## Common Failure Modes

Watch for:

- a change that works in source but fails in `bin\Debug` or `bin\Release`
- macros present as `.swb` source but missing as compiled `.swp`
- COM calls split across different threads or resumed after `Await` onto MTA threads
- template filenames or module names drifting from what `PipelineOrchestrator` and `MacroRunner` expect
- assuming SolidWorks is the problem when the real failure is missing runtime assets

These failures often surface late and look like generic CAD errors unless you inspect the pipeline stage boundaries directly.

## Primary Related Files

- `UI/Forms/MainForm.CAD.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Solidworks/SolidWorksAutomation.vb`
- `Solidworks/MacroRunner.vb`
- `Drone Designer.vbproj`

## Secondary Related Files

- `UI/Forms/CadProgressForm.vb`
- `UI/Forms/MainForm.Logic.vb`
- `Utilities/ConfigManager.vb`
- `Resources/SolidWorks/`
- `docs/solidworks-macro-pipeline.md`
- `docs/reference/configuration-and-runtime-dependencies.md`
- `docs/reference/testing-and-validation.md`
