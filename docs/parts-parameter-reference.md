# Parts Parameter Reference

This document defines the SolidWorks global variable names (`DD_` prefix) and their
sources for every part that needs to be built. Each designer reads this, creates the
template `.SLDPRT` with those variables in the equation manager, and writes the macro
that sets them. Follow the checklist in `solidworks-macro-pipeline.md` for the macro
setup steps.

---

## How every macro works (quick recap)

1. `MacroRunner` injects the parameters as custom document properties before the macro runs.
2. The macro reads them with `swDoc.CustomInfo2("", "PARAMETER_NAME")`.
3. The macro writes the values into the equation manager as global variables (`DD_` names).
4. The macro calls `eqMgr.EvaluateAll` then `swDoc.ForceRebuild3 True`.

See `Resources/SolidWorks/Macros/MotorMount1.swb` for the reference implementation.

---

## Part 1 — Carbon Fiber Arm Tube

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_ArmLengthMm` | Tube length (arm reach) | Computed: propeller radius + clearance margin + body radius |
| `DD_ArmOdMm` | Tube outer diameter | Motor max thrust → `ComputeArmTubeOdMm()` (10 / 12 / 16 / 20 / 25 mm stock) |
| `DD_ArmIdMm` | Tube inner diameter | Depends on the available standard carbon tube stock |

**Parameter keys injected by MacroRunner (read these in the macro):**

```
ARM_LENGTH_MM
ARM_OD_MM
ARM_ID_MM
```

**Arm length formula:**
`ArmLengthMm = (PropDiameterMm / 2) + clearance_margin + body_radius`
Propeller diameter comes from `PropellerSpec.DiameterInches × 25.4`.

---

## Part 2 — Body-to-Arm Connection (Fixed)

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_ArmOdMm` | Arm tube outer diameter (bore to grip) | Same as arm tube `DD_ArmOdMm` |
| `DD_ClampLengthMm` | Clamp length along the arm | Design choice (typically 1.5 × arm OD) |
| `DD_ClampBoltMm` | Clamping bolt diameter | Design choice (M3 = 3.0, M4 = 4.0) |
| `DD_ClampBoltSpacingMm` | Spacing between clamp bolts | Derived from arm OD |
| `DD_ChassisAttachSpacingMm` | Bolt spacing for chassis attachment | Must match chassis plate hole pattern |
| `DD_ChassisAttachBoltMm` | Chassis attachment bolt diameter | Design choice (M3 = 3.0) |
| `DD_WallThicknessMm` | Part wall / material thickness | Design choice (3–5 mm for carbon or aluminium) |

**Parameter keys injected by MacroRunner:**

```
ARM_OD_MM
CLAMP_LENGTH_MM
CLAMP_BOLT_MM
CLAMP_BOLT_SPACING_MM
CHASSIS_ATTACH_SPACING_MM
CHASSIS_ATTACH_BOLT_MM
WALL_THICKNESS_MM
```

---

## Part 3 — Chassis Plates

The chassis receives parameters from several components at once. Group them in the
macro by source for clarity.

### From flight controller spec

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_FcMountPatternMm` | Mounting hole pattern (square side length) | `FlightControllerSpec` — standard values: 20, 25.5, 30.5 mm |
| `DD_FcLengthMm` | FC board length | `FlightControllerSpec.Dimensions.Length` |
| `DD_FcWidthMm` | FC board width | `FlightControllerSpec.Dimensions.Width` |
| `DD_FcHoleMm` | FC mounting hole diameter | M3 clearance = 3.2 mm |

### From onboard computer spec

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_OcLengthMm` | Board length | `OnboardComputerSpec.Dimensions.Length` |
| `DD_OcWidthMm` | Board width | `OnboardComputerSpec.Dimensions.Width` |
| `DD_OcMountPatternMm` | Mounting hole pattern | `OnboardComputerSpec` (varies by board) |
| `DD_OcHoleMm` | Mounting hole diameter | M2.5 = 2.7 mm or M3 = 3.2 mm |

### From GPS module spec

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_GpsDiamMm` | Module diameter (if circular) | `GpsSpec.Dimensions` |
| `DD_GpsLengthMm` | Module length (if rectangular) | `GpsSpec.Dimensions.Length` |
| `DD_GpsWidthMm` | Module width | `GpsSpec.Dimensions.Width` |
| `DD_GpsHoleMm` | Mount hole diameter | M2 = 2.2 mm or M3 = 3.2 mm |

### From battery spec

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_BattLengthMm` | Battery length | `BatterySpec.Dimensions.Length` |
| `DD_BattWidthMm` | Battery width | `BatterySpec.Dimensions.Width` |
| `DD_BattHeightMm` | Battery height | `BatterySpec.Dimensions.Height` |

### From frame / mission config

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_ArmCount` | Number of arms | `MissionSpecs.Configuration` |
| `DD_ChassisRadiusMm` | Chassis outer radius | Computed from arm count and attachment geometry |
| `DD_PlateThicknessMm` | Plate thickness | Design choice (2–4 mm carbon fibre) |
| `DD_ArmAttachSpacingMm` | Arm attachment bolt spacing | Must match body-to-arm connection |

**Parameter keys injected by MacroRunner:**

```
FC_MOUNT_PATTERN_MM
FC_LENGTH_MM
FC_WIDTH_MM
FC_HOLE_MM
OC_LENGTH_MM
OC_WIDTH_MM
OC_MOUNT_PATTERN_MM
OC_HOLE_MM
GPS_DIAM_MM
GPS_LENGTH_MM
GPS_WIDTH_MM
GPS_HOLE_MM
BATT_LENGTH_MM
BATT_WIDTH_MM
BATT_HEIGHT_MM
ARM_COUNT
CHASSIS_RADIUS_MM
PLATE_THICKNESS_MM
ARM_ATTACH_SPACING_MM
```

---

## Part 4 — Landing Gear Connection Part

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_LgTubeOdMm` | Landing gear tube outer diameter (bore to grip) | Computed from total drone weight |
| `DD_LgTubeIdMm` | Landing gear tube inner diameter | Design choice / standard tube stock |
| `DD_LgAngleDeg` | Attachment angle relative to chassis | Design choice (0° vertical or 10–15° outward splay) |
| `DD_LgMountSpacingMm` | Chassis attachment bolt spacing | Must match chassis plate hole pattern |
| `DD_LgMountBoltMm` | Chassis attachment bolt diameter | Design choice (M3 = 3.0) |
| `DD_WallThicknessMm` | Part wall / material thickness | Design choice |

**Parameter keys injected by MacroRunner:**

```
LG_TUBE_OD_MM
LG_TUBE_ID_MM
LG_ANGLE_DEG
LG_MOUNT_SPACING_MM
LG_MOUNT_BOLT_MM
WALL_THICKNESS_MM
```

---

## Part 5 — Landing Gear Tubes

| DD_ Variable | Parameter | Source |
|---|---|---|
| `DD_LgLengthMm` | Tube length (leg height) | Computed: battery height + prop ground-tilt clearance + margin |
| `DD_LgOdMm` | Tube outer diameter | Same value as `DD_LgTubeOdMm` in connection part |
| `DD_LgIdMm` | Tube inner diameter | Design choice / standard tube stock |

**Parameter keys injected by MacroRunner:**

```
LG_LENGTH_MM
LG_OD_MM
LG_ID_MM
```

**Minimum leg length formula:**
```
LgLengthMm = BattHeightMm + (PropDiameterMm × sin(MaxTiltAngleDeg)) + ground_clearance_margin
```
`MaxTiltAngleDeg` is typically 15°. `ground_clearance_margin` is typically 20–30 mm.

---

## Checklist for each designer

1. **Create the SolidWorks template** (`.SLDPRT`) with all the `DD_` variables for
   your part defined as global variables in the equation manager
   (Tools → Equations → add each one with a default value).

2. **Write the macro code** as a `.swb` text file. Use `MotorMount1.swb` as the
   reference — the pattern is:
   - Read each parameter from custom properties using `ReadProp(swDoc, "KEY", default)`
   - Write each value into the equation manager using `SetGlobalVar(eqMgr, "DD_NAME", value)`
   - Call `eqMgr.EvaluateAll` then `swDoc.ForceRebuild3 True`

3. **Create the `.swp`** by opening SolidWorks → Tools → Macros → Edit → open your
   `.swp`, paste your code, add the SolidWorks 2026 type library reference
   (Tools → References), save.

4. **Note the exact module name** shown in the VBA Project Explorer — this is what
   goes into the pipeline call as the third argument to `RunMacroOnTemplate`.

5. **Rebuild the project** so the `.swp` is copied to `bin\Debug\Resources\SolidWorks\Macros\`.

6. **Tell the pipeline lead** (Ahmed) the macro file name, module name, and
   procedure name so the orchestrator call can be wired up.

See `docs/solidworks-macro-pipeline.md` for the full explanation of each step and
why these rules exist.
