Imports System.IO
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Utilities

''' <summary>
''' Read-only data access layer for the component database.
''' Loads components.json via ConfigManager, deserializes each entry into the
''' appropriate ComponentSpec subclass, and exposes typed query methods.
'''
''' This class never writes to the file. All mutation of component data (if ever
''' needed) must go through a separate write-capable service so that the
''' read/write boundary stays explicit.
'''
''' Usage:
'''   Dim repo As New ComponentRepository()
'''   Dim motors = repo.GetMotorsWithMinThrust(500)
''' </summary>
Public Class ComponentRepository

#Region "Private State"

    ''' <summary>All components loaded from JSON, keyed by their unique ID.</summary>
    Private ReadOnly _componentsById As Dictionary(Of String, ComponentSpecs)

    ''' <summary>All components grouped by category string (lower-invariant).</summary>
    Private ReadOnly _componentsByCategory As Dictionary(Of String, List(Of ComponentSpecs))

#End Region

#Region "Constructor"

    ''' <summary>
    ''' Loads and deserializes the component database from the path supplied by
    ''' ConfigManager. Throws <see cref="FileNotFoundException"/> if the file is
    ''' missing, or <see cref="JsonException"/> if the JSON is malformed.
    ''' </summary>
    Public Sub New()
        Dim jsonPath As String = ConfigManager.Settings.ResolvedComponentsDatabasePath

        ' ── TEMPORARY DEBUG ──────────────────────────────────────────
        'MessageBox.Show(
        '$"Looking for components.json at:{Environment.NewLine}{jsonPath}{Environment.NewLine}{Environment.NewLine}" &
        '$"File exists: {File.Exists(jsonPath)}",
        '"Path Debug")
        ' ─────────────────────────────────────────────────────────────

        If Not File.Exists(jsonPath) Then
            Throw New FileNotFoundException(
                $"Component database not found at '{jsonPath}'. " &
                "Check ConfigManager.ComponentDatabasePath.", jsonPath)
        End If

        Dim rawJson As String = File.ReadAllText(jsonPath)
        Dim allComponents As List(Of ComponentSpecs) = DeserializeComponents(rawJson)

        ' Build lookup dictionaries once at construction time so every query is O(1)/O(n).
        _componentsById = New Dictionary(Of String, ComponentSpecs)(StringComparer.OrdinalIgnoreCase)
        _componentsByCategory = New Dictionary(Of String, List(Of ComponentSpecs))(StringComparer.OrdinalIgnoreCase)

        For Each comp As ComponentSpecs In allComponents
            ' ID index — warn on duplicates rather than crashing, so a bad JSON entry
            ' doesn't kill the whole application.
            If _componentsById.ContainsKey(comp.Id) Then
                Debug.WriteLine($"[ComponentRepository] WARNING: Duplicate component ID '{comp.Id}'. " &
                                "Second entry ignored.")
                Continue For
            End If
            _componentsById(comp.Id) = comp

            ' Category index
            Dim cat As String = If(String.IsNullOrEmpty(comp.Category), "unknown", comp.Category)
            If Not _componentsByCategory.ContainsKey(cat) Then
                _componentsByCategory(cat) = New List(Of ComponentSpecs)()
            End If
            _componentsByCategory(cat).Add(comp)

            EmitGeometryWarningIfNeeded(comp)
        Next
    End Sub

#End Region

#Region "Generic Query Methods"

    ''' <summary>
    ''' Returns every component in the database as an untyped list.
    ''' Prefer the typed helpers below for normal use.
    ''' </summary>
    Public Function GetAll() As IReadOnlyList(Of ComponentSpecs)
        Return _componentsById.Values.ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns all components whose Category matches <paramref name="category"/>
    ''' (case-insensitive). Returns an empty list — never Nothing — when no match.
    ''' </summary>
    ''' <param name="category">
    ''' One of the canonical category strings: "motor", "esc", "propeller",
    ''' "flight_controller", "battery", "gps", "telemetry", "camera", "servo",
    ''' "receiver", "pdb", "airframe_material".
    ''' </param>
    Public Function GetAllByCategory(category As String) As IReadOnlyList(Of ComponentSpecs)
        If String.IsNullOrWhiteSpace(category) Then Return New List(Of ComponentSpecs)().AsReadOnly()

        Dim key As String = category.ToLowerInvariant()
        If _componentsByCategory.ContainsKey(key) Then
            Return _componentsByCategory(key).AsReadOnly()
        End If
        Return New List(Of ComponentSpecs)().AsReadOnly()
    End Function

    ''' <summary>
    ''' Looks up a single component by its unique string ID.
    ''' Returns Nothing if not found — callers must null-check.
    ''' </summary>
    Public Function GetById(id As String) As ComponentSpecs
        If String.IsNullOrWhiteSpace(id) Then Return Nothing
        Dim result As ComponentSpecs = Nothing
        _componentsById.TryGetValue(id, result)
        Return result
    End Function

#End Region

#Region "Typed Category Accessors"

    ''' <summary>Returns all motor components as strongly-typed <see cref="MotorSpec"/> objects.</summary>
    Public Function GetMotors() As IReadOnlyList(Of MotorSpec)
        Return GetAllByCategory("motor").OfType(Of MotorSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all ESC components.</summary>
    Public Function GetESCs() As IReadOnlyList(Of ESCSpec)
        Return GetAllByCategory("esc").OfType(Of ESCSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all propeller components.</summary>
    Public Function GetPropellers() As IReadOnlyList(Of PropellerSpec)
        Return GetAllByCategory("propeller").OfType(Of PropellerSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all flight controller components.</summary>
    Public Function GetFlightControllers() As IReadOnlyList(Of FlightControllerSpec)
        Return GetAllByCategory("flight_controller").OfType(Of FlightControllerSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all battery components.</summary>
    Public Function GetBatteries() As IReadOnlyList(Of BatterySpec)
        Return GetAllByCategory("battery").OfType(Of BatterySpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all GPS module components.</summary>
    Public Function GetGPSModules() As IReadOnlyList(Of GPSModuleSpec)
        Return GetAllByCategory("gps").OfType(Of GPSModuleSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all telemetry radio components.</summary>
    Public Function GetTelemetryRadios() As IReadOnlyList(Of TelemetryRadioSpec)
        Return GetAllByCategory("telemetry").OfType(Of TelemetryRadioSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all camera/sensor components.</summary>
    Public Function GetCameras() As IReadOnlyList(Of CameraSpec)
        Return GetAllByCategory("camera").OfType(Of CameraSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all servo components.</summary>
    Public Function GetServos() As IReadOnlyList(Of ServoSpec)
        Return GetAllByCategory("servo").OfType(Of ServoSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all RC receiver components.</summary>
    Public Function GetReceivers() As IReadOnlyList(Of ReceiverSpec)
        Return GetAllByCategory("receiver").OfType(Of ReceiverSpec)().ToList().AsReadOnly()
    End Function

    ''' <summary>Returns all power distribution board components.</summary>
    Public Function GetPowerDistributionBoards() As IReadOnlyList(Of PowerDistributionBoardSpec)
        Return GetAllByCategory("pdb").OfType(Of PowerDistributionBoardSpec)().ToList().AsReadOnly()
    End Function

#End Region

#Region "Filtered Query Methods"

    ''' <summary>
    ''' Returns motors whose maximum single-motor thrust is at least
    ''' <paramref name="minThrustGrams"/> grams.
    ''' </summary>
    ''' <param name="minThrustGrams">Minimum acceptable thrust in grams.</param>
    Public Function GetMotorsWithMinThrust(minThrustGrams As Double) As IReadOnlyList(Of MotorSpec)
        Return GetMotors().Where(Function(m) m.MaxThrustGrams >= minThrustGrams).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns motors within a KV range (RPM per volt), inclusive on both ends.
    ''' </summary>
    Public Function GetMotorsByKVRange(minKV As Integer, maxKV As Integer) As IReadOnlyList(Of MotorSpec)
        Return GetMotors().Where(Function(m) m.KV >= minKV AndAlso m.KV <= maxKV).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns batteries with at least <paramref name="minCapacityMah"/> mAh capacity.
    ''' </summary>
    Public Function GetBatteriesWithMinCapacity(minCapacityMah As Integer) As IReadOnlyList(Of BatterySpec)
        Return GetBatteries().Where(Function(b) b.CapacityMAh >= minCapacityMah).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns batteries matching a specific cell count (e.g. 3 for 3S, 4 for 4S).
    ''' </summary>
    Public Function GetBatteriesByCellCount(cellCount As Integer) As IReadOnlyList(Of BatterySpec)
        Return GetBatteries().Where(Function(b) b.CellCount = cellCount).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns ESCs whose continuous current rating meets or exceeds
    ''' <paramref name="minCurrentAmps"/> amps.
    ''' </summary>
    Public Function GetESCsWithMinCurrent(minCurrentAmps As Integer) As IReadOnlyList(Of ESCSpec)
        Return GetESCs().Where(Function(e) e.ContinuousCurrentAmps >= minCurrentAmps).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns all components whose mass does not exceed <paramref name="maxMassGrams"/>.
    ''' Useful for weight-budget filtering across categories.
    ''' </summary>
    Public Function GetComponentsUnderMass(maxMassGrams As Double) As IReadOnlyList(Of ComponentSpecs)
        Return GetAll().Where(Function(c) c.MassGrams <= maxMassGrams).ToList().AsReadOnly()
    End Function

    ''' <summary>
    ''' Returns all components whose operating temperature range covers the given
    ''' environment temperature in degrees Celsius.
    ''' Components that have not specified temperature limits are included by default
    ''' (assumed to handle standard ranges).
    ''' </summary>
    ''' <param name="temperatureCelsius">Ambient operating temperature to check against.</param>
    Public Function GetComponentsForTemperature(temperatureCelsius As Double) As IReadOnlyList(Of ComponentSpecs)
        Return GetAll().Where(Function(c)
                                  ' If limits not set (default Double.MinValue/MaxValue sentinel), include.
                                  If c.MinOperatingTempC = Double.MinValue AndAlso c.MaxOperatingTempC = Double.MaxValue Then
                                      Return True
                                  End If
                                  Return temperatureCelsius >= c.MinOperatingTempC AndAlso
                                         temperatureCelsius <= c.MaxOperatingTempC
                              End Function).ToList().AsReadOnly()
    End Function

#End Region

#Region "Validation Warnings"

    ''' <summary>
    ''' Emits non-fatal diagnostics for suspicious geometry values that can
    ''' trigger false compatibility failures in propulsion selection.
    ''' </summary>
    Private Shared Sub EmitGeometryWarningIfNeeded(comp As ComponentSpecs)
        Dim motor = TryCast(comp, MotorSpec)
        If motor IsNot Nothing Then
            If motor.ShaftDiameterMm <= 0 Then
                Debug.WriteLine($"[ComponentRepository] WARNING: Motor '{motor.Id}' has non-positive shaft diameter ({motor.ShaftDiameterMm}).")
            End If
            Return
        End If

        Dim prop = TryCast(comp, PropellerSpec)
        If prop IsNot Nothing Then
            If prop.BoreDiameterMm <= 0 Then
                Debug.WriteLine($"[ComponentRepository] WARNING: Propeller '{prop.Id}' has non-positive bore diameter ({prop.BoreDiameterMm}).")
            End If
        End If
    End Sub

#End Region

#Region "Deserialization (Private)"

    ''' <summary>
    ''' Deserializes the JSON array into a heterogeneous list of ComponentSpec
    ''' subclasses. Dispatches on the "category" field of each JSON object so
    ''' each entry lands in the right concrete type.
    ''' </summary>
    Private Function DeserializeComponents(json As String) As List(Of ComponentSpecs)
        Dim result As New List(Of ComponentSpecs)()
        ' JSON root is an object with category-named arrays (motors, escs, etc.)
        ' plus a _meta key. Iterate all keys and collect every array's items.
        Dim root As JObject = JObject.Parse(json)

        Dim allTokens As New List(Of JToken)()
        For Each kvp As KeyValuePair(Of String, JToken) In root
            If kvp.Key = "_meta" Then Continue For          ' skip metadata
            If kvp.Value.Type = JTokenType.Array Then
                allTokens.AddRange(kvp.Value.Children())
            End If
        Next

        For Each token As JToken In allTokens
            Dim obj As JObject = TryCast(token, JObject)
            If obj Is Nothing Then Continue For

            Dim categoryValue As String = obj.Value(Of String)("category")
            Dim category As String = If(categoryValue IsNot Nothing, categoryValue.ToLowerInvariant(), "unknown")

            Dim comp As ComponentSpecs = Nothing
            Select Case category
                Case "motor"
                    comp = obj.ToObject(Of MotorSpec)()
                Case "esc"
                    comp = obj.ToObject(Of ESCSpec)()
                Case "propeller"
                    comp = obj.ToObject(Of PropellerSpec)()
                Case "flightcontroller"
                    comp = obj.ToObject(Of FlightControllerSpec)()
                Case "battery"
                    comp = obj.ToObject(Of BatterySpec)()
                Case "gpsmodule"
                    comp = obj.ToObject(Of GPSModuleSpec)()
                Case "telemetryradio", "telemetry", "TelemetryRadio"
                    comp = obj.ToObject(Of TelemetryRadioSpec)()
                Case "camera"
                    comp = obj.ToObject(Of CameraSpec)()
                Case "servo"
                    comp = obj.ToObject(Of ServoSpec)()
                Case "receiver"
                    comp = obj.ToObject(Of ReceiverSpec)()
                Case "pdb", "powerdistributionboard"
                    comp = obj.ToObject(Of PowerDistributionBoardSpec)()
                Case Else
                    Debug.WriteLine($"[DEBUG] Category string bytes: '{category}' len={category.Length}")
                    Continue For
            End Select

            If comp IsNot Nothing Then
                result.Add(comp)
            End If
        Next

        Return result
    End Function

#End Region

End Class



''' <summary>
''' Reads a JSON string array of ESC protocol names and combines them into
''' a single ESCProtocol Flags enum value via bitwise OR.
''' Handles case-insensitive name matching with common abbreviation variants.
''' </summary>
Public Class ESCProtocolArrayConverter
    Inherits JsonConverter(Of Core.Models.ESCProtocol)

    Private Shared ReadOnly _nameMap As New Dictionary(Of String, Core.Models.ESCProtocol)(
        StringComparer.OrdinalIgnoreCase) From {
        {"PWM", Core.Models.ESCProtocol.PWM},
        {"OneShot125", Core.Models.ESCProtocol.OneShot125},
        {"OneShot42", Core.Models.ESCProtocol.OneShot42},
        {"MultiShot", Core.Models.ESCProtocol.MultiShot},
        {"DSHOT150", Core.Models.ESCProtocol.DShot150},
        {"DSHOT300", Core.Models.ESCProtocol.DShot300},
        {"DSHOT600", Core.Models.ESCProtocol.DShot600},
        {"DSHOT1200", Core.Models.ESCProtocol.DShot1200},
        {"ProShot", Core.Models.ESCProtocol.ProShot}
    }

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type,
                                       existingValue As Core.Models.ESCProtocol,
                                       hasExistingValue As Boolean,
                                       serializer As JsonSerializer) As Core.Models.ESCProtocol
        Dim result As Core.Models.ESCProtocol = Core.Models.ESCProtocol.None
        Dim arr As JArray = JArray.Load(reader)
        For Each token As JToken In arr
            Dim name As String = token.Value(Of String)()
            Dim proto As Core.Models.ESCProtocol
            If name IsNot Nothing AndAlso _nameMap.TryGetValue(name, proto) Then
                result = result Or proto
            Else
                Debug.WriteLine($"[ESCProtocolArrayConverter] Unknown protocol name: '{name}'")
            End If
        Next
        Return result
    End Function

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Core.Models.ESCProtocol,
                                    serializer As JsonSerializer)
        writer.WriteValue(value.ToString())
    End Sub
End Class

Public Class GNSSConstellationArrayConverter
    Inherits JsonConverter(Of Core.Models.GNSSConstellation)

    Private Shared ReadOnly _nameMap As New Dictionary(Of String, Core.Models.GNSSConstellation)(
        StringComparer.OrdinalIgnoreCase) From {
        {"GPS", Core.Models.GNSSConstellation.GPS},
        {"GLONASS", Core.Models.GNSSConstellation.GLONASS},
        {"BeiDou", Core.Models.GNSSConstellation.BeiDou},
        {"Galileo", Core.Models.GNSSConstellation.Galileo},
        {"QZSS", Core.Models.GNSSConstellation.QZSS},
        {"SBAS", Core.Models.GNSSConstellation.SBAS}
    }

    Public Overrides Function ReadJson(reader As JsonReader, objectType As Type,
                                       existingValue As Core.Models.GNSSConstellation,
                                       hasExistingValue As Boolean,
                                       serializer As JsonSerializer) As Core.Models.GNSSConstellation
        Dim result As Core.Models.GNSSConstellation = Core.Models.GNSSConstellation.None
        Dim arr As JArray = JArray.Load(reader)
        For Each token As JToken In arr
            Dim name As String = token.Value(Of String)()
            Dim constellation As Core.Models.GNSSConstellation
            If name IsNot Nothing AndAlso _nameMap.TryGetValue(name, constellation) Then
                result = result Or constellation
            Else
                Debug.WriteLine($"[GNSSConstellationArrayConverter] Unknown constellation: '{name}'")
            End If
        Next
        Return result
    End Function

    Public Overrides Sub WriteJson(writer As JsonWriter, value As Core.Models.GNSSConstellation,
                                    serializer As JsonSerializer)
        writer.WriteValue(value.ToString())
    End Sub
End Class
