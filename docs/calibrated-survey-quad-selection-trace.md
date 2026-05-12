# Calibrated Survey Quad Selection Trace

This note traces the current `ComponentSelectionEngine` behavior for the `Calibrated Survey Quad (Pass)` row in [Test/Scenarios/test_scenarios.csv](../Test/Scenarios/test_scenarios.csv).

It also records the main limitation of the current MVP selector and outlines a clean path for introducing alternative prop/motor pairings once the component database is widened and cleaned up.

## Scenario Input

CSV row:

`Calibrated Survey Quad (Pass),0.1667,1,18,50,7,1500,0,800,0,0,10,35,0,0,1,3,0`

Mapped mission inputs used by the engine:

- Endurance: `10.0 min`
- Range: `1.0 km`
- Cruise speed: `18 km/h = 5.0 m/s`
- Max altitude: `50 m`
- Max wind: `7 km/h = 1.94 m/s`
- MTOW: `1500 g`
- Payload: `0 g`
- Frame size: `800 mm`
- Configuration: `Quadcopter`
- Motor count: `4`
- Mission profile: `Survey`
- Operating environment: `Standard`
- Regulatory class: `BVLOS`

Relevant source:

- [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb)
- [UI/Forms/MainForm.Logic.vb](../UI/Forms/MainForm.Logic.vb)

## Current Engine Path

The active selection entry point is `SelectComponents()` in [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:421).

Important behavior:

- The current path uses `specs.MaxTakeoffMassGrams` directly.
- It does not run the older iterative MTOW estimation path for this scenario.
- The flow is prop-first, then motor selection, then battery, ESC, PDB, and avionics.

### 1. Thrust Requirement

At [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:424):

- `mtowG = 1500`
- `motorCount = 4`
- default thrust-to-weight ratio = `2.0`

Per-motor thrust requirement:

`thrustPerMotorGf = 1500 * 2.0 / 4 = 750 gf`

### 2. Frame-Constrained Maximum Prop Diameter

At [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:428) and [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:176):

- `armMm = 800 / 2 = 400 mm`
- quad prop-to-arm ratio = `0.9 * sqrt(2) = 1.2728`

Maximum allowed prop diameter:

`maxPropDiamIn = (1.2728 * 400) / 25.4 = 20.04 in`

### 3. Air Density

At [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:429) and [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:683):

- altitude = `50 m`
- ambient temperature = `35 C`

Computed air density:

`rho ~= 1.087 kg/m^3`

### 4. Prop Candidate Generation

At [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:820), each propeller is evaluated for:

- frame clearance
- required hover RPM
- hover power
- torque
- feasible cell/KV target table for `3S`, `4S`, `6S`, `8S`

Because the mission is not racing, candidates are sorted by diameter descending at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:860).

Observed candidate order for this scenario:

1. `prop_007` `T18x6.1 Carbon Fiber`
2. `prop_003` `P16x5.4 CF Folding`
3. `prop_005` `Tarot 1555 Folding CF`
4. `prop_004` `APC 10x4.7 SF`
5. `prop_008` `T-Style 10x4.7 CF Survey`
6. `prop_002` `T-Style 9x4.5 Inch`
7. `prop_001` `HQProp 5.1x4.6x3-V1S`

Computed hover RPMs from the live run:

- `prop_007`: `2252 RPM`
- `prop_003`: `2917 RPM`
- `prop_005`: `3272 RPM`
- `prop_004`: `6450 RPM`
- `prop_008`: `6585 RPM`
- `prop_002`: `8623 RPM`
- `prop_001`: `30180 RPM`

### 5. First Winning Prop/Motor/Cell Combination

At [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:933), the engine loops:

1. prop candidates in ranked order
2. cell counts in ascending order
3. motors filtered and ranked for that prop/cell pair

It returns the first successful pairing and stops.

This is the most important current MVP behavior.

### 6. Why `prop_007` Wins

The first candidate is:

- `prop_007`
- model: `T18x6.1 Carbon Fiber`
- diameter: `18 in`
- pitch: `6.1 in`
- bore: `8 mm`
- mass: `68 g`
- max RPM: `5500`
- static thrust: `4200 gf`
- `Ct = 0.11`
- `Cp = 0.04`

For this prop, the motor headroom constant is `1.4` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:159), and nominal LiPo cell voltage is `3.7 V` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:222).

Target KVs for this prop:

- `3S -> 284`
- `4S -> 213`
- `6S -> 142`
- `8S -> 106`

### 7. Why `mot_008` Wins

Motor filtering happens at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:880).

Hard filters:

- `MaxThrustGrams >= requiredThrustPerMotorGf`
- nominal voltage must be inside motor min/max voltage
- `MaxTorqueNm >= requiredTorqueNm`
- prop diameter must be inside motor supported prop diameter range
- shaft/bore fit must be between `0.0` and `0.5 mm`

The winning motor was:

- `mot_008`
- model: `Antigravity MN8014 KV120`
- KV: `120`
- max torque: `3.183 N*m`
- max thrust: `7800 gf`
- voltage range: `14.8 V` to `44.4 V`
- prop range: `18 in` to `22 in`
- shaft diameter: `8 mm`
- mass: `196 g`
- efficiency: `0.87`

Another nearby candidate for the same large prop was:

- `mot_003`
- model: `U8 Lite KV100`
- max torque: `1.910 N*m`
- max thrust: `4800 gf`
- voltage range: `22.2 V` to `51.8 V`
- prop range: `18 in` to `24 in`
- shaft diameter: `8 mm`
- mass: `196 g`
- efficiency: `0.92`

For `prop_007`:

- `3S` fails on voltage minimum for both large-prop motors.
- `4S` still fails `mot_003` because `14.8 V < 22.2 V`.
- `4S` passes `mot_008` because:
- thrust passes
- voltage passes
- prop range passes
- shaft fit passes
- torque passes

Once `prop_007 + 4S + mot_008` succeeds, the search exits immediately.

That is why the engine never reaches smaller, possibly more appropriate alternatives like `10 in` survey props paired with smaller motors.

## Final Selected Components

Live run result:

- Motor: `mot_008` `Antigravity MN8014 KV120`
- Propeller: `prop_007` `T18x6.1 Carbon Fiber`
- Battery: `bat_001` `R-Line Version 5.0 1550mAh 4S`
- ESC: `esc_006` `Alpha 60A HV`
- PDB: `pdb_001` `HV Power Distribution Board 300A`
- Flight controller: `fc_001` `Kakute H7 V1.3`
- GPS: `gps_003` `Here+ RTK GPS v2`
- Telemetry radio: `tel_007` `Herelink Air Unit`
- Receiver: `rec_001` `R-XSR`
- Camera: `cam_009` `Zenmuse P1`

## Power Budget

Computed power budget from the live run:

- Cell count: `4S`
- Nominal voltage: `14.8 V`
- Motor peak current: `40.0 A`
- Motor hover current: `1.19 A`
- Total peak current: `163.0 A`
- Total average current: `7.77 A`
- Hover system power: `115 W`
- Peak system power: `2412.4 W`

Relevant source:

- [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1160)

## Battery Selection

Battery selection is handled by `SelectBatteryFromMassBudget()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:953).

Current logic:

- compute remaining battery mass budget after airframe, motors, props, and payload
- require exact cell count match
- require battery mass <= remaining battery mass budget
- require continuous current >= `peakSystemCurrentA * 1.15`
- sort by capacity descending
- take the first one

For this run, the chosen battery was:

- `bat_001`
- `R-Line Version 5.0 1550mAh 4S`
- `4S`
- `1550 mAh`
- `150 C`
- mass `170 g`

Note: the power budget reports a required capacity around `3250 mAh`, but `SelectBatteryFromMassBudget()` does not currently enforce mission capacity. It only enforces cell count, mass budget, and discharge capability.

## ESC Selection

ESC selection is handled by `SelectEscs()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1360).

Current logic:

- require ESC continuous current >= `motorPeakCurrentA * 1.25`
- require ESC max voltage >= pack nominal voltage
- exclude all-in-one ESCs
- sort by current ascending, then mass ascending

For this run:

- required ESC current = `40 * 1.25 = 50 A`
- selected ESC = `esc_006` `Alpha 60A HV`

## PDB Selection

PDB selection is handled by `SelectPdb()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1409).

Current logic:

- require PDB continuous current >= `TotalPeakCurrentA * 1.30`
- require max input voltage >= pack nominal voltage
- require enough ESC pads for motor count
- sort by BEC presence descending, then mass ascending

For this run:

- required PDB current = `163 * 1.30 = 211.9 A`
- selected PDB = `pdb_001` `HV Power Distribution Board 300A`

## Avionics Selection

### Flight Controller

Selected by `SelectFlightController()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1493).

For survey missions:

- blackbox / SD-card logging is required
- racing loop-rate constraint does not apply
- harsh-environment temperature constraint does not apply

Selected:

- `fc_001` `Kakute H7 V1.3`

### GPS

Selected by `SelectGpsModule()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1575).

For `Survey`:

- required horizontal accuracy <= `2.5 m`
- compass required

Selected:

- `gps_003` `Here+ RTK GPS v2`
- horizontal accuracy `0.025 m`
- compass present

### Telemetry

Selected by `SelectTelemetryRadio()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1658).

For range:

- required telemetry range = `1.0 km * 1.25 = 1.25 km`

Selected:

- `tel_007` `Herelink Air Unit`
- max range `10 km`

### Receiver

Selected by `SelectReceiver()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1736).

Important current note:

- the intended range and channel-count hard filters are present in comments
- but the active checks are currently commented out

Selected:

- `rec_001` `R-XSR`

### Camera

Selected by `SelectCamera()` at [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:1824).

For `Mapping` and `Survey`:

- required horizontal resolution >= mapping threshold
- then sort by resolution descending, stabilisation descending, mass ascending

Selected:

- `cam_009` `Zenmuse P1`
- horizontal resolution `8192 px`

## Warnings Returned

Warnings from the live run:

- `Endurance short: 7.7 / 10.0 min (76%)`
- `Range OK: 2.3 / 1.0 km`

Relevant source:

- [Core/Services/ComponentSelectionEngine.vb](../Core/Services/ComponentSelectionEngine.vb:999)

## MVP Limitation

The current selector is acceptable for the MVP because it gives one valid answer quickly, but its behavior is intentionally narrow:

- prop candidates are tried in a fixed order
- cell counts are tried in ascending order
- only the first successful prop/motor/cell combination is returned
- no global comparison is made across all valid combinations

This means:

- a very large prop can dominate because it is tried first
- an oversized motor can be accepted even if a smaller prop and lighter motor would also pass
- the user does not see alternative valid propulsion sets

## How To Introduce Alternative Pairs

Once the database is broader and cleaner, the next step should be to separate:

- feasibility
- ranking
- presentation

### Proposed Data Model

Introduce a propulsion-combination result type such as:

```vb
Friend Class PropulsionCombination
    Public Property Prop As PropellerSpec
    Public Property Motor As MotorSpec
    Public Property CellCount As Integer
    Public Property KvRequired As Double
    Public Property HoverRpm As Double
    Public Property HoverPowerPerMotorW As Double
    Public Property PropTorqueNm As Double
    Public Property MotorPeakCurrentA As Double
    Public Property MotorHoverCurrentA As Double
    Public Property Score As Double
    Public Property Notes As List(Of String)
End Class
```

### Proposed Selection Flow

Instead of returning on first success:

1. Build all prop candidates.
2. For each prop candidate, test all allowed cell counts.
3. For each prop/cell pair, collect all motors that pass the hard filters.
4. Rank the passing motors for that prop/cell pair.
5. Build a flat list of all valid `PropulsionCombination` objects.
6. Apply a global ranking policy.
7. Return:
- the best overall combination
- alternative combinations
- grouped alternatives by prop

### Recommended Output Shape

The engine could expose:

```vb
Public Property RecommendedPropulsion As PropulsionCombination
Public Property AlternativePropulsion As New List(Of PropulsionCombination)
Public Property PropulsionGroups As New List(Of PropulsionGroup)
```

Where each prop group contains:

- one propeller
- all passing motors for that prop
- optionally split again by cell count

That would let the UI show:

- best overall pair
- all passing pairs for the chosen prop
- all passing props with their best motor

### Recommended Ranking Strategy

Keep the current hard filters exactly as they are first.

Then add a global score using weighted criteria such as:

- total propulsion mass
- hover power
- torque headroom closeness
- KV closeness
- efficiency
- battery feasibility
- mission fit

Example policy options:

- `MinimumMass`
- `MaximumEndurance`
- `Balanced`
- `HighReliability`
- `SurveyOptimized`

This is better than embedding too much behavior in one fixed sort order.

### Recommended UI Behavior

For the MVP-plus stage, the UI should show:

- one recommended propulsion set
- 3 to 5 alternatives
- a reason label for each alternative

Example labels:

- `Lighter`
- `Lower hover power`
- `More torque headroom`
- `Smaller prop footprint`
- `Better survey match`

### Recommended Incremental Refactor

The safest implementation sequence is:

1. Keep current hard filters unchanged.
2. Replace early return in `SelectPropellerAndMotor()` with collection of all passing combinations.
3. Add a global scorer.
4. Return one recommended pair plus alternatives.
5. Update the UI to display alternative propulsion options.

This keeps the MVP behavior stable while opening the path to better pair selection later.

## Practical Note For The Team

Given the current narrow and partially erroneous component database, the present behavior is acceptable for the MVP.

The immediate priority should remain:

- cleaning bad component records
- widening coverage across motor/prop/battery classes
- normalizing units and constraints

After that, alternative-pair support will become much more useful, because the engine will have enough valid combinations to rank meaningfully instead of just finding the first one that works.
