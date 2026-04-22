# Drone Designer — Field & Property Schema
_Auto-generated from source. Do not edit manually._

---

## Enumerations

---

### `MissionProfileCategory` (`MissionSpecs.vb`)
> Renamed from `MissionProfile` (Task 11) to avoid shadowing `MissionSpecs.MissionProfile`.

| Member | Value | Notes |
|---|---|---|
| Surveillance | 0 | Loitering surveillance |
| Mapping | 1 | Photogrammetry / LiDAR |
| Delivery | 2 | Cargo or package delivery |
| Inspection | 3 | Infrastructure inspection |
| SearchAndRescue | 4 | Long range, thermal |
| Racing | 5 | FPV / sport |

---

### `UAVConfiguration` (`MissionSpecs.vb`)
| Member | Value | Notes |
|---|---|---|
| Quadcopter | 0 | 4-motor symmetric |
| Hexacopter | 1 | 6-motor |
| Octocopter | 2 | 8-motor |
| FixedWing | 3 | Fixed wing |
| VTOL | 4 | Hybrid hover/cruise |
| Helicopter | 5 | Single rotor + tail rotor |
| Tricopter | 6 | 3 motors + yaw servo |

---

### `OperatingEnvCategory` (`MissionSpecs.vb`)
> Renamed from `OperatingEnvironment` (Task 11) to avoid shadowing `MissionSpecs.OperatingEnvironment`.

| Member | Value |
|---|---|
| OutdoorStandard | 0 |
| OutdoorHighWind | 1 |
| OutdoorCold | 2 |
| OutdoorHot | 3 |
| OutdoorWet | 4 |
| IndoorGPSDenied | 5 |

---

### `RegulatoryClass` (`MissionSpecs.vb`)
| Member | Value |
|---|---|
| Recreational | 0 |
| CommercialStandard | 1 |
| BVLOS | 2 |
| Military | 3 |

---

### `PowerSourceType` (`MissionSpecs.vb`)
| Member | Value |
|---|---|
| LiPo | 0 |
| LiIon | 1 |
| HydrogenFuelCell | 2 |
| HybridFuelCellLiPo | 3 |
| Tethered | 4 |

---

### `MissionProfileType` (`MissionSpecs.vb`)
> Engine-facing profile type consumed by `ComponentSelectionEngine`.

| Member | Value |
|---|---|
| General | 0 |
| Surveillance | 1 |
| Mapping | 2 |
| Delivery | 3 |
| Inspection | 4 |
| SearchAndRescue | 5 |
| Racing | 6 |
| Survey | 7 |

---

### `EnvironmentType` (`MissionSpecs.vb`)
> Engine-facing environment type consumed by `ComponentSelectionEngine`.

| Member | Value |
|---|---|
| Standard | 0 |
| Harsh | 1 |

---

### `ComponentCategory` (`ComponentSpecs.vb`)
| Member | Notes |
|---|---|
| Motor | Brushless/brushed drive motor |
| ESC | Electronic Speed Controller |
| Propeller | Blade assembly |
| FlightController | Autopilot / stabilisation board |
| Battery | Rechargeable battery pack |
| GPSModule | GNSS positioning module |
| TelemetryRadio | Air-to-ground data-link radio |
| Camera | Imaging sensor |
| Servo | RC servo actuator |
| Receiver | RC link receiver |
| PowerDistributionBoard | PDB — routes battery power to ESCs |

---

### `MotorType` (`ComponentSpecs.vb`)
| Member |
|---|
| Brushless |
| Brushed |

---

### `BatteryCellChemistry` (`ComponentSpecs.vb`)
| Member |
|---|
| LiPo |
| LiIon |
| LiFePO4 |
| NiMH |

---

### `GNSSConstellation` (`ComponentSpecs.vb`)
> Bit-flag enum — combine with `Or`.

| Member | Value |
|---|---|
| None | 0 |
| GPS | 1 |
| GLONASS | 2 |
| BeiDou | 4 |
| Galileo | 8 |
| QZSS | 16 |
| SBAS | 32 |

---

### `ServoSignalType` (`ComponentSpecs.vb`)
| Member |
|---|
| AnalogPWM |
| DigitalPWM |
| SBus |
| HiTecSerial |

---

### `RCLinkProtocol` (`ComponentSpecs.vb`)
| Member |
|---|
| FrSky_ACCST |
| FrSky_ACCESS |
| Futaba_FASST |
| Futaba_FASSTest |
| Spektrum_DSM2 |
| Spektrum_DSMX |
| TBS_Crossfire |
| ExpressLRS |
| FlySky_AFHDS |
| FlySky_AFHDS2A |
| Other |

---

### `FlightControllerFirmware` (`ComponentSpecs.vb`)
| Member |
|---|
| ArduPilot |
| PX4 |
| Betaflight |
| INAV |
| Cleanflight |
| Proprietary |
| Multiple |

---

### `ESCProtocol` (`ComponentSpecs.vb`)
> Bit-flag enum.

| Member | Value |
|---|---|
| None | 0 |
| PWM | 1 |
| OneShot125 | 2 |
| OneShot42 | 4 |
| MultiShot | 8 |
| DShot150 | 16 |
| DShot300 | 32 |
| DShot600 | 64 |
| DShot1200 | 128 |
| ProShot | 256 |

---

### `CameraOutputInterface` (`ComponentSpecs.vb`)
| Member |
|---|
| AnalogNTSC_PAL |
| HDMI |
| MicroHDMI |
| USB |
| MIPI_CSI |
| Ethernet |
| SDI |
| Other |

---

### `PowerConnectorType` (`ComponentSpecs.vb`)
| Member |
|---|
| XT30 |
| XT60 |
| XT90 |
| EC3 |
| EC5 |
| Deans_T |
| JST_PH |
| JST_XH |
| Anderson_Powerpole |
| Bare_Wire |
| Other |

---

### `PipelineStage` (`PipelineResult.vb`)
| Member | Value |
|---|---|
| Validating | 0 |
| SelectingComponents | 1 |
| ConnectingToSolidWorks | 2 |
| GeneratingPart | 3 |
| SavingFile | 4 |
| WritingManifest | 5 |
| Finalising | 6 |
| Failed | 99 |

---

## Data Model Classes

---

## `MissionSpecs` (`MissionSpecs.vb`)
> Primary input to `ComponentSelectionEngine`. Pure data container; defaults represent a small commercial quadcopter surveillance mission.

| Property | Type | Notes |
|---|---|---|
| MissionName | String | Human-readable label. Default: "Untitled Mission" |
| Description | String | Free-text; not used by selection engine |
| CreatedAtUtc | DateTime | UTC timestamp of creation |
| FlightEnduranceMinutes | Double | Required endurance (min). Range: 1–600 |
| MaxRangeKm | Double | Max operational range (km). Range: 0.1–500 |
| CruiseSpeedMs | Double | Steady-state cruise speed (m/s) |
| MaxSpeedMs | Double | Max design speed (m/s). Must be ≥ CruiseSpeedMs |
| MaxAltitudeMeters | Double | Max altitude AGL (m). Range: 10–6000 |
| MaxWindSpeedMs | Double | Max sustained wind speed (m/s). Default: 11 m/s |
| MaxTakeoffMassGrams | Double | Design MTOW limit (g). Range: 100–55 000 |
| PayloadMassGrams | Double | Payload mass (g). Default: 0 |
| PayloadDimensionsMm | PayloadDimensions | 3D bounding box for payload bay sizing |
| MinOperatingTempCelsius | Double | Min ambient temp (°C). Default: 0 |
| MaxOperatingTempCelsius | Double | Max ambient temp (°C). Default: 45 |
| Environment | OperatingEnvCategory | High-level environment class (UI-facing) |
| RequiresWaterproofing | Boolean | Triggered when Environment = OutdoorWet or IndoorGPSDenied |
| RequiredIPRating | String | Target IP rating string, e.g. "IP54". Empty = none |
| Profile | MissionProfileCategory | UI-facing mission profile enum |
| Configuration | UAVConfiguration | Airframe topology (drives motor count, structural layout) |
| PowerSource | PowerSourceType | Power topology. Default: LiPo |
| Regulatory | RegulatoryClass | Airspace class. Default: CommercialStandard |
| MissionProfile | MissionProfileType | ⚠️ Engine-facing alias for Profile (see note below) |
| OperatingEnvironment | EnvironmentType | ⚠️ Engine-facing alias for Environment (see note below) |
| MotorCount | Integer | Number of rotors. Valid: 3, 4, 6, 8, 12. Default: 4 |
| RangeKm | Double | ⚠️ Engine-facing alias/pass-through for MaxRangeKm |
| PayloadWeightGrams | Double | ⚠️ Engine-facing alias/pass-through for PayloadMassGrams |
| ControlLinkRangeKm | Double? | Control link range (km). Defaults to MaxRangeKm |
| RequiresVideoDownlink | Boolean | Triggers VTX selection. Default: True |
| VideoDownlinkRangeKm | Double | Min video downlink range (km). Default: 2.0 |
| RequiresTelemetry | Boolean | MAVLink telemetry required. Default: True |
| RequiresRemoteID | Boolean | ADS-B In / Remote ID required. Default: False |
| RequiresAutopilot | Boolean | Autonomous waypoint nav required. Default: True |
| RequiresOpticalFlow | Boolean | GPS-denied optical flow sensor needed. Default: False |
| RequiresObstacleAvoidance | Boolean | Obstacle avoidance sensors required. Default: False |
| ObstacleAvoidanceDirections | Integer | Number of OA sensor directions (1, 3, or 6). Default: 1 |
| RequiresDualGPS | Boolean | Dual/redundant GPS receivers. Default: False |
| RequiresParachute | Boolean | Parachute recovery system. Default: False |

> **Note:** `MissionProfile`/`OperatingEnvironment` are the properties read by the engine; `Profile`/`Environment` are the UI-facing equivalents. Both sets must be assigned together in `BuildMissionSpecs()`.

---

## `PayloadDimensions` (`MissionSpecs.vb`)
> Bounding box for payload bay sizing in SolidWorks. All zero = unspecified.

| Property | Type | Notes |
|---|---|---|
| LengthMm | Double | Fore–aft axis (mm) |
| WidthMm | Double | Lateral axis (mm) |
| HeightMm | Double | Vertical axis (mm) |
| IsSpecified | Boolean (ReadOnly) | True when all three dimensions > 0 |

---

## `Dimensions3D` (`ComponentSpecs.vb`)
> Axis-aligned bounding box for a physical component (mm). Also carries propeller- and motor-specific nested dimension fields for JSON deserialization.

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| Length | Double | `lengthMm` | X-axis (mm) |
| Width | Double | `widthMm` | Y-axis (mm) |
| Height | Double | `heightMm` | Z-axis (mm) |
| Diameter | Double | `diameterMm` | For circular components (mm) |
| DiameterInches | Double | `diameterInches` | Propeller blade diameter (in) |
| PitchInches | Double | `pitchInches` | Propeller blade pitch (in) |
| BladesCount | Integer | `bladesCount` | Number of blades |
| BoreMm | Double | `boreMm` | Propeller bore diameter (mm) |
| HubDiameterMm | Double | `hubDiameterMm` | Hub outer diameter (mm) |
| ShaftDiameterMm | Double | `shaftDiameterMm` | Motor shaft diameter (mm) |
| MountingPatternMm | Double | `mountingPatternMm` | Motor bolt-circle diameter (mm) |
| OuterDiameterMm | Double | `outerDiameterMm` | Motor outer bell diameter (mm) |

---

## `ComponentSpecs` (`ComponentSpecs.vb`) — **abstract base class**

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| Id | String | — | Unique DB record ID (GUID) |
| ModelName | String | `name` | Product name |
| Manufacturer | String | — | Brand name |
| PartNumber | String | — | SKU / part number |
| _Interface | String | — | Primary comms interface (e.g. "UART") |
| Category | ComponentCategory | — | Functional category |
| MassGrams | Double | — | Mass (g) |
| Dimensions | Dimensions3D | — | Outer envelope (mm) |
| MinVoltage | Double | `voltageMinV` | Min operating voltage (V) |
| NominalVoltageV | Double | — | Nominal operating voltage (V) |
| MaxPowerW | Double | — | Max operating power (W) |
| MaxVoltage | Double | `voltageMaxV` | Max operating voltage (V) |
| MaxCurrentA | Double | — | Max continuous current (A) |
| MinCurrentA | Double | — | Min current draw (A) |
| NominalCurrentA | Double | — | Nominal current draw (A) |
| MinOperatingTempC | Double | `operatingTempMinC` | Min operating temp (°C). Default: −20 |
| MaxOperatingTempC | Double | `operatingTempMaxC` | Max operating temp (°C). Default: 60 |
| PriceUSD | Double | — | Approximate retail price (USD) |
| Notes | String | — | Freeform data-quality notes |

---

## `MotorSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| KV | Integer | `motorKv` | Velocity constant (RPM/V) |
| MaxPowerWatts | Double | `maxPowerW` | ⚠️ Near-duplicate of base `MaxPowerW` — motor-specific override |
| MaxCurrentAmps | Double | `maxContinuousCurrentA` | ⚠️ Near-duplicate of base `MaxCurrentA` |
| NoLoadCurrentAmps | Double | `noLoadCurrentA` | No-load current at 10 V (A) |
| InternalResistanceMilliOhm | Double | `resistance_mOhm` | Winding resistance (mΩ) |
| MaxThrustGrams | Double | `maxThrustG` | Max static thrust with recommended prop (g) |
| MaxThrustTestPropeller | String | — | Prop used for MaxThrustGrams measurement |
| ShaftDiameterMm | Double | — | Shaft diameter (mm); populated from Dimensions on deserialization |
| StatorDiameterMm | Double | — | Stator core diameter (mm) |
| StatorHeightMm | Double | — | Stator winding stack height (mm) |
| PoleCount | Integer | — | Number of magnetic poles |
| PropDiameterMinIn | Double | `designatedPropSizeInMin` | Min recommended prop diameter (in) |
| PropDiameterMaxIn | Double | `designatedPropSizeInMax` | Max recommended prop diameter (in) |
| MountingBoltCircleMm | Double | — | Bolt-circle diameter (mm); populated from Dimensions on deserialization |
| MountingBoltCount | Integer | — | Number of mounting bolts. Default: 4 |
| MotorType | MotorType | — | Brushless / Brushed. Default: Brushless |
| RecommendedMinCells | Integer | — | Min recommended cell count |
| RecommendedMaxCells | Integer | — | Max recommended cell count |
| Efficiency | Double | — | Thrust per Watt (gf/W); computed if not set |

---

## `ESCSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| ContinuousCurrentAmps | Double | `continuousCurrentPerChannelA` | Continuous current rating (A) |
| BurstCurrentAmps | Double | `burstCurrentPerChannelA` | 10-second burst rating (A) |
| MinInputVoltage | Double | `voltageMinV` | ⚠️ Shadows base `MinVoltage` with different JSON key context |
| MaxInputVoltage | Double | `voltageMaxV` | ⚠️ Shadows base `MaxVoltage` with different JSON key context |
| MinCellCount | Integer | `cellCountMin` | Min LiPo cell count supported |
| MaxCellCount | Integer | `cellCountMax` | Max LiPo cell count supported |
| HasBEC | Boolean | — | Onboard BEC present |
| IsAllInOne | Boolean | — | 4-in-1 ESC board |
| BECVoltage | Double? | `becVoltageV` | BEC output voltage (V) |
| BECCurrentAmps | Double? | `becCurrentMaxA` | Max BEC output current (A) |
| SupportedProtocols | ESCProtocol | — | Supported signalling protocols (Flags enum) |
| Firmware | String | `firmwareType` | Factory firmware name |
| SupportsBidirectionalDShot | Boolean | `telemetryOutput` | Supports bidirectional DSHOT |
| IsQuadESC | Boolean | — | 4-in-1 quad ESC. Default: False |

---

## `PropellerSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| DiameterInches | Double | — | Blade diameter (in); populated from Dimensions |
| PitchInches | Double | — | Blade pitch (in); populated from Dimensions |
| BladeCount | Integer | — | Number of blades. Default: 2 |
| BoreDiameterMm | Double | — | Bore diameter (mm); populated from Dimensions |
| Material | String | — | Primary blade material |
| IsFoldable | Boolean | `isFolding` | Folding blades |
| MaxRPM | Integer | — | Max safe rotational speed (RPM) |
| StaticThrustGrams | Double | `maxStaticThrustG` | Static thrust (g) |
| StaticThrustTestRPM | Integer | — | RPM at which StaticThrustGrams was measured |
| IsClockwiseRotation | Boolean | — | CW rotation prop. Default: True |
| Efficiency | Double | — | Thrust/mass proxy; computed if not set |

---

## `FlightControllerSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| Processor | String | `processorType` | CPU / SoC identifier |
| GyroscopeChip | String | `imuPrimary` | Primary IMU / gyro chip |
| AccelerometerChip | String | — | Primary accelerometer chip |
| HasBarometer | Boolean | — | Onboard barometer present. Default: True |
| BarometerChip | String | `barometerModel` | Barometer chip identifier |
| HasMagnetometer | Boolean | `hasOnboardMagnetometer` | Onboard magnetometer present |
| UARTCount | Integer | — | Hardware UART ports |
| I2CBusCount | Integer | `i2cCount` | Hardware I²C buses |
| SPIBusCount | Integer | `spiCount` | Hardware SPI buses |
| PWMOutputCount | Integer | — | PWM / DSHOT output channels |
| AnalogInputCount | Integer | `analogInputs` | ADC input channels |
| MaxLoopRateHz | Integer | — | Max gyro loop rate (Hz); derived from Processor in deserialization |
| HasSDCardSlot | Boolean | — | MicroSD for blackbox logging. Default: True |
| HasUSB | Boolean | — | USB port present. Default: True |
| HasOSD | Boolean | — | Built-in OSD chip |
| HasBuiltInVTx | Boolean | — | Built-in VTx present |
| SupportedFirmware | FlightControllerFirmware | — | Firmware ecosystem |
| FirmwareCompatibility | String | `firmwareCompatibility` | Comma-separated firmware list (from JSON array) |
| MountingPatternMm | String | `formFactor` | Form factor / mounting pattern string |
| InputVoltageMin | Double | `inputVoltageMinV` | Min input supply voltage (V). Default: 4.5 |
| InputVoltageMax | Double | `inputVoltageMaxV` | Max input supply voltage (V). Default: 5.5 |
| HasBlackbox | Boolean (ReadOnly) | — | True when HasSDCardSlot is True |

---

## `BatterySpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| CapacityMAh | Integer | — | Capacity (mAh) |
| CellCount | Integer | — | Number of series cells |
| NominalCellVoltageV | Double | `nominalVoltageV` | Nominal cell voltage (V). Default: 3.7 |
| FullChargeVoltagePerCellV | Double | `maxChargeVoltageV` | Full-charge cell voltage (V). Default: 4.2 |
| MinCellVoltageV | Double | — | Safe discharge cutoff per cell (V). Default: 3.5 |
| ContinuousCRating | Double | `dischargeRatingC` | Continuous discharge C-rating |
| BurstCRating | Double | `burstRatingC` | Peak burst C-rating |
| Chemistry | BatteryCellChemistry | — | Cell chemistry. Default: LiPo |
| FormFactor | String | — | Physical form factor |
| DischargeConnector | String | `mainConnectorType` | Discharge connector type. Default: "XT60" |
| BalanceConnector | String | `balanceConnectorType` | Balance connector. Default: "JST-XH" |
| LipoCellCount | Integer (ReadOnly) | — | Engine-facing alias for CellCount |
| NominalPackVoltageV | Double (ReadOnly) | — | Computed: CellCount × NominalCellVoltageV |
| MaxContinuousCurrentAmps | Double (ReadOnly) | — | ⚠️ Near-duplicate name with `PowerDistributionBoardSpec.MaxContinuousCurrentAmps`; computed: (CapacityMAh/1000) × ContinuousCRating |

---

## `GPSModuleSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| SupportedConstellations | GNSSConstellation | `constellations` | Supported GNSS constellations (from JSON string array) |
| MaxUpdateRateHz | Integer | `updateRateHz` | Max position update rate (Hz) |
| HorizontalAccuracyMeters | Double | `positionAccuracyM` | Typical CEP50 accuracy (m) |
| HasCompass | Boolean | — | Integrated magnetometer. Default: True |
| CompassChip | String | `compassModel` | Compass chip identifier |
| TrackingChannels | Integer | — | Number of satellite tracking channels |
| DefaultBaudRate | Integer | — | Default UART baud rate. Default: 38400 |
| GNSSChipset | String | `chipset` | GNSS chipset identifier |
| PCBDiameterMm | Double | — | Circular module PCB diameter (mm) |
| InterfaceTypes | String | `interfaceType` | Comma-separated interfaces (from JSON array) |
| CurrentDrawMA | Double | `currentDrawMA` | ⚠️ Near-duplicate name: `ReceiverSpec` also has `CurrentDrawMA` |
| CurrentDrawA | Double (ReadOnly) | — | Derived: CurrentDrawMA / 1000 |

---

## `TelemetryRadioSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| FrequencyMHz | Double | — | ⚠️ Near-duplicate: `ReceiverSpec` also has `FrequencyMHz` |
| OutputPowerMW | Double | `maxTxPowerMW` | Tx output power (mW) |
| OutputPowerDBm | Double (ReadOnly) | — | Derived: 10 × log10(OutputPowerMW) |
| MaxRangeKm | Double | `maxRangeKm` | ⚠️ Near-duplicate: `ReceiverSpec` also has `MaxRangeKm` |
| AirDataRateBps | Integer | — | Air-link data rate (bps) |
| TelemetryProtocol | String | `protocol` | Supported protocol. Default: "MAVLink 2" |
| FCInterface | String | — | Physical interface to FC. Default: "UART" |
| SupportsFHSS | Boolean | `frequencyHopping` | Frequency-hopping spread spectrum |
| SupportsRCPassthrough | Boolean | — | Can relay RC commands to FC |
| CurrentDrawActiveMA | Double | `currentDrawActiveMA` | Active Tx current draw (mA) |
| CurrentDrawA | Double (ReadOnly) | — | Derived: CurrentDrawActiveMA / 1000 |

---

## `CameraSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | JSON Field | Notes |
|---|---|---|---|
| SensorType | String | `cameraType` | Sensor description, e.g. "RGB", "Thermal LWIR". Default: "RGB" |
| ResolutionHorizontalPx | Integer | — | Horizontal resolution (px) |
| ResolutionVerticalPx | Integer | — | Vertical resolution (px) |
| ResolutionMpx | Double? | `resolutionMpx` | Sensor resolution (MP) |
| DiagonalFOVDegrees | Double? | `fovDegrees` | Field of view (°) |
| HorizontalFOVDegrees | Double | — | Horizontal FOV (°) |
| MaxFrameRateFPS | Integer | — | Max frame rate at full resolution (fps) |
| OutputInterface | CameraOutputInterface | — | Primary data output interface. Default: USB |
| OutputInterfaces | String | `outputInterfaces` | Comma-separated interfaces (from JSON array) |
| FocalLengthMm | Double | — | Focal length (mm) |
| HasStabilisation | Boolean | `hasElectronicImageStabilization` | Electronic / mechanical image stabiliser |
| IsLowLatency | Boolean | — | FPV camera (<30 ms latency); derived from SensorType |
| IsGimbalMounted | Boolean | — | Designed for gimbal mounting |
| PowerConsumptionWatts | Double | `powerConsumptionW` | Active recording power (W) |
| OperatingVoltageV | Double | — | Operating supply voltage (V) |
| IsThermographic | Boolean | — | Thermal/IR sensor; derived from SensorType |
| IsWeatherproof | Boolean | `isWeatherproof` | Weatherproof rated |
| CurrentDrawA | Double (ReadOnly) | — | Derived: PowerConsumptionWatts / 5.0 |

---

## `ServoSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | Notes |
|---|---|---|
| StallTorqueKgCm | Double | Stall torque (kg·cm) |
| StallTorqueNm | Double (ReadOnly) | Derived: StallTorqueKgCm × 0.0981 |
| SpeedSecPer60Deg | Double | Transit speed (s / 60°) |
| RatedVoltageV | Double | Rated operating voltage (V) |
| NoLoadCurrentMA | Double | No-load current (mA) |
| StallCurrentMA | Double | Stall current (mA) |
| TotalTravelDeg | Double | Mechanical travel (°). Default: 180 |
| SignalType | ServoSignalType | Control signal protocol. Default: AnalogPWM |
| GearMaterial | String | Gear train material |
| HasBallBearings | Boolean | Ball-bearing output shaft |
| BodyWidthMm | Double | Case width (mm) |
| BodyLengthMm | Double | Case length (mm) |
| BodyHeightMm | Double | Case height (mm) |
| MountingLugSpacingMm | Double | Lug centre-to-centre distance (mm) |

---

## `ReceiverSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | Notes |
|---|---|---|
| Protocol | RCLinkProtocol | RC protocol ecosystem |
| ProtocolName | String | Protocol string, e.g. "ELRS", "CRSF". Default: "" |
| FrequencyMHz | Double | ⚠️ Near-duplicate: `TelemetryRadioSpec` also has `FrequencyMHz` |
| ChannelCount | Integer | Number of independent RC channels |
| OutputFormat | String | Signal format to FC. Default: "SBUS" |
| HasRSSIOutput | Boolean | Downlink RSSI output. Default: True |
| HasTelemetry | Boolean | Bidirectional telemetry |
| MaxRangeKm | Double | ⚠️ Near-duplicate: `TelemetryRadioSpec` also has `MaxRangeKm` |
| AntennaCount | Integer | Onboard antenna ports. Default: 1 |
| OperatingVoltageV | Double | Operating voltage (V). Default: 5.0 |
| CurrentDrawMA | Double | ⚠️ Near-duplicate: `GPSModuleSpec` also has `CurrentDrawMA` |

---

## `PowerDistributionBoardSpec` (`ComponentSpecs.vb`) — inherits `ComponentSpecs`

| Property | Type | Notes |
|---|---|---|
| MaxContinuousCurrentAmps | Double | ⚠️ Near-duplicate: `BatterySpec` has a computed `MaxContinuousCurrentAmps` |
| BurstCurrentAmps | Double | Peak burst current (A) |
| MaxInputVoltageV | Double | Max input voltage (V) |
| ESCPadCount | Integer | Number of ESC solder pad sets |
| HasCurrentSensor | Boolean | Onboard current sensor |
| CurrentSensorMaxAmps | Double | Current sensor full-scale range (A) |
| Has5VBEC | Boolean | Regulated 5 V BEC output |
| BEC5VMaxAmps | Double | Max 5 V BEC current (A) |
| Has12VBEC | Boolean | Regulated 12 V output |
| BEC12VMaxAmps | Double | Max 12 V output current (A) |
| InputConnector | PowerConnectorType | Battery input connector. Default: XT60 |
| MountingPatternMm | String | Mounting hole pattern string |
| IsStackDesign | Boolean | ESC pads arranged for stack layout. Default: True |

---

## `ComponentDisplayRow` (`ComponentDisplayRow.vb`)
> Display-only DTO bound to the output DataGridView. Property names match `DataPropertyName` of each DGV column.

| Property | Type | Notes |
|---|---|---|
| Category | String | Category label, e.g. "Motor", "Battery (LiPo)" |
| Manufacturer | String | Brand name |
| ModelName | String | Model name / part number |
| MassGrams | Double | Component mass (g) |
| NominalVoltage | Double | Nominal operating voltage (V) |
| MaxPowerWatts | Double | Max rated power (W); sourced from `ComponentSpecs.MaxPowerW` |
| Dimensions | String | Formatted dimension string, e.g. "38 × 38 × 7 mm" |
| [Interface] | String | Protocol string, e.g. "UART", "DSHOT600" |
| TempRating | String | Formatted temp range, e.g. "−20 to +85 °C" |
| SelectionNotes | String | "Recommended" or "Alternative N" |
| IsRecommended | Boolean | True for top-ranked entry per category (not a DGV column) |

---

## Engine Intermediate / Result Types

---

## `MtowEstimate` (`ComponentSelectionEngine.vb`)
> Output of MTOW fixed-point iteration (Task 7).

| Property | Type | Notes |
|---|---|---|
| StructuralMassGrams | Double | Airframe + electronics + payload before safety factor (g) |
| EstimatedBatteryMassGrams | Double | Battery mass from final iteration (g) |
| TotalMassGrams | Double | MTOW including safety factor (g) |
| MotorCount | Integer | Motor count used in estimate |
| IterationsToConverge | Integer | Iterations until delta MTOW ≤ 1 g |

---

## `ThrustRequirement` (`ComponentSelectionEngine.vb`)
> Per-motor and total thrust requirements (Task 7).

| Property | Type | Notes |
|---|---|---|
| TotalThrustGf | Double | Total thrust at full throttle (gf) |
| ThrustPerMotorGf | Double | Per-motor thrust at full throttle (gf) |
| ThrustToWeightRatio | Double | Applied TWR (dimensionless) |
| MotorCount | Integer | ⚠️ Also present on MtowEstimate and PowerBudget |

---

## `PowerBudget` (`ComponentSelectionEngine.vb`)
> Complete power and current budget (Task 8). All downstream power-system selections read from this.

| Property | Type | Notes |
|---|---|---|
| CellCount | Integer | Selected LiPo cell count (e.g. 4 for "4S") |
| NominalVoltageV | Double | CellCount × 3.7 V; ⚠️ same name on AvionicsBudget |
| MotorPeakCurrentA | Double | Single motor peak current at full throttle (A) |
| MotorHoverCurrentA | Double | Single motor estimated hover current (A) |
| TotalPeakCurrentA | Double | All motors + avionics peak (A) — governs ESC/PDB sizing |
| TotalAverageCurrentA | Double | All motors + avionics hover avg (A) — governs battery sizing |
| MinimumCapacityMah | Double | Min required capacity before margin (mAh) |
| RequiredCapacityMah | Double | Required capacity including 20% margin (mAh) |
| RequiredCRating | Double | Min continuous C-rating for battery (C) |
| PeakSystemPowerW | Double | Peak system power at full throttle (W) |
| HoverSystemPowerW | Double | Hover / cruise system power (W) |
| MotorCount | Integer | ⚠️ Also present on MtowEstimate and ThrustRequirement |

---

## `AvionicsBudget` (`ComponentSelectionEngine.vb`)
> Avionics power and range context (Task 9).

| Property | Type | Notes |
|---|---|---|
| BudgetedCurrentA | Double | Flat 3 A avionics estimate used in Task 8 |
| RealizedCurrentA | Double | Actual summed draw from selected avionics; set post-Task 9 |
| CurrentDeltaA | Double | RealizedCurrentA − BudgetedCurrentA; >1 A suggests second-pass budget |
| NominalVoltageV | Double | Pack nominal voltage (V); ⚠️ same name on PowerBudget |
| MissionRangeKm | Double | Mission range (km) |
| MissionProfile | MissionProfileType | ⚠️ Same property name as on MissionSpecs |
| OperatingEnvironment | EnvironmentType | ⚠️ Same property name as on MissionSpecs |

---

## `SelectionResult` (`ComponentSelectionEngine.vb`)
> Complete output of the selection pipeline (Tasks 7–9).

| Property | Type | Notes |
|---|---|---|
| EstimatedMtowGrams | Double | Final MTOW estimate with safety margin (g) |
| RequiredThrustPerMotorGf | Double | Required per-motor thrust at full throttle (gf) |
| SelectedMotors | List(Of ComponentSpecs) | Up to 5 motor candidates |
| SelectedPropellers | List(Of ComponentSpecs) | Up to 5 propeller candidates |
| PowerBudget | PowerBudget | Full power/current budget |
| SelectedBatteries | List(Of ComponentSpecs) | Up to 3 battery candidates |
| SelectedEscs | List(Of ComponentSpecs) | Up to 3 ESC candidates |
| SelectedPdbs | List(Of ComponentSpecs) | Up to 3 PDB candidates |
| AvionicsBudget | AvionicsBudget | Budgeted vs realized avionics current |
| SelectedFlightControllers | List(Of ComponentSpecs) | Up to 3 FC candidates |
| SelectedGpsModules | List(Of ComponentSpecs) | Up to 3 GPS candidates |
| SelectedTelemetryRadios | List(Of ComponentSpecs) | Up to 3 telemetry radio candidates |
| SelectedReceivers | List(Of ComponentSpecs) | Up to 3 receiver candidates |
| SelectedCameras | List(Of ComponentSpecs) | Up to 3 camera candidates; empty for Delivery |

---

## Pipeline Result Types

---

## `PipelineProgressReport` (`PipelineResult.vb`) — `NotInheritable`

| Property | Type | Notes |
|---|---|---|
| Stage | PipelineStage | Current named stage |
| PercentComplete | Integer | 0–100 overall completion |
| StatusMessage | String | Human-readable operation description |
| DetailMessage | String | Optional sub-status line |
| ErrorMessage | String | Non-Nothing when Stage = Failed |
| IsTerminal | Boolean (ReadOnly) | True when Finalising or Failed |

---

## `GeneratedPartRecord` (`PipelineResult.vb`) — `NotInheritable`

| Property | Type | Notes |
|---|---|---|
| ComponentId | String | Source component DB ID |
| ComponentName | String | Component name, e.g. "T-Motor U8 II" |
| PartType | String | Category of generated part, e.g. "Motor Mount" |
| FilePath | String | Absolute path to saved .SLDPRT file |
| SavedAtUtc | DateTime | UTC timestamp when file was saved |
| Success | Boolean | True if generated successfully |
| ErrorDetail | String | Error message if Success = False |

---

## `PipelineResult` (`PipelineResult.vb`) — `NotInheritable`

| Property | Type | Notes |
|---|---|---|
| Success | Boolean | True if pipeline completed without fatal error |
| ErrorMessage | String | Top-level error; Nothing on success |
| GeneratedParts | IReadOnlyList(Of GeneratedPartRecord) | Records for each CAD part attempted |
| OutputDirectory | String | Folder where files were saved |
| ManifestPath | String | Path to component manifest (.txt) |
| StartedAtUtc | DateTime | UTC start time |
| FinishedAtUtc | DateTime | UTC end time |
| Duration | TimeSpan (ReadOnly) | FinishedAtUtc − StartedAtUtc |
| SuccessfulPartCount | Integer (ReadOnly) | Count of successful parts |
| FailedPartCount | Integer (ReadOnly) | Count of failed parts |

---

## SolidWorks / Macro Types

---

## `MacroParameters` (`MacroRunner.vb`)

| Property | Type | Notes |
|---|---|---|
| Values | Dictionary(Of String, String) (ReadOnly) | Key→value parameter store |
| SourceDescription | String | Source spec file label for traceability |

---

## `MacroRunResult` (`MacroRunner.vb`)

| Property | Type | Notes |
|---|---|---|
| Success | Boolean | |
| OutputFilePath | String | |
| ErrorMessage | String | |
| MacroPath | String | |
| TemplatePath | String | |
| ElapsedMs | Long | |
| SwErrorCode | Long | SolidWorks RunMacro2 error code |

---

## `MacroRunnerOptions` (`MacroRunner.vb`)

| Property | Type | Notes |
|---|---|---|
| CloseDocumentAfterRun | Boolean | Default: True |
| OpenTemplateAsCopy | Boolean | Default: True |
| SilentMode | Boolean | Suppress SW dialogs. Default: True |
| TimeoutMs | Integer | Macro timeout (ms). Default: 60 000 |
| TempDirectory | String | Temp param file directory |
| InjectAsCustomProperties | Boolean | Default: True |
| InjectAsTempFile | Boolean | Default: True |
| TempParameterFileName | String | Default: "DroneDesigner_MacroParams.txt" |
| SaveAsVersion | Integer | swSaveAsVersion_e. Default: 0 (same version) |

---

## `MacroBatchJob` (`MacroRunner.vb`)

| Property | Type |
|---|---|
| TemplatePath | String |
| MacroPath | String |
| MacroModule | String |
| MacroProcedure | String |
| Parameters | MacroParameters |
| OutputPath | String |
| Label | String |

---

## `MacroBatchResult` (`MacroRunner.vb`)

| Property | Type |
|---|---|
| Job | MacroBatchJob (ReadOnly) |
| RunResult | MacroRunResult (ReadOnly) |
| Label | String (ReadOnly) |
| Success | Boolean (ReadOnly) |

---

## Configuration Types

---

## `AppSettings` (`ConfigManager.vb`)
> Serialized to/from `DroneDesigner.config.json` via `DataContractJsonSerializer`.

| Property | Type | JSON Field (`DataMember`) | Notes |
|---|---|---|---|
| ComponentsDatabasePath | String | `componentsDatabasePath` | Path to components.json |
| SolidWorksInstallPath | String | `solidWorksInstallPath` | SolidWorks installation directory |
| TemplatePartsDirectory | String | `templatePartsDirectory` | Template .SLDPRT directory |
| OutputDirectory | String | `outputDirectory` | Generated CAD output directory |
| SolidWorksTargetVersion | String | `solidWorksTargetVersion` | Target SW version, e.g. "2024" |
| LogFilePath | String | `logFilePath` | Application log file path |
| LogLevel | String | `logLevel` | "Debug"/"Info"/"Warning"/"Error". Default: "Info" |
| ResolvedComponentsDatabasePath | String (ReadOnly) | — | Absolute-resolved ComponentsDatabasePath |
| ResolvedTemplatePartsDirectory | String (ReadOnly) | — | Absolute-resolved TemplatePartsDirectory |
| ResolvedOutputDirectory | String (ReadOnly) | — | Absolute-resolved OutputDirectory (creates if missing) |
| ResolvedLogFilePath | String (ReadOnly) | — | Absolute-resolved LogFilePath (creates parent dir if missing) |

---

## UI / Event Types

---

## `PipelineCompletedEventArgs` (`CadProgressForm.vb`)

| Property | Type |
|---|---|
| Result | PipelineResult (ReadOnly) |

---

## MainForm Fields (`MainForm.Logic.vb`, `MainForm.CAD.vb`)
> Non-UI state fields on the partial class.

| Field | Type | Notes |
|---|---|---|
| _engine | ComponentSelectionEngine | Selection engine; Nothing if DB failed to load |
| _lastResult | SelectionResult | Most recent successful engine output |
| _cadGen | SolidWorksAutomation | SW connection (declared in Logic.vb) |
| _orchestrator | PipelineOrchestrator | Lazily initialised in EnsureOrchestratorInitialised() |
| _swAutomation | SolidWorksAutomation | ⚠️ Near-duplicate: _cadGen in Logic.vb vs _swAutomation in CAD.vb — both are SolidWorksAutomation instances |
| _lastOutputDirectory | String | Last user-chosen output folder |
| _pipelineRunning | Boolean | True while pipeline executing |

---

## JSON Field Mapping Reference

All `<JsonProperty>` attributes found in the codebase, mapping JSON field names → VB property names.

### `Dimensions3D`
| JSON Field | VB Property |
|---|---|
| `lengthMm` | `Length` |
| `widthMm` | `Width` |
| `heightMm` | `Height` |
| `diameterMm` | `Diameter` |
| `diameterInches` | `DiameterInches` |
| `pitchInches` | `PitchInches` |
| `bladesCount` | `BladesCount` |
| `boreMm` | `BoreMm` |
| `hubDiameterMm` | `HubDiameterMm` |
| `shaftDiameterMm` | `ShaftDiameterMm` |
| `mountingPatternMm` | `MountingPatternMm` |
| `outerDiameterMm` | `OuterDiameterMm` |

### `ComponentSpecs` (base)
| JSON Field | VB Property |
|---|---|
| `name` | `ModelName` |
| `voltageMinV` | `MinVoltage` |
| `voltageMaxV` | `MaxVoltage` |
| `operatingTempMinC` | `MinOperatingTempC` |
| `operatingTempMaxC` | `MaxOperatingTempC` |

### `MotorSpec`
| JSON Field | VB Property |
|---|---|
| `motorKv` | `KV` |
| `maxPowerW` | `MaxPowerWatts` |
| `maxContinuousCurrentA` | `MaxCurrentAmps` |
| `noLoadCurrentA` | `NoLoadCurrentAmps` |
| `resistance_mOhm` | `InternalResistanceMilliOhm` |
| `maxThrustG` | `MaxThrustGrams` |
| `designatedPropSizeInMin` | `PropDiameterMinIn` |
| `designatedPropSizeInMax` | `PropDiameterMaxIn` |

### `ESCSpec`
| JSON Field | VB Property |
|---|---|
| `continuousCurrentPerChannelA` | `ContinuousCurrentAmps` |
| `burstCurrentPerChannelA` | `BurstCurrentAmps` |
| `voltageMinV` | `MinInputVoltage` |
| `voltageMaxV` | `MaxInputVoltage` |
| `cellCountMin` | `MinCellCount` |
| `cellCountMax` | `MaxCellCount` |
| `becVoltageV` | `BECVoltage` |
| `becCurrentMaxA` | `BECCurrentAmps` |
| `firmwareType` | `Firmware` |
| `telemetryOutput` | `SupportsBidirectionalDShot` |

### `PropellerSpec`
| JSON Field | VB Property |
|---|---|
| `isFolding` | `IsFoldable` |
| `maxStaticThrustG` | `StaticThrustGrams` |

### `FlightControllerSpec`
| JSON Field | VB Property |
|---|---|
| `processorType` | `Processor` |
| `imuPrimary` | `GyroscopeChip` |
| `barometerModel` | `BarometerChip` |
| `hasOnboardMagnetometer` | `HasMagnetometer` |
| `i2cCount` | `I2CBusCount` |
| `spiCount` | `SPIBusCount` |
| `analogInputs` | `AnalogInputCount` |
| `firmwareCompatibility` | `FirmwareCompatibility` |
| `formFactor` | `MountingPatternMm` |
| `inputVoltageMinV` | `InputVoltageMin` |
| `inputVoltageMaxV` | `InputVoltageMax` |

### `BatterySpec`
| JSON Field | VB Property |
|---|---|
| `nominalVoltageV` | `NominalCellVoltageV` |
| `maxChargeVoltageV` | `FullChargeVoltagePerCellV` |
| `dischargeRatingC` | `ContinuousCRating` |
| `burstRatingC` | `BurstCRating` |
| `mainConnectorType` | `DischargeConnector` |
| `balanceConnectorType` | `BalanceConnector` |

### `GPSModuleSpec`
| JSON Field | VB Property |
|---|---|
| `constellations` | `SupportedConstellations` |
| `updateRateHz` | `MaxUpdateRateHz` |
| `positionAccuracyM` | `HorizontalAccuracyMeters` |
| `compassModel` | `CompassChip` |
| `chipset` | `GNSSChipset` |
| `interfaceType` | `InterfaceTypes` |
| `currentDrawMA` | `CurrentDrawMA` |

### `TelemetryRadioSpec`
| JSON Field | VB Property |
|---|---|
| `maxTxPowerMW` | `OutputPowerMW` |
| `maxRangeKm` | `MaxRangeKm` |
| `protocol` | `TelemetryProtocol` |
| `frequencyHopping` | `SupportsFHSS` |
| `currentDrawActiveMA` | `CurrentDrawActiveMA` |

### `CameraSpec`
| JSON Field | VB Property |
|---|---|
| `cameraType` | `SensorType` |
| `resolutionMpx` | `ResolutionMpx` |
| `fovDegrees` | `DiagonalFOVDegrees` |
| `outputInterfaces` | `OutputInterfaces` |
| `hasElectronicImageStabilization` | `HasStabilisation` |
| `powerConsumptionW` | `PowerConsumptionWatts` |
| `isWeatherproof` | `IsWeatherproof` |

### `AppSettings` (DataContract)
| JSON Field | VB Property |
|---|---|
| `componentsDatabasePath` | `ComponentsDatabasePath` |
| `solidWorksInstallPath` | `SolidWorksInstallPath` |
| `templatePartsDirectory` | `TemplatePartsDirectory` |
| `outputDirectory` | `OutputDirectory` |
| `solidWorksTargetVersion` | `SolidWorksTargetVersion` |
| `logFilePath` | `LogFilePath` |
| `logLevel` | `LogLevel` |

---

## ⚠️ Duplicate / Near-Duplicate Name Warnings

| Issue | Details |
|---|---|
| `MaxRangeKm` | Exists on both `TelemetryRadioSpec` and `ReceiverSpec` — same name, similar semantics, but different component categories |
| `CurrentDrawMA` | Exists on both `GPSModuleSpec` and `ReceiverSpec` |
| `FrequencyMHz` | Exists on both `TelemetryRadioSpec` and `ReceiverSpec` |
| `MaxContinuousCurrentAmps` | `PowerDistributionBoardSpec` has a stored property; `BatterySpec` has a computed `ReadOnly` property with the same name |
| `MaxPowerW` vs `MaxPowerWatts` | Base class `ComponentSpecs` has `MaxPowerW`; `MotorSpec` overrides with `MaxPowerWatts`; `ComponentDisplayRow` maps to `MaxPowerWatts` |
| `MaxCurrentA` vs `MaxCurrentAmps` | Base class `ComponentSpecs.MaxCurrentA`; `MotorSpec.MaxCurrentAmps`; `ESCSpec.ContinuousCurrentAmps` — inconsistent naming across the hierarchy |
| `MinVoltage`/`MaxVoltage` vs `MinInputVoltage`/`MaxInputVoltage` | Base class uses shorter names; `ESCSpec` adds `Input` qualifier |
| `RangeKm` vs `MaxRangeKm` | Both on `MissionSpecs` — `RangeKm` is a pass-through property for `MaxRangeKm` |
| `PayloadWeightGrams` vs `PayloadMassGrams` | Both on `MissionSpecs` — `PayloadWeightGrams` is a pass-through for `PayloadMassGrams` |
| `MissionProfile` on both `MissionSpecs` and `AvionicsBudget` | Different types (`MissionProfileType`) — easy to confuse when passing objects |
| `OperatingEnvironment` on both `MissionSpecs` and `AvionicsBudget` | Different types (`EnvironmentType`) |
| `NominalVoltageV` on both `PowerBudget` and `AvionicsBudget` | Same name, forwarded value — intentional but warrants awareness |
| `MotorCount` on `MtowEstimate`, `ThrustRequirement`, and `PowerBudget` | Three intermediate result types carry the same field redundantly |
| `_cadGen` vs `_swAutomation` | Two `SolidWorksAutomation` fields on the same `MainForm` partial class (declared in different partial files) — likely a merge issue |
| `SelectedMotors` vs `Motors` | `MainForm.CAD.vb` references `_lastSelectionResult.Motors` but the property on `SelectionResult` is `SelectedMotors` — probable compile error |
