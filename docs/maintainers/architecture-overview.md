# Architecture Overview

This page explains how the current Drone Designer repository fits together as a working desktop application. It is written for maintainers who need a fast mental model before changing code.

Drone Designer is a WinForms desktop app that does two main jobs:

- turn mission inputs into a design recommendation through the selection engine
- turn a selected design into export artifacts and optional SolidWorks-generated CAD parts

The codebase is organized more by subsystem than by strict layers, but the runtime shape is still clear enough to navigate if you start from the right files.

## Runtime Shape

At runtime, the app is a single Windows process with these major responsibilities:

1. build and display the main form
2. load configuration and the component database
3. map UI controls into `MissionSpecs`
4. run `ComponentSelectionEngine` to produce a `SelectionResult`
5. display the recommendation and allow export
6. optionally run `PipelineOrchestrator` to drive SolidWorks-based CAD generation

There is no web tier, service boundary, or separate worker process in the normal application path. The UI, engine, export logic, and CAD orchestration all live in the same executable.

## Major Subsystems

## WinForms UI

The UI lives under `UI/Forms/` and is centered on `Drone_Designer.MainForm`.

- [`UI/Forms/MainForm.vb`](../../UI/Forms/MainForm.vb) defines the layout, tabs, input groups, output grid, and summary labels.
- [`UI/Forms/MainForm.Logic.vb`](../../UI/Forms/MainForm.Logic.vb) wires the form to the repository and selection engine, validates inputs, builds `MissionSpecs`, and renders `SelectionResult`.
- [`UI/Forms/MainForm.Export.vb`](../../UI/Forms/MainForm.Export.vb) owns the Excel export path.
- [`UI/Forms/MainForm.CAD.vb`](../../UI/Forms/MainForm.CAD.vb) owns the `Generate CAD` path and delegates the long-running work to `PipelineOrchestrator`.
- [`UI/Forms/ConvergenceForm.vb`](../../UI/Forms/ConvergenceForm.vb) visualizes MTOW iteration history.
- [`UI/Forms/CadProgressForm.vb`](../../UI/Forms/CadProgressForm.vb) shows progress for the CAD pipeline.

The main architectural fact to keep in mind is that the UI is not a thin shell. It performs real coordination work:

- control-to-model mapping
- user-facing validation
- display shaping through `ComponentDisplayRow`
- export and CAD entry-point decisions

That means changes to user-facing behavior often span both UI files and core model or service files.

## Core Models

The core data types live under `Core/Models/`.

- [`Core/Models/MissionSpecs.vb`](../../Core/Models/MissionSpecs.vb) is the main input contract for a design run.
- [`Core/Models/ComponentSpecs.vb`](../../Core/Models/ComponentSpecs.vb) and its derived spec types model the component database entries.
- [`Core/Models/ComponentDisplayRow.vb`](../../Core/Models/ComponentDisplayRow.vb) adapts component specs into rows for the output grid.
- [`Core/Models/PipelineResult.vb`](../../Core/Models/PipelineResult.vb) carries the CAD-generation outcome.
- [`Core/Models/SizingPolicy.vb`](../../Core/Models/SizingPolicy.vb) holds sizing-margin inputs that influence the engine.

These models are shared across the UI, engine, export path, and CAD orchestration. They are the closest thing this repo has to stable application contracts.

## Component Database And Repository

The component database is JSON-backed.

- [`Resources/AppData/components.json`](../../Resources/AppData/components.json) is the live catalogue the app loads.
- [`Core/Data/ComponentRepository.vb`](../../Core/Data/ComponentRepository.vb) deserializes that JSON into typed component models and exposes category-specific query methods.
- [`Utilities/ConfigManager.vb`](../../Utilities/ConfigManager.vb) resolves the configured path to the component database relative to the executable directory.

The repository is read-only in the application path. It is a lookup layer, not a persistence workflow.

## Selection Engine

The selection engine lives mainly in [`Core/Services/ComponentSelectionEngine.vb`](../../Core/Services/ComponentSelectionEngine.vb).

It owns the mission-to-design transformation:

- estimate MTOW
- derive thrust and power requirements
- choose motors, propellers, batteries, ESCs, and PDBs
- choose avionics and communications components
- compute warnings and derived summary values
- return a `SelectionResult`

The engine is invoked directly from the main form and indirectly from the CAD pipeline.

## Export Path

The normal operator export path is implemented in [`UI/Forms/MainForm.Export.vb`](../../UI/Forms/MainForm.Export.vb).

It writes an `.xlsx` workbook directly using `System.IO.Compression` and OOXML XML generation. There is no external Excel dependency and no separate report service.

## CAD Generation Path

The CAD path is a second pipeline layered on top of a completed design run.

- [`UI/Forms/MainForm.CAD.vb`](../../UI/Forms/MainForm.CAD.vb) validates UI state and launches the pipeline.
- [`Core/Services/PipelineOrchestrator.vb`](../../Core/Services/PipelineOrchestrator.vb) coordinates selection reuse, SolidWorks connection, macro execution, output-file saving, and manifest writing.
- `Solidworks/` contains the SolidWorks automation helpers and macro runner used by the orchestrator.
- `Resources/SolidWorks/` holds template parts and macros consumed by that pipeline.

This path is optional from the operator perspective but architecturally important because it pulls UI state, engine outputs, config paths, COM automation, and template assets into one workflow.

## End-To-End Flows

## Flow 1: Design Recommendation

The normal design flow is:

1. `MainForm` collects mission inputs.
2. `MainForm.Logic.BuildMissionSpecs()` maps controls into `MissionSpecs`.
3. `ComponentRepository` loads available components from `components.json`.
4. `ComponentSelectionEngine.SelectComponents()` computes a `SelectionResult`.
5. `MainForm.Logic.DisplaySelectionResult()` converts the result into grid rows and summary labels.
6. `ConvergenceForm` may open to show MTOW iteration history.

This is the primary app workflow. Most user-visible changes eventually touch this path.

## Flow 2: Excel Export

After a successful design run:

1. `MainForm.Export.OnExportExcel()` checks `_lastResult`.
2. The current form state and result state are serialized into two workbook sheets.
3. The workbook is written directly to a user-selected `.xlsx` path.

The export path depends on the design flow but does not rerun the selection engine.

## Flow 3: CAD Generation

After a successful design run:

1. `MainForm.CAD.btnGenerateCAD_Click()` checks `_lastResult` and gathers an output directory.
2. The form rebuilds `MissionSpecs` from current UI state.
3. `PipelineOrchestrator.RunFromSelectionAsync()` wraps the existing `SelectionResult` and reuses the same pipeline infrastructure as a fresh run.
4. The orchestrator connects to SolidWorks, runs macros against template parts, writes generated files, and emits a manifest.

This flow depends on the design recommendation already existing in memory. It is not a standalone CAD app.

## Architectural Boundaries

The most important boundaries in the current codebase are:

- UI versus engine: the UI decides how inputs are collected and displayed, while the engine decides how components are sized and selected.
- repository versus engine: `ComponentRepository` serves typed component candidates, while the engine applies heuristics and constraints.
- design recommendation versus downstream outputs: export and CAD generation consume a completed `SelectionResult`; they do not own selection logic.
- configuration versus code: path resolution and runtime asset locations come from `ConfigManager`, but most feature logic is hard-coded in the services and forms.

The boundaries are real, but they are not perfectly sealed. The UI still owns meaningful validation and mapping logic, and the engine still assumes some UI-era model conventions such as dual mission-profile and environment properties on `MissionSpecs`.

## Practical Navigation Advice

When a maintainer needs to understand behavior quickly, start from the user action and walk inward:

- input or validation problem: `MainForm.vb` and `MainForm.Logic.vb`
- selection output problem: `MainForm.Logic.vb` and `ComponentSelectionEngine.vb`
- missing or incorrect catalogue data: `ComponentRepository.vb` and `components.json`
- export problem: `MainForm.Export.vb`
- CAD-generation problem: `MainForm.CAD.vb`, `PipelineOrchestrator.vb`, and `Solidworks/`

That route is usually faster than reading the repo top-down.

## Primary Related Files

- `UI/Forms/MainForm.vb`
- `UI/Forms/MainForm.Logic.vb`
- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Data/ComponentRepository.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Utilities/ConfigManager.vb`

## Secondary Related Files

- `UI/Forms/MainForm.Export.vb`
- `UI/Forms/MainForm.CAD.vb`
- `UI/Forms/ConvergenceForm.vb`
- `UI/Forms/CadProgressForm.vb`
- `Core/Models/MissionSpecs.vb`
- `Core/Models/ComponentSpecs.vb`
- `Core/Models/ComponentDisplayRow.vb`
- `Resources/AppData/components.json`
