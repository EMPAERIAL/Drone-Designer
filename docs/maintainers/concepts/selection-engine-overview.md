# Selection Engine Overview

This page explains what the selection engine owns, what it depends on, and where its boundaries stop.

In the current repository, the selection engine is the logic that turns `MissionSpecs` plus the component database into a ranked design recommendation. Its main implementation lives in [`Core/Services/ComponentSelectionEngine.vb`](../../../Core/Services/ComponentSelectionEngine.vb).

## What The Selection Engine Owns

The engine owns the design-decision path between mission requirements and a candidate component set.

That includes:

- validating the minimum engine-side assumptions on `MissionSpecs`
- estimating MTOW and thrust requirements
- selecting propulsion components
- selecting power-system components
- selecting avionics and communications components
- computing derived values such as required thrust per motor and power-budget figures
- attaching warnings to the result when the recommendation is marginal or infeasible

The engine returns a `SelectionResult`, not a rendered UI or exported artifact.

One current implementation detail matters enough to call out here: the engine is prop-first and first-success, not an exhaustive optimizer. `BuildPropCandidates()` ranks propellers, `SelectPropellerAndMotor()` tries cell counts in ascending order for each candidate, and the first passing propeller-motor-cell combination wins. That means ordering heuristics materially affect the final recommendation.

## What The Selection Engine Does Not Own

The engine does not own:

- WinForms control layout or event wiring
- the main user-facing validation experience on the form
- direct file-path resolution for the component database
- Excel export formatting
- SolidWorks connection or macro execution
- long-running UI progress reporting

Those responsibilities sit in adjacent subsystems:

- UI mapping and presentation: `UI/Forms/MainForm*.vb`
- repository and config: `Core/Data/ComponentRepository.vb`, `Utilities/ConfigManager.vb`
- export: `UI/Forms/MainForm.Export.vb`
- CAD orchestration: `UI/Forms/MainForm.CAD.vb`, `Core/Services/PipelineOrchestrator.vb`

## Inputs To The Engine

The engine relies on three categories of input.

## Mission Inputs

The direct structured input is [`Core/Models/MissionSpecs.vb`](../../../Core/Models/MissionSpecs.vb).

Important fields the engine reads include:

- mission profile and operating environment
- endurance, range, cruise speed, and altitude
- motor count and configuration
- payload mass
- sizing-policy factors and mission-phase fractions

The practical boundary here is important: the engine expects `MissionSpecs` to already be assembled coherently. The form currently performs much of that assembly work in `BuildMissionSpecs()`.

## Component Candidates

The engine does not parse JSON directly. It consumes typed candidates through [`Core/Data/ComponentRepository.vb`](../../../Core/Data/ComponentRepository.vb).

That repository supplies:

- motors
- propellers
- batteries
- ESCs
- PDBs
- flight controllers
- GPS modules
- telemetry radios
- receivers
- cameras

This keeps the engine focused on selection heuristics rather than file-format handling.

## Internal Constants And Heuristics

Many of the engine's decisions are still encoded as constants and helper logic inside `ComponentSelectionEngine.vb`.

Examples include:

- MTOW safety factor
- fallback airframe-mass tables
- thrust-to-weight assumptions
- prop-to-arm sizing ratios
- battery depth-of-discharge and headroom factors
- link-range safety margins

This means the engine is both a pipeline and a policy surface. Many behavior changes happen by changing heuristics, not by adding new architecture.

## Outputs From The Engine

The main output is `SelectionResult`, defined in the same service file.

It carries:

- selected component lists by category
- estimated MTOW
- required thrust per motor
- MTOW iteration history
- power-budget data
- warnings and severe warnings

The UI consumes this result to populate the grid and summary view. The CAD pipeline can also consume it later through `RunFromSelectionAsync()`.

## High-Level Internal Pipeline

At a high level, the engine currently runs in this order:

1. validate the incoming mission specs
2. derive thrust requirements from the current MTOW input
3. build and rank propeller candidates
4. select the first passing propeller and motor combination
5. build the power budget and choose battery, ESC, and PDB candidates
6. build the avionics budget and choose flight controller, GPS, telemetry, receiver, and camera candidates
7. compute range and endurance warnings
8. assemble the final `SelectionResult`

The file still contains an older iterative `EstimateMtow()` implementation for reference, but the main live path now uses `MaxTakeoffMassGrams` directly when running the selector. The detailed heuristic rationale belongs in the deeper pipeline doc. This overview is about subsystem ownership and navigation.

## Boundary With The UI

The UI and engine are adjacent but not interchangeable.

The UI owns:

- reading controls
- converting operator-friendly values into engine units
- top-level input validation messages
- choosing when to run the engine
- presenting the result and status messages

The engine owns:

- candidate filtering
- sizing calculations
- ranking and choosing component sets
- warning generation tied to design feasibility

If a change affects what the operator can enter, the work usually starts in the UI and ends in the engine-facing model contract. If a change affects how the recommendation is computed, the work usually starts in the engine and then propagates outward to presentation or docs.

## Boundary With The Repository

`ComponentRepository` is upstream of the engine, not part of it.

The repository owns:

- deserialization
- category indexing
- typed accessors over the component catalogue

The engine owns:

- interpreting those candidates against mission constraints
- deciding which candidates survive and in what order

This is why data-shape bugs and selection-logic bugs should be investigated separately, even when they surface in the same design run.

## Boundary With Outputs

The engine produces a design recommendation, not a final user artifact.

- Excel export reads the current form state and the last `SelectionResult`.
- CAD generation reuses an existing `SelectionResult` and then enters a separate SolidWorks pipeline.

That distinction matters because downstream failures do not necessarily imply engine failures.

## Practical Change Guidance

When you need to change the engine, first decide which kind of change it is:

- input-contract change: inspect `MissionSpecs.vb` and `MainForm.Logic.vb`
- candidate-data change: inspect `ComponentRepository.vb` and `components.json`
- selection-rule change: inspect `ComponentSelectionEngine.vb`
- result-shape or UI-display change: inspect `ComponentSelectionEngine.vb`, `SelectionResult`, and `ComponentDisplayRow.vb`

This reduces the chance of editing a neighboring subsystem when the real bug is elsewhere.

## Primary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Models/MissionSpecs.vb`
- `Core/Data/ComponentRepository.vb`
- `Core/Interfaces/IComponentSelector.vb`
- `UI/Forms/MainForm.Logic.vb`

## Secondary Related Files

- `Core/Models/ComponentSpecs.vb`
- `Core/Models/ComponentDisplayRow.vb`
- `Core/Models/SizingPolicy.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Resources/AppData/components.json`
