# Drone Designer — Codebase Onboarding

**Platform**: Windows Forms (VB.NET, .NET Framework 4.7.2)  
**Project context**: Teknofest competition entry. Selects UAV components from a JSON database given a user-defined mission spec, then drives SolidWorks to produce a CAD model.

---

## 1. What the App Does

The user fills in a mission form (endurance, range, MTOW, frame size, environment, etc.) and clicks "Design". The app runs a 15-step pipeline that:

1. Estimates MTOW via a fixed-point solver (iterates until convergence within 1 g).
2. Selects propeller + motor pairs from `components.json`.
3. Sizes the battery, ESCs, and PDB.
4. Selects avionics (FC, GPS, telemetry, receiver, camera).
5. Outputs a `SelectionResult` displayed in the UI and optionally exports to SolidWorks.

---

## 2. File Map

```
Core/
  Models/
    MissionSpecs.vb          — Input to the engine. All mission parameters.
    ComponentSpecs.vb        — Prop/Motor/Battery/ESC/FC data models + enums.
    SizingPolicy.vb          — Safety-factor constants (KMotorTorque, TWR, etc.)
    PipelineResult.vb        — SelectionResult, intermediate result types.
  Services/
    ComponentSelectionEngine.vb — The 15-step selection pipeline. ← core logic
    PipelineOrchestrator.vb  — Drives the engine from the UI thread.
  Data/
    ComponentRepository.vb   — Loads + caches components.json.
  Interfaces/
    IComponentSelector.vb    — Interface implemented by ComponentSelectionEngine.

UI/Forms/
  MainForm.vb               — Entry point. Wires up controls and handlers.
  MainForm.Logic.vb         — BuildMissionSpecs(), MapUAVConfiguration(), etc.
  MainForm.Tests.vb         — In-app test harness (Ctrl+Shift+T / Ctrl+Shift+L).
  MainForm.CAD.vb           — SolidWorks export trigger.
  MainForm.Export.vb        — PDF/CSV export.
  ConvergenceForm.vb        — MTOW convergence graph popup.

Solidworks/
  SolidWorksAutomation.vb   — Module 2: drives SolidWorks via COM.
  MacroRunner.vb            — Runs SW macros. MUST run on STA thread (see below).

Utilities/
  ConfigManager.vb          — App-level config (paths, flags).

bin/Debug/Resources/
  components.json           — The live component database. Edit this to add parts.

Test/
  test_scenarios.csv        — 16 test scenarios loaded with Ctrl+Shift+L.
  Logs/                     — Auto-saved test run logs (testrun_YYYYMMDD_HHmmss.log).
```

---

## 3. The Component Selection Pipeline

`ComponentSelectionEngine.SelectComponents(specs As MissionSpecs)` is a linear pipeline. Each step hands structured data to the next — no global state.

| Step | Task | What it does |
|------|------|-------------|
| 1 | 7 | MTOW fixed-point iteration (max 10 passes, 1 g tolerance) |
| 2 | 7 | Per-motor thrust = MTOW × TWR / motorCount |
| 3 | 7 | **`BuildPropCandidates`** — diameter filter → RPM filter → MaxRPM filter |
| 4 | 7 | **`SelectMotorForCandidate`** — MaxThrust / voltage / torque / shaft-bore check |
| 5 | 8 | Total current from selected motors |
| 6 | 8 | Battery cell count and capacity |
| 7 | 8 | Battery selection from repository |
| 8 | 8 | ESC selection (rated above motor peak current) |
| 9 | 8 | PDB selection (rated for full system current) |
| 10 | 9 | Flight controller selection |
| 11 | 9 | GPS/GNSS selection |
| 12 | 9 | Telemetry radio (range × 1.25 margin) |
| 13 | 9 | Receiver (range × 1.50 margin) |
| 14 | 9 | Camera/sensor matched to mission type |
| 15 | 9 | Avionics current tally |

### Prop selection in detail (steps 3–4)

```
armMm = specs.FrameSizeMm / 2
maxPropDiamIn = GetPropToArmRatio(config) × armMm / 25.4     ← Bug A lives here
motorRpmCeiling = Max(motor.KV × motor.MaxVoltage) for all motors

For each prop in database:
  1. Diameter ≤ maxPropDiamIn?
  2. ComputePropellerAero(thrust, prop, altitude) → requiredRpm
  3. requiredRpm ≤ motorRpmCeiling?
  4. requiredRpm ≤ prop.MaxRPM?
  → passes → try all motors for this prop via SelectMotorForCandidate

SelectMotorForCandidate checks:
  • motor.MaxThrustGrams ≥ thrustGf
  • motor voltage range covers computed cell count
  • motor.MaxTorqueNm ≥ propTorqueNm × policy.KMotorTorque (default 2.0)
  • motor.PropDiameterMinIn ≤ propDiam ≤ motor.PropDiameterMaxIn
  • 0 ≤ (prop.BoreMillimeters - motor.ShaftDiameterMm) ≤ 0.5   ← shaft-bore fit
```

### Hover RPM formula

`ComputePropellerAero` (line 699) — current formula:
```
nSquared = T / (Ct × ρ × D^4)
rpm = sqrt(nSquared) × 60
```
- `T` in Newtons, `D` in metres, `ρ` at altitude via ISA model.
- `Ct` comes from `prop.CtStatic`; defaults to **0.115** if not in JSON.
- The old formula `EstimatePropellerHoverRpm` is marked `<Obsolete>` — do not use.

---

## 4. Key Data Models

### `MissionSpecs` (dual-property pattern — important gotcha)

The class carries **two parallel sets** of mission-type and environment properties:

| UI-facing (Category enums) | Engine-facing (Type enums) |
|---|---|
| `Profile As MissionProfileCategory` | `MissionProfile As MissionProfileType` |
| `Environment As OperatingEnvCategory` | `OperatingEnvironment As EnvironmentType` |

Both sets must be assigned simultaneously. `BuildMissionSpecs()` in `MainForm.Logic.vb` does this. If you construct `MissionSpecs` directly (e.g., in tests), set both.

**Why this exists**: VB.NET forbids a property and a type sharing the same name in the same scope. When Task 11 added `MissionProfile` and `OperatingEnvironment` as engine-facing properties, the original enum types had to be renamed (`MissionProfile` → `MissionProfileCategory`, `OperatingEnvironment` → `OperatingEnvCategory`). The engine reads the new names; the UI still writes through both.

### `ComponentSpecs.vb` — Deserialization defaults

`PropellerSpec.OnDeserialized` sets fallback values when JSON omits them:
```vb
If CtStatic <= 0 Then CtStatic = 0.115   ' aerodynamic thrust coefficient
If CpStatic <= 0 Then CpStatic = 0.044   ' power coefficient
```
Only `prop_004` in the current database has real measured Ct/Cp. All others use the fallback, which was NOT calibrated for the current RPM formula. This causes systematic RPM overestimates for those props.

`MotorSpec.OnDeserialized` derives torque from electrical specs when JSON omits it:
```vb
KtNmPerA = 60 / (2π × KV)
MaxTorqueNm = KtNmPerA × MaxCurrentAmps
```

### `SizingPolicy`

Safety factors injected into `MissionSpecs.SizingPolicy`. Key constant:
- `KMotorTorque = 2.0` — motor must supply 2× the prop's hover torque.

---

## 5. Component Database (`components.json`)

Located at `bin/Debug/Resources/components.json` (and `bin/Release/...`). Both copies must be kept in sync.

**Current prop inventory** (8 props):

| ID | Diameter | Bore | MaxRPM | Ct/Cp | Notes |
|----|---------|------|--------|-------|-------|
| prop_001 | 5.1" | 5mm | 35k | fallback | |
| prop_002 | 9" | 6mm | 16k | fallback | |
| prop_003 | 16" CF folding | 8mm | 7k | fallback | |
| prop_004 | 10" APC | 6.35mm | 12k | **real** (Ct=0.1407) | |
| prop_005 | 15" Tarot | 10mm | 8k | fallback | |
| prop_006 | 3" micro | 1.5mm | 55k | fallback | |
| prop_007 | 18" CF | 8mm | 5.5k | fallback | |
| prop_008 | 10" CF Survey | 6.35mm | 12k | fallback | Old formula comment — see Bug C |

**Current motor inventory** (10 motors):

| ID | Model | KV | MaxVoltage | Prop range | Shaft |
|----|-------|----|-----------|-----------|-------|
| mot_001 | F40 Pro IV | 2400 | 16.8V | 4–5" | 3mm |
| mot_002 | MN3110 | 780 | 25.2V | 9–11" | — |
| mot_003 | U8 Lite | 100 | 50.4V | 18–24" | 8mm |
| mot_004 | SunnySky X2212 | 980 | 16.8V | 8–10" | 3.17mm |
| mot_005 | MN4014 | 400 | 29.6V | 13–17" | 6mm |
| mot_006 | KDE2315XF | 885 | 22.2V | 10–13" | 4mm |
| mot_007 | V-Spec 2807 | 1300 | 16.8V | 6–7" | 3mm |
| mot_008 | MN8014 | 120 | 44.4V | 18–22" | 8mm |
| mot_009 | Imaginary | 2400 | 16.8V | 4.5–5.5" | 3mm |
| mot_010 | MN4010 | 430 | 14.8V | 9–11" | 6.1mm |

**Motor RPM ceiling** (current): `Max(KV × MaxVoltage)` = 2400 × 16.8 = **40,320 RPM**. This ceiling is computed live from the database at selection time — adding a higher-KV motor changes it automatically.

---

## 6. The Test Harness

**Ctrl+Shift+T** — runs the 6 hardcoded scenarios in `GetTestScenarios()`.  
**Ctrl+Shift+L** — opens a file picker, loads `Test/test_scenarios.csv` (16 scenarios), runs all.

Both paths call `LoadScenario(s)` → `BuildMissionSpecs()` → `engine.SelectComponents(specs)`.  
Logs are saved to `Test/Logs/testrun_YYYYMMDD_HHmmss.log` automatically.

**To add a scenario**: append a row to `test_scenarios.csv`. Column order is:
```
Name, EnduranceHr, RangeKm, CruiseSpeedKmh, MaxAltitudeM, MaxWindSpeedKmh,
MtowGrams, PayloadGrams, FrameSizeMm,
OperatingEnvIdx, IPRatingIdx, TempMinC, TempMaxC,
MissionProfileIdx, FrameTypeIdx, MotorCountIdx, AutonomyIdx, PayloadTypeIdx
```
Lines starting with `#` are comments and are skipped.

---

## 7. Known Bugs (as of 2026-05-07)

All three bugs below cause the current 0/16 test pass rate. Do not close this issue until all three are fixed and tests pass.

### Bug A — Prop diameter formula treats arm length as max prop diameter (primary blocker)

**Location**: `ComponentSelectionEngine.vb`, `BuildPropCandidates`, ~line 426.

```vb
maxPropDiamIn = (ratio * armMm) / 25.4
```

`armMm` is the arm **length** (i.e., the prop **radius** clearance), but the formula uses it as if it were the max prop **diameter**. The result is approximately half the correct limit. A 250mm quad should accept ~5" props; the formula caps it at 4.43".

The inline comment even contradicts itself: *"250 mm quad → arm 125 mm → max 4.4" (5" prop fits with tolerance)"* — but 5" > 4.43" so 5" does NOT pass the filter. The formula needs either a ×2 correction or a revised ratio table.

**Evidence**: Every small/medium quad fails with "Diameter X in exceeds max allowed Y in" where Y exactly matches `0.90 × (frame/2) / 25.4`.

### Bug B — `MapUAVConfiguration` maps all multirotors to Quadcopter

**Location**: `MainForm.Logic.vb`, `MapUAVConfiguration()`.

All multirotor test scenarios have `FrameTypeIdx = 0` ("Multirotor" as a frame category). `MapUAVConfiguration(0)` returns `UAVConfiguration.Quadcopter`, so Hex and Octo scenarios use ratio 0.90 instead of 0.85/0.65.

The fix is to derive `UAVConfiguration` from motor count (`cboMotorCount`) rather than frame type (`cboFrameType`), or add a dedicated combo for topology.

**Evidence**: Photo Hex (600mm) log shows max 10.6" → `0.90 × 300/25.4 = 10.63"` ✓. Hex ratio would give 10.04".

### Bug C — `components.json` design points calibrated to old RPM formula

**Location**: `bin/Debug/Resources/components.json`, prop_008 comment; `ComponentSelectionEngine.vb`, `ComputePropellerAero`.

The "guaranteed-passing pair" (prop_008 + mot_010) was calibrated using the old `EstimatePropellerHoverRpm` formula (now `<Obsolete>`). The current `ComputePropellerAero` with fallback Ct=0.115 produces ~9,500 RPM for the same conditions instead of the 4,000 RPM the comment assumes — a 2.4× discrepancy.

The fix is to add real measured Ct/Cp values to more props in the database, or to recalibrate the database design points against the current formula.

---

## 8. SolidWorks Integration Gotcha

**The `RunMacro2` API silently no-ops when called from an MTA thread.** SolidWorks requires all COM calls to happen on an STA thread. Any code in `SolidWorksAutomation.vb` or `MacroRunner.vb` must be wrapped in a single `RunOnStaAsync` call that encompasses all SW lifecycle stages (open → macro → close). Splitting those stages across separate `RunOnStaAsync` calls also fails because each call may run on a different thread.

---

## 9. Quick Reference: Adding a Component

1. Open `bin/Debug/Resources/components.json` (and `bin/Release/...`).
2. Add an entry under the appropriate array (`propellers`, `motors`, `batteries`, etc.).
3. For **propellers**: include real `CtStatic`/`CpStatic` values if possible; the fallback Ct=0.115 will overestimate hover RPM for props with larger blades.
4. For **motors**: if `MaxTorqueNm` is omitted, `OnDeserialized` will compute it from `KV × MaxCurrentAmps` — verify the result is physically plausible.
5. Re-run the test harness (Ctrl+Shift+L with `Test/test_scenarios.csv`) to verify.
