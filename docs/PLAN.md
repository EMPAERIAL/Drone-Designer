# Drone Designer — Physics & Selection Engine Improvement Plan

**Date:** 2026-05-06  
**Scope:** Integrate FAST-UAV aerodynamic physics and component selection improvements into the existing VB.NET WinForms application.  
**Prerequisite reading:** `Core/Services/ComponentSelectionEngine.vb`, `Core/Models/ComponentSpecs.vb`, `Resources/components.json`

---

## Background

The current selection engine (Tasks 7–9 in `ComponentSelectionEngine.vb`) uses heuristic-based rules:

- Propeller hover RPM estimated from static thrust test data via a scaling law
- Motor selection filtered by KV range headroom, not torque capacity
- Hover power estimated as a flat `10 W per 100 g` rule
- Battery capacity sized on a single-phase hover-only energy model
- Air density not corrected for altitude or temperature
- Safety margins hard-coded as private constants, not user-adjustable

The FAST-UAV Python research package uses physics-based aerodynamic coefficients (`Ct`, `Cp`) and an explicit safety-margin (`k`-factor) framework. This plan brings those ideas into the Drone Designer without porting the OpenMDAO optimization framework, which is overkill for a desktop selection tool.

---

## Execution Order

```
Phase 1  →  Phase 2  →  Phase 3  →  Phase 4
   ↑
   Prerequisite for all others.
   Phases 2 and 3 can run in parallel once Phase 1 is done.
   Phase 4 depends on both 2 and 3.

Future Work  (independent — wire in when catalogue is large enough)
```

---

## Phase 1 — Aerodynamic Physics Foundation

**Goal:** Replace the static-thrust scaling heuristic with real propeller aerodynamics using dimensionless thrust (`Ct`) and power (`Cp`) coefficients. Every downstream calculation (motor torque, hover power, battery energy) becomes physics-grounded once this is in place.

---

### 1.1 — Add `Ct` and `Cp` to `PropellerSpec` and `components.json`

**Files:**
- `Core/Models/ComponentSpecs.vb` — `PropellerSpec` class
- `Resources/components.json` — all propeller entries

**What to do:**

Add two new properties to `PropellerSpec`:

```vb
''' <summary>
''' Dimensionless static thrust coefficient.
''' Equation: T = Ct × ρ × n² × D⁴
''' where n is in rev/s, D in metres, ρ in kg/m³, T in Newtons.
''' Source: APC propeller database or manufacturer test data.
''' Fallback when not available: 0.115 (population average for 2-blade MR props).
''' </summary>
Public Property CtStatic As Double

''' <summary>
''' Dimensionless static power coefficient.
''' Equation: P = Cp × ρ × n³ × D⁵
''' where n is in rev/s, D in metres, P in Watts.
''' Fallback when not available: 0.044.
''' </summary>
Public Property CpStatic As Double
```

In `OnDeserialized`, apply fallbacks if values were not populated from JSON:

```vb
If CtStatic <= 0 Then CtStatic = 0.115
If CpStatic <= 0 Then CpStatic = 0.044
```

In `components.json`, add `"ctStatic"` and `"cpStatic"` to each propeller object.
Values for APC propellers can be copied directly from FAST-UAV's catalogue at:
`Resources/FAST-UAV/src/fastuav/data/catalogues/Propeller/APC_propellers_MR.csv`
(columns `Ct_static` and `Cp_static`).
For propellers not in APC, leave the fields absent and the fallback applies.

`ComponentRepository.vb` needs no changes — Newtonsoft.Json deserialises the new fields automatically.

---

### 1.2 — Add Air Density Helper

**File:** `Core/Services/ComponentSelectionEngine.vb`

Replace the inline ISA approximation currently in `BuildPropCandidates` (around line 759) with a named, reusable helper. This makes altitude and temperature corrections available throughout the engine with a single call.

```vb
''' <summary>
''' Calculates air density using the International Standard Atmosphere (ISA) model
''' corrected for user-specified altitude and ambient temperature.
'''
''' Formula:
'''   ρ = ρ_sl × (1 − 2.25577×10⁻⁵ × h)^4.2559 × (T_ISA / T_ambient)
'''
''' Parameters:
'''   altitudeM        — altitude above MSL in metres (use specs.MaxAltitudeMeters)
'''   ambientTempC     — ambient air temperature in °C (use specs.MaxOperatingTempCelsius
'''                      for worst-case hot day; MinOperatingTempCelsius for cold)
''' Returns: air density in kg/m³
''' </summary>
Private Shared Function AirDensityKgM3(altitudeM As Double, ambientTempC As Double) As Double
    Const RhoSeaLevel As Double = 1.225      ' kg/m³, ISA sea-level
    Const TIsaK As Double = 288.15           ' K, ISA sea-level temperature
    Dim pressureRatio = Math.Pow(1.0 - 2.25577e-5 * altitudeM, 4.2559)
    Dim tempRatio = TIsaK / (TIsaK + ambientTempC)
    Return RhoSeaLevel * pressureRatio * tempRatio
End Function
```

Call it at the start of `SelectComponents()` and pass `rho` through to all propeller and motor calculations. Use `specs.MaxAltitudeMeters` and `specs.MaxOperatingTempCelsius` so worst-case conditions are always sized for.

---

### 1.3 — Replace `EstimatePropellerHoverRpm` with Physics-Based `ComputePropellerAero`

**File:** `Core/Services/ComponentSelectionEngine.vb`

The current `EstimatePropellerHoverRpm` (line ~641) uses:
```
RPM_hover = RPM_test × √(T_required / T_test)
```
This only works when static test data exists, uses grams not Newtons, and ignores air density entirely.

Replace with a function that solves the propeller thrust equation for RPM and simultaneously computes hover power and propeller torque — all three are needed in Phases 2 and 3:

```vb
''' <summary>
''' Computes hover RPM, shaft power, and propeller torque from aerodynamic first principles.
'''
''' Governing equations:
'''   T = Ct × ρ × n² × D⁴   →   n = √(T / (Ct × ρ × D⁴))
'''   P = Cp × ρ × n³ × D⁵
'''   Q = P / (2π × n)
'''
''' Parameters:
'''   prop              — propeller to evaluate (uses CtStatic, CpStatic, DiameterInches)
'''   requiredThrustN   — per-motor thrust in Newtons (= requiredThrustGf × 0.00981)
'''   rho               — air density in kg/m³ (from AirDensityKgM3)
'''
''' Returns named tuple: (HoverRpm, HoverPowerW, TorqueNm)
''' </summary>
Private Shared Function ComputePropellerAero(
        prop As PropellerSpec,
        requiredThrustN As Double,
        rho As Double) As (HoverRpm As Double, HoverPowerW As Double, TorqueNm As Double)

    Dim D = prop.DiameterInches * 0.0254   ' inches → metres
    Dim Ct = prop.CtStatic                  ' fallback applied in OnDeserialized
    Dim Cp = prop.CpStatic

    Dim nSquared = requiredThrustN / (Ct * rho * D ^ 4)
    Dim n = Math.Sqrt(Math.Max(0.0, nSquared))  ' rev/s
    Dim rpm = n * 60.0
    Dim powerW = Cp * rho * (n ^ 3) * (D ^ 5)
    Dim torqueNm = If(n > 0.0, powerW / (2.0 * Math.PI * n), 0.0)

    Return (rpm, powerW, torqueNm)
End Function
```

Update `PropCandidate` to store the full aero result:

```vb
Friend Class PropCandidate
    Public Property Prop As PropellerSpec
    Public Property RequiredRpmAtAltitude As Double
    Public Property HoverPowerPerMotorW As Double   ' NEW — used in Phase 3
    Public Property PropTorqueNm As Double          ' NEW — used in Phase 2
    Public Property CellKvTable As List(Of (CellCount As Integer, KvRequired As Double))
End Class
```

In `BuildPropCandidates`, replace the call to `EstimatePropellerHoverRpm` with `ComputePropellerAero`. Pass `rho` computed from Phase 1.2.

---

## Phase 2 — Torque-Based Motor Selection

**Goal:** Select motors by torque capacity, not KV range. This is the physically correct criterion — a motor fails to drive a propeller when it cannot produce enough torque, not when its KV is out of a heuristic band.

---

### 2.1 — Add Torque and Electrical Fields to `MotorSpec`

**File:** `Core/Models/ComponentSpecs.vb` — `MotorSpec` class

```vb
''' <summary>
''' Maximum continuous torque the motor can produce (N·m).
''' Preferred source: manufacturer test data.
''' Derived automatically in OnDeserialized if not provided:
'''   MaxTorqueNm = KtNmPerA × MaxCurrentA
''' </summary>
Public Property MaxTorqueNm As Double

''' <summary>
''' Motor torque constant (N·m/A).
''' Relationship to KV:  Kt = 1 / Kv_SI  where Kv_SI = Kv_rpm_per_V × (2π/60)
''' Derived automatically if not provided:
'''   KtNmPerA = 60 / (2π × KV)
''' </summary>
Public Property KtNmPerA As Double

''' <summary>Winding resistance in Ohms. Used for copper-loss efficiency checks.</summary>
Public Property WindingResistanceOhm As Double

''' <summary>No-load current at nominal voltage (A). Represents friction and iron losses.</summary>
Public Property NoLoadCurrentA As Double
```

In `OnDeserialized`, add derived-value logic (order matters):

```vb
' 1. Derive Kt from KV if not supplied
If KtNmPerA <= 0 AndAlso KV > 0 Then
    KtNmPerA = 60.0 / (2.0 * Math.PI * KV)
End If

' 2. Derive MaxTorqueNm from Kt × Imax if not supplied
If MaxTorqueNm <= 0 AndAlso KtNmPerA > 0 AndAlso MaxCurrentA > 0 Then
    MaxTorqueNm = KtNmPerA * MaxCurrentA
End If
```

Add the corresponding JSON fields `"maxTorqueNm"`, `"ktNmPerA"`, `"windingResistanceOhm"`, `"noLoadCurrentA"` to motor entries in `components.json`. Fields that are derivable can be omitted and the `OnDeserialized` fallback handles them.

---

### 2.2 — Create `SizingPolicy` Class

**File:** `Core/Models/SizingPolicy.vb` — new file

Extracts all safety-margin multipliers from the engine's private constants into a user-visible, injectable data object. This makes margins auditable, profile-tunable, and optimizable in Phase 4.

```vb
Namespace Core.Models

    ''' <summary>
    ''' Safety-margin multipliers (k-factors) applied during component sizing.
    ''' Each factor adds headroom above the minimum calculated requirement.
    '''
    ''' Defaults match FAST-UAV reference values for a general-purpose multirotor.
    ''' Increase margins for harsh environments or safety-critical missions.
    ''' Decrease for minimum-weight racing builds.
    ''' </summary>
    Public Class SizingPolicy

        ''' <summary>
        ''' Motor must produce at least (k × propeller hover torque) as max continuous torque.
        ''' Range: 1.5 (racing, short bursts) to 3.0 (heavy-lift, sustained climb).
        ''' Default 2.0 — motor runs at 50% torque at hover, leaving headroom for gusts.
        ''' </summary>
        Public Property KMotorTorque As Double = 2.0

        ''' <summary>
        ''' Battery pack voltage = motor nominal operating voltage × k.
        ''' Range: 1.1 to 1.5.
        ''' Default 1.3 — ensures cell sag under load does not drop below motor minimum.
        ''' </summary>
        Public Property KBatteryVoltage As Double = 1.3

        ''' <summary>
        ''' Battery usable capacity = calculated mission energy requirement × k.
        ''' Range: 1.1 (calm conditions, known route) to 1.5 (wind, unknowns).
        ''' Default 1.2 — 20% reserve covers throttle spikes, headwind, and capacity aging.
        ''' </summary>
        Public Property KBatteryCapacity As Double = 1.2

        ''' <summary>
        ''' ESC continuous current rating ≥ motor peak current × k.
        ''' Range: 1.1 to 1.5.
        ''' Default 1.25 — 25% thermal headroom for sustained full-throttle flight.
        ''' </summary>
        Public Property KEscCurrent As Double = 1.25

        ''' <summary>Returns a SizingPolicy preset tuned for minimum-weight racing builds.</summary>
        Public Shared Function RacingPreset() As SizingPolicy
            Return New SizingPolicy With {
                .KMotorTorque = 1.5,
                .KBatteryVoltage = 1.1,
                .KBatteryCapacity = 1.1,
                .KEscCurrent = 1.15
            }
        End Function

        ''' <summary>Returns a SizingPolicy preset tuned for harsh-environment / safety-critical missions.</summary>
        Public Shared Function HarshEnvironmentPreset() As SizingPolicy
            Return New SizingPolicy With {
                .KMotorTorque = 3.0,
                .KBatteryVoltage = 1.4,
                .KBatteryCapacity = 1.4,
                .KEscCurrent = 1.5
            }
        End Function

    End Class

End Namespace
```

Add to `MissionSpecs`:

```vb
''' <summary>
''' Safety-margin policy applied during sizing.
''' Default-constructed so existing callers require no changes.
''' </summary>
Public Property SizingPolicy As SizingPolicy = New SizingPolicy()
```

---

### 2.3 — Replace KV-Headroom Filter with Torque Filter

**File:** `Core/Services/ComponentSelectionEngine.vb` — `SelectMotorForCandidate` (line ~796)

**Current primary filter (to be replaced):**
```vb
If motor.KV * vNominal < candidate.RequiredRpmAtAltitude * MotorRpmHeadroomFactor Then
    ' reject
End If
```

**New two-stage filter:**

Stage 1 — Hard torque constraint (physics-based, replaces KV ceiling):
```vb
Dim requiredTorqueNm = candidate.PropTorqueNm * specs.SizingPolicy.KMotorTorque
If motor.MaxTorqueNm < requiredTorqueNm Then
    rejections.Add($"Motor {motor.Id} rejected: MaxTorqueNm {motor.MaxTorqueNm:F3} N·m " &
                   $"< required {requiredTorqueNm:F3} N·m " &
                   $"(prop torque {candidate.PropTorqueNm:F3} × k={specs.SizingPolicy.KMotorTorque}).")
    Continue For
End If
```

Stage 2 — Soft KV fit (keep existing 15% → 25% fallback band, but as a secondary sort criterion, not the primary gate):
```vb
' KV fit used only for ranking, not elimination — torque is the hard gate.
Dim kvFitScore = Math.Abs(motor.KV - kvRequired) / kvRequired
```

Update ranking order:
```vb
Return validMotors _
    .OrderBy(Function(m) Math.Abs(m.MaxTorqueNm - requiredTorqueNm) / requiredTorqueNm) _
    .ThenBy(Function(m) Math.Abs(m.KV - kvRequired) / kvRequired) _
    .ThenByDescending(Function(m) m.Efficiency) _
    .ThenBy(Function(m) m.MassGrams) _
    .FirstOrDefault()
```

---

### 2.4 — Add "Advanced Sizing" Panel to MainForm

**File:** `UI/Forms/MainForm.vb`

Add a collapsible `GroupBox` labelled "Advanced Sizing Margins" beneath the main inputs. Collapsed by default.

Contains four `NumericUpDown` controls bound to `SizingPolicy`:

| Label | Property | Min | Max | Step | Default |
|---|---|---|---|---|---|
| Motor torque margin (k) | `KMotorTorque` | 1.0 | 4.0 | 0.1 | 2.0 |
| Battery voltage margin (k) | `KBatteryVoltage` | 1.0 | 2.0 | 0.05 | 1.3 |
| Battery capacity margin (k) | `KBatteryCapacity` | 1.0 | 2.0 | 0.05 | 1.2 |
| ESC current margin (k) | `KEscCurrent` | 1.0 | 2.0 | 0.05 | 1.25 |

Also add a `ComboBox` for presets: "General Purpose", "Racing", "Harsh Environment". Selecting a preset populates all four fields from the corresponding `SizingPolicy` factory method.

Wire into `BuildMissionSpecs()` in `MainForm.Logic.vb`:
```vb
specs.SizingPolicy = New SizingPolicy With {
    .KMotorTorque     = nudKMotor.Value,
    .KBatteryVoltage  = nudKBatVoltage.Value,
    .KBatteryCapacity = nudKBatCapacity.Value,
    .KEscCurrent      = nudKEsc.Value
}
```

---

## Phase 3 — Phased Mission Energy Model

**Goal:** Replace the single-phase `P_hover × endurance × 1.1` battery sizing with a three-phase model that accounts separately for hover, climb, and cruise. Makes battery selection accurate for survey, delivery, and inspection missions where most time is spent cruising, not hovering.

---

### 3.1 — Add Mission Phase Inputs to `MissionSpecs`

**File:** `Core/Models/MissionSpecs.vb`

```vb
''' <summary>
''' Fraction of total endurance spent hovering (takeoff loiter + landing).
''' Default 0.3. Must satisfy: HoverFraction + ClimbFraction + CruiseFraction = 1.0.
''' </summary>
Public Property HoverTimeFraction As Double = 0.3

''' <summary>Fraction of endurance spent climbing. Default 0.1.</summary>
Public Property ClimbTimeFraction As Double = 0.1

''' <summary>Fraction of endurance spent in level cruise. Default 0.6.</summary>
Public Property CruiseTimeFraction As Double = 0.6

''' <summary>Vertical climb rate in m/s. Default 3.0 m/s.</summary>
Public Property ClimbRateMs As Double = 3.0
```

Add a validation helper used by `ValidateMissionSpecs()`:
```vb
Dim phaseSum = specs.HoverTimeFraction + specs.ClimbTimeFraction + specs.CruiseTimeFraction
If Math.Abs(phaseSum - 1.0) > 0.01 Then
    Throw New ArgumentException("HoverTimeFraction + ClimbTimeFraction + CruiseTimeFraction must sum to 1.0.")
End If
```

Add three corresponding numeric inputs to `MainForm.vb`, grouped under a "Mission Profile Detail" section. Pre-populate sensible defaults per mission profile type (e.g. aerial survey: `Hover=0.1, Climb=0.1, Cruise=0.8`; inspection: `Hover=0.4, Climb=0.2, Cruise=0.4`).

---

### 3.2 — Implement `CalculateMissionEnergy`

**File:** `Core/Services/ComponentSelectionEngine.vb` — new private function

This replaces the existing `E_total = P_hover × endurance × 1.1` inside `CalculatePowerBudget`.

```
Hover power (per motor, from Phase 1):
    P_hover = Cp × ρ × n_hover³ × D⁵

Climb power (momentum theory correction):
    v_induced = √(T / (2 × ρ × A_disk))      where A_disk = π × (D/2)²
    P_climb = P_hover × (V_climb / (2 × v_induced) + √((V_climb / (2 × v_induced))² + 1))
    [This is the exact momentum-theory climb power for an actuator disc in axial flight]

Cruise power (simplified momentum-theory level flight):
    P_cruise = P_hover × (V_cruise / v_induced)^(1/2)
    [Conservative estimate; a blade-element model would improve this further]

Total mission energy (Wh):
    t_total  = FlightEnduranceMinutes / 60          (hours)
    t_hover  = t_total × HoverTimeFraction
    t_climb  = t_total × ClimbTimeFraction
    t_cruise = t_total × CruiseTimeFraction

    E_motors = motorCount × (P_hover × t_hover + P_climb × t_climb + P_cruise × t_cruise)
    E_avionics = AvionicCurrentDrawA × PackVoltage × t_total
    E_required = (E_motors + E_avionics) × SizingPolicy.KBatteryCapacity
```

The `PackVoltage` used for avionics is the battery pack voltage estimated from cell count × 3.7 V.

---

### 3.3 — Surface Energy Breakdown in `SelectionResult`

**File:** Wherever `PowerBudget` is defined (check `Core/Models/PipelineResult.vb` or inline in `SelectionResult`)

Add fields to `PowerBudget`:

```vb
Public Property HoverPhaseEnergyWh As Double
Public Property ClimbPhaseEnergyWh As Double
Public Property CruisePhaseEnergyWh As Double
Public Property AvionicsEnergyWh As Double
Public Property TotalMissionEnergyWh As Double
Public Property EstimatedFlightRangeKm As Double   ' = CruiseSpeedMs × 3.6 × t_cruise_h
```

In the UI summary panel (`MainForm.vb`), display a mini energy breakdown:
```
Hover:    12.4 Wh  (30%)
Climb:     4.1 Wh  (10%)
Cruise:   24.6 Wh  (60%)
─────────────────────────
Total:    41.1 Wh  (+20% margin → 49.3 Wh required)
Est. range: 14.2 km
```

---

## Phase 4 — k-Factor Sweep Optimizer

**Goal:** Automatically find the minimum-weight feasible design by sweeping the k-factor space rather than locking in fixed margins. This delivers the core value of FAST-UAV's optimization loop without the OpenMDAO machinery.

---

### 4.1 — Implement `SizingOptimizer`

**File:** `Core/Services/SizingOptimizer.vb` — new class

Algorithm: **coordinate grid sweep** over the four k-factors. Each evaluation is a full `SelectComponents()` call, which runs in under a millisecond, making the full sweep finish in under 100 ms.

```vb
Namespace Core.Services

    Public Class SizingOptimizer

        Private ReadOnly _engine As ComponentSelectionEngine

        Public Sub New(engine As ComponentSelectionEngine)
            _engine = engine
        End Sub

        ''' <summary>
        ''' Sweeps a grid of SizingPolicy k-factor combinations and returns the
        ''' feasible result with the lowest estimated MTOW.
        '''
        ''' Search space (64 evaluations):
        '''   KMotorTorque:     {1.5, 2.0, 2.5, 3.0}
        '''   KBatteryCapacity: {1.1, 1.2, 1.3, 1.4}
        '''   KBatteryVoltage:  {1.1, 1.2, 1.3}
        '''   KEscCurrent:      {1.15, 1.25}   (less impact on mass, coarser grid)
        ''' </summary>
        Public Function FindMinimumMassDesign(specs As MissionSpecs) As OptimizerResult
            Dim kMotorValues = {1.5, 2.0, 2.5, 3.0}
            Dim kBatCapValues = {1.1, 1.2, 1.3, 1.4}
            Dim kBatVoltValues = {1.1, 1.2, 1.3}
            Dim kEscValues = {1.15, 1.25}

            Dim bestResult As SelectionResult = Nothing
            Dim bestPolicy As SizingPolicy = Nothing
            Dim bestMtow As Double = Double.MaxValue
            Dim allAttempts As Integer = 0
            Dim feasibleAttempts As Integer = 0

            For Each km In kMotorValues
                For Each kbc In kBatCapValues
                    For Each kbv In kBatVoltValues
                        For Each ke In kEscValues
                            allAttempts += 1
                            Dim policy As New SizingPolicy With {
                                .KMotorTorque = km,
                                .KBatteryCapacity = kbc,
                                .KBatteryVoltage = kbv,
                                .KEscCurrent = ke
                            }
                            Dim candidate As MissionSpecs = specs.ShallowCopyWithPolicy(policy)

                            Try
                                Dim result = _engine.SelectComponents(candidate)
                                If IsFeasible(result) Then
                                    feasibleAttempts += 1
                                    If result.EstimatedMtowGrams < bestMtow Then
                                        bestMtow = result.EstimatedMtowGrams
                                        bestResult = result
                                        bestPolicy = policy
                                    End If
                                End If
                            Catch ex As ComponentSelectionException
                                ' Infeasible combination — skip silently
                            End Try
                        Next
                    Next
                Next
            Next

            If bestResult Is Nothing Then
                Throw New ComponentSelectionException(
                    "No feasible design found across all k-factor combinations. " &
                    "Relax mission requirements (endurance, payload, altitude) or expand the component database.")
            End If

            Return New OptimizerResult With {
                .BestResult = bestResult,
                .WinningPolicy = bestPolicy,
                .TotalEvaluations = allAttempts,
                .FeasibleEvaluations = feasibleAttempts,
                .MtowReductionVsDefaultG = _engine.SelectComponents(specs).EstimatedMtowGrams - bestMtow
            }
        End Function

        ''' <summary>
        ''' A result is feasible when all components were found and no
        ''' critical warnings were raised (e.g. battery undersized, motor missing).
        ''' </summary>
        Private Shared Function IsFeasible(result As SelectionResult) As Boolean
            If result.SelectedMotors Is Nothing OrElse result.SelectedMotors.Count = 0 Then Return False
            If result.SelectedBatteries Is Nothing OrElse result.SelectedBatteries.Count = 0 Then Return False
            If result.SelectedEscs Is Nothing OrElse result.SelectedEscs.Count = 0 Then Return False
            If result.Warnings.Any(Function(w) w.StartsWith("[CRITICAL]")) Then Return False
            Return True
        End Function

    End Class

    Public Class OptimizerResult
        Public Property BestResult As SelectionResult
        Public Property WinningPolicy As SizingPolicy
        Public Property TotalEvaluations As Integer
        Public Property FeasibleEvaluations As Integer
        Public Property MtowReductionVsDefaultG As Double
    End Class

End Namespace
```

Add `ShallowCopyWithPolicy` to `MissionSpecs`:
```vb
Public Function ShallowCopyWithPolicy(policy As SizingPolicy) As MissionSpecs
    Dim copy = DirectCast(Me.MemberwiseClone(), MissionSpecs)
    copy.SizingPolicy = policy
    Return copy
End Function
```

---

### 4.2 — UI Integration

**File:** `UI/Forms/MainForm.vb` and `MainForm.Logic.vb`

Add an "Optimize for minimum weight" `CheckBox` in the input tab, near the "Select Components" button.

In `OnSelectComponentsAsync`:
```vb
If chkOptimize.Checked Then
    Dim optimizer As New SizingOptimizer(_engine)
    Dim optResult = optimizer.FindMinimumMassDesign(specs)
    DisplaySelectionResult(optResult.BestResult)
    ShowOptimizerSummary(optResult)
Else
    Dim result = _engine.SelectComponents(specs)
    DisplaySelectionResult(result)
End If
```

`ShowOptimizerSummary` adds a panel or message box showing:
```
Optimizer found minimum-mass design in 64 evaluations (18 feasible).
Winning margins:  Motor torque k=2.0 | Battery capacity k=1.2 | Battery voltage k=1.1 | ESC k=1.15
MTOW reduction vs default margins: −47 g
```

---

## Future Work — NearestNeighbor / KDTree Component Selection

**Trigger:** Implement when the component catalogue reaches ~300+ entries per category.
Below that threshold, the current `Where(...).OrderBy(...).Take(5)` approach is adequate and simpler.

**What it solves:** With large catalogues, hard-constraint filtering either misses good candidates (too strict) or returns hundreds of survivors ranked by a single metric (too loose). NearestNeighbor finds the closest match across all relevant parameters simultaneously, with per-dimension constraint direction enforced.

---

### FW.1 — `StandardScaler` Class

**File:** `Core/Services/Catalogues/StandardScaler.vb` — new file

Normalises each feature column to zero mean and unit variance so parameters with different scales (torque 0–5 N·m, KV 100–3000) contribute equally to distance.

```vb
Public Class StandardScaler
    Private _means As Double()
    Private _stds As Double()

    ''' Fit on the full catalogue matrix (rows = components, cols = features).
    Public Sub Fit(data As Double(,))
        Dim cols = data.GetLength(1)
        _means = New Double(cols - 1) {}
        _stds  = New Double(cols - 1) {}
        ' Compute column mean and std dev, store for Transform
    End Sub

    ''' Normalise a single query vector using fitted parameters.
    Public Function Transform(row As Double()) As Double()
        Return row.Select(Function(v, i) (_stds(i) > 0) ? (v - _means(i)) / _stds(i) : 0.0).ToArray()
    End Function
End Class
```

---

### FW.2 — `KDTree` Class

**File:** `Core/Services/Catalogues/KDTree.vb` — new file

A simple recursive KD-tree over normalized feature vectors. Supports 2–5 dimensional spaces (sufficient for all component types). At catalogue sizes under 10,000 entries a basic median-split construction is optimal enough.

```
Construction:
    Split on the dimension with highest variance at each node.
    Leaf threshold: 8 points (stop splitting, do linear scan).
    Build once at startup, cached on ComponentRepository.

Query:
    Walk the tree to the leaf containing the query point.
    Backtrack and check sibling branches within current best distance.
    Return indices of k=5 nearest neighbours.
    Typical query time: < 1 µs for catalogues under 5,000 points.
```

---

### FW.3 — `NearestNeighborSelector` Generic Class

**File:** `Core/Services/Catalogues/NearestNeighborSelector.vb` — new file

```vb
Public Enum SelectionMode
    ''' Catalogue value must be >= query value (e.g. motor torque, battery capacity).
    [Next]
    ''' Catalogue value must be <= query value.
    Previous
    ''' Closest value in either direction (e.g. KV, pitch).
    Average
End Enum

Public Class NearestNeighborSelector(Of T)

    Private ReadOnly _scaler As StandardScaler
    Private ReadOnly _tree As KDTree
    Private ReadOnly _catalogue As List(Of T)
    Private ReadOnly _featureExtractor As Func(Of T, Double())

    Public Sub New(catalogue As List(Of T), featureExtractor As Func(Of T, Double()))
        _catalogue = catalogue
        _featureExtractor = featureExtractor
        Dim matrix = BuildMatrix(catalogue, featureExtractor)
        _scaler = New StandardScaler()
        _scaler.Fit(matrix)
        Dim normalized = NormalizeMatrix(matrix)
        _tree = New KDTree(normalized)
    End Sub

    ''' <summary>
    ''' Finds the catalogue entry that best satisfies the query subject to
    ''' per-dimension constraint directions.
    '''
    ''' Algorithm:
    '''   1. Scale query point using fitted StandardScaler.
    '''   2. KDTree returns k=10 nearest neighbours.
    '''   3. Filter: for each 'Next' dimension, keep only candidates where
    '''      catalogue_value >= query_value.  For 'Previous', the reverse.
    '''   4. From survivors, return the one with minimum scaled Euclidean distance.
    '''   5. If no survivor (constraints too tight), add a warning and return
    '''      the nearest 'Next'-satisfying candidate on the most critical dimension.
    ''' </summary>
    Public Function Predict(
            query As Double(),
            criteria As SelectionMode(),
            Optional warnings As List(Of String) = Nothing) As T
        ...
    End Function

End Class
```

---

### FW.4 — Per-Component Feature Dimensions

| Component | Dimension 1 | Dimension 2 | Dimension 3 | Modes |
|---|---|---|---|---|
| Motor | `MaxTorqueNm` | `KV` | `MassGrams` | `Next`, `Average`, `Average` |
| Propeller | `DiameterInches` | `PitchInches` | — | `Next`, `Average` |
| Battery | `VoltageV` | `CapacityMah` | `CRating` | `Next`, `Next`, `Next` |
| ESC | `MaxCurrentA` | `MaxVoltageV` | — | `Next`, `Next` |

---

### FW.5 — Catalogue Pre-processing at Startup

**File:** `Core/Data/ComponentRepository.vb`

After loading `components.json`, fit one `NearestNeighborSelector` per component category. Cache it on the repository instance. This runs once at application startup in a background thread — adds under 50 ms for catalogues under 5,000 entries.

```vb
Private _motorSelector As NearestNeighborSelector(Of MotorSpec)
Private _propSelector  As NearestNeighborSelector(Of PropellerSpec)
Private _battSelector  As NearestNeighborSelector(Of BatterySpec)
Private _escSelector   As NearestNeighborSelector(Of ESCSpec)

Private Sub BuildSelectors()
    _motorSelector = New NearestNeighborSelector(Of MotorSpec)(
        GetMotors().Cast(Of MotorSpec).ToList(),
        Function(m) {m.MaxTorqueNm, m.KV, m.MassGrams})
    ' ... repeat for each category
End Sub
```

---

### FW.6 — Replace Filter+Rank in `SelectMotorForCandidate`

After FW.1–5 are in place, swap the `Where(...).OrderBy(...).Take(5)` blocks in `SelectMotorForCandidate`, `SelectEscs`, `SelectBatteryFromMassBudget`, and `BuildPropCandidates` with calls to the relevant `NearestNeighborSelector.Predict()`.

The hard-constraint checks (voltage range, shaft fit) remain as pre-filters before passing to the selector — NearestNeighbor handles the multi-dimensional closeness, not categorical incompatibilities.

---

## File Change Summary

| File | Phase | Change |
|---|---|---|
| `Core/Models/ComponentSpecs.vb` | 1, 2 | Add `CtStatic`, `CpStatic` to `PropellerSpec`; add `MaxTorqueNm`, `KtNmPerA`, `WindingResistanceOhm`, `NoLoadCurrentA` to `MotorSpec` |
| `Core/Models/MissionSpecs.vb` | 2, 3 | Add `SizingPolicy` property; add `HoverTimeFraction`, `ClimbTimeFraction`, `CruiseTimeFraction`, `ClimbRateMs`; add `ShallowCopyWithPolicy` |
| `Core/Models/SizingPolicy.vb` | 2 | New file — k-factor data class with presets |
| `Core/Models/PipelineResult.vb` | 3 | Add phase energy breakdown fields to `PowerBudget` |
| `Core/Services/ComponentSelectionEngine.vb` | 1, 2, 3 | Add `AirDensityKgM3`; replace `EstimatePropellerHoverRpm` with `ComputePropellerAero`; replace KV filter with torque filter; replace single-phase energy with `CalculateMissionEnergy` |
| `Core/Services/SizingOptimizer.vb` | 4 | New file — k-factor grid sweep |
| `Resources/components.json` | 1, 2 | Add `ctStatic`, `cpStatic` to propellers; add `maxTorqueNm`, `ktNmPerA`, `windingResistanceOhm`, `noLoadCurrentA` to motors |
| `UI/Forms/MainForm.vb` | 2, 3, 4 | Add "Advanced Sizing" panel; add mission phase fraction inputs; add optimizer toggle |
| `UI/Forms/MainForm.Logic.vb` | 2, 4 | Wire `SizingPolicy` inputs into `BuildMissionSpecs`; wire optimizer into `OnSelectComponentsAsync` |
| `Core/Services/Catalogues/StandardScaler.vb` | Future | New file |
| `Core/Services/Catalogues/KDTree.vb` | Future | New file |
| `Core/Services/Catalogues/NearestNeighborSelector.vb` | Future | New file |
| `Core/Data/ComponentRepository.vb` | Future | Build and cache selectors at startup |
