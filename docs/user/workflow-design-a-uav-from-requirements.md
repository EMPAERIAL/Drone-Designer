# Design A UAV From Requirements

Use this workflow when you want Drone Designer to turn mission inputs into a design recommendation. The goal is not to fill every field mechanically. The goal is to describe the mission honestly enough that the design run reflects the aircraft you actually need.

## What You Will Do

In one normal design run, you will:

1. define the flight envelope
2. describe the payload
3. describe the operating environment
4. choose the mission profile and airframe intent
5. run the selection engine
6. review the design recommendation
7. adjust mission inputs and run again if needed

## Step 1: Define The Flight Envelope

Start in the `Flight Parameters` group.

Enter the mission inputs that describe how the aircraft must perform:

- endurance
- range
- cruise speed
- maximum altitude
- maximum wind speed
- maximum takeoff weight
- frame size

Use the most demanding realistic values, not optimistic values. If the aircraft must survive coastal wind or higher altitude operation, enter those conditions now instead of treating them as a later detail.

Practical guidance:

- Increase `Endurance` when loiter time matters more than sprint speed.
- Increase `Range` when the control link and mission footprint must extend farther from launch.
- Keep `Max Takeoff Weight` realistic. The design run uses it as a hard constraint, not a suggestion.
- Use `Frame Size` to reflect the physical size class you are willing to accept.

## Step 2: Describe The Payload

Move to the `Payload` group and describe what the aircraft must carry.

Set:

- payload weight
- payload type
- camera resolution, if relevant
- payload width, height, and depth

Payload weight is one of the most important mission inputs in the whole workflow. If it is understated, the design recommendation will look better than reality.

Use payload dimensions when packaging matters, not just raw mass. A light payload can still force a larger airframe if it is physically bulky.

## Step 3: Describe The Operating Environment

In the `Environment` group, enter the conditions the aircraft must tolerate:

- operating environment
- minimum IP rating
- maximum humidity
- minimum temperature
- maximum temperature

This is where you tell the app whether the mission is closer to urban inspection, open-field mapping, maritime use, desert use, indoor flight, or another demanding environment.

If weather resistance matters, set the minimum IP rating here instead of assuming it later. If the aircraft only needs standard outdoor use, keep the requirement conservative and realistic.

## Step 4: Choose The Mission Profile And Airframe Intent

Use the `Mission Type / Profile` group to tell Drone Designer what kind of aircraft you want it to bias toward.

Set:

- mission profile
- airframe type
- motor count
- autonomy level
- redundancy options, if required

This section shapes the design recommendation heavily. For example:

- `Aerial Survey / Mapping` and `Inspection` emphasize different mission priorities.
- `Multirotor`, `Fixed Wing`, and `VTOL Hybrid` imply very different aircraft behavior.
- `Motor Count` changes the thrust budget and redundancy profile.
- `Autonomy Level` affects the expected avionics tier.

Use the redundancy checkboxes only when the mission truly requires them. Redundancy raises complexity, mass, and downstream constraints.

## Step 5: Run The Design

After the mission inputs look consistent, click `Select Components`.

During the design run:

- the app validates the mission inputs first
- the selection engine processes the mission specs
- the status bar updates as the run progresses

If the inputs are invalid, Drone Designer stops before the run and tells you what must be corrected. Typical examples include impossible temperature ranges, too little endurance, or payload mass that is too large relative to maximum takeoff weight.

## Step 6: Review The Design Recommendation

After a successful design run, review the output in two places.

First, inspect the main component table. The recommended row in each category is the primary design recommendation, with alternatives listed below it when available.

Second, check the summary information and warnings:

- estimated MTOW
- required thrust per motor
- battery sizing values
- severe warnings or constraint warnings

Drone Designer may also open an `MTOW Convergence` window. Use it as a sanity check on how the battery mass and total mass settled during the run. You do not need to interpret it like an engineer to use the app, but it is useful when a result looks marginal or unstable.

## Step 7: Iterate On Mission Inputs

Most real designs take more than one design run.

Run again when:

- the aircraft is heavier than you can accept
- the recommended configuration is physically larger than you want
- endurance is too low
- the payload requirement is not being met cleanly
- warnings show that the mission inputs are too aggressive for the available component database

Good iteration patterns:

- reduce payload or payload dimensions if the mission can tolerate it
- lower endurance only if the real mission allows it
- relax maximum wind or temperature requirements only if they were overstated
- raise maximum takeoff weight if the airframe class can legitimately grow
- change mission profile or airframe type when the current design intent is wrong for the mission

Bad iteration pattern:

- changing several unrelated mission inputs at once and then guessing which change caused the result

Make one or two meaningful changes at a time, then run the selection engine again.

## Example Flow

For a small inspection mission, a practical operator flow usually looks like this:

1. Set endurance, range, cruise speed, altitude, and wind targets.
2. Enter the camera or sensor payload mass and dimensions.
3. Set the operating environment and weather resistance requirement.
4. Choose `Inspection`, a suitable airframe type, and the expected autonomy level.
5. Run `Select Components`.
6. Check MTOW, propulsion choices, and warnings.
7. Tighten or relax the mission inputs based on what the design recommendation shows.

## When To Move On

Move to the next workflow when the design recommendation is acceptable enough to save or share.

Common signs you are ready:

- the recommended component set is credible
- MTOW is inside your acceptable limit
- the mission inputs no longer need major changes
- you want to export or generate downstream outputs

Continue to [Export And Generate Outputs](workflow-export-and-generate-outputs.md) when you are ready to produce files from the selected design.
