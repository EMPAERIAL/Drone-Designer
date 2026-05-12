# Known Limitations And Risks

This page collects maintainer-facing sharp edges in the current repository. It is not a user troubleshooting page. The goal is to make existing risks visible before a maintainer deepens them by accident.

## Selection Engine Scope Is Narrower Than The UI Suggests

The UI allows multiple airframe and mission shapes, but the engine still carries strong multirotor assumptions.

Current evidence in code:

- [`Core/Services/ComponentSelectionEngine.vb`](../../Core/Services/ComponentSelectionEngine.vb) adds an explicit warning that fixed-wing, VTOL, and helicopter sizing may be inaccurate.
- The MTOW and hover-power comments are still centered on multirotor-style assumptions.
- The CAD pipeline is driven from selected motors and multirotor-style frame parts.

Risk:

- a maintainer may add UI options or doc claims that imply broader support than the engine can actually deliver
- non-multirotor behavior may look superficially valid while still being wrong in sizing details

## UI And Engine Contracts Are Coupled Through Alias Properties

`MissionSpecs` currently carries both legacy UI-facing properties and engine-facing alias properties.

Examples:

- `Profile` and `MissionProfile`
- `Environment` and `OperatingEnvironment`
- `PayloadMassGrams` and `PayloadWeightGrams`
- `MaxRangeKm` and `RangeKm`

[`UI/Forms/MainForm.Logic.vb`](../../UI/Forms/MainForm.Logic.vb) assigns both sets during `BuildMissionSpecs()`, and [`Core/Models/MissionSpecs.vb`](../../Core/Models/MissionSpecs.vb) documents that this duplication exists to keep the current engine working.

Risk:

- a maintainer can update one side of the contract and forget the other
- renames in the UI or engine can silently drift until a runtime path exposes the mismatch

This is one of the highest-friction navigation points in the current codebase.

## Repository Category Names Are Inconsistent

`components.json`, deserialization, and typed category accessors do not use one stable category vocabulary.

Examples:

- the JSON uses `FlightController`, `GpsModule`, and `PowerDistributionBoard`
- deserialization in [`Core/Data/ComponentRepository.vb`](../../Core/Data/ComponentRepository.vb) handles normalized forms such as `flightcontroller`, `gpsmodule`, and `powerdistributionboard`
- typed lookup helpers call `GetAllByCategory("flight_controller")`, `GetAllByCategory("gps")`, and `GetAllByCategory("pdb")`

Risk:

- future repository changes can accidentally bypass typed accessors even when the raw JSON still deserializes
- maintainers may assume the lookup vocabulary is stable when it is actually split across several naming conventions

Any schema or data cleanup work should treat category naming as a migration-sensitive area.

## Validation Is Split And Still Incomplete

The app does not have one central validation layer.

Current state:

- `MainForm.Logic.ValidateFormInputs()` performs user-facing validation before the design run
- `ComponentSelectionEngine.ValidateMissionSpecs()` performs a narrower service-side validation pass
- `MissionSpecs.vb` comments repeatedly refer to a future `SpecValidator` that does not exist yet

Risk:

- the UI and engine can disagree about what is valid
- non-UI callers can bypass checks the form currently performs
- maintainers may add new mission fields without updating all validation touchpoints

This is especially important when changing mission inputs, defaults, or sizing-policy behavior.

## CAD Generation Depends On Precompiled SolidWorks Macros

The CAD pipeline expects compiled `.swp` macro files for several parts, while the repository currently stores mostly `.swb` sources under `Resources/SolidWorks/Macros/`.

[`Core/Services/PipelineOrchestrator.vb`](../../Core/Services/PipelineOrchestrator.vb) explicitly points to `.swp` paths such as:

- `CarbonFiberArm.swp`
- `ArmChassisConnection.swp`
- `ChassisPlates.swp`
- `LandingGearConnection.swp`
- `LandingGearTube.swp`

Only some `.swp` files are present in the repo snapshot.

Risk:

- CAD generation can fail late even when the design recommendation path worked
- maintainers may think the templates are enough, when the compiled macro artifacts are also required
- environment-specific SolidWorks setup issues can look like code regressions

This pipeline should be treated as runtime-fragile until macro assets and setup steps are normalized.

## Export And CAD Actions Depend On Session State

The export and CAD workflows are not independent of the current UI session.

Current behavior:

- Excel export reads `_lastResult` and the current form controls
- CAD generation also depends on `_lastResult`, then rebuilds `MissionSpecs` from the current form state before running `PipelineOrchestrator`

Risk:

- a maintainer can accidentally create drift between the displayed recommendation and the mission inputs later read for export or CAD
- changes that clear, mutate, or partially rebuild form state can break downstream actions without touching the engine itself

When debugging export or CAD issues, inspect session-state assumptions before blaming the engine.

## Display Mapping Still Carries TODO-Level Assumptions

[`Core/Models/ComponentDisplayRow.vb`](../../Core/Models/ComponentDisplayRow.vb) still contains explicit TODO notes about property-name assumptions and formatting behavior.

Risk:

- display regressions can happen when component-model properties change
- a maintainer may trust the grid mapping as stable even though the file itself says parts of the bridge were inferred and should be verified

This is a presentation-layer risk, not a core sizing risk, but it affects user trust immediately.

## Automated Verification Is Thin

The repository has scenario and harness support, but it is still light relative to the amount of heuristic logic in the app.

Current evidence:

- [`UI/Forms/MainForm.Tests.vb`](../../UI/Forms/MainForm.Tests.vb) provides an in-app harness driven by keyboard shortcuts and scenario CSV data
- [`Test/README.md`](../../Test/README.md) describes scenarios and generated logs
- there is no obvious independent automated regression suite covering the full selection pipeline, export path, and CAD orchestration together

Risk:

- heuristic changes can ship without strong regression signals
- user-doc or maintainer-doc claims may drift from actual behavior because runtime validation is manual and path-dependent

Maintainers should plan manual validation deliberately after engine, export, or CAD changes.

## Documentation Migration Is Still In Progress

Several new-tree destinations are still placeholders outside the docs completed in this batch.

Risk:

- maintainers may rely on incomplete workflow or reference pages
- useful facts may still live in older deep-dive notes instead of the intended destination doc

Treat the new docs tree as the preferred entry point, but verify whether the destination page is complete before assuming the migration is finished.

## Primary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Data/ComponentRepository.vb`
- `Core/Models/MissionSpecs.vb`
- `UI/Forms/MainForm.Logic.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Core/Models/ComponentDisplayRow.vb`

## Secondary Related Files

- `UI/Forms/MainForm.CAD.vb`
- `UI/Forms/MainForm.Export.vb`
- `Resources/AppData/components.json`
- `Resources/SolidWorks/Macros/`
- `Test/README.md`
- `UI/Forms/MainForm.Tests.vb`
