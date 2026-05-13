' =============================================================================
' File:    UI/Forms/MainForm.Tests.vb
' Purpose: In-app test harness. Ctrl+Shift+T runs built-in scenarios.
'          Ctrl+Shift+L loads scenarios from CSV.
' =============================================================================

Imports System.Threading.Tasks
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Services

Partial Class MainForm

    Private Enum ScenarioTrack
        MultirotorBlocking = 0
        FixedWingExploratory = 1
    End Enum

    Private Structure TestScenario
        Dim Name As String
        Dim Track As ScenarioTrack
        ' Flight
        Dim EnduranceHr As Decimal
        Dim RangeKm As Decimal
        Dim CruiseSpeedKmh As Decimal
        Dim MaxAltitudeM As Decimal
        Dim MaxWindSpeedKmh As Decimal
        ' Weight
        Dim MtowGrams As Decimal
        Dim PayloadGrams As Decimal
        Dim FrameSizeMm As Decimal
        ' Environment
        Dim OperatingEnvIdx As Integer
        Dim IPRatingIdx As Integer
        Dim TempMinC As Decimal
        Dim TempMaxC As Decimal
        ' Mission
        Dim MissionProfileIdx As Integer
        Dim FrameTypeIdx As Integer
        Dim MotorCountIdx As Integer
        Dim AutonomyIdx As Integer
        Dim PayloadTypeIdx As Integer
    End Structure

    Private Function GetTestScenarios() As List(Of TestScenario)
        Return New List(Of TestScenario) From {
            New TestScenario With {
                .Name = "Light Scout Quad",
                .Track = ScenarioTrack.MultirotorBlocking,
                .EnduranceHr = 0.5D, .RangeKm = 5D, .CruiseSpeedKmh = 30D,
                .MaxAltitudeM = 200D, .MaxWindSpeedKmh = 20D,
                .MtowGrams = 1200D, .PayloadGrams = 100D, .FrameSizeMm = 250D,
                .OperatingEnvIdx = 2, .IPRatingIdx = 0, .TempMinC = -10D, .TempMaxC = 45D,
                .MissionProfileIdx = 8, .FrameTypeIdx = 0, .MotorCountIdx = 1,
                .AutonomyIdx = 2, .PayloadTypeIdx = 0
            },
            New TestScenario With {
                .Name = "Inspection Hex (Camera)",
                .Track = ScenarioTrack.MultirotorBlocking,
                .EnduranceHr = 1.0D, .RangeKm = 10D, .CruiseSpeedKmh = 25D,
                .MaxAltitudeM = 150D, .MaxWindSpeedKmh = 30D,
                .MtowGrams = 4000D, .PayloadGrams = 600D, .FrameSizeMm = 450D,
                .OperatingEnvIdx = 0, .IPRatingIdx = 1, .TempMinC = -5D, .TempMaxC = 50D,
                .MissionProfileIdx = 1, .FrameTypeIdx = 0, .MotorCountIdx = 2,
                .AutonomyIdx = 3, .PayloadTypeIdx = 1
            },
            New TestScenario With {
                .Name = "Package Delivery Quad",
                .Track = ScenarioTrack.MultirotorBlocking,
                .EnduranceHr = 0.4D, .RangeKm = 3D, .CruiseSpeedKmh = 40D,
                .MaxAltitudeM = 120D, .MaxWindSpeedKmh = 25D,
                .MtowGrams = 6000D, .PayloadGrams = 2000D, .FrameSizeMm = 500D,
                .OperatingEnvIdx = 1, .IPRatingIdx = 2, .TempMinC = -10D, .TempMaxC = 45D,
                .MissionProfileIdx = 3, .FrameTypeIdx = 0, .MotorCountIdx = 1,
                .AutonomyIdx = 3, .PayloadTypeIdx = 5
            },
            New TestScenario With {
                .Name = "Agricultural Hex (Sprayer)",
                .Track = ScenarioTrack.MultirotorBlocking,
                .EnduranceHr = 0.5D, .RangeKm = 2D, .CruiseSpeedKmh = 15D,
                .MaxAltitudeM = 50D, .MaxWindSpeedKmh = 15D,
                .MtowGrams = 20000D, .PayloadGrams = 8000D, .FrameSizeMm = 1000D,
                .OperatingEnvIdx = 2, .IPRatingIdx = 3, .TempMinC = 0D, .TempMaxC = 45D,
                .MissionProfileIdx = 6, .FrameTypeIdx = 0, .MotorCountIdx = 2,
                .AutonomyIdx = 2, .PayloadTypeIdx = 6
            },
            New TestScenario With {
                .Name = "BVLOS Fixed Wing",
                .Track = ScenarioTrack.FixedWingExploratory,
                .EnduranceHr = 3.0D, .RangeKm = 80D, .CruiseSpeedKmh = 80D,
                .MaxAltitudeM = 500D, .MaxWindSpeedKmh = 40D,
                .MtowGrams = 9000D, .PayloadGrams = 500D, .FrameSizeMm = 1500D,
                .OperatingEnvIdx = 2, .IPRatingIdx = 0, .TempMinC = -15D, .TempMaxC = 50D,
                .MissionProfileIdx = 7, .FrameTypeIdx = 1, .MotorCountIdx = 5,
                .AutonomyIdx = 4, .PayloadTypeIdx = 2
            },
            New TestScenario With {
                .Name = "Maritime SAR Octo (IP67)",
                .Track = ScenarioTrack.MultirotorBlocking,
                .EnduranceHr = 1.5D, .RangeKm = 15D, .CruiseSpeedKmh = 35D,
                .MaxAltitudeM = 300D, .MaxWindSpeedKmh = 50D,
                .MtowGrams = 12000D, .PayloadGrams = 2000D, .FrameSizeMm = 700D,
                .OperatingEnvIdx = 4, .IPRatingIdx = 4, .TempMinC = -20D, .TempMaxC = 40D,
                .MissionProfileIdx = 2, .FrameTypeIdx = 0, .MotorCountIdx = 3,
                .AutonomyIdx = 3, .PayloadTypeIdx = 3
            }
        }
    End Function

    Friend Sub WireTestControls()
        Me.KeyPreview = True
        AddHandler Me.KeyDown, AddressOf OnKeyDown_Tests
    End Sub

    Private Sub OnKeyDown_Tests(sender As Object, e As KeyEventArgs)
        If e.Control AndAlso e.Shift AndAlso e.KeyCode = Keys.T Then
            e.SuppressKeyPress = True
            RunTestsAsync(Nothing)
        ElseIf e.Control AndAlso e.Shift AndAlso e.KeyCode = Keys.L Then
            e.SuppressKeyPress = True
            LoadAndRunCsvAsync()
        End If
    End Sub

    Private Async Sub LoadAndRunCsvAsync()
        If _engine Is Nothing Then
            MessageBox.Show("Engine not loaded - tests cannot run.", "Test Harness", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Using dlg As New OpenFileDialog() With {
            .Title = "Load Test Scenarios CSV",
            .Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            .FilterIndex = 1
        }
            If dlg.ShowDialog() <> DialogResult.OK Then Return
            Try
                Dim scenarios = ParseCsvScenarios(dlg.FileName)
                If scenarios.Count = 0 Then
                    MessageBox.Show("No valid scenarios found in the CSV file.", "Load CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                RunTestsAsync(scenarios)
            Catch ex As Exception
                MessageBox.Show($"Failed to read CSV:{Environment.NewLine}{ex.Message}", "Load CSV", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ' Expected header:
    ' Name,EnduranceHr,RangeKm,CruiseSpeedKmh,MaxAltitudeM,MaxWindSpeedKmh,
    ' MtowGrams,PayloadGrams,FrameSizeMm,OperatingEnvIdx,IPRatingIdx,TempMinC,TempMaxC,
    ' MissionProfileIdx,FrameTypeIdx,MotorCountIdx,AutonomyIdx,PayloadTypeIdx[,Track]
    ' Track values: blocking (default), exploratory
    Private Shared Function ParseCsvScenarios(filePath As String) As List(Of TestScenario)
        Dim scenarios As New List(Of TestScenario)()
        Dim lines = System.IO.File.ReadAllLines(filePath)
        If lines.Length < 2 Then Return scenarios

        For i As Integer = 1 To lines.Length - 1
            Dim line = lines(i).Trim()
            If String.IsNullOrWhiteSpace(line) OrElse line.StartsWith("#") Then Continue For
            Dim cols() As String = line.Split(","c)
            If cols.Length < 18 Then Continue For

            Try
                Dim s As New TestScenario With {
                    .Name = cols(0).Trim(),
                    .EnduranceHr = Decimal.Parse(cols(1).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .RangeKm = Decimal.Parse(cols(2).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .CruiseSpeedKmh = Decimal.Parse(cols(3).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .MaxAltitudeM = Decimal.Parse(cols(4).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .MaxWindSpeedKmh = Decimal.Parse(cols(5).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .MtowGrams = Decimal.Parse(cols(6).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .PayloadGrams = Decimal.Parse(cols(7).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .FrameSizeMm = Decimal.Parse(cols(8).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .OperatingEnvIdx = Integer.Parse(cols(9).Trim()),
                    .IPRatingIdx = Integer.Parse(cols(10).Trim()),
                    .TempMinC = Decimal.Parse(cols(11).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .TempMaxC = Decimal.Parse(cols(12).Trim(), Globalization.CultureInfo.InvariantCulture),
                    .MissionProfileIdx = Integer.Parse(cols(13).Trim()),
                    .FrameTypeIdx = Integer.Parse(cols(14).Trim()),
                    .MotorCountIdx = Integer.Parse(cols(15).Trim()),
                    .AutonomyIdx = Integer.Parse(cols(16).Trim()),
                    .PayloadTypeIdx = Integer.Parse(cols(17).Trim()),
                    .Track = ScenarioTrack.MultirotorBlocking
                }

                If cols.Length >= 19 Then
                    Dim trackText As String = cols(18).Trim().ToLowerInvariant()
                    If trackText = "exploratory" OrElse trackText = "fixedwing" Then
                        s.Track = ScenarioTrack.FixedWingExploratory
                    End If
                ElseIf s.FrameTypeIdx = 1 Then
                    s.Track = ScenarioTrack.FixedWingExploratory
                End If

                scenarios.Add(s)
            Catch
                ' Skip malformed rows silently.
            End Try
        Next

        Return scenarios
    End Function

    Private Async Sub RunTestsAsync(scenariosOverride As List(Of TestScenario))
        If _engine Is Nothing Then
            MessageBox.Show("Engine not loaded - tests cannot run.", "Test Harness", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim scenarios = If(scenariosOverride, GetTestScenarios())
        Dim results As New System.Text.StringBuilder()
        Dim runStamp As String = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        results.AppendLine($"Drone Designer - Test Run ({runStamp})")
        results.AppendLine($"Scenarios: {scenarios.Count}")
        results.AppendLine(New String("="c, 72))
        results.AppendLine()

        Dim passed As Integer = 0
        Dim failed As Integer = 0
        Dim blockingPassed As Integer = 0
        Dim blockingFailed As Integer = 0
        Dim exploratoryPassed As Integer = 0
        Dim exploratoryFailed As Integer = 0

        For Each s In scenarios
            results.AppendLine($"-> {s.Name} [{GetTrackLabel(s.Track)}]")
            Try
                LoadScenario(s)
                Dim specs As MissionSpecs = BuildMissionSpecs()
                Dim result As SelectionResult = Await Task.Run(Function() _engine.SelectComponents(specs))

                Dim motorName As String = If(result.SelectedMotors?.FirstOrDefault()?.ModelName, "-")
                Dim batName As String = If(result.SelectedBatteries?.FirstOrDefault()?.ModelName, "-")
                Dim escName As String = If(result.SelectedEscs?.FirstOrDefault()?.ModelName, "-")

                results.AppendLine($"   PASS  MTOW={result.EstimatedMtowGrams:N0} g  Motors={result.SelectedMotors?.Count}  Props={result.SelectedPropellers?.Count}  Bats={result.SelectedBatteries?.Count}")
                results.AppendLine($"         Motor:   {motorName}")
                results.AppendLine($"         Battery: {batName}")
                results.AppendLine($"         ESC:     {escName}")

                If result.Warnings IsNot Nothing AndAlso result.Warnings.Count > 0 Then
                    For Each w In result.Warnings
                        results.AppendLine($"         ! {w}")
                    Next
                End If

                passed += 1
                If s.Track = ScenarioTrack.MultirotorBlocking Then
                    blockingPassed += 1
                Else
                    exploratoryPassed += 1
                End If
            Catch ex As ComponentSelectionException
                results.AppendLine($"   FAIL  {ex.Message}")
                failed += 1
                If s.Track = ScenarioTrack.MultirotorBlocking Then
                    blockingFailed += 1
                Else
                    exploratoryFailed += 1
                End If
            Catch ex As Exception
                results.AppendLine($"   ERROR {ex.GetType().Name}: {ex.Message}")
                failed += 1
                If s.Track = ScenarioTrack.MultirotorBlocking Then
                    blockingFailed += 1
                Else
                    exploratoryFailed += 1
                End If
            End Try

            results.AppendLine()
        Next

        results.AppendLine(New String("-"c, 72))
        results.AppendLine($"Results: {passed} passed, {failed} failed out of {scenarios.Count} scenarios.")
        results.AppendLine($"Blocking (multirotor): {blockingPassed} passed, {blockingFailed} failed.")
        results.AppendLine($"Exploratory (fixed-wing): {exploratoryPassed} passed, {exploratoryFailed} failed.")

        If blockingFailed > 0 Then
            results.AppendLine("Interpretation: blocking failures require regression action.")
        Else
            results.AppendLine("Interpretation: no blocking regressions detected.")
        End If

        If exploratoryFailed > 0 Then
            results.AppendLine("Interpretation: exploratory failures are tracked but non-blocking.")
        End If

        Dim logPath As String = SaveTestLog(results.ToString())
        If logPath IsNot Nothing Then
            results.AppendLine()
            results.AppendLine($"Log saved -> {logPath}")
        End If

        ShowTestResults(results.ToString())
    End Sub

    Private Shared Function GetTrackLabel(track As ScenarioTrack) As String
        If track = ScenarioTrack.FixedWingExploratory Then
            Return "exploratory"
        End If
        Return "blocking"
    End Function

    Private Shared Function SaveTestLog(text As String) As String
        Try
            Dim exeDir As String = AppDomain.CurrentDomain.BaseDirectory
            Dim projectRoot As String = System.IO.Path.GetFullPath(System.IO.Path.Combine(exeDir, "..", ".."))
            Dim logDir As String = System.IO.Path.Combine(projectRoot, "Test", "Logs")
            System.IO.Directory.CreateDirectory(logDir)

            Dim fileName As String = $"testrun_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            Dim filePath As String = System.IO.Path.Combine(logDir, fileName)
            System.IO.File.WriteAllText(filePath, text, System.Text.Encoding.UTF8)
            Return filePath
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub LoadScenario(s As TestScenario)
        SetNud(nudEndurance, s.EnduranceHr)
        SetNud(nudRange, s.RangeKm)
        SetNud(nudCruiseSpeed, s.CruiseSpeedKmh)
        SetNud(nudMaxAltitude, s.MaxAltitudeM)
        SetNud(nudMaxWindSpeed, s.MaxWindSpeedKmh)
        SetNud(nudMaxTakeoffWeight, s.MtowGrams)
        SetNud(nudPayloadWeight, s.PayloadGrams)
        SetNud(nudFrameSize, s.FrameSizeMm)
        SetNud(nudTempMin, s.TempMinC)
        SetNud(nudTempMax, s.TempMaxC)
        SetCbo(cboOperatingEnvironment, s.OperatingEnvIdx)
        SetCbo(cboIPRating, s.IPRatingIdx)
        SetCbo(cboMissionProfile, s.MissionProfileIdx)
        SetCbo(cboFrameType, s.FrameTypeIdx)
        SetCbo(cboMotorCount, s.MotorCountIdx)
        SetCbo(cboAutonomyLevel, s.AutonomyIdx)
        SetCbo(cboPayloadType, s.PayloadTypeIdx)
    End Sub

    Private Shared Sub SetNud(nud As NumericUpDown, value As Decimal)
        nud.Value = Math.Max(nud.Minimum, Math.Min(nud.Maximum, value))
    End Sub

    Private Shared Sub SetCbo(cbo As ComboBox, index As Integer)
        If index >= 0 AndAlso index < cbo.Items.Count Then
            cbo.SelectedIndex = index
        End If
    End Sub

    Private Shared Sub ShowTestResults(text As String)
        Dim frm As New Form() With {
            .Text = "Test Results - Drone Designer",
            .Size = New Size(760, 540),
            .StartPosition = FormStartPosition.CenterScreen,
            .FormBorderStyle = FormBorderStyle.Sizable,
            .MinimumSize = New Size(500, 300)
        }

        Dim rtb As New RichTextBox() With {
            .Dock = DockStyle.Fill,
            .ReadOnly = True,
            .Font = New Font("Consolas", 9.5F),
            .BackColor = Color.FromArgb(22, 22, 30),
            .ForeColor = Color.FromArgb(220, 220, 230),
            .BorderStyle = BorderStyle.None,
            .Text = text
        }

        ColourTestOutput(rtb)
        frm.Controls.Add(rtb)
        frm.Show()
    End Sub

    Private Shared Sub ColourTestOutput(rtb As RichTextBox)
        Dim lines() As String = rtb.Text.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
        rtb.Clear()

        For Each line As String In lines
            Dim colour As Color
            If line.Contains("PASS") Then
                colour = Color.FromArgb(100, 220, 120)
            ElseIf line.Contains("FAIL") OrElse line.Contains("ERROR") Then
                colour = Color.FromArgb(230, 100, 100)
            ElseIf line.StartsWith("   !") Then
                colour = Color.FromArgb(255, 200, 80)
            ElseIf line.StartsWith("->") Then
                colour = Color.FromArgb(130, 180, 255)
            ElseIf line.StartsWith("Results:") OrElse line.StartsWith("Blocking (") OrElse line.StartsWith("Exploratory (") Then
                colour = Color.FromArgb(255, 220, 100)
            Else
                colour = Color.FromArgb(200, 200, 210)
            End If

            rtb.SelectionStart = rtb.TextLength
            rtb.SelectionLength = 0
            rtb.SelectionColor = colour
            rtb.AppendText(line & Environment.NewLine)
        Next
    End Sub

End Class
