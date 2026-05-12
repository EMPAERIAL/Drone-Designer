# Troubleshooting And FAQ

Use this page when the normal Drone Designer workflow breaks down. It focuses on operator-facing problems and the first checks you can do without digging into the codebase.

Use it as the recovery page for the rest of the user doc set:

- startup and packaging problems after [Installation And Prerequisites](installation-and-prerequisites.md)
- design-run failures after [Design A UAV From Requirements](workflow-design-a-uav-from-requirements.md)
- export or CAD failures after [Export And Generate Outputs](workflow-export-and-generate-outputs.md)

## Quick Triage

Start with the symptom that matches what you see:

- the app opens but says the component database could not be loaded
- `Select Components` does not complete
- export is unavailable or fails
- CAD generation cannot start
- CAD generation starts but fails

If you are still in the middle of setting up the app, go back to [Installation And Prerequisites](installation-and-prerequisites.md) first.

## The App Says The Component Database Could Not Be Loaded

Typical signs:

- a startup warning appears
- the app says the component database failed to load
- component selection is unavailable

Most likely cause:

- the packaged release was extracted incorrectly
- `Resources\AppData\components.json` is missing or moved

What to do:

1. Close the app.
2. Open the extracted Drone Designer folder.
3. Confirm that `Resources\AppData\components.json` still exists under that folder.
4. If it is missing, re-extract the release ZIP.
5. Launch the app again from the extracted folder.

If the file structure was changed manually, restore the original release layout and try again.

## `Select Components` Stops With An Input Validation Message

Typical signs:

- a dialog titled `Input Validation` appears
- the design run does not start

This usually means one or more mission inputs are inconsistent.

Common causes:

- minimum temperature is not lower than maximum temperature
- endurance is zero or unrealistic
- maximum altitude or range is too low or invalid
- payload mass is too large relative to maximum takeoff weight
- a multirotor setup was chosen with too few motors

What to do:

1. Read the validation message carefully.
2. Correct the specific field it names.
3. Run `Select Components` again.

When in doubt, simplify the mission inputs first, get one successful design run, then tighten the requirements gradually.

## `Select Components` Fails After The Run Starts

Typical signs:

- the app starts the design run, then shows `Component Selection Failed`
- warnings suggest the mission is too aggressive for the available components

Most likely cause:

- the current mission inputs cannot be satisfied by the component database

What to do:

1. Reduce one or two demanding mission inputs.
2. Try again with lower payload, lower endurance, or a higher acceptable takeoff weight.
3. Re-run the design instead of changing many unrelated inputs at once.

Good first adjustments:

- lower payload mass
- lower endurance
- increase maximum takeoff weight
- choose a more suitable airframe or mission profile

## `Export to Excel` Is Unavailable Or Says There Is Nothing To Export

Typical signs:

- clicking export shows `Nothing to Export`
- the export button is available but the app says no design exists yet

Most likely cause:

- no successful design run exists in the current session

What to do:

1. Return to the mission inputs.
2. Run `Select Components`.
3. Confirm the component table is populated.
4. Try `Export to Excel` again.

Excel export depends on the current design recommendation in memory. If you cleared the session or never completed a design run, there is nothing to export.

## Excel Export Fails

Typical signs:

- the app shows `Export failed`
- no `.xlsx` file appears where you expected it

Most likely causes:

- the chosen save location is not writable
- the file is already open in another program
- the filename or path is problematic

What to do:

1. Choose a simple save location such as Desktop or Documents.
2. Use a new filename.
3. Make sure the previous export file is not already open in Excel.
4. Run the export again.

If the export succeeds, the app offers to open the workbook immediately.

## `Generate CAD` Says To Run `Select Components` First

Typical signs:

- a dialog says `Please run "Select Components" first`
- CAD generation does not start

Most likely cause:

- no successful design recommendation exists yet

What to do:

1. Run `Select Components`.
2. Confirm the component table is populated.
3. Start `Generate CAD` again.

CAD generation depends on the current selected components. It cannot run from mission inputs alone.

## CAD Generation Fails

Typical signs:

- the app says `CAD generation failed`
- the CAD progress flow starts but ends with an error

Most likely causes:

- SolidWorks is not available or not usable on the machine
- the CAD pipeline could not initialize correctly
- the selected design was not ready for CAD generation

What to do:

1. Confirm you already completed a successful design run.
2. Try again with the same selected design once.
3. If the same error returns immediately, treat it as a SolidWorks-side or CAD-setup issue.
4. Use Excel export if you only need the design recommendation and cannot wait for CAD setup to be fixed.

For users, the practical rule is simple: CAD generation is optional. If it fails, the main design workflow can still be useful.

## SolidWorks Does Not Connect

Typical signs:

- the app says SolidWorks is not connected
- CAD initialization fails before part generation begins

What to do:

1. Make sure SolidWorks is installed on the machine if you intend to use CAD generation.
2. Close and reopen Drone Designer.
3. Try `Generate CAD` again.
4. If it still fails, continue the project with Excel export and pass the CAD problem to whoever maintains the SolidWorks environment.

SolidWorks is not required for component selection or Excel export.

## I Cancelled CAD Generation

If you cancel the CAD process intentionally, that is not the same as a design failure.

What to do:

- review whether the output folder contains any partial results
- restart CAD generation only when you are ready to let it finish

## FAQ

### Do I need SolidWorks to use Drone Designer?

No. SolidWorks is only needed for the optional CAD-generation workflow.

### Can I use the app without editing config files?

Yes. The normal MVP operator path assumes the packaged release should work without manual config editing.

### Why does changing one requirement affect many components?

Because a design run is coupled. Endurance, payload, takeoff weight, airframe type, and environment all influence the design recommendation together.

### Why does the app reject my mission even though the numbers look reasonable?

The mission may still be beyond the currently available component database or may violate one of the app's input checks. Reduce the mission to a simpler valid case first, then tighten it gradually.

### When should I stop troubleshooting and ask for maintainer help?

Ask for maintainer help when:

- the component database cannot be loaded even after re-extracting the release
- Excel export repeatedly fails in writable folders
- CAD generation consistently fails on a machine that is supposed to support SolidWorks
- the app reports unexpected errors instead of clear operator messages
