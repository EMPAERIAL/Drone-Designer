' =============================================================================
' File:    UI/Forms/MainForm.vb
' Project: Drone Designer
' Task:    10 — Main Form Layout
'
' Purpose: Root application window. Hosts two TabPages:
'            Tab 1 — Mission Specifications  (input controls)
'            Tab 2 — Selected Components     (DataGridView output)
'          A StatusStrip at the bottom provides progress / status messages.
'
' Wiring:  This file contains LAYOUT only. No business-logic calls are made
'          from here yet. The "Select Components" button handler is stubbed
'          so other chats / tasks can wire ComponentSelectionEngine into it
'          without touching the layout code.
'
' Author:  [Solo Dev] — generated via Task 10
' Target:  .NET Framework 4.7.2 / WinForms / VB.NET
' =============================================================================

Imports System.Windows.Forms
Imports System.Drawing
Imports Drone_Designer.Core.Services



''' <summary>
''' Main application window for the UAV Design Tool.
''' Provides a two-tab interface: Mission Specs input and Component output.
''' All controls are created and positioned in code (no .Designer.vb) so
''' every chat that touches this file sees the complete picture in one place.
''' </summary>
Partial Public Class MainForm
        Inherits Form

        ' ── Tab container ────────────────────────────────────────────────────
        Private tabMain As TabControl
        Private tabInput As TabPage
        Private tabOutput As TabPage

        ' ── Status strip ─────────────────────────────────────────────────────
        Private statusStrip As StatusStrip
        ''' <summary>Main status label. Write to this from any module.</summary>
        Friend lblStatus As ToolStripStatusLabel
        Private lblVersion As ToolStripStatusLabel

        ' ── INPUT TAB — outer scroll panel ───────────────────────────────────
        Private pnlInputScroll As Panel          ' AutoScroll wrapper

        ' ── GROUP: Flight Parameters ──────────────────────────────────────────
        Private grpFlight As GroupBox

        Private lblFrameSize As Label
        '''<summary>UAV frame size.</summary>
        Friend nudFrameSize As NumericUpDown

        Private lblEndurance As Label
        ''' <summary>Mission endurance in decimal hours (e.g. 1.5 = 90 min).</summary>
        Friend nudEndurance As NumericUpDown     ' hours

        Private lblRange As Label
        ''' <summary>Maximum operational range in kilometres.</summary>
        Friend nudRange As NumericUpDown         ' km

        Private lblCruiseSpeed As Label
        ''' <summary>Nominal cruise speed in km/h.</summary>
        Friend nudCruiseSpeed As NumericUpDown   ' km/h

        Private lblMaxAltitude As Label
        ''' <summary>Maximum operating altitude in metres AGL.</summary>
        Friend nudMaxAltitude As NumericUpDown   ' m AGL

        Private lblMaxWindSpeed As Label
        ''' <summary>Maximum wind speed the UAV must tolerate, km/h.</summary>
        Friend nudMaxWindSpeed As NumericUpDown  ' km/h

        Private lblTakeoffWeight As Label
        ''' <summary>Maximum allowable take-off weight in grams.</summary>
        Friend nudMaxTakeoffWeight As NumericUpDown ' g

        ' ── GROUP: Payload ────────────────────────────────────────────────────
        Private grpPayload As GroupBox

        Private lblPayloadWeight As Label
        ''' <summary>Payload mass in grams.</summary>
        Friend nudPayloadWeight As NumericUpDown  ' g

        Private lblPayloadType As Label
        ''' <summary>Category of carried payload (maps to Core.Models.PayloadType enum).</summary>
        Friend cboPayloadType As ComboBox

        Private lblPayloadVolume As Label
        Private lblPayloadW As Label
        Friend nudPayloadWidth As NumericUpDown   ' mm
        Private lblPayloadH As Label
        Friend nudPayloadHeight As NumericUpDown  ' mm
        Private lblPayloadD As Label
        Friend nudPayloadDepth As NumericUpDown   ' mm

        Private lblCameraResolution As Label
        ''' <summary>Camera resolution in megapixels; 0 = no camera required.</summary>
        Friend nudCameraResolution As NumericUpDown ' MP

        ' ── GROUP: Environment ───────────────────────────────────────────────
        Private grpEnvironment As GroupBox

        Private lblOperatingEnv As Label
        ''' <summary>Operating environment class (maps to Core.Models.OperatingEnvironment enum).</summary>
        Friend cboOperatingEnvironment As ComboBox

        Private lblTempMin As Label
        Friend nudTempMin As NumericUpDown        ' °C

        Private lblTempMax As Label
        Friend nudTempMax As NumericUpDown        ' °C

        Private lblIPRating As Label
        ''' <summary>Minimum IP ingress-protection rating required.</summary>
        Friend cboIPRating As ComboBox

        Private lblHumidity As Label
        ''' <summary>Maximum relative humidity (%). Affects electronics selection.</summary>
        Friend nudHumidity As NumericUpDown       ' %

        ' ── GROUP: Mission Type / Profile ────────────────────────────────────
        Private grpMission As GroupBox

        Private lblMissionProfile As Label
        ''' <summary>High-level mission category (maps to Core.Models.MissionProfile enum).</summary>
        Friend cboMissionProfile As ComboBox

        Private lblFrameType As Label
        ''' <summary>Requested airframe configuration (maps to Core.Models.FrameType enum).</summary>
        Friend cboFrameType As ComboBox

        Private lblMotorCount As Label
        ''' <summary>Number of motors / rotor count.</summary>
        Friend cboMotorCount As ComboBox

        Private lblAutonomy As Label
        ''' <summary>Required autonomy / flight-mode level.</summary>
        Friend cboAutonomyLevel As ComboBox

        Private lblRedundancy As Label
        ''' <summary>Whether motor/ESC redundancy is required.</summary>
        Friend chkMotorRedundancy As CheckBox
        Friend chkGPSRedundancy As CheckBox
        Friend chkBatteryRedundancy As CheckBox

        Private lblNotes As Label
        ''' <summary>Free-text notes forwarded to the selection engine as hints.</summary>
        Friend txtNotes As TextBox

        ' ── GROUP: Advanced Sizing Margins + Mission Phase Profile ────────────
        Private grpSizing As GroupBox
        Private lblKMotor As Label
        Friend nudKMotor As NumericUpDown
        Private lblKBatVoltage As Label
        Friend nudKBatVoltage As NumericUpDown
        Private lblKBatCapacity As Label
        Friend nudKBatCapacity As NumericUpDown
        Private lblKEsc As Label
        Friend nudKEsc As NumericUpDown
        Private lblSizingPreset As Label
        Friend cboSizingPreset As ComboBox
        Private lblHoverFraction As Label
        Friend nudHoverFraction As NumericUpDown
        Private lblClimbFraction As Label
        Friend nudClimbFraction As NumericUpDown
        Private lblCruiseFraction As Label
        Friend nudCruiseFraction As NumericUpDown
        Private lblClimbRate As Label
        Friend nudClimbRate As NumericUpDown

        ' ── ACTION BUTTON ─────────────────────────────────────────────────────
        Private pnlAction As Panel
        Friend btnSelectComponents As Button
        Friend btnClearInputs As Button

        ' ── OUTPUT TAB ────────────────────────────────────────────────────────
        Private pnlOutputTop As Panel
        Private lblOutputTitle As Label
        Friend btnExportCSV As Button
    Public btnSendToSolidWorks As Button

    ''' <summary>
    ''' Displays the selected component list.
    ''' Columns are defined here; rows are bound by the service layer.
    ''' Expected columns (match ComponentSpecs properties):
    '''   Category | Name | Manufacturer | Model | Mass(g) | Power(W) |
    '''   Voltage(V) | Notes
    ''' </summary>
    Friend dgvComponents As DataGridView

        Private pnlOutputSummary As Panel
        Friend lblSummaryMass As Label
        Friend lblSummaryPower As Label
        Friend lblSummaryBudget As Label

    ' ─────────────────────────────────────────────────────────────────────
    '  CONSTRUCTOR
    ' ─────────────────────────────────────────────────────────────────────

    Public Sub New()
        InitializeComponent()
        Me.SuspendLayout()

        InitializeFormProperties()
            InitializeStatusStrip()
            InitializeTabControl()
            InitializeInputTab()
            InitializeOutputTab()
            WireLayoutEvents()

            Me.ResumeLayout(False)
            Me.PerformLayout()
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  FORM-LEVEL PROPERTIES
        ' ─────────────────────────────────────────────────────────────────────

        Private Sub InitializeFormProperties()
            Me.Text = "UAV Design Tool — Mission Specs → Component Selection"
            Me.Size = New Size(1180, 760)
            Me.MinimumSize = New Size(900, 600)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            Me.BackColor = Color.FromArgb(245, 245, 248)
            Me.Icon = Nothing  ' TODO: assign application icon from Resources
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  STATUS STRIP
        ' ─────────────────────────────────────────────────────────────────────

        Private Sub InitializeStatusStrip()
            statusStrip = New StatusStrip()
            statusStrip.SizingGrip = True
            statusStrip.BackColor = Color.FromArgb(230, 230, 236)

            lblStatus = New ToolStripStatusLabel()
            lblStatus.Text = "Ready — enter mission parameters and click 'Select Components'."
            lblStatus.Spring = True          ' fills available space
            lblStatus.TextAlign = ContentAlignment.MiddleLeft

            lblVersion = New ToolStripStatusLabel()
            lblVersion.Text = "v0.1-MVP"
            lblVersion.BorderSides = ToolStripStatusLabelBorderSides.Left
            lblVersion.BorderStyle = Border3DStyle.Etched

            statusStrip.Items.AddRange(New ToolStripItem() {lblStatus, lblVersion})
            Me.Controls.Add(statusStrip)
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  TAB CONTROL
        ' ─────────────────────────────────────────────────────────────────────

        Private Sub InitializeTabControl()
            tabMain = New TabControl()
            tabMain.Dock = DockStyle.Fill
            tabMain.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            tabMain.Padding = New Point(14, 6)

            tabInput = New TabPage("  ① Mission Specifications  ")
            tabInput.BackColor = Color.FromArgb(245, 245, 248)
            tabInput.UseVisualStyleBackColor = True

            tabOutput = New TabPage("  ② Selected Components  ")
            tabOutput.BackColor = Color.White
            tabOutput.UseVisualStyleBackColor = True

            tabMain.TabPages.Add(tabInput)
            tabMain.TabPages.Add(tabOutput)

            Me.Controls.Add(tabMain)
            ' StatusStrip must be on top of the z-order hierarchy so Dock = Fill
            ' for the TabControl doesn't obscure it.
            statusStrip.BringToFront()
        End Sub

        ' ═════════════════════════════════════════════════════════════════════
        '  INPUT TAB — all mission specification controls
        ' ═════════════════════════════════════════════════════════════════════

        Private Sub InitializeInputTab()
            ' Outer scrollable panel so the form remains usable at small heights
            pnlInputScroll = New Panel()
            pnlInputScroll.Dock = DockStyle.Fill
            pnlInputScroll.AutoScroll = True
            pnlInputScroll.Padding = New Padding(12, 10, 12, 4)
            tabInput.Controls.Add(pnlInputScroll)

            Dim currentY As Integer = 8

            ' ── Build each group box and stack them vertically ──────────────
            currentY = BuildGroupFlight(currentY)
            currentY += 10
            currentY = BuildGroupPayload(currentY)
            currentY += 10
            currentY = BuildGroupEnvironment(currentY)
            currentY += 10
            currentY = BuildGroupMission(currentY)
            currentY += 10
            currentY = BuildGroupSizingMargins(currentY)
            currentY += 10
            BuildActionPanel(currentY)
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  Helper: standard label factory
        ' ─────────────────────────────────────────────────────────────────────
        Private Function MakeLabel(text As String, x As Integer, y As Integer,
                                   Optional width As Integer = 170) As Label
            Dim lbl As New Label()
            lbl.Text = text
            lbl.Location = New Point(x, y)
            lbl.Size = New Size(width, 22)
            lbl.TextAlign = ContentAlignment.MiddleRight
            lbl.ForeColor = Color.FromArgb(50, 50, 60)
            Return lbl
        End Function

        ' ─────────────────────────────────────────────────────────────────────
        '  Helper: standard NumericUpDown factory
        ' ─────────────────────────────────────────────────────────────────────
        Private Function MakeNUD(x As Integer, y As Integer,
                                 minVal As Decimal, maxVal As Decimal,
                                 decimalPlaces As Integer,
                                 Optional width As Integer = 100,
                                 Optional suffix As String = "") As NumericUpDown
            Dim nud As New NumericUpDown()
            nud.Location = New Point(x, y)
            nud.Size = New Size(width, 24)
            nud.Minimum = minVal
            nud.Maximum = maxVal
            nud.DecimalPlaces = decimalPlaces
            nud.Value = minVal
            nud.ThousandsSeparator = (maxVal >= 1000)
            nud.Tag = suffix     ' unit suffix stored for tooltip use
            Return nud
        End Function

        ' ─────────────────────────────────────────────────────────────────────
        '  Helper: standard ComboBox factory (DropDownList style)
        ' ─────────────────────────────────────────────────────────────────────
        Private Function MakeCombo(x As Integer, y As Integer,
                                   items As String(),
                                   Optional width As Integer = 180) As ComboBox
            Dim cbo As New ComboBox()
            cbo.Location = New Point(x, y)
            cbo.Size = New Size(width, 24)
            cbo.DropDownStyle = ComboBoxStyle.DropDownList
            cbo.FlatStyle = FlatStyle.System
            cbo.Items.AddRange(items)
            If cbo.Items.Count > 0 Then cbo.SelectedIndex = 0
            Return cbo
        End Function

        ' ─────────────────────────────────────────────────────────────────────
        '  Helper: attach unit tooltip to a control
        ' ─────────────────────────────────────────────────────────────────────
        Private ReadOnly _tt As New ToolTip()

        Private Sub SetTip(ctrl As Control, tip As String)
            _tt.SetToolTip(ctrl, tip)
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  Helper: standard GroupBox factory
        ' ─────────────────────────────────────────────────────────────────────
        Private Function MakeGroup(title As String, x As Integer, y As Integer,
                                   width As Integer, height As Integer) As GroupBox
            Dim grp As New GroupBox()
            grp.Text = "  " & title
            grp.Location = New Point(x, y)
            grp.Size = New Size(width, height)
            grp.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            grp.ForeColor = Color.FromArgb(30, 90, 170)
            grp.BackColor = Color.White
            grp.FlatStyle = FlatStyle.Standard
            Return grp
        End Function

        ' =====================================================================
        '  GROUP 1 — Flight Parameters
        ' =====================================================================

        ''' <summary>Builds the Flight Parameters group and returns next Y offset.</summary>
        Private Function BuildGroupFlight(startY As Integer) As Integer
            Const GRP_W As Integer = 1100
            Const GRP_H As Integer = 150
            Const LBL_X As Integer = 10
            Const CTL_X As Integer = 186
            Const ROW_H As Integer = 34
            Const ROW1 As Integer = 26
            Const COL2_LBL As Integer = 350
            Const COL2_CTL As Integer = 526
            Const COL3_LBL As Integer = 690
            Const COL3_CTL As Integer = 866

            grpFlight = MakeGroup("Flight Parameters", 0, startY, GRP_W, GRP_H)

            ' Row 1 ── Endurance | Range | Cruise Speed
            lblEndurance = MakeLabel("Endurance (hours):", LBL_X, ROW1)
            nudEndurance = MakeNUD(CTL_X, ROW1, 0.1D, 24D, 1, 90, "h")
            nudEndurance.Value = 0.4D
            nudEndurance.Increment = 0.1D
            SetTip(nudEndurance, "Required flight endurance in hours (e.g. 1.5 = 90 minutes).")

            lblRange = MakeLabel("Range (km):", COL2_LBL, ROW1)
            nudRange = MakeNUD(COL2_CTL, ROW1, 0, 500D, 0, 90, "km")
            nudRange.Value = 5
            SetTip(nudRange, "Maximum operational radius or one-way distance in kilometres.")

            lblCruiseSpeed = MakeLabel("Cruise Speed (km/h):", COL3_LBL, ROW1)
            nudCruiseSpeed = MakeNUD(COL3_CTL, ROW1, 0, 300D, 0, 90, "km/h")
            nudCruiseSpeed.Value = 30
            SetTip(nudCruiseSpeed, "Nominal cruise speed in km/h.")

            ' Row 2 ── Max Altitude | Max Wind Speed | Max Takeoff Weight
            Dim row2 As Integer = ROW1 + ROW_H

            lblMaxAltitude = MakeLabel("Max Altitude (m AGL):", LBL_X, row2)
            nudMaxAltitude = MakeNUD(CTL_X, row2, 0, 10000D, 0, 90, "m")
            nudMaxAltitude.Value = 100
            nudMaxAltitude.Increment = 50
            SetTip(nudMaxAltitude, "Maximum altitude above ground level in metres.")

            lblMaxWindSpeed = MakeLabel("Max Wind Speed (km/h):", COL2_LBL, row2)
            nudMaxWindSpeed = MakeNUD(COL2_CTL, row2, 0, 150D, 0, 90, "km/h")
            nudMaxWindSpeed.Value = 30
            SetTip(nudMaxWindSpeed, "Maximum sustained wind speed the UAV must remain stable in.")

            lblTakeoffWeight = MakeLabel("Max Takeoff Weight (g):", COL3_LBL, row2)
            nudMaxTakeoffWeight = MakeNUD(COL3_CTL, row2, 100D, 50000D, 0, 100, "g")
            nudMaxTakeoffWeight.Value = 2000
            nudMaxTakeoffWeight.Increment = 100
            SetTip(nudMaxTakeoffWeight, "Maximum allowable total takeoff weight including payload, in grams.")

            ' Row 3 ── Frame Size
            Dim row3 As Integer = row2 + ROW_H

            lblFrameSize = MakeLabel("Frame Size (mm):", LBL_X, row3)
            nudFrameSize = MakeNUD(CTL_X, row3, 100D, 1500D, 0, 90, "mm")
            nudFrameSize.Value = 250
            nudFrameSize.Increment = 10
            SetTip(nudFrameSize, "UAV frame size (diagonal or largest dimension).")

            grpFlight.Controls.AddRange(New Control() {
                lblEndurance, nudEndurance,
                lblRange, nudRange,
                lblCruiseSpeed, nudCruiseSpeed,
                lblMaxAltitude, nudMaxAltitude,
                lblMaxWindSpeed, nudMaxWindSpeed,
                lblTakeoffWeight, nudMaxTakeoffWeight,
                lblFrameSize, nudFrameSize
            })

            pnlInputScroll.Controls.Add(grpFlight)
            Return startY + GRP_H
        End Function

        ' =====================================================================
        '  GROUP 2 — Payload
        ' =====================================================================

        ''' <summary>Builds the Payload group and returns next Y offset.</summary>
        Private Function BuildGroupPayload(startY As Integer) As Integer
            Const GRP_W As Integer = 1100
            Const GRP_H As Integer = 188
            Const LBL_X As Integer = 10
            Const CTL_X As Integer = 186
            Const ROW_H As Integer = 34
            Const ROW1 As Integer = 26
            Const COL2_LBL As Integer = 350
            Const COL2_CTL As Integer = 526
            Const COL3_LBL As Integer = 690
            Const COL3_CTL As Integer = 866

            grpPayload = MakeGroup("Payload", 0, startY, GRP_W, GRP_H)

            ' Row 1 ── Payload Weight | Payload Type | Camera Resolution
            lblPayloadWeight = MakeLabel("Payload Weight (g):", LBL_X, ROW1)
            nudPayloadWeight = MakeNUD(CTL_X, ROW1, 0, 20000D, 0, 100, "g")
            nudPayloadWeight.Value = 0
            nudPayloadWeight.Increment = 50
            SetTip(nudPayloadWeight, "Mass of the carried payload in grams. Affects battery and motor sizing.")

            lblPayloadType = MakeLabel("Payload Type:", COL2_LBL, ROW1)
            cboPayloadType = MakeCombo(COL2_CTL, ROW1, New String() {
                "None",
                "Optical Camera",
                "Multispectral Camera",
                "Thermal Camera",
                "LiDAR",
                "Package / Delivery",
                "Custom Sensor Suite"
            }, 200)
            SetTip(cboPayloadType, "Category of payload being carried. Influences camera/sensor component selection.")

            lblCameraResolution = MakeLabel("Camera Res. (MP):", COL3_LBL, ROW1)
            nudCameraResolution = MakeNUD(COL3_CTL, ROW1, 0, 200D, 0, 90, "MP")
            nudCameraResolution.Value = 0
            SetTip(nudCameraResolution, "Required camera resolution in megapixels. Leave 0 if no camera needed.")

            ' Row 2 ── Payload Volume (W × H × D in mm)
            Dim row2 As Integer = ROW1 + ROW_H

            lblPayloadVolume = MakeLabel("Payload Volume:", LBL_X, row2)
            lblPayloadVolume.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)

            lblPayloadW = MakeLabel("W (mm):", COL2_LBL, row2, 60)
            lblPayloadW.TextAlign = ContentAlignment.MiddleLeft
            nudPayloadWidth = MakeNUD(COL2_LBL + 64, row2, 0, 2000D, 0, 80, "mm")
            SetTip(nudPayloadWidth, "Payload width in millimetres (for bay sizing).")

            lblPayloadH = MakeLabel("H (mm):", COL2_LBL + 160, row2, 60)
            lblPayloadH.TextAlign = ContentAlignment.MiddleLeft
            nudPayloadHeight = MakeNUD(COL2_LBL + 224, row2, 0, 2000D, 0, 80, "mm")
            SetTip(nudPayloadHeight, "Payload height in millimetres.")

            lblPayloadD = MakeLabel("D (mm):", COL2_LBL + 320, row2, 60)
            lblPayloadD.TextAlign = ContentAlignment.MiddleLeft
            nudPayloadDepth = MakeNUD(COL2_LBL + 384, row2, 0, 2000D, 0, 80, "mm")
            SetTip(nudPayloadDepth, "Payload depth in millimetres.")

            ' Dimension labels inherit group font — override to regular weight
            For Each lbl As Label In New Label() {lblPayloadW, lblPayloadH, lblPayloadD}
                lbl.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
                lbl.ForeColor = Color.FromArgb(50, 50, 60)
            Next

            grpPayload.Controls.AddRange(New Control() {
                lblPayloadWeight, nudPayloadWeight,
                lblPayloadType, cboPayloadType,
                lblCameraResolution, nudCameraResolution,
                lblPayloadVolume,
                lblPayloadW, nudPayloadWidth,
                lblPayloadH, nudPayloadHeight,
                lblPayloadD, nudPayloadDepth
            })

            pnlInputScroll.Controls.Add(grpPayload)
            Return startY + GRP_H
        End Function

        ' =====================================================================
        '  GROUP 3 — Environment
        ' =====================================================================

        ''' <summary>Builds the Environment group and returns next Y offset.</summary>
        Private Function BuildGroupEnvironment(startY As Integer) As Integer
            Const GRP_W As Integer = 1100
            Const GRP_H As Integer = 150
            Const LBL_X As Integer = 10
            Const CTL_X As Integer = 186
            Const ROW_H As Integer = 34
            Const ROW1 As Integer = 26
            Const COL2_LBL As Integer = 350
            Const COL2_CTL As Integer = 526
            Const COL3_LBL As Integer = 690
            Const COL3_CTL As Integer = 866

            grpEnvironment = MakeGroup("Environment", 0, startY, GRP_W, GRP_H)

            ' Row 1 ── Operating Environment | IP Rating | Humidity
            lblOperatingEnv = MakeLabel("Operating Environment:", LBL_X, ROW1)
            cboOperatingEnvironment = MakeCombo(CTL_X, ROW1, New String() {
                "Urban",
                "Sub-urban",
                "Rural / Open Field",
                "Forested",
                "Maritime / Coastal",
                "Desert",
                "Arctic / High Altitude",
                "Indoor"
            }, 200)
            SetTip(cboOperatingEnvironment, "Deployment environment — affects motor/ESC thermal ratings and waterproofing requirements.")

            lblIPRating = MakeLabel("Min. IP Rating:", COL2_LBL, ROW1)
            cboIPRating = MakeCombo(COL2_CTL, ROW1, New String() {
                "None (IP00)",
                "IP43 — Splash proof",
                "IP54 — Dust + Splash",
                "IP65 — Dust tight + Water jets",
                "IP67 — Immersion 30 min",
                "IP68 — Immersion >1 m"
            }, 230)
            SetTip(cboIPRating, "Minimum ingress protection required for electronic components.")

            lblHumidity = MakeLabel("Max Humidity (%):", COL3_LBL, ROW1)
            nudHumidity = MakeNUD(COL3_CTL, ROW1, 10D, 100D, 0, 90, "%")
            nudHumidity.Value = 80
            SetTip(nudHumidity, "Maximum relative humidity the electronics will be exposed to.")

            ' Row 2 ── Temp Min | Temp Max
            Dim row2 As Integer = ROW1 + ROW_H

            lblTempMin = MakeLabel("Min Temperature (°C):", LBL_X, row2)
            nudTempMin = MakeNUD(CTL_X, row2, -60D, 60D, 0, 90, "°C")
            nudTempMin.Value = -10
            SetTip(nudTempMin, "Minimum ambient temperature the system must operate in (°C).")

            lblTempMax = MakeLabel("Max Temperature (°C):", COL2_LBL, row2)
            nudTempMax = MakeNUD(COL2_CTL, row2, -60D, 120D, 0, 90, "°C")
            nudTempMax.Value = 45
            SetTip(nudTempMax, "Maximum ambient temperature the system must operate in (°C).")

            grpEnvironment.Controls.AddRange(New Control() {
                lblOperatingEnv, cboOperatingEnvironment,
                lblIPRating, cboIPRating,
                lblHumidity, nudHumidity,
                lblTempMin, nudTempMin,
                lblTempMax, nudTempMax
            })

            pnlInputScroll.Controls.Add(grpEnvironment)
            Return startY + GRP_H
        End Function

        ' =====================================================================
        '  GROUP 4 — Mission Type / Profile
        ' =====================================================================

        ''' <summary>Builds the Mission Type group and returns next Y offset.</summary>
        Private Function BuildGroupMission(startY As Integer) As Integer
            Const GRP_W As Integer = 1100
            Const GRP_H As Integer = 220
            Const LBL_X As Integer = 10
            Const CTL_X As Integer = 186
            Const ROW_H As Integer = 34
            Const ROW1 As Integer = 26
            Const COL2_LBL As Integer = 350
            Const COL2_CTL As Integer = 526
            Const COL3_LBL As Integer = 690
            Const COL3_CTL As Integer = 866

            grpMission = MakeGroup("Mission Type / Profile", 0, startY, GRP_W, GRP_H)

            ' Row 1 ── Mission Profile | Frame Type | Motor Count
            lblMissionProfile = MakeLabel("Mission Profile:", LBL_X, ROW1)
            cboMissionProfile = MakeCombo(CTL_X, ROW1, New String() {
                "Aerial Survey / Mapping",
                "Inspection (Structures)",
                "Search and Rescue",
                "Package Delivery",
                "FPV Racing",
                "Photography / Videography",
                "Agricultural Spraying",
                "BVLOS Long Range",
                "Rapid Deployment / Scout",
                "Research / Experimental"
            }, 200)
            SetTip(cboMissionProfile, "Primary mission type — drives component priority weighting in the selection algorithm.")
            cboMissionProfile.SelectedIndex = 1

            lblFrameType = MakeLabel("Airframe Type:", COL2_LBL, ROW1)
            cboFrameType = MakeCombo(COL2_CTL, ROW1, New String() {
                "Multirotor",
                "Fixed Wing",
                "VTOL Hybrid",
                "Single Rotor (Helicopter)",
                "Coaxial"
            }, 200)
            SetTip(cboFrameType, "Desired airframe configuration. Drives propulsion and structural design.")

            lblMotorCount = MakeLabel("Motor Count:", COL3_LBL, ROW1)
            cboMotorCount = MakeCombo(COL3_CTL, ROW1, New String() {
                "3 — Tricopter",
                "4 — Quadrotor",
                "6 — Hexarotor",
                "8 — Octorotor",
                "12 — Dodecacopter",
                "1 — Fixed Wing",
                "2 — Twin Engine FW"
            }, 200)
            cboMotorCount.SelectedIndex = 1   ' default Quad
            SetTip(cboMotorCount, "Number of motors / rotors. Affects redundancy, thrust budget, and frame design.")

            ' Row 2 ── Autonomy Level
            Dim row2 As Integer = ROW1 + ROW_H

            lblAutonomy = MakeLabel("Autonomy Level:", LBL_X, row2)
            cboAutonomyLevel = MakeCombo(CTL_X, row2, New String() {
                "Manual (RC Only)",
                "Stabilised (Acro + Level)",
                "GPS Hold / Loiter",
                "Waypoint Autonomous",
                "Fully Autonomous (No RC)"
            }, 200)
            cboAutonomyLevel.SelectedIndex = 2
            SetTip(cboAutonomyLevel, "Required autonomy capability — determines flight controller and GPS module tier.")

            ' Row 3 ── Redundancy checkboxes
            Dim row3 As Integer = row2 + ROW_H

            lblRedundancy = MakeLabel("Redundancy Required:", LBL_X, row3)
            lblRedundancy.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            lblRedundancy.ForeColor = Color.FromArgb(50, 50, 60)

            chkMotorRedundancy = New CheckBox()
            chkMotorRedundancy.Text = "Motor / ESC Redundancy"
            chkMotorRedundancy.Location = New Point(CTL_X, row3)
            chkMotorRedundancy.Size = New Size(200, 22)
            chkMotorRedundancy.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            SetTip(chkMotorRedundancy, "Require extra motors so flight is maintained if one motor fails (needs even motor count).")

            chkGPSRedundancy = New CheckBox()
            chkGPSRedundancy.Text = "Dual GPS"
            chkGPSRedundancy.Location = New Point(CTL_X + 210, row3)
            chkGPSRedundancy.Size = New Size(130, 22)
            chkGPSRedundancy.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            SetTip(chkGPSRedundancy, "Include a secondary GPS module for redundancy.")

            chkBatteryRedundancy = New CheckBox()
            chkBatteryRedundancy.Text = "Dual Battery"
            chkBatteryRedundancy.Location = New Point(CTL_X + 350, row3)
            chkBatteryRedundancy.Size = New Size(130, 22)
            chkBatteryRedundancy.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            SetTip(chkBatteryRedundancy, "Include parallel battery configuration for extended endurance or fault tolerance.")

            ' Row 4 ── Notes
            Dim row4 As Integer = row3 + ROW_H

            lblNotes = MakeLabel("Additional Notes / Hints:", LBL_X, row4)
            lblNotes.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            lblNotes.ForeColor = Color.FromArgb(50, 50, 60)

            txtNotes = New TextBox()
            txtNotes.Location = New Point(CTL_X, row4)
            txtNotes.Size = New Size(GRP_W - CTL_X - 16, 50)
            txtNotes.Multiline = True
            txtNotes.ScrollBars = ScrollBars.Vertical
            txtNotes.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            SetTip(txtNotes, "Free-text hints passed to the component selection engine. Not parsed — used as context.")

            grpMission.Controls.AddRange(New Control() {
                lblMissionProfile, cboMissionProfile,
                lblFrameType, cboFrameType,
                lblMotorCount, cboMotorCount,
                lblAutonomy, cboAutonomyLevel,
                lblRedundancy, chkMotorRedundancy, chkGPSRedundancy, chkBatteryRedundancy,
                lblNotes, txtNotes
            })

            pnlInputScroll.Controls.Add(grpMission)
            Return startY + GRP_H
        End Function

        ' =====================================================================
        '  GROUP 5 — Advanced Sizing Margins
        ' =====================================================================

        ''' <summary>Builds the Advanced Sizing Margins + Mission Phase Profile group and returns next Y offset.</summary>
        Private Function BuildGroupSizingMargins(startY As Integer) As Integer
            Const GRP_W As Integer = 1100
            Const GRP_H As Integer = 160
            Const LBL_X As Integer = 10
            Const CTL_X As Integer = 186
            Const ROW1 As Integer = 26
            Const COL2_LBL As Integer = 350
            Const COL2_CTL As Integer = 526
            Const COL3_LBL As Integer = 690
            Const COL3_CTL As Integer = 866

            grpSizing = MakeGroup("Advanced Sizing Margins", 0, startY, GRP_W, GRP_H)

            ' Row 1 ── Motor torque k | Battery voltage k | Preset
            lblKMotor = MakeLabel("Motor torque margin (k):", LBL_X, ROW1)
            nudKMotor = MakeNUD(CTL_X, ROW1, 1.0D, 4.0D, 1, 80)
            nudKMotor.Value = 1.5D
            nudKMotor.Increment = 0.1D
            SetTip(nudKMotor, "Motor must sustain at least (k × hover torque). Higher = more gust/climb headroom.")

            lblKBatVoltage = MakeLabel("Battery voltage margin (k):", COL2_LBL, ROW1)
            nudKBatVoltage = MakeNUD(COL2_CTL, ROW1, 1.0D, 2.0D, 2, 80)
            nudKBatVoltage.Value = 1.3D
            nudKBatVoltage.Increment = 0.05D
            SetTip(nudKBatVoltage, "Pack voltage = motor nominal × k. Covers cell sag under full load.")

            lblSizingPreset = MakeLabel("Preset:", COL3_LBL, ROW1)
            cboSizingPreset = MakeCombo(COL3_CTL, ROW1, New String() {
                "General Purpose",
                "Racing (minimum weight)",
                "Harsh Environment"
            }, 180)
            SetTip(cboSizingPreset, "Quick-fill all four margin fields from a preset profile.")
            AddHandler cboSizingPreset.SelectedIndexChanged, AddressOf OnSizingPresetChanged

            ' Row 2 ── Battery capacity k | ESC current k
            Dim row2 As Integer = ROW1 + 34
            lblKBatCapacity = MakeLabel("Battery capacity margin (k):", LBL_X, row2)
            nudKBatCapacity = MakeNUD(CTL_X, row2, 1.0D, 2.0D, 2, 80)
            nudKBatCapacity.Value = 1.2D
            nudKBatCapacity.Increment = 0.05D
            SetTip(nudKBatCapacity, "Required battery energy = calculated mission energy × k. 1.2 = 20% reserve.")

            lblKEsc = MakeLabel("ESC current margin (k):", COL2_LBL, row2)
            nudKEsc = MakeNUD(COL2_CTL, row2, 1.0D, 2.0D, 2, 80)
            nudKEsc.Value = 1.25D
            nudKEsc.Increment = 0.05D
            SetTip(nudKEsc, "ESC continuous rating ≥ motor peak current × k. 1.25 = 25% thermal headroom.")

            ' Row 3 ── Hover fraction | Climb fraction | Cruise fraction
            Dim row3 As Integer = row2 + 34
            lblHoverFraction = MakeLabel("Hover time (0-1):", LBL_X, row3)
            nudHoverFraction = MakeNUD(CTL_X, row3, 0.0D, 1.0D, 2, 80)
            nudHoverFraction.Value = 0.3D
            nudHoverFraction.Increment = 0.05D
            SetTip(nudHoverFraction, "Fraction of total endurance spent hovering (takeoff loiter + landing). Must sum to 1.0 with climb and cruise fractions.")

            lblClimbFraction = MakeLabel("Climb time (0-1):", COL2_LBL, row3)
            nudClimbFraction = MakeNUD(COL2_CTL, row3, 0.0D, 1.0D, 2, 80)
            nudClimbFraction.Value = 0.1D
            nudClimbFraction.Increment = 0.05D
            SetTip(nudClimbFraction, "Fraction of total endurance spent climbing. Must sum to 1.0 with hover and cruise fractions.")

            lblCruiseFraction = MakeLabel("Cruise time (0-1):", COL3_LBL, row3)
            nudCruiseFraction = MakeNUD(COL3_CTL, row3, 0.0D, 1.0D, 2, 80)
            nudCruiseFraction.Value = 0.6D
            nudCruiseFraction.Increment = 0.05D
            SetTip(nudCruiseFraction, "Fraction of total endurance spent in level cruise. Must sum to 1.0 with hover and climb fractions.")

            ' Row 4 ── Climb rate
            Dim row4 As Integer = row3 + 34
            lblClimbRate = MakeLabel("Climb rate (m/s):", LBL_X, row4)
            nudClimbRate = MakeNUD(CTL_X, row4, 0.5D, 20.0D, 1, 80, "m/s")
            nudClimbRate.Value = 3.0D
            nudClimbRate.Increment = 0.5D
            SetTip(nudClimbRate, "Vertical climb rate in m/s. Used by momentum-theory climb power model.")

            grpSizing.Controls.AddRange(New Control() {
                lblKMotor, nudKMotor,
                lblKBatVoltage, nudKBatVoltage,
                lblSizingPreset, cboSizingPreset,
                lblKBatCapacity, nudKBatCapacity,
                lblKEsc, nudKEsc,
                lblHoverFraction, nudHoverFraction,
                lblClimbFraction, nudClimbFraction,
                lblCruiseFraction, nudCruiseFraction,
                lblClimbRate, nudClimbRate
            })

            pnlInputScroll.Controls.Add(grpSizing)
            Return startY + GRP_H
        End Function

        ''' <summary>Populates all four k-factor NUDs from the selected preset.</summary>
        Private Sub OnSizingPresetChanged(sender As Object, e As EventArgs)
            Select Case cboSizingPreset.SelectedIndex
                Case 1  ' Racing
                    nudKMotor.Value = 1.5D
                    nudKBatVoltage.Value = 1.1D
                    nudKBatCapacity.Value = 1.1D
                    nudKEsc.Value = 1.15D
                Case 2  ' Harsh Environment
                    nudKMotor.Value = 3.0D
                    nudKBatVoltage.Value = 1.4D
                    nudKBatCapacity.Value = 1.4D
                    nudKEsc.Value = 1.5D
                Case Else  ' General Purpose
                    nudKMotor.Value = 1.5D
                    nudKBatVoltage.Value = 1.3D
                    nudKBatCapacity.Value = 1.2D
                    nudKEsc.Value = 1.25D
            End Select
        End Sub

        ' =====================================================================
        '  ACTION PANEL — Select Components + Clear buttons
        ' =====================================================================

        Private Sub BuildActionPanel(startY As Integer)
            pnlAction = New Panel()
            pnlAction.Location = New Point(0, startY)
            pnlAction.Size = New Size(1100, 56)
            pnlAction.BackColor = Color.Transparent

            btnSelectComponents = New Button()
            btnSelectComponents.Text = "⚙  Select Components"
            btnSelectComponents.Size = New Size(220, 40)
            btnSelectComponents.Location = New Point(0, 8)
            btnSelectComponents.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
            btnSelectComponents.BackColor = Color.FromArgb(30, 90, 170)
            btnSelectComponents.ForeColor = Color.White
            btnSelectComponents.FlatStyle = FlatStyle.Flat
            btnSelectComponents.FlatAppearance.BorderSize = 0
            btnSelectComponents.Cursor = Cursors.Hand
            SetTip(btnSelectComponents, "Run the component selection engine with the current mission parameters.")
            ' TODO (Task 11): wire Click → ComponentSelectionEngine.SelectComponents(BuildMissionSpecs())

            btnClearInputs = New Button()
            btnClearInputs.Text = "✕  Clear All"
            btnClearInputs.Size = New Size(120, 40)
            btnClearInputs.Location = New Point(230, 8)
            btnClearInputs.Font = New Font("Segoe UI", 9.5F, FontStyle.Regular)
            btnClearInputs.BackColor = Color.FromArgb(200, 200, 210)
            btnClearInputs.ForeColor = Color.FromArgb(30, 30, 40)
            btnClearInputs.FlatStyle = FlatStyle.Flat
            btnClearInputs.FlatAppearance.BorderSize = 0
            btnClearInputs.Cursor = Cursors.Hand
            SetTip(btnClearInputs, "Reset all input fields to their default values.")
            AddHandler btnClearInputs.Click, AddressOf OnClearInputs

            pnlAction.Controls.AddRange(New Control() {btnSelectComponents, btnClearInputs})
            pnlInputScroll.Controls.Add(pnlAction)
        End Sub

        ' ═════════════════════════════════════════════════════════════════════
        '  OUTPUT TAB — component list grid and summary bar
        ' ═════════════════════════════════════════════════════════════════════

        Private Sub InitializeOutputTab()
            ' ── Top toolbar ─────────────────────────────────────────────────
            pnlOutputTop = New Panel()
            pnlOutputTop.Dock = DockStyle.Top
            pnlOutputTop.Height = 48
            pnlOutputTop.BackColor = Color.FromArgb(245, 245, 248)
            pnlOutputTop.Padding = New Padding(8, 6, 8, 0)

            lblOutputTitle = New Label()
            lblOutputTitle.Text = "Selected Component List"
            lblOutputTitle.Font = New Font("Segoe UI", 10.5F, FontStyle.Bold)
            lblOutputTitle.ForeColor = Color.FromArgb(30, 90, 170)
            lblOutputTitle.Location = New Point(8, 12)
            lblOutputTitle.AutoSize = True

            btnExportCSV = New Button()
            btnExportCSV.Text = "Export to Excel"
            btnExportCSV.Size = New Size(130, 32)
            btnExportCSV.Location = New Point(820, 8)
            btnExportCSV.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)
            btnExportCSV.FlatStyle = FlatStyle.Flat
            btnExportCSV.Cursor = Cursors.Hand
            SetTip(btnExportCSV, "Export the component list to an Excel workbook (.xlsx).")

            btnSendToSolidWorks = New Button()
            btnSendToSolidWorks.Text = "▶  Send to SolidWorks"
            btnSendToSolidWorks.Size = New Size(170, 32)
            btnSendToSolidWorks.Location = New Point(960, 8)
            btnSendToSolidWorks.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            btnSendToSolidWorks.BackColor = Color.FromArgb(20, 140, 70)
            btnSendToSolidWorks.ForeColor = Color.White
            btnSendToSolidWorks.FlatStyle = FlatStyle.Flat
            btnSendToSolidWorks.FlatAppearance.BorderSize = 0
            btnSendToSolidWorks.Cursor = Cursors.Hand
            SetTip(btnSendToSolidWorks, "Pass the selected component specs to Module 2 (SolidWorks Automation).")
            ' TODO (Task 14): wire Click → SolidWorksAutomation.RunWorkflow(componentList)

            pnlOutputTop.Controls.AddRange(New Control() {
                lblOutputTitle, btnExportCSV, btnSendToSolidWorks
            })
            tabOutput.Controls.Add(pnlOutputTop)

            ' ── Summary footer ───────────────────────────────────────────────
            pnlOutputSummary = New Panel()
            pnlOutputSummary.Dock = DockStyle.Bottom
            pnlOutputSummary.Height = 36
            pnlOutputSummary.BackColor = Color.FromArgb(235, 240, 255)
            pnlOutputSummary.Padding = New Padding(12, 6, 12, 0)

            lblSummaryMass = New Label()
            lblSummaryMass.Text = "Total Component Mass:  — g"
            lblSummaryMass.AutoSize = True
            lblSummaryMass.Location = New Point(12, 8)
            lblSummaryMass.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)

            lblSummaryPower = New Label()
            lblSummaryPower.Text = "Total System Power:  — W"
            lblSummaryPower.AutoSize = True
            lblSummaryPower.Location = New Point(280, 8)
            lblSummaryPower.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)

            lblSummaryBudget = New Label()
            lblSummaryBudget.Text = "Component Count:  —"
            lblSummaryBudget.AutoSize = True
            lblSummaryBudget.Location = New Point(550, 8)
            lblSummaryBudget.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular)

            pnlOutputSummary.Controls.AddRange(New Control() {
                lblSummaryMass, lblSummaryPower, lblSummaryBudget
            })
            tabOutput.Controls.Add(pnlOutputSummary)

            ' ── DataGridView ─────────────────────────────────────────────────
            dgvComponents = New DataGridView()
            dgvComponents.Dock = DockStyle.Fill
            dgvComponents.BackgroundColor = Color.White
            dgvComponents.BorderStyle = BorderStyle.None
            dgvComponents.RowHeadersVisible = False
            dgvComponents.AllowUserToAddRows = False
            dgvComponents.AllowUserToDeleteRows = False
            dgvComponents.ReadOnly = True
            dgvComponents.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            dgvComponents.MultiSelect = False
            dgvComponents.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            dgvComponents.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            dgvComponents.Font = New Font("Segoe UI", 9.0F)

            ' Styling
            dgvComponents.DefaultCellStyle.WrapMode = DataGridViewTriState.True
            dgvComponents.DefaultCellStyle.Padding = New Padding(4, 2, 4, 2)
            dgvComponents.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 248, 255)
            dgvComponents.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
            dgvComponents.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(220, 228, 248)
            dgvComponents.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(20, 50, 120)
            dgvComponents.EnableHeadersVisualStyles = False

            ' ── Column definitions ───────────────────────────────────────────
            ' These column names MUST match the property names used by
            ' Core/Services/ComponentSelectionEngine when binding results.
            ' (Task 11 will bind DataSource = engine.SelectComponents(specs))

            dgvComponents.Columns.Add(MakeColumn("Category", "Category", 130))
            dgvComponents.Columns.Add(MakeColumn("Manufacturer", "Manufacturer", 120))
            dgvComponents.Columns.Add(MakeColumn("ModelName", "Model / Part No.", 160))
            dgvComponents.Columns.Add(MakeColumn("MassGrams", "Mass (g)", 80,
                                                  DataGridViewContentAlignment.MiddleRight))
            dgvComponents.Columns.Add(MakeColumn("NominalVoltage", "Voltage (V)", 90,
                                                  DataGridViewContentAlignment.MiddleRight))
            dgvComponents.Columns.Add(MakeColumn("MaxPowerWatts", "Max Power (W)", 105,
                                                  DataGridViewContentAlignment.MiddleRight))
            dgvComponents.Columns.Add(MakeColumn("Dimensions", "Dimensions (mm)", 130))
            dgvComponents.Columns.Add(MakeColumn("Interface", "Interface / Protocol", 140))
            dgvComponents.Columns.Add(MakeColumn("TempRating", "Temp Range (°C)", 120,
                                                  DataGridViewContentAlignment.MiddleCenter))
            dgvComponents.Columns.Add(MakeColumn("SelectionNotes", "Selection Notes",
                                                  200, DataGridViewContentAlignment.MiddleLeft, autoFill:=True))

            tabOutput.Controls.Add(dgvComponents)   ' add before panels so Fill works correctly
            pnlOutputTop.BringToFront()
            pnlOutputSummary.BringToFront()
        End Sub

        ''' <summary>Convenience factory for a DataGridView column.</summary>
        Private Function MakeColumn(dataPropertyName As String,
                                    headerText As String,
                                    width As Integer,
                                    Optional align As DataGridViewContentAlignment =
                                        DataGridViewContentAlignment.MiddleLeft,
                                    Optional autoFill As Boolean = False) As DataGridViewTextBoxColumn
            Dim col As New DataGridViewTextBoxColumn()
            col.DataPropertyName = dataPropertyName
            col.HeaderText = headerText
            col.DefaultCellStyle.Alignment = align
            If autoFill Then
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            Else
                col.Width = width
            End If
            Return col
        End Function

        ' ═════════════════════════════════════════════════════════════════════
        '  LAYOUT EVENTS (resize / DPI awareness)
        ' ═════════════════════════════════════════════════════════════════════

        Private Sub WireLayoutEvents()
            AddHandler Me.Resize, AddressOf OnFormResize
        End Sub

        ''' <summary>
        ''' Adjusts group widths when the form is resized so controls don't
        ''' clip or leave excess whitespace on wide monitors.
        ''' </summary>
        Private Sub OnFormResize(sender As Object, e As EventArgs)
            Dim availW As Integer = pnlInputScroll.ClientSize.Width - 24
            If availW < 600 Then Return

            For Each grp As GroupBox In New GroupBox() {
                    grpFlight, grpPayload, grpEnvironment, grpMission}
                If grp IsNot Nothing Then grp.Width = availW
            Next

            If pnlAction IsNot Nothing Then pnlAction.Width = availW

            ' Stretch notes TextBox to group width
            If txtNotes IsNot Nothing AndAlso grpMission IsNot Nothing Then
                txtNotes.Width = grpMission.Width - txtNotes.Left - 16
            End If
        End Sub

        ' ═════════════════════════════════════════════════════════════════════
        '  BUTTON HANDLERS — layout-level only (no business logic)
        ' ═════════════════════════════════════════════════════════════════════

        ''' <summary>
        ''' Resets all input controls to their initial default values.
        ''' Business logic is NOT called here.
        ''' </summary>
        Private Sub OnClearInputs(sender As Object, e As EventArgs)
            nudEndurance.Value = 0.4D
            nudRange.Value = 5
            nudCruiseSpeed.Value = 30
            nudMaxAltitude.Value = 100
            nudMaxWindSpeed.Value = 30
            nudMaxTakeoffWeight.Value = 2000

            nudPayloadWeight.Value = 0
            cboPayloadType.SelectedIndex = 0
            nudCameraResolution.Value = 0
            nudPayloadWidth.Value = 0
            nudPayloadHeight.Value = 0
            nudPayloadDepth.Value = 0

            cboOperatingEnvironment.SelectedIndex = 0
            cboIPRating.SelectedIndex = 0
            nudHumidity.Value = 80
            nudTempMin.Value = -10
            nudTempMax.Value = 45

            cboMissionProfile.SelectedIndex = 1
            cboFrameType.SelectedIndex = 0
            cboMotorCount.SelectedIndex = 1
            cboAutonomyLevel.SelectedIndex = 2
            chkMotorRedundancy.Checked = False
            chkGPSRedundancy.Checked = False
            chkBatteryRedundancy.Checked = False
            txtNotes.Clear()

            lblStatus.Text = "Inputs cleared — ready for new mission parameters."
        End Sub

        ' ─────────────────────────────────────────────────────────────────────
        '  PUBLIC HELPER — called by business-logic layer (Task 11)
        '  to update the status bar from any thread.
        ' ─────────────────────────────────────────────────────────────────────

        ''' <summary>
        ''' Thread-safe method to update the status bar label.
        ''' Can be called from background threads (e.g. async selection engine).
        ''' </summary>
        ''' <param name="message">Status message to display.</param>
        Public Sub UpdateStatus(message As String)
            If Me.InvokeRequired Then
                Me.Invoke(Sub() lblStatus.Text = message)
            Else
                lblStatus.Text = message
            End If
        End Sub

        ''' <summary>
        ''' Thread-safe helper to switch to the Output tab.
        ''' Called by the selection engine after populating the grid.
        ''' </summary>
        Public Sub ShowOutputTab()
            If Me.InvokeRequired Then
                Me.Invoke(Sub() tabMain.SelectedTab = tabOutput)
            Else
                tabMain.SelectedTab = tabOutput
            End If
        End Sub

        ''' <summary>
        ''' Updates the summary footer labels.
        ''' Called by the service layer after binding the DataGridView.
        ''' </summary>
        ''' <param name="totalMassG">Sum of component masses in grams.</param>
        ''' <param name="totalPowerW">Sum of peak power consumption in watts.</param>
        ''' <param name="count">Number of components selected.</param>
        Public Sub UpdateSummary(totalMassG As Double, totalPowerW As Double, count As Integer,
                                 Optional budget As PowerBudget = Nothing)
            Dim update As Action = Sub()
                                       lblSummaryMass.Text = $"Total Component Mass:  {totalMassG:N0} g"
                                       lblSummaryPower.Text = $"Total System Power:  {totalPowerW:N1} W"
                                       If budget IsNot Nothing AndAlso budget.TotalMissionEnergyWh > 0 Then
                                           Dim noMarginWh = budget.TotalMissionEnergyWh / Math.Max(0.01, budget.TotalMissionEnergyWh /
                                               (budget.HoverPhaseEnergyWh + budget.ClimbPhaseEnergyWh + budget.CruisePhaseEnergyWh + budget.AvionicsEnergyWh))
                                           lblSummaryBudget.Text =
                                               $"Energy — Hover: {budget.HoverPhaseEnergyWh:N1} Wh  " &
                                               $"Climb: {budget.ClimbPhaseEnergyWh:N1} Wh  " &
                                               $"Cruise: {budget.CruisePhaseEnergyWh:N1} Wh  " &
                                               $"| Total (×k): {budget.TotalMissionEnergyWh:N1} Wh  " &
                                               $"| Range: {budget.EstimatedFlightRangeKm:N1} km"
                                       Else
                                           lblSummaryBudget.Text = $"Component Count:  {count}"
                                       End If
                                   End Sub

            If Me.InvokeRequired Then
                Me.Invoke(update)
            Else
                update()
            End If
        End Sub

    End Class


