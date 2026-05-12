# Mechanical Team Briefing

---

## What this project is

We are building a software tool that designs a drone automatically.

The user enters the mission requirements — flight time, payload, range — and the software
selects the components (motors, battery, propellers, etc.) and then generates the CAD
parts for the frame automatically in SolidWorks.

No manual modelling for every drone variant. The software does it.

---

## What the software does, step by step

1. User inputs mission requirements
2. AI engine selects the best components
3. Software opens SolidWorks, opens a template part, and runs a macro
4. The macro reads the component dimensions and writes them into the SolidWorks equations
5. SolidWorks rebuilds the part with the correct geometry
6. The finished part is saved automatically

---

## What is already done

The motor mount is fully working end-to-end.
It takes the motor dimensions, runs through the full pipeline, and outputs a `.SLDPRT` file.

That is the reference for everything else.

---

## What the mechanical team needs to build

Five parts, each following the same pattern as the motor mount:

1. **Carbon fiber arm tube** — length and diameter driven by motor thrust and propeller size
2. **Body-to-arm connection** (fixed, foldable comes later) — clamps the arm to the chassis
3. **Chassis plates** — two-plate configuration, must accommodate the flight controller,
   onboard computer, GPS, and battery (hole patterns and positions are automatic)
4. **Landing gear connection part** — connects the landing gear legs to the chassis
5. **Landing gear tubes** — leg height driven by battery size and propeller tilt clearance

---

## What you need to deliver for each part

For each part, two things:

**A. A SolidWorks template** (`.SLDPRT`)
- The geometry is parametric
- All dimensions that change are driven by global variables in the equation manager
- Variable names are defined in `docs/parts-parameter-reference.md`
- Use a sensible default value for each variable so the part looks correct when opened alone

**B. A macro** (`.swb` text file, then converted to `.swp`)
- Reads the component parameters and writes them into the equation manager variables
- Forces a rebuild
- The reference implementation is `Resources/SolidWorks/Macros/MotorMount1.swb` — read it,
  it is short and the pattern is always the same

---

## What you do not need to worry about

- How the software selects components — that is done
- The pipeline code (VB.NET) — Ahmed wires your macro into it
- SolidWorks installation or COM setup — already solved

---

## Where to find everything

| File | What it contains |
|---|---|
| `docs/parts-parameter-reference.md` | All DD_ variable names and parameter sources for every part |
| `docs/solidworks-macro-pipeline.md` | Step-by-step guide for creating the .swp and wiring it up |
| `Resources/SolidWorks/Macros/MotorMount1.swb` | Reference macro — copy this pattern for every new part |
| `Resources/SolidWorks/Templates/MotorMount_Template.SLDPRT` | Reference template — shows how the equations are set up |

---

## When you are done with a part

Tell Ahmed:
- The macro file name
- The module name (visible in the VBA editor Project Explorer)
- The procedure name (the Sub that runs, e.g. `BuildMotorMount`)

He wires it into the pipeline and it runs automatically from that point.

---

*Questions → ask Ahmed.*
