# Component Database And Schema

This page is the stable reference for the live component catalogue and the code contract around it. It replaces the old field dump in `docs/SCHEMA.md` for day-to-day maintenance work.

Use this page when you need to know:

- where the component database is loaded from
- how the repository deserializes records
- which schema conventions the runtime depends on
- which fields are derived or normalized in code instead of being stored directly

Use the other reference pages alongside this one when the change also affects runtime path assumptions or validation:

- [Configuration And Runtime Dependencies](configuration-and-runtime-dependencies.md) for where the component database is expected to live at runtime
- [Testing And Validation](testing-and-validation.md) for what to re-check after data or schema changes

## Source Of Truth

The live catalogue is the JSON file at [`Resources/AppData/components.json`](../../Resources/AppData/components.json).

At runtime, [`Utilities/ConfigManager.vb`](../../Utilities/ConfigManager.vb) resolves the configured database path through `AppSettings.ResolvedComponentsDatabasePath`, and [`Core/Data/ComponentRepository.vb`](../../Core/Data/ComponentRepository.vb) reads that file into typed component models.

In the normal application path, the repository is read-only. The app does not write component data back to JSON.

## File Shape

`components.json` is one root JSON object with:

- a `_meta` object for descriptive metadata
- one top-level array per component family

The current top-level arrays are:

- `motors`
- `escs`
- `pdbs`
- `propellers`
- `flightControllers`
- `batteries`
- `gpsModules`
- `telemetryRadios`
- `cameras`
- `receivers`

`ComponentRepository.DeserializeComponents()` ignores `_meta`, walks every other array, and dispatches each object by its per-record `"category"` value.

That means the runtime depends more on each record's `category` field than on the top-level array name, even though the array names still matter for human organization.

## Required Record Conventions

Every component record should have these stable fields:

- `id`: unique repo-wide identifier
- `category`: functional type used for deserialization
- `name`: mapped to `ModelName`
- `manufacturer`
- `massGrams`
- `dimensions`

Most categories also rely on some combination of:

- `voltageMinV`
- `voltageMaxV`
- `operatingTempMinC`
- `operatingTempMaxC`
- `priceUSD`
- `notes`

The base model for those shared fields is [`Core/Models/ComponentSpecs.vb`](../../Core/Models/ComponentSpecs.vb).

## Category Vocabulary The Runtime Accepts

The schema is not normalized to one perfect vocabulary yet. The current repository code accepts these important forms:

- `motor`
- `esc`
- `propeller`
- `flightcontroller`
- `battery`
- `gpsmodule`
- `telemetryradio` and `telemetry`
- `camera`
- `servo`
- `receiver`
- `pdb` and `powerdistributionboard`

This matters because the codebase currently mixes:

- PascalCase values in the JSON data, such as `Motor` and `FlightController`
- lowercased normalized dispatch values in `DeserializeComponents()`
- typed accessor lookup keys like `"flight_controller"`, `"gps"`, `"telemetry"`, and `"pdb"`

When changing schema or category labels, verify both deserialization and accessor behavior. Category renames are migration-sensitive.

## Dimensions Contract

`dimensions` is the main nested object used across component families. [`Dimensions3D`](../../Core/Models/ComponentSpecs.vb) supports both generic and category-specific fields.

Stable shared fields include:

- `lengthMm`
- `widthMm`
- `heightMm`
- `diameterMm`

Important specialized nested fields include:

- propellers: `diameterInches`, `pitchInches`, `bladesCount`, `boreMm`
- motors: `shaftDiameterMm`, `mountingPatternMm`, `outerDiameterMm`

Several spec classes lift nested `dimensions` values into top-level convenience properties during `OnDeserialized`. For example:

- `MotorSpec` fills `ShaftDiameterMm` and `MountingBoltCircleMm`
- `PropellerSpec` fills `DiameterInches`, `PitchInches`, `BladeCount`, and `BoreDiameterMm`

If you remove or rename nested dimension fields, check those derived properties before assuming the change is harmless.

## High-Value Fields By Component Family

This section lists the stable fields that current runtime behavior depends on most. It is intentionally selective.

### Motors

Most important live fields:

- `motorKv`
- `maxContinuousCurrentA`
- `maxThrustG`
- `maxPowerW`
- `designatedPropSizeInMin`
- `designatedPropSizeInMax`
- `voltageMinV`
- `voltageMaxV`
- `dimensions.shaftDiameterMm`
- `ktNmPerA` and `maxTorqueNm` when present

Runtime notes:

- `KtNmPerA` is derived from `KV` if omitted.
- `MaxTorqueNm` is derived from `KtNmPerA * MaxCurrentAmps` if omitted.
- `windingResistanceOhm` is derived from `resistance_mOhm / 1000` if omitted.
- `Efficiency` is filled from `MaxThrustGrams / MaxPowerWatts` if not already populated.

Those derived values matter because the current selector uses torque gating, not just KV heuristics.

### ESCs

Most important live fields:

- `continuousCurrentPerChannelA`
- `burstCurrentPerChannelA`
- `cellCountMin`
- `cellCountMax`
- `voltageMinV`
- `voltageMaxV`
- `supportedProtocols`
- `telemetryOutput`

Runtime notes:

- `supportedProtocols` is read through `ESCProtocolArrayConverter` into the `ESCProtocol` flags enum.
- The engine primarily cares about current rating and pack-voltage compatibility.

### PDBs

Most important live fields:

- current rating
- maximum input voltage
- ESC pad count
- BEC presence and output current where relevant

Runtime notes:

- the engine uses PDB current, voltage, and pad-count fit
- richer board metadata is informational unless a downstream workflow reads it explicitly

### Propellers

Most important live fields:

- `dimensions.diameterInches`
- `dimensions.pitchInches`
- `dimensions.bladesCount`
- `dimensions.boreMm`
- `maxStaticThrustG`
- `maxRPM`
- `ctStatic`
- `cpStatic`

Runtime notes:

- `CtStatic` falls back to `0.115` if omitted or non-positive
- `CpStatic` falls back to `0.044` if omitted or non-positive
- `Efficiency` is derived from static thrust divided by mass if not populated

The live selector uses `Ct` and `Cp` directly in `ComputePropellerAero()`, so these are now first-class schema fields, not optional documentation detail.

### Flight Controllers

Most important live fields:

- processor type
- barometer and magnetometer presence
- bus and port counts
- firmware compatibility
- input voltage range

Runtime notes:

- `firmwareCompatibility` is stored as a JSON string array and converted into one comma-separated string
- `MaxLoopRateHz` is derived from `processorType` if not explicitly populated

### Batteries

Most important live fields:

- `capacityMah`
- `cellCount`
- `nominalVoltageV`
- `maxChargeVoltageV`
- `dischargeRatingC`
- `burstRatingC`
- connector fields

Runtime notes:

- `NominalPackVoltageV` is derived from `CellCount * NominalCellVoltageV`
- `MaxContinuousCurrentAmps` is derived from capacity and continuous C-rating
- the selection engine still uses exact cell-count matching plus mass and discharge feasibility in the final battery choice

### GPS Modules

Most important live fields:

- `constellations`
- `updateRateHz`
- `positionAccuracyM`
- `compassModel`
- `interfaceType`
- `currentDrawMA`

Runtime notes:

- `constellations` is converted from a JSON string array into a flags enum
- `interfaceType` is flattened into one comma-separated string
- `CurrentDrawA` is derived from `currentDrawMA`

### Telemetry Radios

Most important live fields:

- `frequencyMHz`
- `maxTxPowerMW`
- `maxRangeKm`
- `protocol`
- `frequencyHopping`
- `currentDrawActiveMA`

Runtime notes:

- `OutputPowerDBm` is derived from `maxTxPowerMW`
- `CurrentDrawA` is derived from `currentDrawActiveMA`
- the engine mainly uses link range and mission-profile fit

### Cameras

Most important live fields:

- sensor or camera type
- resolution fields
- FOV
- output interfaces
- stabilization flag
- power consumption
- weatherproofing

Runtime notes:

- some older records use `cameraType` plus megapixel-style fields
- some newer records use the engine-facing `sensorType` and explicit horizontal and vertical resolution fields
- `OnDeserialized` derives flags like `IsThermographic` and `IsLowLatency` from the sensor-type text

This category is one of the least uniform parts of the live schema, so changes should be verified against actual selector behavior.

### Receivers

Most important live fields:

- protocol
- `frequencyMHz`
- `channelCount`
- `outputFormat`
- telemetry support
- `maxRangeKm`
- `currentDrawMA`

Runtime notes:

- receiver records currently rely more on direct property-name matching than on explicit `JsonProperty` attributes
- because the current JSON names already match those property names closely enough, schema edits here should be made carefully and verified with a real load

## Derived And Normalized Values

Not every runtime value is stored directly in JSON. The current schema contract includes code-time derivation and normalization.

Important examples:

- nested `dimensions` values are copied into top-level convenience properties
- string arrays may be converted into comma-separated strings
- some string arrays become bit-flag enums
- motor torque and winding resistance can be derived if omitted
- propeller `Ct` and `Cp` have hardcoded fallbacks
- camera and flight-controller capability flags can be inferred from other fields

This means a schema edit can change runtime behavior even when the raw JSON still looks plausible.

## Duplicate IDs And Unknown Categories

`ComponentRepository` builds two indexes at load time:

- by `id`
- by normalized category string

Current behavior:

- duplicate IDs are ignored after a debug warning
- unknown category values are skipped
- repository accessors return empty lists rather than `Nothing`

Preserve unique IDs. Silent drops are harder to notice than parse failures.

## Practical Editing Rules

When editing `components.json`, keep these rules in mind:

1. Keep `id` values stable once code or docs may reference them.
2. Preserve units exactly as the models expect: grams, volts, amps, inches, millimetres, metres, and km.
3. Treat `category` strings as code-coupled data, not presentation labels.
4. Prefer adding explicit source fields over relying on derived fallbacks when the data is known.
5. Verify any new field name against `JsonProperty` attributes before assuming it will bind automatically.
6. Verify any category rename against both deserialization and typed repository accessors.

## What This Page Does Not Try To Be

This page is not the old schema monolith.

For exhaustive field inventories or one-off audit details, use the source files directly:

- [`Core/Models/ComponentSpecs.vb`](../../Core/Models/ComponentSpecs.vb)
- [`Core/Data/ComponentRepository.vb`](../../Core/Data/ComponentRepository.vb)
- [`Resources/AppData/components.json`](../../Resources/AppData/components.json)

The goal here is to preserve the stable contract maintainers actually need.

## Primary Related Files

- `Resources/AppData/components.json`
- `Core/Models/ComponentSpecs.vb`
- `Core/Data/ComponentRepository.vb`
- `Utilities/ConfigManager.vb`
- `docs/maintainers/workflows/changing-the-component-database-and-schema.md`

## Secondary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Core/Models/ComponentDisplayRow.vb`
- `docs/SCHEMA.md`
- `docs/reference/configuration-and-runtime-dependencies.md`
- `docs/reference/testing-and-validation.md`
