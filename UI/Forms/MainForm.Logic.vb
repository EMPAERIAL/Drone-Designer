' =============================================================================
' File:    UI/Forms/MainForm.Logic.vb
' Project: Drone Designer
' Task:    11 — Wire UI to Engine
'
' Purpose: Partial class companion to MainForm.vb (Task 10 layout).
'          The layout file is NOT modified by this task.
'
'          This file provides:
'            • OnLoad override      — initialises ComponentSelectionEngine
'            • OnSelectComponentsAsync — async button handler (keeps UI live)
'            • BuildMissionSpecs    — maps every form control → MissionSpecs
'            • ValidateFormInputs   — returns user-facing error strings
'            • DisplaySelectionResult / AppendRows — binds result to DGV
'            • SetProcessingState   — disables controls during engine run
'            • Enum-mapping helpers — combo index → domain enum value
'
' Unit conversions applied in BuildMissionSpecs (NOT in the engine):
'   Endurance : UI hours   → MissionSpecs minutes  (× 60)
'   Speeds    : UI km/h    → MissionSpecs m/s       (÷ 3.6)
'
' Inter-task notes for future chats:
'   • MissionSpecs.vb was updated (Task 11) to rename OperatingEnvironment
'     enum → OperatingEnvCategory and MissionProfile enum →
'     MissionProfileCategory to prevent property/type name shadowing.
'     The new engine-facing properties RangeKm, MotorCount,
'     PayloadWeightGrams, MissionProfile (As MissionProfileType), and
'     OperatingEnvironment (As EnvironmentType) were also added there.
'   • ComponentDisplayRow.vb (new, Task 11) is the DGV DataSource type;
'     its property names match the column DataPropertyNames in MainForm.vb.
'
' Author:  [Solo Dev] — Task 11
' Target:  .NET Framework 4.7.2 / WinForms / VB.NET
' =============================================================================

Imports System.Linq
Imports System.Threading.Tasks
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Services
Imports Drone_Designer.SolidWorks



''' <summary>
''' Partial class extension of MainForm — business-logic wiring for Task 11.
''' All layout controls declared in MainForm.vb are accessible here because
''' VB.NET partial classes share every access level across files.
''' </summary>
Partial Class MainForm

    ' ── Dependencies ─────────────────────────────────────────────────────
    ''' <summary>Selection engine instance. Nothing if database failed to load.</summary>
    Private _engine As ComponentSelectionEngine

    ''' <summary>Most recent successful SelectionResult — available for export (Task 13).</summary>
    Friend _lastResult As SelectionResult

    ''' <summary>
    ''' SolidWorks automation wrapper. Instantiated eagerly so MainForm.CAD.vb can
    ''' call _cadGen.Connect() on demand (deferred — see that file).
    ''' Connect() is NOT called here; the COM handshake only happens when the user
    ''' clicks "Send to SolidWorks", because SW startup is slow and the user may
    ''' only need Module 1 (component selection).
    ''' </summary>
    Private _cadGen As SolidWorksAutomation

    ' =====================================================================
    '  ON LOAD — initialise engine, wire stubbed button handler
    ' =====================================================================

    ''' <summary>
    ''' Overrides OnLoad to initialise the engine and wire the "Select Components"
    ''' button that was left as a TODO in Task 10.
    ''' Runs after the base constructor so all controls are already created.
    ''' </summary>
    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        InitialiseEngine()
        AddHandler btnSelectComponents.Click, AddressOf OnSelectComponentsAsync
        WireCADControls()
    End Sub

    ''' <summary>
    ''' Instantiates <see cref="ComponentRepository"/> (reads components.json) and
    ''' <see cref="ComponentSelectionEngine"/>.  Shows a warning and leaves _engine
    ''' as Nothing if the database cannot be loaded so the form stays usable.
    ''' </summary>
    Private Sub InitialiseEngine()
        Try
            Dim repo As New ComponentRepository()   ' reads Resources/components.json
            _engine = New ComponentSelectionEngine(repo)
            _cadGen = New SolidWorksAutomation()
            UpdateStatus("Ready — enter mission parameters and click 'Select Components'.")
        Catch ex As Exception
            UpdateStatus("⚠  Component database failed to load — check Resources/components.json.")
            MessageBox.Show(
                    "The component database could not be loaded:" &
                    Environment.NewLine & ex.Message &
                    Environment.NewLine & Environment.NewLine &
                    "Ensure 'components.json' is present in the Resources folder. " &
                    "Component selection will not be available until this is fixed.",
                    "Database Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
        End Try
    End Sub

    ' =====================================================================
    '  SELECT COMPONENTS BUTTON — async handler
    ' =====================================================================

    ''' <summary>
    ''' Async handler wired to btnSelectComponents.Click.
    ''' Pipeline: Validate → Build specs → Engine on background thread → Display results.
    ''' Task.Run keeps the UI thread free during the CPU-bound selection pass;
    ''' all UI updates happen after Await, which resumes on the UI thread.
    ''' </summary>
    Private Async Sub OnSelectComponentsAsync(sender As Object, e As EventArgs)
        If _engine Is Nothing Then
            MessageBox.Show("Component database is not loaded. Cannot run selection.",
                        "Engine Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' ── Validate inputs ───────────────────────────────────────────────────
        Dim errors As List(Of String) = ValidateFormInputs()
        If errors.Count > 0 Then
            MessageBox.Show(String.Join(Environment.NewLine, errors),
                        "Input Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' ── Run engine on background thread ───────────────────────────────────
        SetProcessingState(True)
        UpdateStatus("⏳  Running component selection…")

        Try
            Dim specs As MissionSpecs = BuildMissionSpecs()
            Dim result As SelectionResult = Await Task.Run(Function() _engine.SelectComponents(specs))

            _lastResult = result
            UpdateCADButtonState()
            DisplaySelectionResult(result)
            UpdateStatus($"✔  Selection complete — {result.SelectedMotors.Count} motor(s) found. " &
                     $"Estimated MTOW: {result.EstimatedMtowGrams:N0} g.")
        Catch ex As ComponentSelectionException
            UpdateStatus("⚠  Selection failed — see message.")
            MessageBox.Show(ex.Message, "Component Selection Failed",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Catch ex As Exception
            UpdateStatus("⚠  Unexpected error during selection.")
            MessageBox.Show($"Unexpected error:{Environment.NewLine}{ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            SetProcessingState(False)
        End Try
    End Sub
    ' =====================================================================
    '  INPUT VALIDATION
    ' =====================================================================

    ''' <summary>
    ''' Validates all form controls before they reach the engine.
    ''' Returns a list of plain-English error strings; an empty list means pass.
    ''' Checks are ordered top-to-bottom matching the visual layout.
    ''' </summary>
    Private Function ValidateFormInputs() As List(Of String)
        Dim errs As New List(Of String)

        ' ── Flight parameters ─────────────────────────────────────────────
        If nudEndurance.Value <= 0D Then
            errs.Add("Flight Endurance must be greater than 0.")
        ElseIf nudEndurance.Value > 24D Then
            errs.Add("Flight Endurance cannot exceed 24 hours.")
        End If

        If nudRange.Value < 0.1D Then
            errs.Add("Range must be at least 0.1 km.")
        End If

        If nudCruiseSpeed.Value < 1D Then
            errs.Add("Cruise Speed must be at least 1 km/h.")
        End If

        If nudMaxAltitude.Value <= 0D Then
            errs.Add("Max Altitude must be greater than 0 m.")
        ElseIf nudMaxAltitude.Value > 10000D Then
            errs.Add("Max Altitude cannot exceed 10,000 m AGL.")
        End If

        If nudMaxTakeoffWeight.Value < 100D Then
            errs.Add("Max Takeoff Weight must be at least 100 g.")
        End If

        ' ── Temperature cross-check ───────────────────────────────────────
        If nudTempMin.Value >= nudTempMax.Value Then
            errs.Add("Min Temperature must be less than Max Temperature.")
        End If

        ' ── Payload vs MTOW sanity ────────────────────────────────────────
        If nudPayloadWeight.Value > nudMaxTakeoffWeight.Value * 0.6D Then
            errs.Add(
                    $"Payload ({nudPayloadWeight.Value:N0} g) exceeds 60 % of " &
                    $"Max Takeoff Weight ({nudMaxTakeoffWeight.Value:N0} g). " &
                    "Reduce payload or increase MTOW.")
        End If

        ' ── Multirotor motor count ────────────────────────────────────────
        If cboFrameType.SelectedIndex = 0 Then   ' "Multirotor" selected
            Dim count As Integer = ParseMotorCount(cboMotorCount.Text)
            If count < 3 Then
                errs.Add(
                        "A Multirotor frame requires at least 3 motors. " &
                        "Change Frame Type to Fixed Wing or select a higher motor count.")
            End If
        End If

        Return errs
    End Function

    ' =====================================================================
    '  BUILD MISSION SPECS — form controls → MissionSpecs
    ' =====================================================================

    ''' <summary>
    ''' Reads every form control and constructs a fully-populated
    ''' <see cref="MissionSpecs"/> object ready for the selection engine.
    '''
    ''' UNIT CONVERSIONS (applied here, not in the engine):
    '''   Endurance : hours   → minutes  (×60)
    '''   Speeds    : km/h    → m/s      (÷3.6)
    '''
    ''' DUAL PROPERTIES:
    '''   Some properties are set twice — once for the legacy MissionSpecs enum
    '''   (Profile, Environment) and once for the engine-facing alias property
    '''   (MissionProfile, OperatingEnvironment). Both must be set so that
    '''   both the UI and engine see consistent values.
    ''' </summary>
    Private Function BuildMissionSpecs() As MissionSpecs
        Dim rangeKm As Double = CDbl(nudRange.Value)
        Dim motorCount As Integer = ParseMotorCount(cboMotorCount.Text)
        Dim autoLevel As Integer = cboAutonomyLevel.SelectedIndex
        Dim envIdx As Integer = cboOperatingEnvironment.SelectedIndex

        Return New MissionSpecs With {
                .MissionName = $"{cboMissionProfile.Text} | {cboFrameType.Text} | {motorCount} motors",
                .Description = txtNotes.Text.Trim(),
                .FlightEnduranceMinutes = CDbl(nudEndurance.Value) * 60.0,
                .MaxRangeKm = rangeKm,
                .CruiseSpeedMs = CDbl(nudCruiseSpeed.Value) / 3.6,
                .MaxSpeedMs = CDbl(nudCruiseSpeed.Value) / 3.6 * 1.3,   ' 30 % over cruise
                .MaxAltitudeMeters = CDbl(nudMaxAltitude.Value),
                .MaxWindSpeedMs = CDbl(nudMaxWindSpeed.Value) / 3.6,
                .MaxTakeoffMassGrams = CDbl(nudMaxTakeoffWeight.Value),
                .PayloadMassGrams = CDbl(nudPayloadWeight.Value),
                .PayloadDimensionsMm = New PayloadDimensions(
                    CDbl(nudPayloadDepth.Value),    ' Length = Depth field on form
                    CDbl(nudPayloadWidth.Value),
                    CDbl(nudPayloadHeight.Value)),
                .MinOperatingTempCelsius = CDbl(nudTempMin.Value),
                .MaxOperatingTempCelsius = CDbl(nudTempMax.Value),
                .Environment = MapOperatingEnvCategory(envIdx),   ' legacy enum
                .RequiresWaterproofing = (cboIPRating.SelectedIndex > 0),
                .RequiredIPRating = MapIPRating(cboIPRating.SelectedIndex),
                .Profile = MapMissionProfileCategory(cboMissionProfile.SelectedIndex),   ' legacy enum
                .Configuration = MapUAVConfiguration(cboFrameType.SelectedIndex),
                .PowerSource = PowerSourceType.LiPo,
                .Regulatory = If(autoLevel >= 3,
                                    RegulatoryClass.BVLOS,
                                    RegulatoryClass.CommercialStandard),
                .MotorCount = motorCount,
                .RangeKm = rangeKm,
                .PayloadWeightGrams = CDbl(nudPayloadWeight.Value),
                .MissionProfile = MapMissionProfileType(cboMissionProfile.SelectedIndex),
                .OperatingEnvironment = MapEnvironmentType(envIdx),
                .RequiresAutopilot = (autoLevel >= 2),     ' GPS Hold and above
                .RequiresOpticalFlow = (envIdx = 7),         ' Indoor environment
                .RequiresObstacleAvoidance = (envIdx = 7),
                .RequiresDualGPS = chkGPSRedundancy.Checked,
                .RequiresTelemetry = True,
                .RequiresVideoDownlink = True,
                .VideoDownlinkRangeKm = rangeKm,
                .ControlLinkRangeKm = rangeKm
        }

    End Function

    ' =====================================================================
    '  DISPLAY SELECTION RESULT
    ' =====================================================================

    ''' <summary>
    ''' Flattens the engine's <see cref="SelectionResult"/> into a
    ''' <see cref="ComponentDisplayRow"/> list and binds it to the output
    ''' DataGridView.  Updates the summary footer and highlights recommended rows.
    '''
    ''' Category order matches the engine pipeline (Task 7 → 8 → 9):
    '''   Motors → Propellers → Batteries → ESCs → PDB →
    '''   Flight Controllers → GPS → Telemetry Radios → Receivers → Cameras.
    '''
    ''' Within each category: first row = "Recommended" (green);
    ''' remaining rows = "Alternative N" (standard background).
    ''' </summary>
    Private Sub DisplaySelectionResult(result As SelectionResult)
        Dim rows As New List(Of ComponentDisplayRow)()

        AppendRows(rows, result.SelectedMotors, "Motor")
        AppendRows(rows, result.SelectedPropellers, "Propeller")
        AppendRows(rows, result.SelectedBatteries, "Battery (LiPo)")
        AppendRows(rows, result.SelectedEscs, "ESC")
        AppendRows(rows, result.SelectedPdbs, "Power Dist. Board")
        AppendRows(rows, result.SelectedFlightControllers, "Flight Controller")
        AppendRows(rows, result.SelectedGpsModules, "GPS / GNSS Module")
        AppendRows(rows, result.SelectedTelemetryRadios, "Telemetry Radio")
        AppendRows(rows, result.SelectedReceivers, "RC Receiver")
        AppendRows(rows, result.SelectedCameras, "Camera / Sensor")

        ' ── Bind data source ─────────────────────────────────────────────
        ' Set to Nothing first to force full column re-bind.
        dgvComponents.DataSource = Nothing
        dgvComponents.DataSource = rows

        ' ── Highlight recommended rows ────────────────────────────────────
        For Each row As DataGridViewRow In dgvComponents.Rows
            Dim item As ComponentDisplayRow = TryCast(row.DataBoundItem, ComponentDisplayRow)
            If item IsNot Nothing AndAlso item.IsRecommended Then
                row.DefaultCellStyle.BackColor = Color.FromArgb(228, 248, 228)
                row.DefaultCellStyle.Font = New Font(dgvComponents.Font, FontStyle.Bold)
            End If
        Next

        ' ── Update summary footer (recommended components only) ───────────
        Dim recommended As List(Of ComponentDisplayRow) =
                rows.Where(Function(r) r.IsRecommended).ToList()

        UpdateSummary(
                recommended.Sum(Function(r) r.MassGrams),
                recommended.Sum(Function(r) r.MaxPowerWatts),
                recommended.Count)
    End Sub

    ''' <summary>
    ''' Appends display rows for one component category to the master row list.
    ''' First entry = "Recommended"; subsequent entries = "Alternative N".
    ''' Empty lists (e.g., no camera for Delivery builds) are silently skipped.
    ''' </summary>
    Private Shared Sub AppendRows(rows As List(Of ComponentDisplayRow),
                                      components As List(Of ComponentSpecs),
                                      category As String)
        If components Is Nothing OrElse components.Count = 0 Then Return

        For i As Integer = 0 To components.Count - 1
            Dim label As String = If(i = 0, "Recommended", $"Alternative {i + 1}")
            rows.Add(ComponentDisplayRow.FromComponentSpecs(
                             components(i), category, label, isRecommended:=(i = 0)))
        Next
    End Sub

    ' =====================================================================
    '  UI STATE — processing indicator
    ' =====================================================================

    ''' <summary>
    ''' Enters or leaves the processing state:
    '''   isProcessing = True  → disable inputs, show spinner text, wait cursor.
    '''   isProcessing = False → re-enable inputs, restore normal button state.
    ''' </summary>
    Private Sub SetProcessingState(isProcessing As Boolean)
        btnSelectComponents.Enabled = Not isProcessing
        btnClearInputs.Enabled = Not isProcessing

        If isProcessing Then
            btnSelectComponents.Text = "⏳  Processing…"
            btnSelectComponents.BackColor = Color.FromArgb(110, 140, 190)
            Me.Cursor = Cursors.WaitCursor
        Else
            btnSelectComponents.Text = "⚙  Select Components"
            btnSelectComponents.BackColor = Color.FromArgb(30, 90, 170)
            Me.Cursor = Cursors.Default
        End If
    End Sub

    ' =====================================================================
    '  MAPPING HELPERS — combo index → domain enum / value
    ' =====================================================================

    ''' <summary>
    ''' Parses the motor count from combo text like "4 — Quadrotor" → 4.
    ''' Defaults to 4 (quad) if the parse fails.
    ''' </summary>
    Private Shared Function ParseMotorCount(text As String) As Integer
        If String.IsNullOrWhiteSpace(text) Then Return 4
        Dim count As Integer
        Return If(Integer.TryParse(text.Trim().Split(" "c)(0), count), count, 4)
    End Function

    ''' <summary>
    ''' Maps Mission Profile combo SelectedIndex to the <see cref="MissionProfileType"/>
    ''' enum used directly by <see cref="ComponentSelectionEngine"/>.
    ''' Combo order is defined in BuildGroupMission() in MainForm.vb.
    ''' </summary>
    Private Shared Function MapMissionProfileType(idx As Integer) As MissionProfileType
        Select Case idx
            Case 0 : Return MissionProfileType.Mapping          ' Aerial Survey / Mapping
            Case 1 : Return MissionProfileType.Inspection        ' Inspection (Structures)
            Case 2 : Return MissionProfileType.SearchAndRescue   ' Search and Rescue
            Case 3 : Return MissionProfileType.Delivery          ' Package Delivery
            Case 4 : Return MissionProfileType.Racing            ' FPV Racing
            Case 5 : Return MissionProfileType.Surveillance      ' Photography / Videography
            Case 6 : Return MissionProfileType.General           ' Agricultural Spraying
            Case 7 : Return MissionProfileType.General           ' BVLOS Long Range
            Case 8 : Return MissionProfileType.General           ' Rapid Deployment / Scout
            Case 9 : Return MissionProfileType.General           ' Research / Experimental
            Case Else : Return MissionProfileType.General
        End Select
    End Function

    ''' <summary>
    ''' Maps Mission Profile combo SelectedIndex to the renamed
    ''' <see cref="MissionProfileCategory"/> enum (was MissionProfile before Task 11 rename).
    ''' Used to populate <see cref="MissionSpecs.Profile"/>.
    ''' </summary>
    Private Shared Function MapMissionProfileCategory(idx As Integer) As MissionProfileCategory
        Select Case idx
            Case 0 : Return MissionProfileCategory.Mapping
            Case 1 : Return MissionProfileCategory.Inspection
            Case 2 : Return MissionProfileCategory.SearchAndRescue
            Case 3 : Return MissionProfileCategory.Delivery
            Case 4 : Return MissionProfileCategory.Racing
            Case 5 : Return MissionProfileCategory.Surveillance
            Case Else : Return MissionProfileCategory.Surveillance
        End Select
    End Function

    ''' <summary>
    ''' Maps Operating Environment combo SelectedIndex to <see cref="EnvironmentType"/>.
    ''' Maritime (4), Desert (5), and Arctic (6) map to Harsh; everything else → Standard.
    ''' </summary>
    Private Shared Function MapEnvironmentType(idx As Integer) As EnvironmentType
        Select Case idx
            Case 4, 5, 6 : Return EnvironmentType.Harsh    ' Maritime / Desert / Arctic
            Case Else : Return EnvironmentType.Standard
        End Select
    End Function

    ''' <summary>
    ''' Maps Operating Environment combo SelectedIndex to the renamed
    ''' <see cref="OperatingEnvCategory"/> enum (was OperatingEnvironment before Task 11).
    ''' Used to populate the detailed <see cref="MissionSpecs.Environment"/> property.
    ''' </summary>
    Private Shared Function MapOperatingEnvCategory(idx As Integer) As OperatingEnvCategory
        Select Case idx
            Case 4 : Return OperatingEnvCategory.OutdoorWet        ' Maritime / Coastal
            Case 5 : Return OperatingEnvCategory.OutdoorHot        ' Desert
            Case 6 : Return OperatingEnvCategory.OutdoorCold       ' Arctic / High Altitude
            Case 7 : Return OperatingEnvCategory.IndoorGPSDenied   ' Indoor
            Case Else : Return OperatingEnvCategory.OutdoorStandard
        End Select
    End Function

    ''' <summary>
    ''' Maps Airframe Type combo SelectedIndex to <see cref="UAVConfiguration"/>.
    ''' "Coaxial" has no dedicated enum value; it is mapped to Quadcopter as the
    ''' closest structural equivalent. Update if Coaxial is added to UAVConfiguration.
    ''' </summary>
    Private Shared Function MapUAVConfiguration(idx As Integer) As UAVConfiguration
        Select Case idx
            Case 0 : Return UAVConfiguration.Quadcopter   ' Multirotor → default quad
            Case 1 : Return UAVConfiguration.FixedWing
            Case 2 : Return UAVConfiguration.VTOL
            Case 3 : Return UAVConfiguration.Helicopter
            Case 4 : Return UAVConfiguration.Quadcopter   ' Coaxial — best available match
            Case Else : Return UAVConfiguration.Quadcopter
        End Select
    End Function

    ''' <summary>
    ''' Returns the IP-rating string for the selected combo index.
    ''' Returns Empty string when "None (IP00)" is selected.
    ''' </summary>
    Private Shared Function MapIPRating(idx As Integer) As String
        Select Case idx
            Case 1 : Return "IP43"
            Case 2 : Return "IP54"
            Case 3 : Return "IP65"
            Case 4 : Return "IP67"
            Case 5 : Return "IP68"
            Case Else : Return String.Empty
        End Select
    End Function

End Class


