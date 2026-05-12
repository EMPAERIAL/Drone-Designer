# Export And Generate Outputs

Use this workflow after you already have a design recommendation you want to save, share, or turn into downstream artifacts.

Drone Designer currently supports two operator-facing output paths:

- Excel export for the selected design
- CAD generation as an advanced optional step

## Before You Start

Run a successful design run first.

You are ready for this workflow when:

- the component table is populated
- the design recommendation looks acceptable
- you want a saved file or generated output folder

If you have not run `Select Components` yet, go back to [Design A UAV From Requirements](workflow-design-a-uav-from-requirements.md) first.

## Output Types

### Excel export

Use this when you want a portable record of the current design recommendation.

The Excel workbook includes:

- mission parameters
- selection summary values
- the selected components table

This is the normal operator output when you want to review the design outside the app or send it to someone else.

### CAD generation

Use this when you want Drone Designer to generate downstream CAD outputs from the selected design.

This is the advanced path. It depends on SolidWorks being available and correctly usable on the machine running the app.

## Workflow 1: Export The Selected Design To Excel

After a successful design run:

1. Go to the output area with the component table.
2. Click `Export to Excel`.
3. Choose where to save the `.xlsx` file.
4. Confirm the export.

Drone Designer writes an Excel workbook and then offers to open it immediately.

Use Excel export when you want to:

- archive a design run
- share the recommendation with teammates
- review mission inputs and selected components side by side

## What The Excel Export Contains

The exported workbook is organized around the current design recommendation.

Expect to see:

- the mission inputs used for the design run
- summary sizing values such as estimated MTOW and battery information
- the selected component list, with recommended rows distinguished from alternatives

If you rerun the design with different mission inputs, export again. The workbook reflects the current design run, not every run from the whole session.

## Workflow 2: Generate CAD

Use this only after component selection is complete.

The CAD-generation flow is:

1. Run `Select Components`.
2. Confirm the design recommendation is the one you want to use.
3. Click `Generate CAD`.
4. Choose an output folder.
5. Wait for the CAD pipeline to finish.
6. Open the output folder if needed.

Drone Designer shows progress while the CAD pipeline runs. When it succeeds, it offers to open the generated output folder.

## What CAD Generation Produces

CAD generation writes its outputs into the folder you choose.

That folder is expected to contain:

- the generated CAD part outputs
- a plain-text component manifest describing what was generated

Treat the chosen output folder as the result package for that CAD-generation run.

## When To Use Excel Export Versus CAD Generation

Use Excel export when:

- you need a readable design summary
- you want to share the selected components and mission inputs
- you do not need downstream CAD files

Use CAD generation when:

- the selected design is final enough to push into downstream mechanical work
- SolidWorks is available on the machine
- you want generated parts rather than only a report-style export

In many operator sessions, Excel export is the normal output and CAD generation is only used for the final pass.

## CAD Generation Expectations

CAD generation is more demanding than Excel export.

Before you start it, assume:

- it depends on the selected design already being valid
- it depends on SolidWorks being available
- it may take longer than a normal design run

If you only need the component recommendation, skip CAD generation and export the design to Excel instead.

## Common Operator Checks Before Exporting

Before you save outputs, verify:

- the mission inputs are the final ones you want recorded
- the recommended components match the mission intent
- warnings have been reviewed
- the estimated MTOW looks acceptable

This avoids exporting a stale or exploratory run by mistake.

## If An Output Action Is Unavailable

`Export to Excel` and `Generate CAD` depend on the current app state.

If an output action is not usable, the most common reason is that no successful design run exists yet in the session.

Typical recovery:

1. return to the mission inputs
2. run `Select Components`
3. verify the component table is populated
4. try the output action again

## Next Step

Use [Troubleshooting And FAQ](troubleshooting-and-faq.md) if export fails, CAD generation cannot start, or the resulting files do not appear where expected.
