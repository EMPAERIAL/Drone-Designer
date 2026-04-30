# Drone Designer Codebase Audit Report
**Date:** 2026-04-26  
**Scope:** Read-only verification of MissionSpecs, MTOW iteration, selection ordering, data layer, and project structure.

---

## Section A — MissionSpecs.vb Field Inventory

**File:** `Core/Models/MissionSpecs.vb`

### A.1 Full Property List (Declaration Order)

| # | Property Name | Type | Default Value | XML Summary |
|---|---|---|---|---|
| 1 | MissionName | String | "Untitled Mission" | Human-readable label for mission config |
| 2 | Description | String | String.Empty | Optional free-text description |
| 3 | CreatedAtUtc | DateTime | DateTime.UtcNow | Timestamp (UTC) when created |
| 4 | FlightEnduranceMinutes | Double | 30.0 | Required flight endurance in minutes |
| 5 | MaxRangeKm | Double | 5.0 | Maximum operational range in km |
| 6 | CruiseSpeedMs | Double | 10.0 | Steady-state cruise speed in m/s |
| 7 | MaxSpeedMs | Double | 15.0 | Maximum design speed in m/s |
| 8 | MaxAltitudeMeters | Double | 120.0 | Maximum altitude AGL in meters |
| 9 | MaxWindSpeedMs | Double | 11.0 | Maximum sustained wind speed in m/s |
| 10 | MaxTakeoffMassGrams | Double | 2000.0 | Design limit MTOW incl. battery and payload (g) |
| 11 | PayloadMassGrams | Double | 0.0 | Payload mass in grams |
| 12 | PayloadDimensionsMm | PayloadDimensions | New PayloadDimensions() | 3D bounding box for payload bay |
| 13 | MinOperatingTempCelsius | Double | 0.0 | Minimum expected ambient temperature (°C) |
| 14 | MaxOperatingTempCelsius | Double | 45.0 | Maximum expected ambient temperature (°C) |
| 15 | Environment | OperatingEnvCategory | OperatingEnvCategory.OutdoorStandard | High-level environment classification |
| 16 | RequiresWaterproofing | Boolean | False | True if waterproofing/dust sealing required |
| 17 | *(orphan XML doc)* | — | — | "Target IP rating string…" — **NO PROPERTY ATTACHED** |
| 18 | Profile | MissionProfileCategory | MissionProfileCategory.Surveillance | User-selected mission profile |
| 19 | Configuration | UAVConfiguration | UAVConfiguration.Quadcopter | User-selected UAV configuration |
| 20 | PowerSource | PowerSourceType | PowerSourceType.LiPo | Power source topology |
| 21 | Regulatory | RegulatoryClass | RegulatoryClass.CommercialStandard | Regulatory/airspace class |
| 22 | MissionProfile | MissionProfileType | MissionProfileType.General | Engine-facing mission profile enum |
| 23 | OperatingEnvironment | EnvironmentType | EnvironmentType.Standard | Engine-facing environment enum |
| 24 | MotorCount | Integer | 4 | Number of motors/rotors |
| 25 | RangeKm | Double (alias) | — | Engine-facing alias for MaxRangeKm |
| 26 | PayloadWeightGrams | Double (alias) | — | Engine-facing alias for PayloadMassGrams |
| 27 | ControlLinkRangeKm | Double? | Nothing | Control link range (km) |
| 28 | RequiresVideoDownlink | Boolean | True | True if real-time video downlink required |
| 29 | VideoDownlinkRangeKm | Double | 2.0 | Minimum video downlink range (km) |
| 30 | RequiresTelemetry | Boolean | True | True if MAVLink telemetry required |
| 31 | RequiresRemoteID | Boolean | False | True if Remote ID required |
| 32 | RequiresAutopilot | Boolean | True | True if autonomous waypoint nav required |
| 33 | RequiresOpticalFlow | Boolean | False | True if optical flow sensor needed |
| 34 | RequiresObstacleAvoidance | Boolean | False | True if obstacle avoidance sensors required |
| 35 | ObstacleAvoidanceDirections | Integer | 1 | Number of OA sensor directions |
| 36 | RequiresDualGPS | Boolean | False | True if dual GPS required |
| 37 | RequiresParachute | Boolean | False | True if parachute recovery required |
| 38 | RequiredIPRating | String | String.Empty | Required IP rating string (e.g. "IP54") |

---

### A.2 Confirmed/Denied Properties

All ten queried properties are **absent**:

| Property | Present? |
|---|---|
| MissionSegments | NO |
| HoverFractionOfMission | NO |
| PayloadPowerWatts | NO |
| AvionicsPowerWatts | NO |
| ClimbRateMs | NO |
| MaxClimbAngleDeg | NO |
| TargetThrustToWeightRatio | NO |
| BatteryMaxDepthOfDischarge | NO |
| BatterySpecificEnergyWhPerKgOverride | NO |
| OperationalReserveFraction | NO |

---

### A.3 Enum Duality

**Both pairs survive — duality confirmed.**

- `MissionProfileCategory` (lines 38–51, UI-facing) coexists with `MissionProfileType` (lines 125–134, engine-facing)
- `OperatingEnvCategory` (lines 79–92, UI-facing) coexists with `EnvironmentType` (lines 140–143, engine-facing)

A Task 11 comment (lines 15–31) explains the rename was made to avoid property/type shadowing. `ComponentSelectionEngine` reads `specs.MissionProfile` (type `MissionProfileType`) and `specs.OperatingEnvironment` (type `EnvironmentType`).

---

### A.4 MaxTakeoffMassGrams

Lines 200–206:

```vb
        ''' <summary>
        ''' Maximum take-off mass (design limit) in grams, including battery and payload.
        ''' Engine uses this as target to estimate required thrust and power.
        ''' Valid range: 100–55,000 g (smallest ready-to-fly quad to heavy industrial).
        ''' Default: 2000 g (2 kg, typical commercial quadcopter).
        ''' </summary>
        Public Property MaxTakeoffMassGrams As Double = 2000.0
```

**Usages:** Only the definition in `MissionSpecs.vb`. No other `.vb` file references `MaxTakeoffMassGrams` by that name. The selection engine derives MTOW internally via `EstimateMtow()` and uses `specs.PayloadWeightGrams` (the alias).

---

### A.5 UAVConfiguration vs MotorCount

Both are present as distinct properties:

- `Configuration` — type `UAVConfiguration`, default `Quadcopter`, line 274
- `MotorCount` — type `Integer`, default `4`, line 318
- `UAVConfiguration` enum — lines 57–72, values: Quadcopter, Hexacopter, Octocopter, FixedWing, VTOL, Helicopter, Tricopter

---

### A.6 Orphan XML Doc

**YES — confirmed at lines 253–257:**

```vb
        ''' <summary>
        ''' Target IP rating string (e.g. "IP54", "IP65"). Empty = no requirement.
        ''' Drives enclosure material and seal selection in Module 2.
        ''' Default: empty (no waterproofing).
        ''' </summary>


        ' ── MISSION PROFILE & CONFIGURATION ───────────────────────────
```

No property is attached. `RequiredIPRating` exists at line 395 with its own separate summary — this block is a dangling refactoring artifact.

---

## Section B — EstimateMtow() and the MTOW Iteration

**File:** `Core/Services/ComponentSelectionEngine.vb`

### B.1 Full Function Body (lines 351–388)

```vb
        Public Function EstimateMtow(specs As MissionSpecs) As MtowEstimate
            Dim motorCount As Integer = NormaliseMotorCount(specs.MotorCount)

            Dim structuralMass As Double =
                GetAirframeMass(motorCount) +
                FcStackMassG +
                ReceiverMassG +
                GpsMassG +
                WiringMassG +
                specs.PayloadWeightGrams

            Dim batteryMassG As Double = structuralMass * 0.25   ' initial guess: 25% of structure
            Dim totalMass As Double = 0.0
            Dim prevMtow As Double = 0.0
            Dim iteration As Integer = 0

            Do
                prevMtow = totalMass
                totalMass = (structuralMass + batteryMassG) * MtowSafetyFactor

                Dim hoverPowerW As Double = (totalMass / 100.0) * 10.0            ' 10 W per 100 g
                Dim enduranceH As Double = specs.FlightEnduranceMinutes / 60.0
                Dim totalEnergyWh As Double = hoverPowerW * enduranceH * 1.2       ' +20% overhead

                Dim usableDensity As Double = LipoEnergyDensityWhPerKg * LipoMaxDod
                batteryMassG = (totalEnergyWh / usableDensity) * 1000.0

                iteration += 1
            Loop While Math.Abs(totalMass - prevMtow) > 1.0 AndAlso iteration < 10

            Return New MtowEstimate With {
                .StructuralMassGrams = structuralMass,
                .EstimatedBatteryMassGrams = batteryMassG,
                .TotalMassGrams = totalMass,
                .MotorCount = motorCount,
                .IterationsToConverge = iteration
            }
        End Function
```

---

### B.2 Task 7 Constants Region (lines 58–87)

```vb
        ' =======================================================================
        ' TASK 7 CONSTANTS — Weight and Thrust
        ' =======================================================================

        Private Shared ReadOnly AirframeMassTable As New Dictionary(Of Integer, Double) From {
            {4, 250.0},
            {6, 420.0},
            {8, 680.0}
        }

        Private Const FcStackMassG As Double = 35.0
        Private Const ReceiverMassG As Double = 10.0
        Private Const GpsMassG As Double = 20.0
        Private Const WiringMassG As Double = 30.0

        Private Const MtowSafetyFactor As Double = 1.1

        Private Const QuadThrustToWeightRatio As Double = 2.0
```

Task 8 power constants (lines 98–111):

```vb
        Private Const LipoCellNominalV As Double = 3.7

        Private Const LipoMaxDod As Double = 0.8

        Private Const LipoEnergyDensityWhPerKg As Double = 130.0
```

---

### B.3 Constants Summary

| Constant | Present? | Value |
|---|---|---|
| MtowMaxIterations | **NO** — literal `10` at line 379 | — |
| MtowConvergenceToleranceGrams | **NO** — literal `1.0` at line 379 | — |
| MtowSafetyFactor | YES | 1.1 |
| LipoMaxDod | YES | 0.8 |
| LipoEnergyDensityWhPerKg | YES | 130.0 |
| QuadThrustToWeightRatio | YES | 2.0 |

---

### B.4 Initial Battery Seed

Line 362:

```vb
            Dim batteryMassG As Double = structuralMass * 0.25   ' initial guess: 25% of structure
```

---

### B.5 Loop Termination

Line 379:

```vb
            Loop While Math.Abs(totalMass - prevMtow) > 1.0 AndAlso iteration < 10
```

Terminates when `|totalMass − prevMtow| ≤ 1.0 g` **OR** `iteration ≥ 10`.

---

### B.6 Divergence Handling

**NO.** No consecutive-increase counter, no exception on growth. The loop silently hits the cap.

---

### B.7 Non-Convergence Return

Returns `MtowEstimate` with `TotalMassGrams` = last `totalMass`, `IterationsToConverge` = 10. No exception thrown; no log entry.

---

### B.8 Helper Functions

- `EstimateInitialBatterySeedFraction` — **does not exist**
- `GetDefaultSpecificEnergyWhPerKg` — **does not exist**

Both values are hard-coded constants.

---

### B.9 Non-Hover Overhead

Line 373:

```vb
                Dim totalEnergyWh As Double = hoverPowerW * enduranceH * 1.2       ' +20% overhead
```

Literal `* 1.2` (×1.20, +20%).

---

## Section C — Selection Ordering: Motors vs. Propellers

### C.1 Orchestrating Block (lines 290–329)

```vb
        Public Function SelectComponents(specs As MissionSpecs) As SelectionResult Implements IComponentSelector.SelectComponents
            ValidateMissionSpecs(specs)

            ' ── Task 7 ─────────────────────────────────────────────────────────
            Dim mtow As MtowEstimate = EstimateMtow(specs)
            Dim thrust As ThrustRequirement = CalculateThrustRequirement(mtow, specs.MotorCount)
            Dim motors As List(Of ComponentSpecs) = SelectMotors(thrust, specs)
            Dim propellers As List(Of ComponentSpecs) = SelectPropellers(motors, specs)

            ' ── Task 8 ─────────────────────────────────────────────────────────
            Dim power As PowerBudget = CalculatePowerBudget(motors, thrust, specs)
            Dim batteries As List(Of ComponentSpecs) = SelectBatteries(power, specs)
            Dim escs As List(Of ComponentSpecs) = SelectEscs(motors, power, specs)
            Dim pdbs As List(Of ComponentSpecs) = SelectPdb(power, specs)

            ' ── Task 9 ─────────────────────────────────────────────────────────
            Dim avionics As AvionicsBudget = BuildAvionicsBudget(power, specs)
            Dim fcs As List(Of ComponentSpecs) = SelectFlightController(power, avionics, specs)
            Dim gpsModules As List(Of ComponentSpecs) = SelectGpsModule(avionics, specs)
            Dim telemetryRadios As List(Of ComponentSpecs) = SelectTelemetryRadio(avionics, specs)
            Dim receivers As List(Of ComponentSpecs) = SelectReceiver(avionics, specs)
            Dim cameras As List(Of ComponentSpecs) = SelectCamera(avionics, specs)
            TallyRealizedAvionicsCurrent(avionics, fcs, gpsModules, telemetryRadios, receivers, cameras)

            Return New SelectionResult With { ... }
        End Function
```

---

### C.2 Call Order

**SelectMotors is called before SelectPropellers** — line 296 then line 297.

---

### C.3 SelectPropellers Signature

Line 472:

```vb
        Private Function SelectPropellers(selectedMotors As List(Of ComponentSpecs), specs As MissionSpecs) As List(Of ComponentSpecs)
```

**Yes — `selectedMotors` is the first parameter.** Propeller selection is motor-first.

---

### C.4 SelectMotors Signature

Line 431:

```vb
        Private Function SelectMotors(thrust As ThrustRequirement, specs As MissionSpecs) As List(Of ComponentSpecs)
```

**No propeller input.** Motors selected independently on thrust and voltage.

---

### C.5 SelectMotors Filter Predicate (lines 436–442)

```vb
            Dim candidates As List(Of ComponentSpecs) =
                allMotors.Where(Function(m As MotorSpec)
                                    If m.MaxThrustGrams < thrust.ThrustPerMotorGf Then Return False
                                    If m.MinVoltage > nominalVoltage Then Return False
                                    If m.MaxVoltage < nominalVoltage Then Return False
                                    'If specs.OperatingEnvironment = EnvironmentType.Harsh AndAlso m.IpRating = 0 Then Return False
                                    Return True
                                End Function) _
```

Hard filter: `MaxThrustGrams >= ThrustPerMotorGf` plus voltage compatibility. IP-rating check is commented out.

---

### C.6 SelectPropellers Filter Predicate (lines 481–486)

```vb
            Dim candidates As List(Of ComponentSpecs) =
                allProps.Where(Function(p As PropellerSpec)
                                   If p.DiameterInches > maxPropDiamIn Then Return False
                                   If p.DiameterInches < refMotor.PropDiameterMinIn Then Return False
                                   If p.DiameterInches > refMotor.PropDiameterMaxIn Then Return False
                                   Return True
                               End Function) _
```

Filters on diameter only: within frame clearance (`maxPropDiamIn`) and motor's recommended range. No thrust-coefficient check at selection time.

---

### C.7 Ranking Chains

**SelectMotors (lines 443–446):**

```vb
                         .OrderByDescending(Function(m As MotorSpec) m.Efficiency) _
                         .ThenBy(Function(m As MotorSpec) m.MassGrams) _
                         .Take(5) _
                         .ToList()
```

Efficiency DESC, then mass ASC. Top 5 returned.

**SelectPropellers (lines 487–489):**

```vb
                        .OrderBy(Function(p As PropellerSpec) If(isRacing, -p.PitchInches, p.PitchInches)) _
                        .Take(5) _
                        .ToList()
```

Racing: pitch DESC (via negation). Non-racing/endurance: pitch ASC. Top 5 returned. Propeller efficiency sort is commented out.

---

### C.8 EstimateMaxPropDiameter (lines 1357–1368)

```vb
        Private Shared Function EstimateMaxPropDiameter(motorCount As Integer) As Double
            Dim diagonalMm As Double
            Select Case NormaliseMotorCount(motorCount)
                Case 4 : diagonalMm = 250.0
                Case 6 : diagonalMm = 380.0
                Case 8 : diagonalMm = 500.0
                Case Else : diagonalMm = 250.0
            End Select
            Dim armLengthMm As Double = diagonalMm / Math.Sqrt(2.0)
            Dim maxPropRadiusMm As Double = armLengthMm / 1.3
            Return (maxPropRadiusMm * 2.0) / 25.4
        End Function
```

**Frame size is derived from MotorCount** — motor-first dependency direction.  
Formula: `frame diagonal → arm length = diagonal / √2 → max prop radius = arm length / 1.3 (30% tip clearance) → diameter (in) = radius × 2 / 25.4`

---

### C.9 Disk Loading

**NO** — no references to `DiskLoading`, `BEMT`, `BladeElement`, `1.19`, or any disk-loading formula.

### C.10 BEMT / Coupled Motor-Prop Modeling

**NO** — absent entirely.

---

## Section D — Repository and Data Layer

### D.1 MotorSpec Properties

**File:** `Core/Models/ComponentSpecs.vb`, lines 488–616

Inherits base properties: `Id, ModelName, Manufacturer, PartNumber, Category, MassGrams, Dimensions, MinVoltage, NominalVoltageV, MaxPowerW, MaxVoltage, MaxCurrentA, MinCurrentA, NominalCurrentA, MinOperatingTempC, MaxOperatingTempC, PriceUSD, Notes`

Own properties:

| Property | Type | Notes |
|---|---|---|
| KV | Integer | Velocity constant (RPM/V) |
| MaxPowerWatts | Double | Maximum continuous shaft power (W) |
| MaxCurrentAmps | Double | Maximum continuous current at rated voltage |
| NoLoadCurrentAmps | Double | No-load current at 10 V |
| InternalResistanceMilliOhm | Double | Winding resistance (mΩ) |
| MaxThrustGrams | Double | Static thrust with recommended propeller |
| MaxThrustTestPropeller | String | Prop size at which thrust was measured |
| ShaftDiameterMm | Double | Motor shaft diameter |
| StatorDiameterMm | Double | Stator diameter |
| StatorHeightMm | Double | Stator winding stack height |
| PoleCount | Integer | Magnetic poles on rotor bell |
| PropDiameterMinIn | Double | Minimum recommended prop diameter (in) |
| PropDiameterMaxIn | Double | Maximum recommended prop diameter (in) |
| MountingBoltCircleMm | Double | Bolt-circle diameter for mounting |
| MountingBoltCount | Integer | Number of mounting bolts |
| MotorType | MotorType | Brushless (default) or Brushed |
| RecommendedMinCells | Integer | Minimum cell count |
| RecommendedMaxCells | Integer | Maximum cell count |
| Efficiency | Double | Thrust per Watt (gf/W) |

**Confirmed present:** ✓ Kv (as `KV`), ✓ MaxCurrentA (as `MaxCurrentAmps`), ✓ MaxPowerW (as `MaxPowerWatts`), ✓ Efficiency, ✓ MinVoltage, ✓ MaxVoltage, ✓ PropDiameterMinIn, ✓ PropDiameterMaxIn, ✓ MassGrams, ✓ MaxThrustGrams

---

### D.2 PropellerSpec Properties

**File:** `Core/Models/ComponentSpecs.vb`, lines 716–778

| Property | Type | Notes |
|---|---|---|
| DiameterInches | Double | Blade diameter (in) |
| PitchInches | Double | Blade pitch (in) |
| BladeCount | Integer | Number of blades (default 2) |
| BoreDiameterMm | Double | Centre hole diameter (mm) |
| Material | String | Primary blade material |
| IsFoldable | Boolean | True if blades are foldable |
| MaxRPM | Integer | Maximum safe rotational speed |
| StaticThrustGrams | Double | Static thrust (g) |
| StaticThrustTestRPM | Integer | RPM at which thrust was measured |
| IsClockwiseRotation | Boolean | True if CW rotation prop |
| Efficiency | Double | Efficiency proxy (StaticThrustGrams / mass) |

**Confirmed present:** ✓ DiameterInches, ✓ PitchInches, ✓ BladeCount, ✓ Material, ✓ MassGrams  
No blade-element thrust-coefficient fields present.

---

### D.3 Sample components.json Entries

**Motor (mot_001, T-Motor F40 Pro IV):**

```json
{
  "id": "mot_001",
  "category": "Motor",
  "name": "F40 Pro IV",
  "manufacturer": "T-Motor",
  "massGrams": 30.5,
  "motorKv": 2400,
  "statorDiameterMm": 24.0,
  "statorHeightMm": 4.0,
  "designatedPropSizeInMin": 4.0,
  "designatedPropSizeInMax": 5.0,
  "voltageMinV": 11.1,
  "voltageMaxV": 16.8,
  "maxContinuousCurrentA": 34.0,
  "maxBurstCurrentA": 40.0,
  "maxThrustG": 820,
  "maxPowerW": 530,
  "efficiency": 0.89,
  "resistance_mOhm": 66.0,
  "noLoadCurrentA": 0.9
}
```

**Propeller (prop_001, HQProp 5.1×4.6×3-V1S):**

```json
{
  "id": "prop_001",
  "category": "Propeller",
  "name": "HQProp 5.1x4.6x3-V1S",
  "manufacturer": "HQProp",
  "massGrams": 4.5,
  "dimensions": {
    "diameterInches": 5.1,
    "pitchInches": 4.6,
    "bladesCount": 3,
    "boreMm": 5.0
  },
  "material": "Polycarbonate",
  "maxRPM": 35000,
  "maxStaticThrustG": 920,
  "recommendedKvRange": [2000, 2600],
  "isFolding": false
}
```

---

## Section E — Project Structure

### E.1 Top-Level Directory

```
.claude/
.git/
.vs/
bin/
obj/
Core/
  ├── Data/
  │   └── ComponentRepository.vb
  ├── Interfaces/
  │   └── IComponentSelector.vb
  ├── Models/
  │   ├── ComponentDisplayRow.vb
  │   ├── ComponentSpecs.vb
  │   ├── MissionSpecs.vb
  │   └── PipelineResult.vb
  └── Services/
      ├── ComponentSelectionEngine.vb
      └── PipelineOrchestrator.vb
My Project/
Resources/
  ├── components.json
  ├── Macros/
  │   └── MotorMount.swb
  └── Templates/
Solidworks/
  ├── MacroRunner.vb
  ├── Module2UsageExample.vb
  └── SolidWorksAutomation.vb
Test/
UI/
  └── Forms/
      ├── .Archives/
      │   └── MainForm.Logic.vb
      ├── CadProgressForm.vb
      └── MainForm.CAD.vb
Utilities/
docs/
packages/
```

---

### E.2 File Presence

| File | Present? |
|---|---|
| MainForm.CAD.vb | **YES** — `UI/Forms/MainForm.CAD.vb` |
| CadProgressForm.vb | **YES** — `UI/Forms/CadProgressForm.vb` |
| MotorMount.swb | **YES** — `Resources/Macros/MotorMount.swb` |

All three files previously reported as missing are now present.

---

### E.3 Namespace Declarations

- `MissionSpecs.vb` line 6: `Namespace Core.Models`
- `ComponentSelectionEngine.vb` line 6: `Namespace Core.Services`
- `ComponentSpecs.vb` line 30: `Namespace Core.Models`

All three use relative namespace declarations. The full qualified root resolves to `Drone_Designer` (with underscore) as declared in `Drone Designer.vbproj`. No mismatches.

---

## Incidental Observations

1. **Hardcoded iteration cap and tolerance:** `MtowMaxIterations` and `MtowConvergenceToleranceGrams` are literals (`10`, `1.0`) in the loop condition at line 379, not named constants — unlike the rest of Task 7/8/9 which all use named, documented `Private Const` values.

2. **Orphaned IP rating doc block** at lines 253–257 is a refactoring artifact; `RequiredIPRating` (line 395) has its own separate summary.

3. **Commented-out Harsh environment motor filter** (line 440): `'If specs.OperatingEnvironment = EnvironmentType.Harsh AndAlso m.IpRating = 0 Then Return False` — motor IP rating data may be absent from the database, or filtering is deferred.

4. **Receiver filters fully commented out** (lines 1048–1051): both range-margin and channel-count checks are disabled, so all receivers pass to the sort/rank stage without hard constraints.

5. **Telemetry voltage filter also commented out** (lines 979–982): unlike flight controller selection which has strict voltage filtering, telemetry radio selection bypasses it.

6. **Propeller efficiency sort commented out** in `SelectPropellers` — `.ThenByDescending(Function(p As PropellerSpec) p.Efficiency)` is present but disabled.

7. **No two-pass power budget:** avionics draw is estimated flat at `3.0 A` during Task 8; the actual avionics draw is tallied at the end of Task 9 (`TallyRealizedAvionicsCurrent`) but is not fed back into battery/ESC calculations. A comment at lines 129–134 notes this as a known gap.

8. **No coupled motor–propeller analysis anywhere:** selection is purely sequential (motors by thrust → propellers by diameter fit), with no blade-element theory, disk loading, or joint efficiency surface.

9. **`MaxTakeoffMassGrams` is effectively unused by the engine** — it lives on `MissionSpecs` as a user-supplied design limit but `EstimateMtow()` derives its own MTOW from structural mass + iterated battery mass. The property is never read inside `ComponentSelectionEngine.vb`.

10. **`NormaliseMotorCount()`** (lines 1342–1346) maps all non-standard counts (5, 7, 9, etc.) silently to 4. Valid values accepted without mapping: {3, 4, 6, 8, 12}.
