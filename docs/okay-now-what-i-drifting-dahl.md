# Replace `SelectComponents` body with a frame-first / propeller-first pipeline

## Context

The current `SelectComponents` in `Core/Services/ComponentSelectionEngine.vb` (line 369) runs a multi-stage pipeline that begins with an iterative MTOW estimator (`EstimateMtow`) and derives the prop-diameter target from disk loading. It does not consume the user's frame size or design MTOW directly, and it does not check whether the resulting battery can actually meet the requested endurance and range.

The user wants the body of `SelectComponents` to be replaced by a **frame-first / propeller-first** pipeline that:

1. Takes the design MTOW from the user (no iteration).
2. Derives the maximum propeller diameter from the frame size and configuration.
3. Picks propellers by size and thrust capability, then computes the RPM required to deliver thrust at the mission's max altitude (ISA density correction).
4. For each propeller, builds a {3S, 4S, 6S, 8S} cell-count → KV-required table (KV = RPM / (V·η), η = 0.95).
5. Walks props (largest first) and cell counts (smallest first), matching motors by KV, cell range, RPM headroom, prop-diameter range, and shaft-bore compatibility. First match wins.
6. Computes battery mass budget (MTOW − airframe − motors − props − payload) and selects a battery by cells, mass, and C-rating.
7. Computes available flight time and travel distance, and emits green / yellow / severe warnings against the user's requested endurance and range.
8. Reuses the existing power-budget / ESC / PDB / avionics chain unchanged.

Existing helpers (`EstimateMtow`, `CalculateThrustRequirement`, `ComputeTargetPropellerDiameter`, `EstimateMaxPropDiameter`, `SelectPropellersByTargetDiameter`, `SelectMotorsForPropeller`, `SelectMotors`, `SelectPropellers`) are **kept in place but marked `<Obsolete>`** so they remain available for future reuse without polluting compile output. Downstream helpers (`CalculatePowerBudget`, `SelectEscs`, `SelectPdb`, avionics) are reused.

## User decisions locked in

- **MTOW input**: repurpose `MissionSpecs.MaxTakeoffMassGrams` as the design MTOW (update XML doc accordingly).
- **Legacy helpers**: `<Obsolete("Replaced by frame-first pipeline 2026-04. Retained for reference.")>` attribute, not commented bodies.
- **Configuration ratios**: extrapolate for all configs (see ratio table below).
- **Warnings**: status bar for green/yellow, MessageBox (yellow icon) for severe.

## Prop-diameter-to-arm-length ratio table

Computed as `MaxPropDiameter = ratio × ArmLength`, where `ArmLength = FrameSizeMm / 2`.

| Configuration | Ratio | Notes |
|---|---|---|
| Quadcopter | 2/3 | User spec |
| Hexacopter | 1/3 | User spec |
| Tricopter | 2/3 | Same arm-spacing as Quad (120° vs 90° geometry actually allows more, conservative match) |
| Octocopter | 1/4 | Extrapolated — eight motors at 45° pack tighter than hex |
| FixedWing / VTOL / Helicopter | 1/2 | Placeholder — pipeline is multirotor-focused; surface a yellow warning if selected |

Implementation: a single `Private Shared Function GetPropToArmRatio(config As UAVConfiguration) As Double`.

## Decomposition (8 build-testable tasks)

Each task ends with a successful build and, where applicable, an end-to-end smoke test of `SelectComponents` from the form. Task numbering matches the order of implementation.

### T1 — Inputs and result-shape changes

**Files**: `Core/Models/MissionSpecs.vb`, `Core/Services/ComponentSelectionEngine.vb` (only the `SelectionResult` class at line 1862), `UI/Forms/MainForm.Logic.vb`, `UI/Forms/MainForm.vb` (designer).

- Add `Public Property FrameSizeMm As Double = 250.0` to `MissionSpecs`. Document units (mm) and that `ArmLength = FrameSizeMm / 2`.
- Update XML doc on `MaxTakeoffMassGrams` to state it is now the **design MTOW** (no longer a hard ceiling). Remove the "TODO (engine-side, future task)" paragraph.
- Add a numeric input on `MainForm` (label `Frame Size (mm)`, `NumericUpDown`, range 100–1500, default 250). Bind in `BuildMissionSpecs()` — same pattern as `nudPayloadWeight` at MainForm.Logic.vb:298.
- Add `Public Property Warnings As New List(Of String)` to `SelectionResult` (line 1862 area). Surface in `DisplaySelectionResult()` later in T8.

**Build test**: project compiles, app launches, frame-size field is wired both ways (BuildMissionSpecs reads it, defaults persist on form load).

### T2 — `GetPropToArmRatio` helper

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add `Private Shared Function GetPropToArmRatio(config As UAVConfiguration) As Double` near the existing constants block (lines 65–159). Implements the ratio table above. Logs a `Warnings`-bound message via the orchestrator when an unsupported config (FixedWing/VTOL/Heli) is requested.
- Skip per-Plan-agent critique: do **not** add `ComputeMaxPropDiameterFromFrame`, `ComputeRequiredThrustPerMotorGf`, `ApplyAltitudeCorrection`, `GetGlobalMotorRpmCeiling` as separate functions — they are one-liners and inline cleanly into T3 and the orchestrator.

**Build test**: compiles cleanly, no callers yet.

### T3 — `BuildPropCandidates` (merged prop filter + RPM + KV table)

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add a small `Friend Class PropCandidate` (or `Structure`) inside the namespace, with fields:
  - `Prop As PropellerSpec`
  - `RequiredRpmAtAltitude As Double`
  - `CellKvTable As List(Of (CellCount As Integer, KvRequired As Double))`
- Add `Private Function BuildPropCandidates(specs As MissionSpecs, maxPropDiameterIn As Double, requiredThrustPerMotorGf As Double, motorRpmCeiling As Double, rejections As List(Of String)) As List(Of PropCandidate)`:
  1. Pull all props from `_repository.GetAllByCategory(ComponentCategory.Propeller)`.
  2. Filter by `DiameterInches <= maxPropDiameterIn`. Log rejected prop IDs.
  3. For each survivor: compute sea-level required RPM via the **existing** `EstimatePropellerHoverRpm(prop, requiredThrustPerMotorGf)` (line 607).
  4. Apply ISA altitude correction: `rho_ratio = (1.0 - 2.25577e-5 * specs.MaxAltitudeMeters) ^ 4.2559` ; `RequiredRpmAtAltitude = rpmSeaLevel * Math.Sqrt(1.0 / rho_ratio)`.
  5. Drop props where `RequiredRpmAtAltitude > motorRpmCeiling` or `> prop.MaxRPM`. Log.
  6. For each surviving prop, build the `CellKvTable` for cells `{3, 4, 6, 8}`: `vNominal = cells * LipoCellNominalV` ; `kvRequired = RequiredRpmAtAltitude / (vNominal * 0.95)` (η = 0.95 for back-EMF). Use existing `LipoCellNominalV = 3.7` constant (line 170).
- Order returned candidates by `Prop.DiameterInches` **descending** (largest first); invert when `specs.MissionProfile = MissionProfileType.Racing`.

**`motorRpmCeiling` source**: computed inline in the orchestrator as `_repository.GetAllByCategory(ComponentCategory.Motor).Cast(Of MotorSpec).Max(Function(m) m.KV * m.MaxVoltage)`.

**Build test**: unit-style debug call — feed sample inputs, log candidates and rejections to console, verify order and content.

### T4 — `SelectMotorForCandidate`

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add `Private Function SelectMotorForCandidate(candidate As PropCandidate, cellCount As Integer, kvRequired As Double, requiredThrustPerMotorGf As Double, rejections As List(Of String)) As MotorSpec`. Returns `Nothing` if no match.
- Hard constraints:
  - `motor.MaxThrustGrams >= requiredThrustPerMotorGf`
  - `motor.MinVoltage <= cellCount * LipoCellNominalV <= motor.MaxVoltage`
  - `motor.KV * (cellCount * LipoCellNominalV) >= candidate.RequiredRpmAtAltitude * MotorRpmHeadroomFactor` (existing constant `1.4`, line 148)
  - `candidate.Prop.DiameterInches` within `[motor.PropDiameterMinIn, motor.PropDiameterMaxIn]`
  - Shaft-bore fit: `0.0 <= candidate.Prop.BoreDiameterMm - motor.ShaftDiameterMm <= 0.5` (no oversized adapters; reject if bore < shaft).
- Soft constraint: `Abs(motor.KV - kvRequired) / kvRequired <= 0.15` (±15% KV band). If zero matches, **fall back to ±25%**. Log fall-back.
- Sort surviving motors by `Abs(KV - kvRequired)` ascending, then `Efficiency` desc, then `MassGrams` asc. Return the first.

**Build test**: debug call with one candidate, verify rejection log accuracy.

### T5 — `SelectPropellerAndMotor` orchestrator

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add `Private Function SelectPropellerAndMotor(candidates As List(Of PropCandidate), requiredThrustPerMotorGf As Double, rejections As List(Of String)) As (Prop As PropellerSpec, Motor As MotorSpec, CellCount As Integer, RequiredRpm As Double)`.
- Iterate `candidates` in order (largest first); for each, iterate its `CellKvTable` ascending by `CellCount` (3S → 8S). Call T4 for each `(candidate, cells, kvRequired)`.
- First non-`Nothing` motor wins → return tuple.
- If exhausted: throw `ComponentSelectionException` with the full `rejections` log embedded in the message (follow existing pattern at lines 658–662, 694–697).

**Build test**: full orchestrator dry run from a debug method.

### T6 — `SelectBatteryFromMassBudget`

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add `Private Function SelectBatteryFromMassBudget(mtowGrams As Double, motorCount As Integer, motor As MotorSpec, prop As PropellerSpec, payloadMassGrams As Double, cellCount As Integer, peakSystemCurrentA As Double) As BatterySpec`.
- Mass budget: `batteryMassBudgetG = mtowGrams - GetAirframeMass(motorCount) - motorCount * motor.MassGrams - motorCount * prop.MassGrams - payloadMassGrams`. (Reuses existing `GetAirframeMass`, line 1656.) Throws if budget ≤ 0 with a clear "design infeasible" message.
- Filter from `_repository.GetAllByCategory(ComponentCategory.Battery)`:
  - `battery.CellCount = cellCount`
  - `battery.MassGrams <= batteryMassBudgetG`
  - `battery.ContinuousCRating * (battery.CapacityMAh / 1000.0) >= peakSystemCurrentA * CRatingHeadroomFactor` (existing constant, line ~234)
- Pick the **maximum-capacity** survivor. If none, throw with a list of rejection reasons.

**Note on naming**: codebase has both `CapacityMah` and `CapacityMAh` in different places. Use the canonical class property name from `BatterySpec` at `ComponentSpecs.vb:911–1005` — confirm the exact casing before writing the field reference.

**Build test**: debug call with sample motor/prop/cells.

### T7 — `ComputeRangeAndEnduranceWarnings`

**File**: `Core/Services/ComponentSelectionEngine.vb`

- Add `Private Function ComputeRangeAndEnduranceWarnings(battery As BatterySpec, hoverPowerW As Double, cruiseSpeedMs As Double, specs As MissionSpecs) As (FlightMinutes As Double, RangeKm As Double, Warnings As List(Of String))`.
- Available energy `Wh = battery.CapacityMAh / 1000.0 * battery.CellCount * LipoCellNominalV * specs.BatteryMaxDepthOfDischarge * (1.0 - specs.OperationalReserveFraction)`.
- `flightHours = Wh / hoverPowerW` ; `flightMinutes = flightHours * 60` ; `rangeKm = flightHours * cruiseSpeedMs * 3.6`.
- Compare to `specs.FlightEnduranceMinutes` and `specs.MaxRangeKm`. For each: ratio = achieved / requested.
  - `ratio >= 1.0` → green: `"Endurance OK: 31.2 / 30.0 min"`.
  - `0.7 <= ratio < 1.0` → yellow: `"Endurance short: 27.5 / 30.0 min (92%)"`.
  - `ratio < 0.7` → severe: `"Endurance severely short: 18.0 / 30.0 min (60%) — design likely infeasible"`. Tag with a leading `"[SEVERE]"` so the UI can detect.

**Build test**: feed a known battery, confirm threshold buckets fire correctly.

### T8 — Wire it all together + legacy `<Obsolete>` + UI surface

**Files**: `Core/Services/ComponentSelectionEngine.vb`, `UI/Forms/MainForm.Logic.vb`.

- Replace the body of `SelectComponents` (lines 369–419) with the new pipeline. Sketch:
  ```
  ValidateMissionSpecs(specs)
  Dim mtowG = specs.MaxTakeoffMassGrams
  Dim motorCount = NormaliseMotorCount(specs.MotorCount)
  Dim twr = If(specs.TargetThrustToWeightRatio.HasValue, specs.TargetThrustToWeightRatio.Value, QuadThrustToWeightRatio)
  Dim thrustPerMotorGf = mtowG * twr / motorCount
  Dim armMm = specs.FrameSizeMm / 2.0
  Dim ratio = GetPropToArmRatio(specs.Configuration)
  Dim maxPropDiamIn = (ratio * armMm) / 25.4
  Dim motorRpmCeiling = ...                      ' inlined Max(KV * MaxVoltage) over motor DB
  Dim rejections = New List(Of String)
  Dim warnings = New List(Of String)
  Dim candidates = BuildPropCandidates(specs, maxPropDiamIn, thrustPerMotorGf, motorRpmCeiling, rejections)
  Dim picked = SelectPropellerAndMotor(candidates, thrustPerMotorGf, rejections)
  Dim motors = New List(Of ComponentSpecs) From {picked.Motor}
  Dim propellers = New List(Of ComponentSpecs) From {picked.Prop}
  Dim thrust = New ThrustRequirement With {.TotalThrustGf = thrustPerMotorGf * motorCount, .ThrustPerMotorGf = thrustPerMotorGf, .ThrustToWeightRatio = twr, .MotorCount = motorCount}
  Dim power = CalculatePowerBudget(motors, thrust, specs)
  Dim battery = SelectBatteryFromMassBudget(mtowG, motorCount, picked.Motor, picked.Prop, specs.PayloadMassGrams, picked.CellCount, power.TotalPeakCurrentA)
  Dim batteries = New List(Of ComponentSpecs) From {battery}
  Dim rangeReport = ComputeRangeAndEnduranceWarnings(battery, power.HoverSystemPowerW, specs.CruiseSpeedMs, specs)
  warnings.AddRange(rangeReport.Warnings)
  Dim escs = SelectEscs(motors, power, specs)
  Dim pdbs = SelectPdb(power, specs)
  Dim avionics = BuildAvionicsBudget(power, specs)
  Dim fcs = SelectFlightController(power, avionics, specs)
  Dim gpsModules = SelectGpsModule(avionics, specs)
  Dim telemetryRadios = SelectTelemetryRadio(avionics, specs)
  Dim receivers = SelectReceiver(avionics, specs)
  Dim cameras = SelectCamera(avionics, specs)
  TallyRealizedAvionicsCurrent(avionics, fcs, gpsModules, telemetryRadios, receivers, cameras)

  Return New SelectionResult With {
      .EstimatedMtowGrams = mtowG,
      .RequiredThrustPerMotorGf = thrustPerMotorGf,
      .MtowIterationHistory = New List(Of MtowIterationPoint),  ' empty in new pipeline
      .SelectedMotors = motors, .SelectedPropellers = propellers,
      .PowerBudget = power,
      .SelectedBatteries = batteries, .SelectedEscs = escs, .SelectedPdbs = pdbs,
      .AvionicsBudget = avionics,
      .SelectedFlightControllers = fcs, .SelectedGpsModules = gpsModules,
      .SelectedTelemetryRadios = telemetryRadios, .SelectedReceivers = receivers, .SelectedCameras = cameras,
      .Warnings = warnings
  }
  ```
- Tag legacy helpers with `<Obsolete("Replaced by frame-first pipeline 2026-04. Retained for reference.")>`:
  - `EstimateMtow` (line 495)
  - `CalculateThrustRequirement` (line 585) — wait, the new pipeline still constructs `ThrustRequirement` inline, but does not call this function. Mark obsolete.
  - `EstimateInitialBatterySeedFraction` (line 448), `GetDefaultSpecificEnergyWhPerKg` (line 430)
  - `ComputeTargetPropellerDiameter` (line 1701), `EstimateMaxPropDiameter` (line 1720)
  - `SelectPropellersByTargetDiameter` (line 721), `SelectMotorsForPropeller` (line 631), `SelectMotors` (line 677), `SelectPropellers` (line 785)
- Leave **un-marked** (still in use): `EstimatePropellerHoverRpm`, `EstimateNominalVoltage`, `DetermineCellCount`, `NormaliseMotorCount`, `GetAirframeMass`, `CalculatePowerBudget`, `SelectEscs`, `SelectPdb`, all avionics helpers, `ValidateMissionSpecs`.
- In `MainForm.Logic.vb`, after the `_lastResult = result` line (~147), iterate `result.Warnings`:
  - Append non-`[SEVERE]` warnings to the status bar (e.g. `lblStatus.Text &= " | " & w`).
  - For each warning starting with `"[SEVERE]"`, raise a `MessageBox.Show(w, "Design warning", MessageBoxButtons.OK, MessageBoxIcon.Warning)`.

**Build test (end-to-end)**:
1. Launch app, set Configuration = Quadcopter, FrameSizeMm = 250, MTOW = 1500 g, payload = 200 g, endurance = 25 min, range = 5 km, altitude = 120 m.
2. Click `SelectComponents`. Verify a prop+motor+battery is picked, status bar shows the green range/endurance line, and no MessageBox fires.
3. Set MTOW = 800 g and re-run — prop should shrink, motor switches to a smaller KV. Confirm the result panel populates and no exceptions throw.
4. Set endurance = 60 min on the same MTOW=800 — the available battery probably can't deliver. Confirm a `[SEVERE]` MessageBox fires with the underrun percentage.
5. Set Configuration = FixedWing — confirm the unsupported-config yellow warning surfaces.

## Critical files

- `Core/Services/ComponentSelectionEngine.vb` — the rewrite happens here; legacy helpers stay tagged `<Obsolete>`.
- `Core/Models/MissionSpecs.vb` — add `FrameSizeMm`, redocument `MaxTakeoffMassGrams`.
- `Core/Models/ComponentSpecs.vb` — read-only reference; do not modify (verifies `BatterySpec.CapacityMAh` casing, `MotorSpec.ShaftDiameterMm`, `PropellerSpec.BoreDiameterMm`).
- `UI/Forms/MainForm.vb` (designer) — add `nudFrameSize` numeric input.
- `UI/Forms/MainForm.Logic.vb` — `BuildMissionSpecs()` reads new field; warning surfacing post-`SelectComponents`.

## Helpers reused (do not re-implement)

| Helper | File:Line | Use |
|---|---|---|
| `EstimatePropellerHoverRpm(prop, thrustGf)` | ComponentSelectionEngine.vb:607 | Sea-level required RPM in T3 |
| `EstimateNominalVoltage(specs)` | :1643 | Used by reused downstream helpers |
| `DetermineCellCount(motor, specs)` | :1622 | Used by reused downstream helpers |
| `NormaliseMotorCount(int)` | :1667 | Defensive normalisation in T8 |
| `GetAirframeMass(motorCount)` | :1656 | Battery mass budget in T6 |
| `CalculatePowerBudget(motors, thrust, specs)` | :846 | Power budget in T8 |
| `SelectEscs`, `SelectPdb` | :987 / :1036 | Power-system rest |
| `BuildAvionicsBudget`, `SelectFlightController`, `SelectGpsModule`, `SelectTelemetryRadio`, `SelectReceiver`, `SelectCamera`, `TallyRealizedAvionicsCurrent` | :1073–1576 | Avionics chain |
| Constants `LipoCellNominalV`, `MotorRpmHeadroomFactor`, `QuadThrustToWeightRatio`, `CRatingHeadroomFactor`, `MtowSafetyFactor` | :128–238 | Used inline |

## Variable-naming guardrails

To prevent the rename pitfalls from past tasks:

- Mass: `MtowGrams` or `mtowG` (never `mass`, never `weight`).
- Thrust: `ThrustPerMotorGf` / `thrustPerMotorGf` (gram-force). Total: `TotalThrustGf`.
- Prop: `DiameterInches`, `PitchInches`, `MaxRPM`, `StaticThrustGrams`, `StaticThrustTestRPM`, `BoreDiameterMm`, `MassGrams`.
- Motor: `KV`, `MaxRPM`, `MinVoltage`, `MaxVoltage`, `MaxCurrentAmps`, `MaxThrustGrams`, `MaxPowerW`, `Efficiency`, `MassGrams`, `PropDiameterMinIn`, `PropDiameterMaxIn`, `ShaftDiameterMm`, `RecommendedMinCells`, `RecommendedMaxCells`.
- Battery: confirm exact casing of `CapacityMAh` vs `CapacityMah` against `ComponentSpecs.vb` before T6 — codebase has both. Use `CellCount`, `ContinuousCRating`, `MassGrams`, `Chemistry`.
- Frame: `FrameSizeMm` (new), `ArmLengthMm` (local), `MaxPropDiameterIn` (local). Do **not** confuse with the existing `FrameDiagonalTable`.
- Cells / voltage: `CellCount` (Int), `NominalVoltageV` (Double), `LipoCellNominalV` (constant 3.7).
- RPM: `RequiredRpmAtAltitude`, `propHoverRpm` (local; pattern from existing line 638).

## Verification end-to-end

Per T8 build test. After all eight tasks are complete:

1. **Compile** — solution builds with zero errors and only `Obsolete` warnings on the legacy helpers.
2. **Form smoke test** — five UI scenarios (Quad happy-path, small Quad, infeasible-endurance Quad, FixedWing-warning, Hex). Each must either populate the result grid or surface a clear error/warning.
3. **Watch for stale callers** — grep for any remaining direct calls to `EstimateMtow`, `CalculateThrustRequirement`, `ComputeTargetPropellerDiameter`, `EstimateMaxPropDiameter`, `SelectPropellersByTargetDiameter`, `SelectMotorsForPropeller`, `SelectMotors`, `SelectPropellers`. The new pipeline should not call any of these. Any compiler `BC40000` (Obsolete) warning is a missed reference.
4. **Manual sanity** — for a typical Quad250 / 1500 g MTOW / 5" prop, confirm the picked motor KV ≈ `RPM_required / (cells · 3.7 · 0.95)` to within ±15%.
