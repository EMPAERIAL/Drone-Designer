# Selection Engine Pipeline And Rationale

This page documents the selection engine as it exists on `master` today. It is the deeper companion to [Selection Engine Overview](selection-engine-overview.md): the overview explains subsystem boundaries, while this page explains the implemented pipeline, the main heuristics, and the reasoning encoded in the current source.

The current engine is implemented primarily in [`Core/Services/ComponentSelectionEngine.vb`](../../../Core/Services/ComponentSelectionEngine.vb). It is still heuristic-heavy, but it is no longer the older motor-first selector described in some legacy notes. The live path is now:

1. validate `MissionSpecs`
2. derive thrust demand from `MaxTakeoffMassGrams`
3. compute frame-constrained propeller candidates
4. evaluate each propeller with `Ct`/`Cp` aerodynamic equations
5. choose the first propeller and motor pairing that passes the hard filters
6. compute a phased power budget
7. choose battery, ESC, and PDB candidates
8. choose avionics and communications components
9. return one `SelectionResult` plus warnings and intermediate budgets

## Entry Point And Main Data Flow

`SelectComponents()` is the live orchestration method.

Important current behavior:

- It uses `specs.MaxTakeoffMassGrams` directly as the design MTOW for the main path.
- It still keeps the older iterative `EstimateMtow()` implementation in the file, but that path is marked obsolete and is not the primary selector flow.
- It is prop-first, not motor-first.
- It returns a single winning propulsion combination rather than a globally ranked set of all feasible combinations.

In practical terms, the pipeline is:

1. Normalize motor count and compute the thrust-to-weight requirement.
2. Convert frame size into a maximum prop diameter using configuration-specific geometry.
3. Compute worst-case air density from mission altitude and max operating temperature.
4. Build a ranked list of prop candidates with required hover RPM, hover power, torque, and per-cell KV targets.
5. Walk those prop candidates in order and, for each candidate, try `3S`, `4S`, `6S`, then `8S`.
6. For each prop-plus-cell combination, find the best passing motor.
7. Stop on the first passing prop-plus-motor combination.
8. Use that combination to compute the power budget, then size the rest of the electrical and avionics stack.

That early-exit behavior is one of the most important implementation facts for maintainers. The engine is a feasibility-first selector with a local ranking policy, not an exhaustive optimizer.

## Mission Inputs That Actually Drive The Pipeline

The main contract is [`Core/Models/MissionSpecs.vb`](../../../Core/Models/MissionSpecs.vb). In the current path, the highest-impact fields are:

- `MaxTakeoffMassGrams`
- `FrameSizeMm`
- `MotorCount`
- `Configuration`
- `MissionProfile`
- `OperatingEnvironment`
- `MaxAltitudeMeters`
- `MaxOperatingTempCelsius`
- `CruiseSpeedMs`
- `FlightEnduranceMinutes`
- `HoverTimeFraction`, `ClimbTimeFraction`, and `CruiseTimeFraction`
- `ClimbRateMs`
- `SizingPolicy`

The UI assembles those fields in [`UI/Forms/MainForm.Logic.vb`](../../../UI/Forms/MainForm.Logic.vb), including the duplicated UI-facing and engine-facing mission and environment enums. If the form stops populating one side of that alias pair, the engine behavior will drift even if the UI still looks correct.

## Step 1: Validation

The engine begins with `ValidateMissionSpecs(specs)`.

This is not the only validation layer in the repo. The form also performs validation before the engine is called. The engine-side pass exists to protect service callers and to enforce rules that the selector depends on, such as:

- coherent mission-phase fractions
- valid numeric ranges
- a usable motor count
- a physically meaningful frame and MTOW input

That split validation model is why maintainers should treat `MainForm.Logic.vb` and `ComponentSelectionEngine.vb` as a pair when changing mission inputs.

## Step 2: Thrust Requirement

The live selector computes required per-motor thrust from:

`thrustPerMotorGf = MaxTakeoffMassGrams * thrustToWeightRatio / motorCount`

Current default behavior:

- baseline TWR is `2.0`
- `MissionSpecs.TargetThrustToWeightRatio` can override it
- `NormaliseMotorCount()` maps the UI input onto the engine-supported counts

The rationale is still pragmatic rather than aircraft-specific. The comments describe `2.0` as a stable baseline for multirotors with moderate agility, while allowing more aggressive builds to raise the ratio.

## Step 3: Frame-Constrained Propeller Envelope

The propeller search space is constrained by:

- `FrameSizeMm`
- `Configuration`
- `GetPropToArmRatio()`

The code does not use one fixed prop-clearance heuristic for every airframe. Instead it maps configuration to a geometry ratio:

- quadcopter and tricopter: about `1.27`
- hexacopter: `0.85`
- octocopter: `0.65`
- fixed-wing, VTOL, helicopter: allowed but explicitly warned as inaccurate

The result is a maximum prop diameter in inches derived from frame arm length. This is a hard upstream limit for `BuildPropCandidates()`.

The comments in `GetPropToArmRatio()` are also the clearest source for the repo's current geometric rationale. They explain why the chosen ratios intentionally leave tip-clearance margin instead of letting the selector inflate into oversized props.

## Step 4: Air Density And Propeller Aerodynamics

The current propeller evaluation path is grounded in explicit `Ct` and `Cp` fields on [`PropellerSpec`](../../../Core/Models/ComponentSpecs.vb).

`AirDensityKgM3()` applies a worst-case atmospheric correction using:

- `specs.MaxAltitudeMeters`
- `specs.MaxOperatingTempCelsius`

The aerodynamic evaluation then uses `ComputePropellerAero()` to derive:

- hover RPM
- hover shaft power
- propeller torque

The governing equations are the dimensionless thrust and power forms already referenced in repo notes and the FAST-UAV migration plan:

- `T = Ct * rho * n^2 * D^4`
- `P = Cp * rho * n^3 * D^5`
- `Q = P / (2 * pi * n)`

That means the current selector no longer depends on the older static-thrust square-root scaling path for hover RPM. It now treats `CtStatic` and `CpStatic` as first-class catalogue data, with model-level fallbacks when JSON entries omit them.

Inline research trail already named in the repo:

- FAST-UAV is the main upstream reference the plan cites for `Ct`/`Cp` catalogue use and safety-margin framing.
- `MissionSpecs.vb` also names Yang et al. (`Aerospace 2024`) around mission-phase energy assumptions.

## Step 5: Prop Candidate Construction

`BuildPropCandidates()` is where the prop-first pipeline becomes concrete.

For each propeller in the repository, it:

1. rejects anything above the frame-constrained diameter
2. computes hover RPM, hover power, and torque at the mission thrust requirement
3. rejects props that need more RPM than the repository's overall motor ceiling
4. rejects props that exceed their own `MaxRPM`
5. builds a `CellKvTable` for `3S`, `4S`, `6S`, and `8S`

Current ranking rule:

- racing missions sort by smaller diameter first
- non-racing missions sort by larger diameter first

That ranking matters because the later propulsion-selection step returns the first passing combination. For non-racing builds, the engine is intentionally biased toward larger-diameter props that can meet the required thrust at lower RPM.

## Step 6: Motor Selection For A Prop Candidate

`SelectMotorForCandidate()` applies the hard gates for one prop-plus-cell combination.

Hard filters:

1. `MaxThrustGrams` must meet required thrust
2. nominal pack voltage must fit the motor voltage range
3. `MaxTorqueNm` must cover prop hover torque times `SizingPolicy.KMotorTorque`
4. prop diameter must fit the motor's supported prop range
5. shaft and bore fit must stay between `0.0` and `0.5 mm`

The key implemented change versus older notes is the torque gate. KV is still used, but it is now a soft ranking signal rather than the primary pass/fail rule.

Ranking order among passing motors:

1. torque headroom closeness
2. KV closeness
3. efficiency descending
4. mass ascending

This aligns with the repo's current `SizingPolicy` model and with the FAST-UAV-inspired shift from pure KV heuristics toward a torque-margin view of propulsion sizing.

## Step 7: Propeller And Motor Pairing

`SelectPropellerAndMotor()` loops:

1. prop candidates in ranked order
2. candidate cell counts in ascending order
3. motors ranked for that exact prop-plus-cell combination

The first successful pairing wins.

This is the single biggest selection-policy shortcut in the current engine. It keeps the implementation simple and fast, but it also means:

- the recommendation is not globally optimal across all valid combinations
- early prop ordering strongly influences the final answer
- feasible smaller-prop or higher-cell alternatives may never be compared once an earlier pairing passes

The older `docs/calibrated-survey-quad-selection-trace.md` remains useful mainly because it captures this first-success behavior clearly, even though some surrounding details in that note are now stale.

## Step 8: Power Budget

`CalculatePowerBudget()` uses the chosen propulsion pair to build `PowerBudget`.

Important current behavior:

- cell count is derived again through `DetermineCellCount(refMotor, specs)`
- peak motor current comes from `MaxCurrentAmps`, then falls back to `MaxPowerW / V`, then a thrust-based estimate
- hover current uses a momentum-theory-inspired thrust-ratio scaling
- total peak and average current add a flat avionics estimate before real avionics are selected

The battery-energy part of the calculation is no longer single-phase. The engine now uses `CalculateMissionEnergy()` to split mission energy across:

- hover
- climb
- cruise
- avionics

The implementation uses:

- induced velocity from actuator-disc theory
- an axial climb correction
- a simplified cruise-power scaling
- `SizingPolicy.KBatteryCapacity` as the final energy margin

The code comments explicitly tie this phase-based model back to mission-phase literature already named in the repo, especially Yang et al. (`Aerospace 2024`). `MissionSpecs.vb` also names Bershadsky et al. (`AIAA 2016-0581`) and Stolz et al. (`AIAA 2018-1009`) around mission-segment and climb modeling.

## Step 9: Battery Selection

The live battery selector is still mass-budget-first.

`SelectBatteryFromMassBudget()` computes the battery mass budget by subtracting:

- airframe mass
- motor mass for all motors
- prop mass for all motors
- payload mass

from `MaxTakeoffMassGrams`.

Hard filters:

- exact cell-count match
- battery mass within budget
- discharge current above `peakSystemCurrentA * CRatingHeadroomFactor`

Then it chooses the highest-capacity passing battery.

That has one important consequence: the engine computes a detailed `PowerBudget.RequiredCapacityMah`, but the actual battery selection method is still constrained first by mass and discharge feasibility. If a future maintainer wants the battery recommendation to follow the budget more directly, this is one of the first places to inspect.

## Step 10: ESC And PDB Selection

The electrical downstream path is straightforward:

- `SelectEscs()` sizes ESCs from motor current and pack voltage
- `SelectPdb()` sizes the power board from total peak current, pack voltage, and ESC pad count

These steps are downstream of the propulsion decision. They do not revisit the propulsion choice if the result becomes electrically awkward. That is another reason the engine should be understood as a linear pipeline rather than a full-system optimizer.

## Step 11: Avionics And Communications Selection

After the power budget, the engine builds `AvionicsBudget` and chooses:

- flight controller
- GPS module
- telemetry radio
- receiver
- camera

The selectors use mission-profile and environment-specific rules, but the design remains intentionally simple:

- the power budget starts from a flat avionics estimate
- actual avionics current is tallied only after component selection
- `CurrentDeltaA` is exposed to warn when the selected avionics drift materially from the estimate

The selectors are therefore useful and grounded, but they are not coupled into a second-pass propulsion or battery recalculation.

## Outputs And Diagnostics

The final `SelectionResult` exposes more than just the recommended components.

It includes:

- `Warnings`
- propulsion selections
- `PowerBudget`
- `AvionicsBudget`
- downstream electrical and avionics selections
- `MtowIterationHistory`, even though the main live path now uses direct MTOW input

That output shape is deliberate. The UI, export path, and maintainer diagnostics all depend on the engine returning its intermediate calculations, not just the final choices.

## What Changed Relative To Legacy Deep Dives

Older repo notes still contain useful reasoning, but maintainers should read them as source material, not as the authority on current behavior.

The main deltas are:

- the live selector is prop-first, not motor-first
- `Ct` and `Cp` are implemented in the repository model and used in the live propeller calculations
- motor gating now includes torque headroom through `SizingPolicy`
- mission energy is phase-based rather than a single hover-only energy estimate
- `MaxTakeoffMassGrams` drives the live selection path directly

That is why this doc should be treated as the maintainer-facing ground truth unless code changes after it.

## Practical Debugging Guidance

When a design recommendation looks wrong, debug in this order:

1. Check `BuildMissionSpecs()` to confirm the UI is populating the intended engine-facing fields.
2. Check `BuildPropCandidates()` to see whether the right propellers were rejected too early.
3. Check `SelectMotorForCandidate()` for torque, voltage, prop-range, and shaft-fit rejections.
4. Check `CalculatePowerBudget()` and `SelectBatteryFromMassBudget()` for battery-feasibility mismatches.
5. Check the avionics selectors only after the propulsion and power path looks sane.

That path mirrors the actual execution order and is usually faster than starting from the final warning text alone.

## Primary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Models/MissionSpecs.vb`
- `Core/Models/ComponentSpecs.vb`
- `Core/Models/SizingPolicy.vb`
- `UI/Forms/MainForm.Logic.vb`

## Secondary Related Files

- `Core/Data/ComponentRepository.vb`
- `UI/Forms/MainForm.vb`
- `UI/Forms/MainForm.Export.vb`
- `UI/Forms/MainForm.CAD.vb`
- `Resources/AppData/components.json`
- `docs/calibrated-survey-quad-selection-trace.md`
- `docs/PLAN.md`
