' =============================================================================
' File:    Core/Models/ComponentSpecs.vb
' Purpose: Data model classes for UAV component specifications.
'          Contains a base ComponentSpec class and one subclass per component
'          category.  These are pure data-holders — no business logic lives here.
'          All selection / scoring logic belongs in Core/Services/.
'
' Usage across chats:
'   - ComponentSelectionEngine.vb reads and returns instances of these types.
'   - Module 2 (SolidWorks automation) consumes the mechanical properties
'     (Mass, Dimensions, MountingPattern, ShaftDiameter, etc.) to drive CAD.
'   - The JSON in Resources/components.json should map 1-to-1 to these classes;
'     ComponentRepository.vb is responsible for deserialisation.
'
' Naming convention:  every property uses PascalCase.
'                     Units are noted in the XML summary of each property.
'
' Task note (JsonProperty pass):
'   All <JsonProperty> attributes and OnDeserialized callbacks were added in
'   this pass to align property names with the components.json field names.
'   A StringArrayConverter was added for fields stored as JSON string arrays
'   (firmwareCompatibility, interfaceType, outputInterfaces) to prevent
'   deserialisation crashes.  No existing property names were changed.
' =============================================================================

Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Runtime.Serialization

Namespace Core.Models

    ' =========================================================================
    ' Enum: ComponentCategory
    ' =========================================================================

    ''' <summary>
    ''' Identifies the functional category of a UAV component.
    ''' Used for filtering, grouping, and driving subclass instantiation.
    ''' </summary>
    Public Enum ComponentCategory
        ''' <summary>Brushless or brushed drive motor.</summary>
        Motor

        ''' <summary>Electronic Speed Controller — drives a motor.</summary>
        ESC

        ''' <summary>Propeller blade assembly.</summary>
        Propeller

        ''' <summary>Autopilot / stabilisation flight controller board.</summary>
        FlightController

        ''' <summary>Rechargeable battery pack.</summary>
        Battery

        ''' <summary>GNSS positioning module.</summary>
        GPSModule

        ''' <summary>Air-to-ground data-link radio.</summary>
        TelemetryRadio

        ''' <summary>Optical, thermal, or multispectral imaging sensor.</summary>
        Camera

        ''' <summary>RC servo actuator (control surfaces, gimbal, etc.).</summary>
        Servo

        ''' <summary>RC link receiver.</summary>
        Receiver

        ''' <summary>Power Distribution Board — routes battery power to ESCs and BECs.</summary>
        PowerDistributionBoard
    End Enum

    ' =========================================================================
    ' Enum: MotorType
    ' =========================================================================

    ''' <summary>Electrical topology of a drive motor.</summary>
    Public Enum MotorType
        ''' <summary>Three-phase brushless outrunner or inrunner.</summary>
        Brushless
        ''' <summary>Traditional brushed DC motor.</summary>
        Brushed
    End Enum

    ' =========================================================================
    ' Enum: BatteryCellChemistry
    ' =========================================================================

    ''' <summary>Electrochemical cell type used in a battery pack.</summary>
    Public Enum BatteryCellChemistry
        ''' <summary>Lithium Polymer — most common for UAVs.</summary>
        LiPo
        ''' <summary>Lithium-Ion — higher energy density, lower C-rating.</summary>
        LiIon
        ''' <summary>Lithium Iron Phosphate — safer, heavier.</summary>
        LiFePO4
        ''' <summary>Nickel-Metal Hydride.</summary>
        NiMH
    End Enum

    ' =========================================================================
    ' Enum: GNSSConstellation
    ' =========================================================================

    ''' <summary>
    ''' Bit-flag enum of GNSS constellations a GPS module may support.
    ''' Combine with the Or operator: GPS Or GLONASS.
    ''' </summary>
    <Flags>
    Public Enum GNSSConstellation
        None = 0
        GPS = 1
        GLONASS = 2
        BeiDou = 4
        Galileo = 8
        QZSS = 16
        SBAS = 32
    End Enum

    ' =========================================================================
    ' Enum: ServoSignalType
    ' =========================================================================

    ''' <summary>Control signal protocol accepted by a servo or ESC.</summary>
    Public Enum ServoSignalType
        ''' <summary>Standard 50 Hz PWM (1000–2000 µs pulse).</summary>
        AnalogPWM
        ''' <summary>Digital PWM — faster refresh, same pulse range.</summary>
        DigitalPWM
        ''' <summary>Serial bus protocol (e.g. Futaba S.Bus).</summary>
        SBus
        ''' <summary>Digital Serial (e.g. Hitec HSB).</summary>
        HiTecSerial
    End Enum

    ' =========================================================================
    ' Enum: RCLinkProtocol
    ' =========================================================================

    ''' <summary>Manufacturer / protocol family for an RC receiver or transmitter.</summary>
    Public Enum RCLinkProtocol
        FrSky_ACCST
        FrSky_ACCESS
        Futaba_FASST
        Futaba_FASSTest
        Spektrum_DSM2
        Spektrum_DSMX
        TBS_Crossfire
        ExpressLRS
        FlySky_AFHDS
        FlySky_AFHDS2A
        Other
    End Enum

    ' =========================================================================
    ' Enum: FlightControllerFirmware
    ' =========================================================================

    ''' <summary>Open- or closed-source firmware ecosystem a flight controller supports.</summary>
    Public Enum FlightControllerFirmware
        ArduPilot
        PX4
        Betaflight
        INAV
        Cleanflight
        Proprietary
        Multiple
    End Enum

    ' =========================================================================
    ' Enum: ESCProtocol
    ' =========================================================================

    ''' <summary>Digital or analogue signalling protocols supported by an ESC.</summary>
    <Flags>
    Public Enum ESCProtocol
        None = 0
        PWM = 1
        OneShot125 = 2
        OneShot42 = 4
        MultiShot = 8
        DShot150 = 16
        DShot300 = 32
        DShot600 = 64
        DShot1200 = 128
        ProShot = 256
    End Enum

    ' =========================================================================
    ' Enum: CameraOutputInterface
    ' =========================================================================

    ''' <summary>Video or data output interface of a camera / imaging sensor.</summary>
    Public Enum CameraOutputInterface
        AnalogNTSC_PAL
        HDMI
        MicroHDMI
        USB
        MIPI_CSI
        Ethernet
        SDI
        Other
    End Enum

    ' =========================================================================
    ' Enum: PowerConnectorType
    ' =========================================================================

    ''' <summary>Main battery / power connector type.</summary>
    Public Enum PowerConnectorType
        XT30
        XT60
        XT90
        EC3
        EC5
        Deans_T
        JST_PH
        JST_XH
        Anderson_Powerpole
        Bare_Wire
        Other
    End Enum

    ' =========================================================================
    ' Converter: StringArrayConverter
    ' Joins a JSON string array into a single comma-separated String property.
    ' Used for fields like firmwareCompatibility, interfaceType, outputInterfaces.
    ' =========================================================================

    ''' <summary>
    ''' Converts a JSON array of strings to a single comma-separated String and back.
    ''' Prevents deserialisation crashes for fields stored as arrays in components.json
    ''' but mapped to plain String properties on the spec classes.
    ''' </summary>
    Public Class StringArrayConverter
        Inherits JsonConverter(Of String)

        Public Overrides Function ReadJson(reader As JsonReader,
                                           objectType As Type,
                                           existingValue As String,
                                           hasExistingValue As Boolean,
                                           serializer As JsonSerializer) As String
            Dim token As JToken = JToken.Load(reader)
            Select Case token.Type
                Case JTokenType.Array
                    Return String.Join(", ", token.ToObject(Of String())())
                Case JTokenType.String
                    Return token.Value(Of String)()
                Case JTokenType.Null
                    Return String.Empty
                Case Else
                    Return token.ToString()
            End Select
        End Function

        Public Overrides Sub WriteJson(writer As JsonWriter,
                                        value As String,
                                        serializer As JsonSerializer)
            writer.WriteValue(value)
        End Sub
    End Class

    ' =========================================================================
    ' Class: Dimensions3D
    ' =========================================================================

    ''' <summary>
    ''' Axis-aligned bounding box dimensions for a physical component.
    ''' All values in millimetres (mm) unless otherwise noted.
    ''' Also carries propeller-specific and motor-specific nested dimension fields
    ''' so that Newtonsoft can deserialise them from the "dimensions" JSON object.
    ''' </summary>
    Public Class Dimensions3D

        ' --- Generic box dimensions -------------------------------------------

        ''' <summary>Overall length along the X-axis in mm.</summary>
        <JsonProperty("lengthMm")>
        Public Property Length As Double

        ''' <summary>Overall width along the Y-axis in mm.</summary>
        <JsonProperty("widthMm")>
        Public Property Width As Double

        ''' <summary>Overall height / depth along the Z-axis in mm.</summary>
        <JsonProperty("heightMm")>
        Public Property Height As Double

        ''' <summary>Overall diameter in mm (circular components).</summary>
        <JsonProperty("diameterMm")>
        Public Property Diameter As Double

        ' --- Propeller-specific nested dimension fields -----------------------

        ''' <summary>Propeller blade diameter in inches. Sourced from dimensions.diameterInches.</summary>
        <JsonProperty("diameterInches")>
        Public Property DiameterInches As Double

        ''' <summary>Propeller blade pitch in inches. Sourced from dimensions.pitchInches.</summary>
        <JsonProperty("pitchInches")>
        Public Property PitchInches As Double

        ''' <summary>Number of blades. Sourced from dimensions.bladesCount.</summary>
        <JsonProperty("bladesCount")>
        Public Property BladesCount As Integer

        ''' <summary>Propeller bore (centre hole) diameter in mm. Sourced from dimensions.boreMm.</summary>
        <JsonProperty("boreMm")>
        Public Property BoreMm As Double

        ''' <summary>Hub outer diameter in mm. Sourced from dimensions.hubDiameterMm.</summary>
        <JsonProperty("hubDiameterMm")>
        Public Property HubDiameterMm As Double

        ' --- Motor-specific nested dimension fields ---------------------------

        ''' <summary>Motor shaft diameter in mm. Sourced from dimensions.shaftDiameterMm.</summary>
        <JsonProperty("shaftDiameterMm")>
        Public Property ShaftDiameterMm As Double

        ''' <summary>Mounting bolt-circle diameter in mm. Sourced from dimensions.mountingPatternMm.</summary>
        <JsonProperty("mountingPatternMm")>
        Public Property MountingPatternMm As Double

        ''' <summary>Outer bell diameter in mm. Sourced from dimensions.outerDiameterMm.</summary>
        <JsonProperty("outerDiameterMm")>
        Public Property OuterDiameterMm As Double

        ' --- Constructors ----------------------------------------------------

        ''' <summary>Creates a zero-size instance.</summary>
        Public Sub New()
        End Sub

        ''' <summary>Creates an instance with specified dimensions.</summary>
        Public Sub New(length As Double, width As Double, height As Double, diameter As Double)
            Me.Length = length
            Me.Width = width
            Me.Height = height
            Me.Diameter = diameter
        End Sub

        ''' <summary>Returns a human-readable string: "LxWxH mm".</summary>
        Public Overrides Function ToString() As String
            Return $"{Length}×{Width}×{Height} mm"
        End Function

    End Class

    ' =========================================================================
    ' Class: ComponentSpec  (base class)
    ' =========================================================================

    ''' <summary>
    ''' Abstract base class for all UAV component specifications.
    ''' Contains identity, physical, and environmental properties common to
    ''' every component category.  Concrete subclasses add category-specific
    ''' properties.
    ''' </summary>
    Public MustInherit Class ComponentSpecs

        ' --- Identity --------------------------------------------------------

        ''' <summary>
        ''' Unique identifier for this component record.
        ''' Populated by ComponentRepository when loading from components.json.
        ''' </summary>
        Public Property Id As String = Guid.NewGuid().ToString()

        ''' <summary>
        ''' Human-readable product name (e.g. "T-Motor F60 Pro IV 1750KV").
        ''' JSON field: "name"
        ''' </summary>
        <JsonProperty("name")>
        Public Property ModelName As String = String.Empty

        ''' <summary>
        ''' Brand or manufacturer name (e.g. "T-Motor", "Holybro").
        ''' </summary>
        Public Property Manufacturer As String = String.Empty

        ''' <summary>
        ''' Manufacturer part number or SKU.  Useful for sourcing and BOM export.
        ''' </summary>
        Public Property PartNumber As String = String.Empty

        ''' <summary>
        ''' Primary communication interface (e.g. "UART", "I2C").
        ''' </summary>
        Public Property _Interface As String = String.Empty

        ''' <summary>
        ''' The functional category this component belongs to.
        ''' Determines which subclass should be used at runtime.
        ''' </summary>
        Public Property Category As ComponentCategory

        ' --- Physical --------------------------------------------------------

        ''' <summary>
        ''' Component mass in grams (g), excluding cables unless stated.
        ''' Critical for weight-budget calculations in the selection engine.
        ''' </summary>
        Public Property MassGrams As Double

        ''' <summary>
        ''' Outer envelope dimensions of the component (mm).
        ''' Used by Module 2 to size mounting features and clearance volumes.
        ''' </summary>
        Public Property Dimensions As New Dimensions3D()

        ' --- Electrical (common subset) --------------------------------------

        ''' <summary>Minimum operating supply voltage in Volts (V). JSON field: "voltageMinV"</summary>
        <JsonProperty("voltageMinV")>
        Public Property MinVoltage As Double

        ''' <summary>Nominal operating supply voltage in Volts (V).</summary>
        Public Property NominalVoltageV As Double

        ''' <summary>Maximum operating power in Watts (W).</summary>
        Public Property MaxPowerW As Double

        ''' <summary>Maximum operating supply voltage in Volts (V). JSON field: "voltageMaxV"</summary>
        <JsonProperty("voltageMaxV")>
        Public Property MaxVoltage As Double

        ''' <summary>Maximum continuous current draw in Amperes (A).</summary>
        Public Property MaxCurrentA As Double

        ''' <summary>Minimum current draw in Amperes (A).</summary>
        Public Property MinCurrentA As Double

        ''' <summary>Nominal current draw in Amperes (A).</summary>
        Public Property NominalCurrentA As Double

        ' --- Environmental ---------------------------------------------------

        ''' <summary>
        ''' Minimum operating ambient temperature in degrees Celsius (°C).
        ''' JSON field: "operatingTempMinC"
        ''' </summary>
        <JsonProperty("operatingTempMinC")>
        Public Property MinOperatingTempC As Double = -20.0

        ''' <summary>
        ''' Maximum operating ambient temperature in degrees Celsius (°C).
        ''' JSON field: "operatingTempMaxC"
        ''' </summary>
        <JsonProperty("operatingTempMaxC")>
        Public Property MaxOperatingTempC As Double = 60.0

        ' --- Sourcing / meta -------------------------------------------------

        ''' <summary>
        ''' Approximate retail price in USD at time of data entry.
        ''' Not used in selection scoring; informational only.
        ''' </summary>
        Public Property PriceUSD As Double

        ''' <summary>
        ''' Freeform notes — data-quality warnings, compatibility caveats, etc.
        ''' </summary>
        Public Property Notes As String = String.Empty

        ' --- Convenience -----------------------------------------------------

        ''' <summary>
        ''' Returns a short display string: "Category | Manufacturer Name".
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"{Category} | {Manufacturer} {ModelName}"
        End Function

    End Class

    ' =========================================================================
    ' Class: MotorSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a brushless or brushed drive motor.
    ''' KV, thrust, power, and mounting data are the primary inputs to the
    ''' propulsion sizing algorithm in ComponentSelectionEngine.
    ''' </summary>
    Public Class MotorSpec
        Inherits ComponentSpecs

        ''' <summary>
        ''' Velocity constant in RPM per Volt (KV).
        ''' JSON field: "motorKv"
        ''' </summary>
        <JsonProperty("motorKv")>
        Public Property KV As Integer

        ''' <summary>
        ''' Maximum continuous shaft power output in Watts (W).
        ''' JSON field: "maxPowerW"
        ''' </summary>
        <JsonProperty("maxPowerW")>
        Public Property MaxPowerWatts As Double

        ''' <summary>
        ''' Maximum continuous current draw in Amperes (A) at rated voltage.
        ''' JSON field: "maxContinuousCurrentA"
        ''' </summary>
        <JsonProperty("maxContinuousCurrentA")>
        Public Property MaxCurrentAmps As Double

        ''' <summary>
        ''' No-load current draw at 10 V in Amperes (A).
        ''' JSON field: "noLoadCurrentA"
        ''' </summary>
        <JsonProperty("noLoadCurrentA")>
        Public Property NoLoadCurrentAmps As Double

        ''' <summary>
        ''' Internal resistance of the motor windings in milliohms (mΩ).
        ''' JSON field: "resistance_mOhm"
        ''' </summary>
        <JsonProperty("resistance_mOhm")>
        Public Property InternalResistanceMilliOhm As Double

        ''' <summary>
        ''' Maximum static thrust produced with the recommended propeller in grams (g).
        ''' JSON field: "maxThrustG"
        ''' </summary>
        <JsonProperty("maxThrustG")>
        Public Property MaxThrustGrams As Double

        ''' <summary>
        ''' Propeller size at which MaxThrustGrams was measured. Format: "DiameterxPitch" in inches.
        ''' </summary>
        Public Property MaxThrustTestPropeller As String = String.Empty

        ''' <summary>
        ''' Motor shaft diameter in mm. Populated from dimensions.shaftDiameterMm via OnDeserialized.
        ''' </summary>
        Public Property ShaftDiameterMm As Double

        ''' <summary>
        ''' Stator diameter (the wound core) in mm.
        ''' </summary>
        Public Property StatorDiameterMm As Double

        ''' <summary>
        ''' Stator height (winding stack height) in mm.
        ''' </summary>
        Public Property StatorHeightMm As Double

        ''' <summary>
        ''' Number of magnetic poles on the rotor bell.
        ''' </summary>
        Public Property PoleCount As Integer

        ''' <summary>
        ''' Minimum recommended propeller diameter in inches.
        ''' JSON field: "designatedPropSizeInMin"
        ''' </summary>
        <JsonProperty("designatedPropSizeInMin")>
        Public Property PropDiameterMinIn As Double

        ''' <summary>
        ''' Maximum recommended propeller diameter in inches.
        ''' JSON field: "designatedPropSizeInMax"
        ''' </summary>
        <JsonProperty("designatedPropSizeInMax")>
        Public Property PropDiameterMaxIn As Double

        ''' <summary>
        ''' Bolt-circle diameter for the motor-to-mount bolt pattern in mm.
        ''' Populated from dimensions.mountingPatternMm via OnDeserialized.
        ''' </summary>
        Public Property MountingBoltCircleMm As Double

        ''' <summary>
        ''' Number of mounting bolts (typically 3 or 4).
        ''' </summary>
        Public Property MountingBoltCount As Integer = 4

        ''' <summary>
        ''' Motor topology — brushless is the norm for UAVs.
        ''' </summary>
        Public Property MotorType As MotorType = MotorType.Brushless

        ''' <summary>Recommended minimum cell count (e.g. 4 for "4S min").</summary>
        Public Property RecommendedMinCells As Integer

        ''' <summary>Recommended maximum cell count (e.g. 6 for "6S max").</summary>
        Public Property RecommendedMaxCells As Integer

        ''' <summary>Thrust per Watt in gf/W.</summary>
        Public Property Efficiency As Double

        ''' <summary>
        ''' Motor torque constant (N·m/A).
        ''' Relationship to KV: Kt = 1 / Kv_SI where Kv_SI = KV × (2π/60).
        ''' Derived automatically if not provided: KtNmPerA = 60 / (2π × KV).
        ''' </summary>
        <JsonProperty("ktNmPerA")>
        Public Property KtNmPerA As Double

        ''' <summary>
        ''' Maximum continuous torque the motor can produce (N·m).
        ''' Derived automatically if not provided: MaxTorqueNm = KtNmPerA × MaxCurrentAmps.
        ''' </summary>
        <JsonProperty("maxTorqueNm")>
        Public Property MaxTorqueNm As Double

        ''' <summary>
        ''' Winding resistance in Ohms. Used for copper-loss efficiency checks.
        ''' Derived automatically from InternalResistanceMilliOhm / 1000 if not provided.
        ''' </summary>
        <JsonProperty("windingResistanceOhm")>
        Public Property WindingResistanceOhm As Double

        ''' <summary>
        ''' Copies nested dimension fields to top-level properties after JSON deserialisation.
        ''' </summary>
        <OnDeserialized>
        Private Sub OnDeserialized(context As StreamingContext)
            If Dimensions IsNot Nothing Then
                ShaftDiameterMm = Dimensions.ShaftDiameterMm
                MountingBoltCircleMm = Dimensions.MountingPatternMm
            End If
            If Efficiency = 0 AndAlso MaxPowerWatts > 0 Then
                Efficiency = MaxThrustGrams / MaxPowerWatts
            End If
            If WindingResistanceOhm <= 0 AndAlso InternalResistanceMilliOhm > 0 Then
                WindingResistanceOhm = InternalResistanceMilliOhm / 1000.0
            End If
            If KtNmPerA <= 0 AndAlso KV > 0 Then
                KtNmPerA = 60.0 / (2.0 * Math.PI * KV)
            End If
            If MaxTorqueNm <= 0 AndAlso KtNmPerA > 0 AndAlso MaxCurrentAmps > 0 Then
                MaxTorqueNm = KtNmPerA * MaxCurrentAmps
            End If
        End Sub

        Public Sub New()
            Category = ComponentCategory.Motor
        End Sub

    End Class

    ' =========================================================================
    ' Class: ESCSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for an Electronic Speed Controller.
    ''' One ESC drives one motor.  The selection engine ensures each ESC's
    ''' current rating >= the paired motor's MaxCurrentAmps with headroom.
    ''' </summary>
    Public Class ESCSpec
        Inherits ComponentSpecs

        ''' <summary>
        ''' Continuous current rating in Amperes (A).
        ''' JSON field: "continuousCurrentPerChannelA"
        ''' </summary>
        <JsonProperty("continuousCurrentPerChannelA")>
        Public Property ContinuousCurrentAmps As Double

        ''' <summary>
        ''' Burst current rating in Amperes (A), sustainable for ~10 seconds.
        ''' JSON field: "burstCurrentPerChannelA"
        ''' </summary>
        <JsonProperty("burstCurrentPerChannelA")>
        Public Property BurstCurrentAmps As Double

        ''' <summary>
        ''' Minimum input voltage in Volts (V).
        ''' JSON field: "voltageMinV"
        ''' </summary>
        <JsonProperty("voltageMinV")>
        Public Property MinInputVoltage As Double

        ''' <summary>
        ''' Maximum input voltage in Volts (V).
        ''' JSON field: "voltageMaxV"
        ''' </summary>
        <JsonProperty("voltageMaxV")>
        Public Property MaxInputVoltage As Double

        ''' <summary>Minimum Li-Po cell count supported. JSON field: "cellCountMin"</summary>
        <JsonProperty("cellCountMin")>
        Public Property MinCellCount As Integer

        ''' <summary>Maximum Li-Po cell count supported. JSON field: "cellCountMax"</summary>
        <JsonProperty("cellCountMax")>
        Public Property MaxCellCount As Integer

        ''' <summary>True if this ESC includes an onboard BEC.</summary>
        Public Property HasBEC As Boolean

        ''' <summary>True if this is a 4-in-1 ESC board.</summary>
        Public Property IsAllInOne As Boolean

        ''' <summary>BEC output voltage in Volts (V). JSON field: "becVoltageV"</summary>
        <JsonProperty("becVoltageV")>
        Public Property BECVoltage As Double?

        ''' <summary>Maximum BEC output current in Amperes (A). JSON field: "becCurrentMaxA"</summary>
        <JsonProperty("becCurrentMaxA")>
        Public Property BECCurrentAmps As Double?

        ''' <summary>
        ''' Bitfield of digital/analogue signalling protocols this ESC accepts.
        ''' </summary>
        <JsonConverter(GetType(ESCProtocolArrayConverter))>
        Public Property SupportedProtocols As ESCProtocol = ESCProtocol.PWM

        ''' <summary>Firmware shipped from the factory. JSON field: "firmwareType"</summary>
        <JsonProperty("firmwareType")>
        Public Property Firmware As String = String.Empty

        ''' <summary>
        ''' True if the ESC supports bidirectional DSHOT telemetry.
        ''' JSON field: "telemetryOutput"
        ''' </summary>
        <JsonProperty("telemetryOutput")>
        Public Property SupportsBidirectionalDShot As Boolean

        ''' <summary>True if this is a 4-in-1 ESC board (all four ESCs on one PCB).</summary>
        Public Property IsQuadESC As Boolean = False

        Public Sub New()
            Category = ComponentCategory.ESC
        End Sub

    End Class

    ' =========================================================================
    ' Class: PropellerSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a propeller blade assembly.
    ''' Diameter and pitch are the primary aerodynamic sizing inputs.
    ''' DiameterInches and PitchInches are nested under "dimensions" in the JSON
    ''' and are lifted to top-level properties via OnDeserialized.
    ''' </summary>
    Public Class PropellerSpec
        Inherits ComponentSpecs

        ''' <summary>Blade diameter in inches. Populated from dimensions.diameterInches.</summary>
        Public Property DiameterInches As Double

        ''' <summary>Blade pitch in inches. Populated from dimensions.pitchInches.</summary>
        Public Property PitchInches As Double

        ''' <summary>Number of blades. Populated from dimensions.bladesCount.</summary>
        Public Property BladeCount As Integer = 2

        ''' <summary>Bore diameter in mm. Populated from dimensions.boreMm.</summary>
        Public Property BoreDiameterMm As Double

        ''' <summary>Primary blade material.</summary>
        Public Property Material As String = String.Empty

        ''' <summary>True if the blades are foldable. JSON field: "isFolding"</summary>
        <JsonProperty("isFolding")>
        Public Property IsFoldable As Boolean = False

        ''' <summary>Maximum safe rotational speed in RPM.</summary>
        Public Property MaxRPM As Integer

        ''' <summary>
        ''' Static thrust in grams (g).
        ''' JSON field: "maxStaticThrustG"
        ''' </summary>
        <JsonProperty("maxStaticThrustG")>
        Public Property StaticThrustGrams As Double

        ''' <summary>RPM at which StaticThrustGrams was measured.</summary>
        Public Property StaticThrustTestRPM As Integer

        ''' <summary>True if this is a clockwise (CW) rotation prop.</summary>
        Public Property IsClockwiseRotation As Boolean = True

        ''' <summary>Efficiency proxy (StaticThrustGrams / mass) — computed.</summary>
        Public Property Efficiency As Double

        ''' <summary>
        ''' Dimensionless static thrust coefficient.
        ''' Equation: T = Ct × ρ × n² × D⁴
        ''' where n is in rev/s, D in metres, ρ in kg/m³, T in Newtons.
        ''' Source: APC propeller database or manufacturer test data.
        ''' Fallback when not available: 0.115 (population average for 2-blade MR props).
        ''' </summary>
        <JsonProperty("ctStatic")>
        Public Property CtStatic As Double

        ''' <summary>
        ''' Dimensionless static power coefficient.
        ''' Equation: P = Cp × ρ × n³ × D⁵
        ''' where n is in rev/s, D in metres, P in Watts.
        ''' Fallback when not available: 0.044.
        ''' </summary>
        <JsonProperty("cpStatic")>
        Public Property CpStatic As Double

        ''' <summary>
        ''' Copies diameter, pitch, blade count, and bore from the nested Dimensions
        ''' object to top-level properties after JSON deserialisation.
        ''' </summary>
        <OnDeserialized>
        Private Sub OnDeserialized(context As StreamingContext)
            If Dimensions IsNot Nothing Then
                DiameterInches = Dimensions.DiameterInches
                PitchInches = Dimensions.PitchInches
                BladeCount = If(Dimensions.BladesCount > 0, Dimensions.BladesCount, BladeCount)
                BoreDiameterMm = Dimensions.BoreMm
            End If
            If Efficiency = 0 AndAlso MassGrams > 0 Then
                Efficiency = StaticThrustGrams / MassGrams
            End If
            If CtStatic <= 0 Then CtStatic = 0.115
            If CpStatic <= 0 Then CpStatic = 0.044
        End Sub

        Public Sub New()
            Category = ComponentCategory.Propeller
        End Sub

    End Class

    ' =========================================================================
    ' Class: FlightControllerSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a UAV flight controller / autopilot board.
    ''' Interface counts (UART, PWM outputs) determine peripheral compatibility.
    ''' </summary>
    Public Class FlightControllerSpec
        Inherits ComponentSpecs

        ''' <summary>CPU / SoC identifier. JSON field: "processorType"</summary>
        <JsonProperty("processorType")>
        Public Property Processor As String = String.Empty

        ''' <summary>Primary IMU / gyroscope chip. JSON field: "imuPrimary"</summary>
        <JsonProperty("imuPrimary")>
        Public Property GyroscopeChip As String = String.Empty

        ''' <summary>Primary accelerometer chip.</summary>
        Public Property AccelerometerChip As String = String.Empty

        ''' <summary>True if an onboard barometer is present.</summary>
        Public Property HasBarometer As Boolean = True

        ''' <summary>Barometer chip identifier. JSON field: "barometerModel"</summary>
        <JsonProperty("barometerModel")>
        Public Property BarometerChip As String = String.Empty

        ''' <summary>True if an onboard magnetometer is present. JSON field: "hasOnboardMagnetometer"</summary>
        <JsonProperty("hasOnboardMagnetometer")>
        Public Property HasMagnetometer As Boolean

        ''' <summary>Number of full hardware UART serial ports.</summary>
        Public Property UARTCount As Integer

        ''' <summary>Number of hardware I²C buses. JSON field: "i2cCount"</summary>
        <JsonProperty("i2cCount")>
        Public Property I2CBusCount As Integer

        ''' <summary>Number of hardware SPI buses. JSON field: "spiCount"</summary>
        <JsonProperty("spiCount")>
        Public Property SPIBusCount As Integer

        ''' <summary>Number of PWM or DSHOT motor/servo output channels.</summary>
        Public Property PWMOutputCount As Integer

        ''' <summary>Number of ADC input channels. JSON field: "analogInputs"</summary>
        <JsonProperty("analogInputs")>
        Public Property AnalogInputCount As Integer

        ''' <summary>Maximum gyro loop rate in Hz. Derived from processor type in OnDeserialized.</summary>
        Public Property MaxLoopRateHz As Integer

        ''' <summary>True if a microSD card slot is present for blackbox logging.</summary>
        Public Property HasSDCardSlot As Boolean = True

        ''' <summary>True if a USB port is present.</summary>
        Public Property HasUSB As Boolean = True

        ''' <summary>True if the board includes a built-in OSD chip.</summary>
        Public Property HasOSD As Boolean

        ''' <summary>True if a built-in VTx is present.</summary>
        Public Property HasBuiltInVTx As Boolean

        ''' <summary>Firmware ecosystem(s) the FC supports.</summary>
        Public Property SupportedFirmware As FlightControllerFirmware

        ''' <summary>
        ''' Firmware compatibility as a comma-separated string.
        ''' JSON field: "firmwareCompatibility" (stored as array in JSON).
        ''' </summary>
        <JsonProperty("firmwareCompatibility")>
        <JsonConverter(GetType(StringArrayConverter))>
        Public Property FirmwareCompatibility As String = String.Empty

        ''' <summary>Form factor / mounting pattern string. JSON field: "formFactor"</summary>
        <JsonProperty("formFactor")>
        Public Property MountingPatternMm As String = String.Empty

        ''' <summary>Input supply voltage minimum in Volts (V). JSON field: "inputVoltageMinV"</summary>
        <JsonProperty("inputVoltageMinV")>
        Public Property InputVoltageMin As Double = 4.5

        ''' <summary>Input supply voltage maximum in Volts (V). JSON field: "inputVoltageMaxV"</summary>
        <JsonProperty("inputVoltageMaxV")>
        Public Property InputVoltageMax As Double = 5.5

        ''' <summary>True if blackbox logging is available (SD slot present).</summary>
        Public ReadOnly Property HasBlackbox As Boolean
            Get
                Return HasSDCardSlot
            End Get
        End Property

        ''' <summary>
        ''' Derives MaxLoopRateHz from processor type after JSON deserialisation.
        ''' H7 = 8 kHz, F7 = 4 kHz, F4 = 2 kHz, anything else = 1 kHz.
        ''' </summary>
        <OnDeserialized>
        Private Sub OnDeserialized(context As StreamingContext)
            If MaxLoopRateHz = 0 Then
                Dim proc As String = If(Processor, "").ToUpperInvariant()
                If proc.Contains("H7") Then
                    MaxLoopRateHz = 8000
                ElseIf proc.Contains("F7") Then
                    MaxLoopRateHz = 4000
                ElseIf proc.Contains("F4") Then
                    MaxLoopRateHz = 2000
                Else
                    MaxLoopRateHz = 1000
                End If
            End If
        End Sub

        Public Sub New()
            Category = ComponentCategory.FlightController
        End Sub

    End Class

    ' =========================================================================
    ' Class: BatterySpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a rechargeable battery pack.
    ''' The capacity (mAh), cell count (S), and C-rating are the three values
    ''' most critical to flight-time estimation.
    ''' </summary>
    Public Class BatterySpec
        Inherits ComponentSpecs

        ''' <summary>Energy capacity in milliamp-hours (mAh).</summary>
        Public Property CapacityMAh As Integer

        ''' <summary>Number of cells wired in series.</summary>
        Public Property CellCount As Integer

        ''' <summary>
        ''' Nominal pack voltage in Volts (V).
        ''' JSON field: "nominalVoltageV"
        ''' </summary>
        <JsonProperty("nominalVoltageV")>
        Public Property NominalCellVoltageV As Double = 3.7

        ''' <summary>
        ''' Fully-charged cell voltage in Volts (V).
        ''' JSON field: "maxChargeVoltageV"
        ''' </summary>
        <JsonProperty("maxChargeVoltageV")>
        Public Property FullChargeVoltagePerCellV As Double = 4.2

        ''' <summary>Safe minimum discharge cell voltage in Volts (V).</summary>
        Public Property MinCellVoltageV As Double = 3.5

        ''' <summary>
        ''' Continuous discharge rate in C.
        ''' JSON field: "dischargeRatingC"
        ''' </summary>
        <JsonProperty("dischargeRatingC")>
        Public Property ContinuousCRating As Double

        ''' <summary>
        ''' Peak burst discharge rate in C.
        ''' JSON field: "burstRatingC"
        ''' </summary>
        <JsonProperty("burstRatingC")>
        Public Property BurstCRating As Double

        ''' <summary>Cell electrochemistry type.</summary>
        Public Property Chemistry As BatteryCellChemistry = BatteryCellChemistry.LiPo

        ''' <summary>Physical form factor.</summary>
        Public Property FormFactor As String = String.Empty

        ''' <summary>
        ''' Discharge connector type fitted to this pack.
        ''' JSON field: "mainConnectorType"
        ''' </summary>
        <JsonProperty("mainConnectorType")>
        Public Property DischargeConnector As String = "XT60"

        ''' <summary>
        ''' Balance connector standard.
        ''' JSON field: "balanceConnectorType"
        ''' </summary>
        <JsonProperty("balanceConnectorType")>
        Public Property BalanceConnector As String = "JST-XH"

        ''' <summary>
        ''' Engine-facing alias: cell count.
        ''' The selection engine queries LipoCellCount; maps to CellCount.
        ''' </summary>
        Public ReadOnly Property LipoCellCount As Integer
            Get
                Return CellCount
            End Get
        End Property

        ''' <summary>
        ''' Calculated nominal pack voltage in Volts (V).
        ''' Read-only derived: CellCount × NominalCellVoltageV.
        ''' </summary>
        Public ReadOnly Property NominalPackVoltageV As Double
            Get
                Return CellCount * NominalCellVoltageV
            End Get
        End Property

        ''' <summary>
        ''' Calculated maximum continuous current in Amperes (A).
        ''' Read-only derived: (CapacityMAh / 1000) × ContinuousCRating.
        ''' </summary>
        Public ReadOnly Property MaxContinuousCurrentAmps As Double
            Get
                Return (CapacityMAh / 1000.0) * ContinuousCRating
            End Get
        End Property

        Public Sub New()
            Category = ComponentCategory.Battery
        End Sub

    End Class

    ' =========================================================================
    ' Class: GPSModuleSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a GNSS positioning module.
    ''' Compass presence and update rate are important for autonomous flight.
    ''' </summary>
    Public Class GPSModuleSpec
        Inherits ComponentSpecs

        ''' <summary>
        ''' Bitfield of GNSS constellations the module can receive simultaneously.
        ''' JSON field: "constellations" (stored as string array).
        ''' </summary>
        <JsonProperty("constellations")>
        <JsonConverter(GetType(GNSSConstellationArrayConverter))>
        Public Property SupportedConstellations As GNSSConstellation =
            GNSSConstellation.GPS Or GNSSConstellation.GLONASS

        ''' <summary>
        ''' Maximum position update rate in Hz.
        ''' JSON field: "updateRateHz"
        ''' </summary>
        <JsonProperty("updateRateHz")>
        Public Property MaxUpdateRateHz As Integer

        ''' <summary>
        ''' Typical horizontal CEP50 accuracy in metres (m).
        ''' JSON field: "positionAccuracyM"
        ''' </summary>
        <JsonProperty("positionAccuracyM")>
        Public Property HorizontalAccuracyMeters As Double

        ''' <summary>True if an onboard compass is integrated.</summary>
        Public Property HasCompass As Boolean = True

        ''' <summary>Compass chip identifier. JSON field: "compassModel"</summary>
        <JsonProperty("compassModel")>
        Public Property CompassChip As String = String.Empty

        ''' <summary>Number of satellite channels the receiver can track.</summary>
        Public Property TrackingChannels As Integer

        ''' <summary>Default UART baud rate.</summary>
        Public Property DefaultBaudRate As Integer = 38400

        ''' <summary>GNSS chipset inside the module. JSON field: "chipset"</summary>
        <JsonProperty("chipset")>
        Public Property GNSSChipset As String = String.Empty

        ''' <summary>Diameter of the circular module PCB in mm.</summary>
        Public Property PCBDiameterMm As Double

        ''' <summary>
        ''' Interface types as a comma-separated string.
        ''' JSON field: "interfaceType" (stored as string array).
        ''' </summary>
        <JsonProperty("interfaceType")>
        <JsonConverter(GetType(StringArrayConverter))>
        Public Property InterfaceTypes As String = String.Empty

        ''' <summary>
        ''' Current draw in milliamps (mA) at operating voltage.
        ''' JSON field: "currentDrawMA"
        ''' </summary>
        <JsonProperty("currentDrawMA")>
        Public Property CurrentDrawMA As Double

        ''' <summary>Current draw in Amperes (A). Derived from CurrentDrawMA.</summary>
        Public ReadOnly Property CurrentDrawA As Double
            Get
                Return CurrentDrawMA / 1000.0
            End Get
        End Property

        Public Sub New()
            Category = ComponentCategory.GPSModule
        End Sub

    End Class

    ' =========================================================================
    ' Class: TelemetryRadioSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for an air-to-ground data-link radio.
    ''' </summary>
    Public Class TelemetryRadioSpec
        Inherits ComponentSpecs

        ''' <summary>Radio carrier frequency in MHz.</summary>
        Public Property FrequencyMHz As Double

        ''' <summary>
        ''' Transmitter output power in milliwatts (mW).
        ''' JSON field: "maxTxPowerMW"
        ''' </summary>
        <JsonProperty("maxTxPowerMW")>
        Public Property OutputPowerMW As Double

        ''' <summary>Equivalent output power in dBm. Read-only derived.</summary>
        Public ReadOnly Property OutputPowerDBm As Double
            Get
                If OutputPowerMW <= 0 Then Return Double.NegativeInfinity
                Return 10.0 * Math.Log10(OutputPowerMW)
            End Get
        End Property

        ''' <summary>
        ''' Quoted maximum range in kilometres (km).
        ''' JSON field: "typicalRangeKm"
        ''' </summary>
        <JsonProperty("maxRangeKm")>
        Public Property MaxRangeKm As Double

        ''' <summary>Air-link data rate in bits per second (bps).</summary>
        Public Property AirDataRateBps As Integer

        ''' <summary>
        ''' Telemetry protocol natively supported.
        ''' JSON field: "protocol"
        ''' </summary>
        <JsonProperty("protocol")>
        Public Property TelemetryProtocol As String = "MAVLink 2"

        ''' <summary>Physical interface to the flight controller.</summary>
        Public Property FCInterface As String = "UART"

        ''' <summary>
        ''' True if this radio supports frequency-hopping spread spectrum (FHSS).
        ''' JSON field: "frequencyHopping"
        ''' </summary>
        <JsonProperty("frequencyHopping")>
        Public Property SupportsFHSS As Boolean

        ''' <summary>True if this module can relay RC commands to the FC.</summary>
        Public Property SupportsRCPassthrough As Boolean

        ''' <summary>
        ''' Active current draw in milliamps (mA).
        ''' JSON field: "currentDrawActiveMA"
        ''' </summary>
        <JsonProperty("currentDrawActiveMA")>
        Public Property CurrentDrawActiveMA As Double

        ''' <summary>Current draw in Amperes (A). Derived from CurrentDrawActiveMA.</summary>
        Public ReadOnly Property CurrentDrawA As Double
            Get
                Return CurrentDrawActiveMA / 1000.0
            End Get
        End Property

        Public Sub New()
            Category = ComponentCategory.TelemetryRadio
        End Sub

    End Class

    ' =========================================================================
    ' Class: CameraSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for an imaging sensor mounted on the UAV.
    ''' </summary>
    Public Class CameraSpec
        Inherits ComponentSpecs

        ''' <summary>
        ''' Sensor type description (e.g. "RGB", "Thermal LWIR", "Action").
        ''' JSON field: "cameraType"
        ''' </summary>
        <JsonProperty("cameraType")>
        Public Property SensorType As String = "RGB"

        ''' <summary>Horizontal image resolution in pixels.</summary>
        Public Property ResolutionHorizontalPx As Integer

        ''' <summary>Vertical image resolution in pixels.</summary>
        Public Property ResolutionVerticalPx As Integer

        ''' <summary>
        ''' Sensor resolution in megapixels.
        ''' JSON field: "resolutionMpx"
        ''' </summary>
        <JsonProperty("resolutionMpx")>
        Public Property ResolutionMpx As Double?

        ''' <summary>
        ''' Field of view in degrees (°).
        ''' JSON field: "fovDegrees"
        ''' </summary>
        <JsonProperty("fovDegrees")>
        Public Property DiagonalFOVDegrees As Double?

        ''' <summary>Horizontal field of view in degrees (°). Zero if not specified.</summary>
        Public Property HorizontalFOVDegrees As Double

        ''' <summary>Maximum video frame rate at full resolution in fps.</summary>
        Public Property MaxFrameRateFPS As Integer

        ''' <summary>Primary data output interface.</summary>
        Public Property OutputInterface As CameraOutputInterface = CameraOutputInterface.USB

        ''' <summary>
        ''' Output interfaces as a comma-separated string.
        ''' JSON field: "outputInterfaces" (stored as string array).
        ''' </summary>
        <JsonProperty("outputInterfaces")>
        <JsonConverter(GetType(StringArrayConverter))>
        Public Property OutputInterfaces As String = String.Empty

        ''' <summary>Focal length in millimetres (mm).</summary>
        Public Property FocalLengthMm As Double

        ''' <summary>
        ''' True if the camera has an onboard electronic or mechanical image stabiliser.
        ''' JSON field: "hasElectronicImageStabilization"
        ''' </summary>
        <JsonProperty("hasElectronicImageStabilization")>
        Public Property HasStabilisation As Boolean

        ''' <summary>True if this is a low-latency FPV camera (&lt;30 ms).</summary>
        Public Property IsLowLatency As Boolean

        ''' <summary>True if this camera is designed for gimbal mounting.</summary>
        Public Property IsGimbalMounted As Boolean

        ''' <summary>
        ''' Average power consumption during active recording in Watts (W).
        ''' JSON field: "powerConsumptionW"
        ''' </summary>
        <JsonProperty("powerConsumptionW")>
        Public Property PowerConsumptionWatts As Double

        ''' <summary>Operating supply voltage in Volts (V).</summary>
        Public Property OperatingVoltageV As Double

        ''' <summary>
        ''' True if this is a thermal/infrared sensor.
        ''' Derived from SensorType containing "Thermal" or "IR".
        ''' </summary>
        Public Property IsThermographic As Boolean

        ''' <summary>True if camera is weatherproof. JSON field: "isWeatherproof"</summary>
        <JsonProperty("isWeatherproof")>
        Public Property IsWeatherproof As Boolean

        ''' <summary>Current draw in Amperes (A). Derived from PowerConsumptionWatts / 5V.</summary>
        Public ReadOnly Property CurrentDrawA As Double
            Get
                Return If(PowerConsumptionWatts > 0, PowerConsumptionWatts / 5.0, 0.0)
            End Get
        End Property

        ''' <summary>
        ''' Derives IsThermographic and IsLowLatency from SensorType after deserialisation.
        ''' </summary>
        <OnDeserialized>
        Private Sub OnDeserialized(context As StreamingContext)
            Dim t As String = If(SensorType, "").ToUpperInvariant()
            IsThermographic = t.Contains("THERMAL") OrElse t.Contains("IR") OrElse t.Contains("LWIR")
            IsLowLatency = t.Contains("FPV") OrElse t.Contains("ANALOG") OrElse t.Contains("ANALOGUE")
        End Sub

        Public Sub New()
            Category = ComponentCategory.Camera
        End Sub

    End Class

    ' =========================================================================
    ' Class: ServoSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for an RC servo actuator.
    ''' </summary>
    Public Class ServoSpec
        Inherits ComponentSpecs

        ''' <summary>Stall torque at rated voltage in kilogram-centimetres (kg·cm).</summary>
        Public Property StallTorqueKgCm As Double

        ''' <summary>Stall torque in Newton-metres (N·m). Read-only derived.</summary>
        Public ReadOnly Property StallTorqueNm As Double
            Get
                Return StallTorqueKgCm * 0.0981
            End Get
        End Property

        ''' <summary>Transit speed over 60 degrees in seconds per 60°.</summary>
        Public Property SpeedSecPer60Deg As Double

        ''' <summary>Rated operating voltage in Volts (V).</summary>
        Public Property RatedVoltageV As Double

        ''' <summary>No-load current draw in mA at rated voltage.</summary>
        Public Property NoLoadCurrentMA As Double

        ''' <summary>Stall current draw in mA at rated voltage.</summary>
        Public Property StallCurrentMA As Double

        ''' <summary>Total mechanical travel in degrees (°).</summary>
        Public Property TotalTravelDeg As Double = 180.0

        ''' <summary>Control signal protocol this servo accepts.</summary>
        Public Property SignalType As ServoSignalType = ServoSignalType.AnalogPWM

        ''' <summary>Gear train material.</summary>
        Public Property GearMaterial As String = String.Empty

        ''' <summary>True if the servo output shaft has ball bearings.</summary>
        Public Property HasBallBearings As Boolean

        ''' <summary>Servo case body width in mm.</summary>
        Public Property BodyWidthMm As Double

        ''' <summary>Servo case body length in mm.</summary>
        Public Property BodyLengthMm As Double

        ''' <summary>Servo case body height in mm.</summary>
        Public Property BodyHeightMm As Double

        ''' <summary>Mounting lug centre-to-centre distance in mm.</summary>
        Public Property MountingLugSpacingMm As Double

        Public Sub New()
            Category = ComponentCategory.Servo
        End Sub

    End Class

    ' =========================================================================
    ' Class: ReceiverSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for an RC link receiver.
    ''' </summary>
    Public Class ReceiverSpec
        Inherits ComponentSpecs

        ''' <summary>RC protocol / manufacturer ecosystem.</summary>
        Public Property Protocol As RCLinkProtocol

        ''' <summary>Protocol name as a string (e.g. "ELRS", "CRSF", "SBUS").</summary>
        Public Property ProtocolName As String = String.Empty

        ''' <summary>Carrier frequency in MHz.</summary>
        Public Property FrequencyMHz As Double

        ''' <summary>Number of independent RC channels output.</summary>
        Public Property ChannelCount As Integer

        ''' <summary>Output signal format to the flight controller.</summary>
        Public Property OutputFormat As String = "SBUS"

        ''' <summary>True if the receiver provides a downlink RSSI output.</summary>
        Public Property HasRSSIOutput As Boolean = True

        ''' <summary>True if the receiver supports bidirectional telemetry.</summary>
        Public Property HasTelemetry As Boolean

        ''' <summary>Quoted maximum control range in kilometres (km).</summary>
        Public Property MaxRangeKm As Double

        ''' <summary>Number of onboard antenna ports.</summary>
        Public Property AntennaCount As Integer = 1

        ''' <summary>Operating supply voltage in Volts (V).</summary>
        Public Property OperatingVoltageV As Double = 5.0

        ''' <summary>Current draw in mA at operating voltage.</summary>
        Public Property CurrentDrawMA As Double

        Public Sub New()
            Category = ComponentCategory.Receiver
        End Sub

    End Class

    ' =========================================================================
    ' Class: PowerDistributionBoardSpec
    ' =========================================================================

    ''' <summary>
    ''' Specifications for a Power Distribution Board (PDB) or Power Module.
    ''' </summary>
    Public Class PowerDistributionBoardSpec
        Inherits ComponentSpecs

        ''' <summary>Maximum continuous through-current in Amperes (A).</summary>
        Public Property MaxContinuousCurrentAmps As Double

        ''' <summary>Peak burst current in Amperes (A).</summary>
        Public Property BurstCurrentAmps As Double

        ''' <summary>Maximum input voltage in Volts (V).</summary>
        Public Property MaxInputVoltageV As Double

        ''' <summary>Number of ESC solder pad sets.</summary>
        Public Property ESCPadCount As Integer

        ''' <summary>True if the board includes an onboard current sensor.</summary>
        Public Property HasCurrentSensor As Boolean

        ''' <summary>Current sensor full-scale range in Amperes (A).</summary>
        Public Property CurrentSensorMaxAmps As Double

        ''' <summary>True if the board has a regulated 5 V BEC output.</summary>
        Public Property Has5VBEC As Boolean

        ''' <summary>Maximum current of the 5 V BEC output in Amperes (A).</summary>
        Public Property BEC5VMaxAmps As Double

        ''' <summary>True if the board has a regulated 12 V output.</summary>
        Public Property Has12VBEC As Boolean

        ''' <summary>Maximum current of the 12 V output in Amperes (A).</summary>
        Public Property BEC12VMaxAmps As Double

        ''' <summary>Battery input connector type on the board.</summary>
        Public Property InputConnector As PowerConnectorType = PowerConnectorType.XT60

        ''' <summary>Mounting hole pattern string.</summary>
        Public Property MountingPatternMm As String = String.Empty

        ''' <summary>True if ESC pads are arranged for a stack layout.</summary>
        Public Property IsStackDesign As Boolean = True

        Public Sub New()
            Category = ComponentCategory.PowerDistributionBoard
        End Sub

    End Class

End Namespace
