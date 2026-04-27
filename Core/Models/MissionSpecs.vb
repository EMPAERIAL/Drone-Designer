Imports System.Collections.Generic

''' <summary>
''' Namespace for all core data models used across the UAV Design Tool.
''' These classes are pure data containers — no logic, no UI dependencies.
''' They can be serialized/deserialized to JSON for future web migration.
''' </summary>
Namespace Core.Models

    ' ─────────────────────────────────────────────────────────────────
    '  ENUMERATIONS
    '  Categorical fields use enums instead of magic strings so that
    '  the compiler catches typos and other chats can enumerate options.
    ' ─────────────────────────────────────────────────────────────────

    ' -------------------------------------------------------------------------
    ' TASK 11 RENAME NOTE
    ' -------------------------------------------------------------------------
    ' The enums below were originally named MissionProfile and OperatingEnvironment.
    ' Task 11 added properties of those exact names to MissionSpecs so that
    ' ComponentSelectionEngine (which uses specs.MissionProfile and
    ' specs.OperatingEnvironment) compiles without modification.
    '
    ' In VB.NET a property and a type cannot share a name in the same scope
    ' without ambiguity errors, so the enums were renamed:
    '
    '   MissionProfile       → MissionProfileCategory
    '   OperatingEnvironment → OperatingEnvCategory
    '
    ' All enum VALUES are identical — only the TYPE NAME changed.
    ' Update any call-site that referenced the old type names (e.g.,
    ' MissionProfile.Mapping → MissionProfileCategory.Mapping).
    ' -------------------------------------------------------------------------

    ''' <summary>
    ''' Defines the primary operational purpose of the UAV mission.
    ''' Renamed from MissionProfile (Task 11) to avoid shadowing the engine-
    ''' facing MissionSpecs.MissionProfile property of type MissionProfileType.
    ''' </summary>
    Public Enum MissionProfileCategory
        ''' <summary>Loitering surveillance — long endurance, quiet, live video.</summary>
        Surveillance = 0
        ''' <summary>Photogrammetry / LiDAR mapping — stable hover, precise GPS, wide FOV.</summary>
        Mapping = 1
        ''' <summary>Cargo or package delivery — high thrust, drop mechanism.</summary>
        Delivery = 2
        ''' <summary>Infrastructure inspection — maneuverability, close-range sensing.</summary>
        Inspection = 3
        ''' <summary>Search and rescue — long range, thermal, loud alarm.</summary>
        SearchAndRescue = 4
        ''' <summary>Racing / sport — max speed and agility, FPV, minimal payload.</summary>
        Racing = 5
    End Enum

    ''' <summary>
    ''' Physical airframe and propulsion topology of the UAV.
    ''' Determines motor count, ESC count, and structural layout for Module 2.
    ''' </summary>
    Public Enum UAVConfiguration
        ''' <summary>4-motor symmetric layout. Most common.</summary>
        Quadcopter = 0
        ''' <summary>6-motor layout. Redundancy and higher payload.</summary>
        Hexacopter = 1
        ''' <summary>8-motor layout. Maximum lift and full motor-loss redundancy.</summary>
        Octocopter = 2
        ''' <summary>Fixed wing. Best endurance and range.</summary>
        FixedWing = 3
        ''' <summary>VTOL hybrid. Combines multirotor hover with fixed-wing cruise.</summary>
        VTOL = 4
        ''' <summary>Single rotor with tail rotor.</summary>
        Helicopter = 5
        ''' <summary>Tricopter — 3 motors with a yaw servo.</summary>
        Tricopter = 6
    End Enum

    ''' <summary>
    ''' Broad classification of the outdoor/indoor operating environment.
    ''' Renamed from OperatingEnvironment (Task 11) to avoid shadowing the
    ''' engine-facing MissionSpecs.OperatingEnvironment property of type EnvironmentType.
    ''' </summary>
    Public Enum OperatingEnvCategory
        ''' <summary>Standard outdoor. Mild temperature, low humidity, open airspace.</summary>
        OutdoorStandard = 0
        ''' <summary>High-wind or coastal. Requires higher thrust margin.</summary>
        OutdoorHighWind = 1
        ''' <summary>Arctic or alpine. Low-temperature specs; battery heating may be needed.</summary>
        OutdoorCold = 2
        ''' <summary>Desert or equatorial. Thermal management critical.</summary>
        OutdoorHot = 3
        ''' <summary>Rain, fog, or maritime. Waterproofing required.</summary>
        OutdoorWet = 4
        ''' <summary>Indoor / GPS-denied. Optical flow / lidar needed.</summary>
        IndoorGPSDenied = 5
    End Enum

    ''' <summary>
    ''' Regulatory or airspace class the mission will operate under.
    ''' Affects telemetry requirements, fail-safe rules, and Remote ID hardware.
    ''' </summary>
    Public Enum RegulatoryClass
        ''' <summary>Recreational / hobbyist. Minimal regulatory hardware.</summary>
        Recreational = 0
        ''' <summary>Commercial standard (FAA Part 107 / EASA A2).</summary>
        CommercialStandard = 1
        ''' <summary>BVLOS. Redundant telemetry, detect-and-avoid, Remote ID required.</summary>
        BVLOS = 2
        ''' <summary>Military / special operations. Treated as custom.</summary>
        Military = 3
    End Enum

    ''' <summary>Power source topology.</summary>
    Public Enum PowerSourceType
        ''' <summary>Lithium Polymer — lightweight, high discharge.</summary>
        LiPo = 0
        ''' <summary>Lithium-Ion — higher energy density; good for fixed-wing.</summary>
        LiIon = 1
        ''' <summary>Hydrogen fuel cell — long endurance, low noise.</summary>
        HydrogenFuelCell = 2
        ''' <summary>Hybrid fuel cell + LiPo buffer.</summary>
        HybridFuelCellLiPo = 3
        ''' <summary>Tethered power supply — unlimited endurance, limited range.</summary>
        Tethered = 4
    End Enum
    ''' <summary>
    ''' Engine-facing mission profile type used by ComponentSelectionEngine.
    ''' </summary>
    Public Enum MissionProfileType
        General = 0
        Surveillance = 1
        Mapping = 2
        Delivery = 3
        Inspection = 4
        SearchAndRescue = 5
        Racing = 6
        Survey = 7
    End Enum

    ''' <summary>
    ''' Engine-facing environment classification.
    ''' Standard = normal conditions, Harsh = extreme temps, maritime, desert, arctic.
    ''' </summary>
    Public Enum EnvironmentType
        Standard = 0
        Harsh = 1
    End Enum

    ''' <summary>
    ''' Discrete phase of flight within a mission segment.
    ''' Used by <see cref="MissionSegment"/> to label segments in a mission profile.
    ''' Source: Bershadsky et al. (AIAA 2016-0581) define missions as hover, climb,
    ''' or dash segments; Yang et al. (Aerospace 2024) extend this to include descend.
    ''' </summary>
    Public Enum MissionPhase
        ''' <summary>Stationary hover at constant altitude.</summary>
        Hover = 0
        ''' <summary>Powered ascent at the user's specified climb rate.</summary>
        Climb = 1
        ''' <summary>Steady-state forward flight at cruise airspeed.</summary>
        Cruise = 2
        ''' <summary>Controlled descent. May be powered or autorotative.</summary>
        Descend = 3
    End Enum


    ' ─────────────────────────────────────────────────────────────────
    '  MAIN DATA CLASS
    ' ─────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Represents a complete set of UAV mission specifications.
    ''' This is the primary input to ComponentSelectionEngine (Module 1).
    '''
    ''' Design rules:
    '''   • Pure data container — no validation or selection logic.
    '''   • All properties have sensible defaults (typical small commercial quad).
    '''   • Nullable Double? is used where "not specified" ≠ zero.
    '''   • Two sets of mission-type and environment properties coexist:
    '''       - Profile / Environment use the legacy Category enums (UI-facing).
    '''       - MissionProfile / OperatingEnvironment use the engine enums
    '''         MissionProfileType / EnvironmentType (defined in ComponentSpecs.vb)
    '''         and are the properties ComponentSelectionEngine reads directly.
    '''     Both sets must be kept in sync; BuildMissionSpecs() in
    '''     MainForm.Logic.vb assigns both sets simultaneously.
    ''' </summary>
    Public Class MissionSpecs

        ' ── IDENTITY ──────────────────────────────────────────────────

        ''' <summary>Human-readable label for this mission configuration.</summary>
        Public Property MissionName As String = "Untitled Mission"

        ''' <summary>Optional free-text description. Not used by the selection engine.</summary>
        Public Property Description As String = String.Empty

        ''' <summary>Timestamp (UTC) when this spec set was created.</summary>
        Public Property CreatedAtUtc As DateTime = DateTime.UtcNow


        ' ── FLIGHT PERFORMANCE ────────────────────────────────────────

        ''' <summary>Required flight endurance in minutes. Valid range: 1–600 min.</summary>
        Public Property FlightEnduranceMinutes As Double = 30.0

        ''' <summary>Maximum operational range in kilometres. Valid range: 0.1–500 km.</summary>
        Public Property MaxRangeKm As Double = 5.0

        ''' <summary>Steady-state cruise speed in m/s. Valid range: 0–120 m/s.</summary>
        Public Property CruiseSpeedMs As Double = 10.0

        ''' <summary>Maximum design speed in m/s. Must be ≥ CruiseSpeedMs.</summary>
        Public Property MaxSpeedMs As Double = 15.0

        ''' <summary>Maximum altitude AGL in meters. Valid range: 10–6000 m.</summary>
        Public Property MaxAltitudeMeters As Double = 120.0

        ''' <summary>
        ''' Design vertical climb rate in m/s.
        '''
        ''' Drives peak-thrust calculation in any climb segment of the mission profile.
        ''' For a multirotor, climb power adds to hover power roughly linearly with
        ''' climb rate, so this value materially affects motor and battery sizing
        ''' for missions with significant altitude gain.
        '''
        ''' Typical values:
        '''   • Conservative commercial / mapping: 2–3 m/s
        '''   • Standard delivery / surveillance:  3–5 m/s
        '''   • Aggressive racing / sport:        10–15 m/s
        '''
        ''' Valid range: 0.1–20 m/s. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        ''' Default: 3.0 m/s — typical commercial multirotor.
        '''
        ''' Source: Yang et al. (Aerospace 2024) treat climb as a primary mission
        ''' phase with rate-driven energy demand.
        ''' </summary>
        Public Property ClimbRateMs As Double = 3.0

        ''' <summary>
        ''' Maximum climb angle in degrees, measured from the horizontal.
        '''
        ''' For a multirotor in pure vertical climb this is 90°; for an inclined
        ''' climb (forward + upward), it is the resultant velocity vector's angle
        ''' above horizontal. Drives the gravity component of design thrust:
        '''   T_design = m·g·sin(γ) + ½·ρ·V²·C_D·S_ref
        ''' (Stolz et al., DLR, AIAA 2018-1009).
        '''
        ''' Typical values:
        '''   • Mapping / inspection (gentle inclined climb):  20–30°
        '''   • Standard multirotor takeoff:                    30–45°
        '''   • Vertical takeoff / aggressive climb:            60–90°
        '''
        ''' Valid range: 0–90°. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        ''' Default: 30° — typical inclined climb-out for a commercial multirotor.
        ''' </summary>
        Public Property MaxClimbAngleDeg As Double = 30.0

        ''' <summary>Maximum sustained wind speed in m/s. Valid range: 0–30 m/s. Default ~11 m/s (Beaufort 5).</summary>
        Public Property MaxWindSpeedMs As Double = 11.0

        ''' <summary>
        ''' Regulatory / structural ceiling on take-off mass in grams, including
        ''' battery and payload. This is a HARD UPPER LIMIT the design must not
        ''' exceed — NOT a target the engine sizes to.
        '''
        ''' The component selection engine derives its own MTOW iteratively from
        ''' payload + airframe + avionics + battery (see <c>EstimateMtow</c> in
        ''' <c>ComponentSelectionEngine.vb</c>). This property is currently NOT
        ''' read by the engine.
        '''
        ''' TODO (engine-side, future task): <c>SelectComponents</c> should
        ''' compare its calculated MTOW against this field and raise
        ''' <c>ComponentSelectionException</c> if the design exceeds the limit.
        '''
        ''' Common ceilings:
        '''   •   250 g — sub-registration recreational class (FAA / EASA A1)
        '''   •  2,000 g — typical commercial quadcopter
        '''   • 25,000 g — EASA Specific Category / Italian regulatory cap
        '''   • 55,000 g — heavy industrial multirotor upper bound
        '''
        ''' Valid range: 100–55,000 g. Default: 2000 g (typical commercial quad).
        ''' </summary>
        Public Property MaxTakeoffMassGrams As Double = 2000.0

        ''' <summary>
        ''' User override for the design thrust-to-weight ratio (TWR).
        '''
        ''' When <c>Nothing</c> (default), the component selection engine
        ''' looks up a profile-appropriate default by mission type:
        '''   • Surveillance, Mapping:        2.0   (Zhang et al., wind-resistance floor)
        '''   • Delivery, Inspection,
        '''     SearchAndRescue:              2.5   (margin for payload + maneuvering)
        '''   • Racing:                       4.0   (aggressive maneuvering)
        '''   • Agriculture / endurance
        '''     fallback (low-mobility):      1.8   (Vu et al., calm-flight profile)
        '''   • General:                      2.0
        '''
        ''' When set to a numeric value, the engine uses that exact ratio for
        ''' per-motor thrust requirement calculation, ignoring the profile lookup.
        ''' Useful for:
        '''   • Custom builds outside the standard mission profiles.
        '''   • High-wind environments requiring extra hover margin (3.0+).
        '''   • Constrained-payload missions where every gram of overbuilt thrust
        '''     is a battery penalty (1.8–2.0 even on a "racing" classification).
        '''
        ''' Valid range: 1.5–6.0. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        '''
        ''' Sources: Zhang et al. (Lifting-Wing Multicopters, IEEE/ASME) for the
        ''' 2:1 wind-resistance floor; Vu et al. (Aerosp. Sci. Tech. 2019) for the
        ''' 1.8 endurance/agriculture baseline.
        ''' </summary>
        Public Property TargetThrustToWeightRatio As Double? = Nothing


        ' ── MISSION PROFILE SEGMENTS (Phase 1, TODO 16) ───────────────

        ''' <summary>
        ''' Ordered list of mission phase segments (hover, climb, cruise, descend).
        ''' When this list is non-empty, the component selection engine will
        ''' eventually integrate energy demand across each segment rather than
        ''' assuming a single all-hover mission.
        '''
        ''' When this list is empty (default), the engine should fall back to
        ''' <see cref="HoverFractionOfMission"/> as a coarser approximation.
        '''
        ''' Sources: Bershadsky et al. (AIAA 2016-0581), Yang et al. (Aerospace 2024).
        ''' </summary>
        Public Property MissionSegments As List(Of MissionSegment) = New List(Of MissionSegment)()

        ''' <summary>
        ''' Fraction of <see cref="FlightEnduranceMinutes"/> spent in hover (0.0–1.0).
        ''' This is a coarse-grained alternative to populating <see cref="MissionSegments"/>
        ''' for users who do not want to define a full segment list.
        '''
        ''' The engine should ONLY consult this property when MissionSegments is empty.
        ''' If MissionSegments is non-empty, this value is ignored.
        '''
        ''' Default: 1.0 (all hover) — preserves backward-compatible behavior with
        ''' the legacy single-scalar endurance model.
        '''
        ''' Valid range: 0.0–1.0. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        ''' </summary>
        Public Property HoverFractionOfMission As Double = 1.0


        ' ── PAYLOAD ───────────────────────────────────────────────────

        ''' <summary>Payload mass in grams (camera, gimbal, sensor, etc.). Default: 0 (no payload).</summary>
        Public Property PayloadMassGrams As Double = 0.0

        ''' <summary>
        ''' 3D bounding box for the mission payload (camera gimbal, LiDAR, etc.).
        ''' Used by Module 2 to size the payload bay on the airframe.
        ''' Default: unspecified (all zeros).
        ''' </summary>
        Public Property PayloadDimensionsMm As PayloadDimensions = New PayloadDimensions()

        ''' <summary>
        ''' Steady-state electrical power consumed by the mission payload during
        ''' active operation, in watts.
        '''
        ''' Examples:
        '''   • Passive optical camera + gimbal: 5–15 W
        '''   • LiDAR scanner: 20–40 W
        '''   • Agricultural sprayer pump (Vu et al. 2019, MG-1 reference): 40 W
        '''   • Thermal + visible dual-payload: 25–50 W
        '''
        ''' The component selection engine will eventually add this draw to the
        ''' propulsion current when sizing battery capacity.
        '''
        ''' Default: 0.0 W (no powered payload).
        ''' Sources: Vu et al. (Aerosp. Sci. Tech. 2019), Bershadsky et al. (AIAA 2016-0581).
        ''' </summary>
        Public Property PayloadPowerWatts As Double = 0.0


        ' ── AVIONICS POWER BUDGET ────────────────────────────────────

        ''' <summary>
        ''' Steady-state electrical power consumed by the avionics stack
        ''' (flight controller, GPS, telemetry radio, receiver, etc.) in watts.
        '''
        ''' Typical values:
        '''   • Bare-bones racing build (FC + RX only): 2–3 W
        '''   • Standard commercial quad (FC + GPS + telemetry + RX): 5–8 W
        '''   • Heavy mapping/inspection (FC + dual GPS + companion computer + RTK):
        '''     15–25 W
        '''
        ''' Excludes the camera/video transmitter — those are part of the payload
        ''' budget when present (see <see cref="PayloadPowerWatts"/>).
        '''
        ''' Default: 5.0 W — reflects a typical FC + GPS + telemetry + RX stack
        ''' at idle. Refine upward when adding a companion computer, RTK GPS,
        ''' or obstacle-avoidance sensors.
        '''
        ''' Sources: Vu et al. (Aerosp. Sci. Tech. 2019, P_avionic = 10 W for
        ''' agriculture multicopter), Bershadsky et al. (AIAA 2016-0581, I_a tracked
        ''' separately in EMST validator).
        ''' </summary>
        Public Property AvionicsPowerWatts As Double = 5.0


        ' ── ENVIRONMENT ───────────────────────────────────────────────

        ''' <summary>
        ''' Minimum expected ambient temperature in °C.
        ''' Drives operating temperature range filtering for batteries and other temperature-sensitive components.
        ''' Default: 0°C (standard commercial outdoor).
        ''' </summary>
        Public Property MinOperatingTempCelsius As Double = 0.0

        ''' <summary>
        ''' Maximum expected ambient temperature in °C.
        ''' Drives operating temperature range filtering and may trigger thermal management
        ''' (heatsinks, fans) in the SolidWorks CAD model.
        ''' Default: 45°C (typical commercial outdoor).
        ''' </summary>
        Public Property MaxOperatingTempCelsius As Double = 45.0

        ''' <summary>
        ''' High-level classification of the operating environment.
        ''' Used by the selection engine to pick weatherproofing levels and derate component ratings.
        ''' Default: OutdoorStandard (mild, dry outdoor).
        ''' </summary>
        Public Property Environment As OperatingEnvCategory = OperatingEnvCategory.OutdoorStandard

        ''' <summary>
        ''' True if waterproofing / dust sealing is required.
        ''' Usually triggered when Environment = OutdoorWet or IndoorGPSDenied.
        ''' Default: False.
        ''' </summary>
        Public Property RequiresWaterproofing As Boolean = False

        ' ── MISSION PROFILE & CONFIGURATION ───────────────────────────

        ''' <summary>
        ''' User-selected mission profile (surveillance, mapping, etc.).
        ''' Mapped from <see cref="MissionProfileCategory"/> for UI display.
        ''' Default: Surveillance (typical long-endurance quadcopter mission).
        ''' </summary>
        Public Property Profile As MissionProfileCategory = MissionProfileCategory.Surveillance

        ''' <summary>
        ''' User-selected UAV configuration (quad, hex, fixed-wing, etc.).
        ''' Drives motor count, ESC layout, and structural design in Module 2.
        ''' Default: Quadcopter.
        ''' </summary>
        Public Property Configuration As UAVConfiguration = UAVConfiguration.Quadcopter

        ''' <summary>Power source topology (LiPo, LiIon, fuel cell, etc.).</summary>
        Public Property PowerSource As PowerSourceType = PowerSourceType.LiPo

        ''' <summary>Regulatory or airspace class the mission will operate under.</summary>
        Public Property Regulatory As RegulatoryClass = RegulatoryClass.CommercialStandard


        ' ── ENGINE-COMPATIBILITY PROPERTIES (added Task 11) ───────────
        '
        ' ComponentSelectionEngine reads specs.MissionProfile, specs.OperatingEnvironment,
        ' specs.MotorCount, specs.RangeKm, and specs.PayloadWeightGrams directly.
        ' These properties were not on the original MissionSpecs (which used different
        ' names and enum types). They are added here as first-class properties so
        ' the engine compiles without modification.
        '
        ' MissionProfileType and EnvironmentType enums are defined in ComponentSpecs.vb.
        ' Both this property set AND the legacy Profile/Environment set must be assigned
        ' when constructing a MissionSpecs — see BuildMissionSpecs() in MainForm.Logic.vb.
        ' ─────────────────────────────────────────────────────────────────────────────

        ''' <summary>
        ''' Mission profile using the <see cref="MissionProfileType"/> enum from ComponentSpecs.vb.
        ''' This is the property read by <see cref="ComponentSelectionEngine"/> for all
        ''' profile-specific selection logic (racing loop rate, mapping resolution, etc.).
        ''' Set in sync with <see cref="Profile"/> by BuildMissionSpecs().
        ''' </summary>
        Public Property MissionProfile As MissionProfileType = MissionProfileType.General

        ''' <summary>
        ''' Operating environment using the <see cref="EnvironmentType"/> enum from ComponentSpecs.vb.
        ''' This is the property read by <see cref="ComponentSelectionEngine"/> for
        ''' temperature and IP-rating filtering (Harsh vs Standard).
        ''' Set in sync with <see cref="Environment"/> by BuildMissionSpecs().
        ''' </summary>
        Public Property OperatingEnvironment As EnvironmentType = EnvironmentType.Standard

        ''' <summary>
        ''' Number of motors / rotors on this UAV.
        ''' Read by the engine in MTOW iteration, thrust calculation, and ESC/PDB sizing.
        ''' Parsed from the Motor Count combo box in BuildMissionSpecs().
        ''' Valid multirotor values: 3, 4, 6, 8, 12. Engine normalises any other value to 4.
        ''' </summary>
        Public Property MotorCount As Integer = 4

        ''' <summary>
        ''' Maximum operational range in kilometres — engine-facing alias for
        ''' <see cref="MaxRangeKm"/>.  ComponentSelectionEngine reads specs.RangeKm
        ''' for telemetry, receiver, and GPS range margin calculations.
        ''' Both properties are set to the same value in BuildMissionSpecs().
        ''' Unit: km.
        ''' </summary>
        Public Property RangeKm As Double
            Get
                Return MaxRangeKm
            End Get
            Set(value As Double)
                MaxRangeKm = value
            End Set
        End Property

        ''' <summary>
        ''' Payload mass in grams — engine-facing alias for <see cref="PayloadMassGrams"/>.
        ''' ComponentSelectionEngine reads specs.PayloadWeightGrams in MTOW iteration
        ''' and cell-count determination.
        ''' Both properties are set to the same value in BuildMissionSpecs().
        ''' Unit: g.
        ''' </summary>
        Public Property PayloadWeightGrams As Double
            Get
                Return PayloadMassGrams
            End Get
            Set(value As Double)
                PayloadMassGrams = value
            End Set
        End Property


        ' ── ENERGY POLICY ────────────────────────────────────────────

        ''' <summary>
        ''' Maximum depth of discharge (DoD) the user is willing to apply to
        ''' the battery per flight, expressed as a fraction of nominal capacity.
        '''
        ''' Example: 0.80 means the engine sizes the battery so that 80% of
        ''' nominal capacity satisfies the mission energy budget, leaving 20%
        ''' unused at the mission's planned end-of-flight to preserve cycle life.
        '''
        ''' Distinct from <c>OperationalReserveFraction</c> (added in a future TODO),
        ''' which represents safety margin against unplanned events (wind,
        ''' return-to-home, navigation drift) layered on top of cycle-life DoD.
        ''' Effective usable energy is the product of both factors:
        '''   E_usable = E_nominal × DoD × (1 − reserve_fraction)
        '''
        ''' Typical values:
        '''   • Conservative / mission-critical / BVLOS:    0.70
        '''   • Standard commercial:                        0.80   (Vu et al. 2019 baseline)
        '''   • Hobbyist / demo / single-flight tolerance:  0.90
        '''
        ''' Valid range: 0.50–0.95. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        '''
        ''' Default: 0.80 — matches the existing engine default
        ''' (<c>LipoMaxDod</c> in <c>ComponentSelectionEngine.vb</c>) and the
        ''' agriculture-multicopter validation baseline from Vu et al. 2019.
        ''' Source: Vu et al. (Aerosp. Sci. Tech. 2019), Bershadsky et al.
        ''' (AIAA 2016-0581, EMST validator).
        ''' </summary>
        Public Property BatteryMaxDepthOfDischarge As Double = 0.80

        ''' <summary>
        ''' User override for the battery pack's specific energy in Wh/kg.
        '''
        ''' When <c>Nothing</c> (default), the component selection engine
        ''' looks up a <i>pack-level</i> default by <see cref="PowerSource"/>:
        '''   • LiPo:                130 Wh/kg   (matches existing engine default)
        '''   • LiIon:               180 Wh/kg   (higher density, better for fixed-wing)
        '''   • HydrogenFuelCell:    350 Wh/kg   (long endurance, low noise)
        '''   • HybridFuelCellLiPo:  240 Wh/kg   (between fuel cell and LiPo)
        '''   • Tethered:              0 Wh/kg   (no onboard storage)
        '''
        ''' These are <b>pack-level</b> values, accounting for cells + wiring +
        ''' BMS / connector overhead. Cell-level datasheet values are typically
        ''' 30–40% higher (e.g., a 180 Wh/kg cell yields ~130 Wh/kg at the pack).
        '''
        ''' When set to a numeric value, the engine uses that exact specific
        ''' energy in battery mass / capacity calculations. Useful for:
        '''   • Modeling a specific cell or pack datasheet.
        '''   • Sweeping next-generation chemistries (e.g., 200, 300, 400, 500
        '''     Wh/kg per Yang et al. 2024 Fig. 12).
        '''   • Modeling a known low-density rugged or high-temperature pack.
        '''
        ''' Valid range: 50–600 Wh/kg. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        '''
        ''' Source: Yang et al. (Sizing of Multicopter Air Taxis, Aerospace 2024)
        ''' parameterize 200–500 Wh/kg sweeps; current LiPo packs measure
        ''' 100–150 Wh/kg pack-level.
        ''' </summary>
        Public Property BatterySpecificEnergyWhPerKgOverride As Double? = Nothing

        ''' <summary>
        ''' Per-flight energy reserve as a fraction of usable battery energy,
        ''' withheld at mission-planning time to protect against unplanned events.
        '''
        ''' Distinct from <see cref="BatteryMaxDepthOfDischarge"/>, which protects
        ''' pack cycle life over many flights. Operational reserve protects a
        ''' single flight against:
        '''   • Stronger headwinds than forecast.
        '''   • Navigation drift / route-planning slack.
        '''   • Return-to-home and loiter contingencies.
        '''   • Payload-driven hover or wait time beyond the nominal mission profile.
        '''
        ''' The component selection engine will eventually compute usable mission
        ''' energy as:
        '''   E_usable = E_nominal × BatteryMaxDepthOfDischarge × (1 − OperationalReserveFraction)
        '''
        ''' Example: a 100 Wh nominal pack with DoD = 0.80 and reserve = 0.20 has
        ''' 100 × 0.80 × 0.80 = 64 Wh available for the planned mission profile.
        ''' The remaining 16 Wh (DoD-permitted, reserve-withheld) is available for
        ''' contingencies without exceeding cycle-life limits; the final 20 Wh
        ''' (below DoD) remains untouched to preserve pack longevity.
        '''
        ''' Typical values:
        '''   • Hobbyist / line-of-sight, calm conditions:   0.10
        '''   • Standard commercial / mapping:               0.20   (aviation default)
        '''   • BVLOS / mission-critical / high-wind:        0.30
        '''
        ''' Valid range: 0.0–0.50. Out-of-range values are not enforced here;
        ''' validation is the responsibility of <c>SpecValidator</c> when added.
        '''
        ''' Default: 0.20 — matches aviation-standard 20% energy reserve and
        ''' aligns with Bershadsky et al. (AIAA 2016-0581) reserve handling.
        ''' </summary>
        Public Property OperationalReserveFraction As Double = 0.20


        ' ── COMMUNICATION & CONTROL ───────────────────────────────────

        ''' <summary>Control link range (km). Defaults to MaxRangeKm if Nothing.</summary>
        Public Property ControlLinkRangeKm As Double? = Nothing

        ''' <summary>True if a real-time video downlink is required (triggers VTX selection).</summary>
        Public Property RequiresVideoDownlink As Boolean = True

        ''' <summary>Minimum video downlink range in km. Relevant only if RequiresVideoDownlink.</summary>
        Public Property VideoDownlinkRangeKm As Double = 2.0

        ''' <summary>True if a MAVLink telemetry data link is required.</summary>
        Public Property RequiresTelemetry As Boolean = True

        ''' <summary>True if ADS-B In or Remote ID hardware is required.</summary>
        Public Property RequiresRemoteID As Boolean = False


        ' ── AUTONOMY & NAVIGATION ─────────────────────────────────────

        ''' <summary>True if autonomous waypoint navigation is required.</summary>
        Public Property RequiresAutopilot As Boolean = True

        ''' <summary>True if optical flow / downward sensor is needed for GPS-denied hover.</summary>
        Public Property RequiresOpticalFlow As Boolean = False

        ''' <summary>True if obstacle avoidance sensors are required.</summary>
        Public Property RequiresObstacleAvoidance As Boolean = False

        ''' <summary>Number of obstacle-avoidance sensor directions (1, 3, or 6).</summary>
        Public Property ObstacleAvoidanceDirections As Integer = 1


        ' ── REDUNDANCY & SAFETY ───────────────────────────────────────

        ''' <summary>True if dual (redundant) GPS receivers are required.</summary>
        Public Property RequiresDualGPS As Boolean = False

        ''' <summary>True if a parachute recovery system should be included.</summary>
        Public Property RequiresParachute As Boolean = False

        ''' <summary>Required IP rating string (e.g. "IP54"). Empty = no requirement.</summary>
        Public Property RequiredIPRating As String = String.Empty


        ' ── CONSTRUCTOR ───────────────────────────────────────────────

        ''' <summary>
        ''' Initialises a MissionSpecs instance with all default values.
        ''' Defaults represent a typical small commercial quadcopter surveillance mission.
        ''' </summary>
        Public Sub New()
            ' Defaults are set inline on each property above.
            ' Cross-field derivations (e.g., auto-setting RequiresWaterproofing when
            ' Environment = OutdoorWet) are the responsibility of Core.Services.SpecValidator.
        End Sub

    End Class


    ' ─────────────────────────────────────────────────────────────────
    '  SUPPORTING VALUE TYPE
    ' ─────────────────────────────────────────────────────────────────

    ''' <summary>
    ''' Bounding box dimensions of the mission payload in millimetres.
    ''' Used by SolidWorks automation (Module 2) to size the payload bay cavity.
    ''' All three values default to 0, meaning "unspecified."
    ''' </summary>
    Public Class PayloadDimensions

        ''' <summary>Payload length (fore–aft axis) in mm.</summary>
        Public Property LengthMm As Double = 0.0

        ''' <summary>Payload width (lateral axis) in mm.</summary>
        Public Property WidthMm As Double = 0.0

        ''' <summary>Payload height (vertical axis) in mm.</summary>
        Public Property HeightMm As Double = 0.0

        ''' <summary>True if all three dimensions have been specified (non-zero).</summary>
        Public ReadOnly Property IsSpecified As Boolean
            Get
                Return LengthMm > 0 AndAlso WidthMm > 0 AndAlso HeightMm > 0
            End Get
        End Property

        Public Sub New()
        End Sub

        Public Sub New(lengthMm As Double, widthMm As Double, heightMm As Double)
            Me.LengthMm = lengthMm
            Me.WidthMm = widthMm
            Me.HeightMm = heightMm
        End Sub

        Public Overrides Function ToString() As String
            Return If(IsSpecified,
                      $"{LengthMm:0.#} × {WidthMm:0.#} × {HeightMm:0.#} mm",
                      "Unspecified")
        End Function

    End Class

    ''' <summary>
    ''' One discrete phase of a UAV mission profile (hover/climb/cruise/descend),
    ''' with a duration and an airspeed.
    '''
    ''' A complete mission is represented as an ordered <see cref="System.Collections.Generic.List(Of MissionSegment)"/>
    ''' on <see cref="MissionSpecs.MissionSegments"/>. The component selection
    ''' engine will eventually integrate energy demand across all segments
    ''' rather than assuming a single all-hover mission.
    '''
    ''' Sources: Bershadsky et al. (AIAA 2016-0581), Yang et al. (Aerospace 2024).
    ''' </summary>
    Public Class MissionSegment

        ''' <summary>Phase of flight for this segment.</summary>
        Public Property Phase As MissionPhase = MissionPhase.Hover

        ''' <summary>Duration of this segment in seconds. Must be ≥ 0.</summary>
        Public Property DurationSeconds As Double = 0.0

        ''' <summary>
        ''' Airspeed during this segment in m/s. For Hover, this is 0.
        ''' For Climb and Descend, this is the horizontal airspeed component.
        ''' </summary>
        Public Property AirspeedMs As Double = 0.0

        ''' <summary>Default constructor — produces a zero-duration hover segment.</summary>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' Constructs a fully-specified segment.
        ''' </summary>
        ''' <param name="phase">Flight phase.</param>
        ''' <param name="durationSeconds">Segment duration (s).</param>
        ''' <param name="airspeedMs">Segment airspeed (m/s); 0 for hover.</param>
        Public Sub New(phase As MissionPhase, durationSeconds As Double, airspeedMs As Double)
            Me.Phase = phase
            Me.DurationSeconds = durationSeconds
            Me.AirspeedMs = airspeedMs
        End Sub

        Public Overrides Function ToString() As String
            Return $"{Phase} for {DurationSeconds:F0} s @ {AirspeedMs:F1} m/s"
        End Function

    End Class

End Namespace
