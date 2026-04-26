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

        ''' <summary>Maximum sustained wind speed in m/s. Valid range: 0–30 m/s. Default ~11 m/s (Beaufort 5).</summary>
        Public Property MaxWindSpeedMs As Double = 11.0

        ''' <summary>
        ''' Maximum take-off mass (design limit) in grams, including battery and payload.
        ''' Engine uses this as target to estimate required thrust and power.
        ''' Valid range: 100–55,000 g (smallest ready-to-fly quad to heavy industrial).
        ''' Default: 2000 g (2 kg, typical commercial quadcopter).
        ''' </summary>
        Public Property MaxTakeoffMassGrams As Double = 2000.0


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

        ''' <summary>
        ''' Target IP rating string (e.g. "IP54", "IP65"). Empty = no requirement.
        ''' Drives enclosure material and seal selection in Module 2.
        ''' Default: empty (no waterproofing).
        ''' </summary>


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
