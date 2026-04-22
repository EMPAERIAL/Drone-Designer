' =============================================================================
' File:    UI/Forms/MainForm.CAD.vb
' Project: Drone Designer
' Task:    15 — End-to-End Pipeline
'
' Purpose: Partial class for MainForm — contains everything related to the
'          "Generate CAD" button (Module 2 pipeline).
'
'          Keeps the CAD wiring isolated from Module 1 wiring (MainForm.Logic.vb)
'          so the two halves can be edited independently without merge conflicts.
'
' Prerequisites in MainForm.vb (Task 10):
'   • btnSendToSolidWorks — Button already declared and placed in the output tab.
'   • lblStatus            — StatusStrip label for progress messages.
'   • _lastResult — Field holding the last SelectionResult (added below
'                            if not already present in MainForm.Logic.vb).
'
' How it works:
'   1. User clicks "Generate CAD".
'   2. This file validates that a component selection has been run first.
'   3. User picks an output folder via FolderBrowserDialog.
'   4. CadProgressForm is shown (modeless) with a Cancel button.
'   5. PipelineOrchestrator.RunFromSelectionAsync runs on a background task.
'   6. Progress reports stream into CadProgressForm via IProgress.
'   7. When done, the result is shown in CadProgressForm and the status strip
'      is updated.
'
' .NET Framework 4.7.2 / VB.NET / WinForms
' =============================================================================

Imports System
Imports System.IO
Imports System.Threading
Imports System.Windows.Forms
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Services
Imports Drone_Designer.Drone_Designer.Core.Models
Imports Drone_Designer.Drone_Designer.Core.Services
Imports Drone_Designer.Drone_Designer.SolidWorks
Imports Drone_Designer.Drone_Designer.UI.Forms
Imports Drone_Designer.SolidWorks
Imports Drone_Designer.Utilities



Partial Public Class MainForm

    ' ------------------------------------------------------------------
    ' Module-level state for the SolidWorks pipeline
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Lazily created orchestrator.  Reused across runs so SolidWorks
    ''' stays connected between multiple Generate CAD calls in the
    ''' same session.
    ''' </summary>
    Private _orchestrator As PipelineOrchestrator

    ''' <summary>
    ''' Connection manager — held at form level so the status label can
    ''' show SW version on connect.
    ''' </summary>
    Private _swAutomation As SolidWorksAutomation

    Private _selectionEngine As ComponentSelectionEngine

    ''' <summary>
    ''' Last directory chosen by the user.  Remembered for the duration
    ''' of the session so the picker opens in the same place.
    ''' </summary>
    Private _lastOutputDirectory As String = String.Empty

    ''' <summary>
    ''' True while the pipeline is running — disables both action buttons.
    ''' </summary>
    Private _pipelineRunning As Boolean = False

    ' NOTE: _lastResult is declared in MainForm.Logic.vb (Task 11).
    ' If for any reason it wasn't added there, uncomment this line:
    ' Private _lastResult As SelectionResult = Nothing

    ' ------------------------------------------------------------------
    ' Form-load hook — wire the Generate CAD button
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Call this from MainForm_Load (in MainForm.Logic.vb) to attach
    ''' all CAD-related event handlers.  Kept separate so Task 11 code
    ''' doesn't need to be modified.
    ''' </summary>
    Private Sub WireCADControls()
        AddHandler btnSendToSolidWorks.Click, AddressOf btnGenerateCAD_Click

        ' Wire the SolidWorks status check menu item if it exists
        ' (optional — gracefully skipped if the control isn't present)
        'Dim swStatusItem = TryCast(Me.Controls.Find("mnuCheckSolidWorks",
        'searchAllChildren:=True).
        'FirstOrDefault(), ToolStripMenuItem)
        'If swStatusItem IsNot Nothing Then
        'Ad'dHandler swStatusItem.Click, AddressOf CheckSolidWorksStatus_Click
        'End If

        ' Update button tooltip
        btnSendToSolidWorks.Text = "Generate CAD ▶"
        If TypeOf btnSendToSolidWorks Is Button Then
            Dim tip As New ToolTip()
            tip.SetToolTip(btnSendToSolidWorks,
                               "Select components first, then click to generate SolidWorks parts.")
        End If

        UpdateCADButtonState()
    End Sub

    ' ------------------------------------------------------------------
    ' Generate CAD button click handler
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Entry point for the end-to-end pipeline.
    ''' Validates state, picks output folder, then runs the pipeline async.
    ''' </summary>
    Private Async Sub btnGenerateCAD_Click(sender As Object, e As EventArgs)

        ' --- Guard: must have run component selection first ---
        If _lastResult Is Nothing OrElse
               _lastResult.SelectedMotors Is Nothing OrElse
               _lastResult.SelectedMotors.Count = 0 Then

            MessageBox.Show(
                    "Please run ""Select Components"" first." & Environment.NewLine &
                    "The component selection provides the motor dimensions needed " &
                    "to generate the motor-mount CAD model.",
                    "No Components Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            Return
        End If

        ' --- Guard: don't start a second run while one is in progress ---
        If _pipelineRunning Then Return

        ' --- Pick output directory ---
        Dim outputDir As String = PickOutputDirectory()
        If String.IsNullOrEmpty(outputDir) Then Return   ' user cancelled folder picker

        ' --- Get current mission specs from the form (for manifest writing) ---
        Dim specs As MissionSpecs = TryBuildMissionSpecs()
        If specs Is Nothing Then
            MessageBox.Show("Could not read mission specs from the form. " &
                                "Please check your inputs.",
                                "Input Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning)
            Return
        End If

        ' --- Initialise services (lazy — only on first use) ---
        If Not EnsureOrchestratorInitialised() Then Return

        ' --- Lock the UI ---
        SetPipelineRunning(True)

        ' --- Create CancellationTokenSource and progress form ---
        Dim cts As New CancellationTokenSource()
        Dim progressForm As New CadProgressForm(cts)

        ' Handle the PipelineCompleted event to update the main form status bar
        'AddHandler progressForm.PipelineCompleted, AddressOf OnPipelineCompleted

        progressForm.Show(Me)

        Try
            ' --- Run the pipeline ---
            ' RunFromSelectionAsync skips re-running the selection engine
            ' because the user already did that via the "Select Components" button.
            Dim result As PipelineResult = Await _orchestrator.RunFromSelectionAsync(
                    _lastResult,
                    specs,
                    outputDir,
                    progressForm.Progress,
                    cts.Token)

            ' --- Update progress form with the final result ---
            progressForm.MarkComplete(result)

            ' --- Update main form ---
            If result.Success Then
                UpdateStatus($"✓  {result.SuccessfulPartCount} CAD part(s) generated — {outputDir}")
                _lastOutputDirectory = outputDir

                ' Offer to open the output folder in Explorer
                Dim answer = MessageBox.Show(
        $"{result.SuccessfulPartCount} part(s) were saved to:" &
        Environment.NewLine & Environment.NewLine &
        result.OutputDirectory & Environment.NewLine & Environment.NewLine &
        "Open the folder now?",
        "CAD Generation Complete",
        MessageBoxButtons.YesNo,
        MessageBoxIcon.Information)

                If answer = DialogResult.Yes Then
                    Try
                        System.Diagnostics.Process.Start("explorer.exe", result.OutputDirectory)
                    Catch
                        ' Explorer failed to open — not critical
                    End Try
                End If

            Else
                UpdateStatus($"✗  CAD generation failed: {result.ErrorMessage}")

                ' Only show a second error dialog if it wasn't a user cancellation
                If Not result.ErrorMessage.Contains("cancelled") Then
                    MessageBox.Show(
            "CAD generation failed." & Environment.NewLine & Environment.NewLine &
            result.ErrorMessage,
            "Generation Failed",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error)
                End If
            End If

        Catch ex As Exception
            ' Should not reach here — RunFromSelectionAsync never throws.
            ' Belt-and-suspenders for unexpected runtime errors.
            progressForm.MarkComplete(
                    New PipelineResult($"Unexpected error: {ex.Message}",
                                       New List(Of GeneratedPartRecord)(),
                                       DateTime.UtcNow))
            UpdateStatus($"✗  Unexpected error: {ex.Message}")

        Finally
            SetPipelineRunning(False)
            cts.Dispose()
            ' Don't dispose progressForm here — it's shown and the user needs to
            ' read the results.  It disposes itself when the user clicks Close.
        End Try

    End Sub

    ' ------------------------------------------------------------------
    ' PipelineCompleted event handler
    ' (fires when the user clicks Close on CadProgressForm)
    ' ------------------------------------------------------------------

    'Private Sub OnPipelineCompleted(sender As Object, e As PipelineCompletedEventArgs)
    'Dim result = e.Result
    'If result.Success Then
    ' Offer to open the output folder in Explorer
    'Dim answer = MessageBox.Show(
    '           $"{result.SuccessfulPartCount} part(s) were saved to:" &
    '           Environment.NewLine & Environment.NewLine &
    '            result.OutputDirectory & Environment.NewLine & Environment.NewLine &
    '            "Open the folder now?",
    '            "CAD Generation Complete",
    '            MessageBoxButtons.YesNo,
    '            MessageBoxIcon.Information)

    ' If answer = DialogResult.Yes Then
    'Try
    '                System.Diagnostics.Process.Start("explorer.exe", result.OutputDirectory)
    'Catch
    ' Explorer failed to open — not critical
    'End Try
    'End If
    'Else
    ' Only show a second error dialog if the error wasn't a cancellation
    'If Not result.ErrorMessage.Contains("cancelled") Then
    '            MessageBox.Show(
    '               "CAD generation failed." & Environment.NewLine & Environment.NewLine &
    '               result.ErrorMessage,
    '              "Generation Failed",
    '             MessageBoxButtons.OK,
    '            MessageBoxIcon.Error)
    'End If
    'End If
    'End Sub

    ' ------------------------------------------------------------------
    ' Optional: SolidWorks connection status menu item
    ' ------------------------------------------------------------------

    Private Async Sub CheckSolidWorksStatus_Click(sender As Object, e As EventArgs)
        UpdateStatus("Checking SolidWorks connection…")

        If Not EnsureOrchestratorInitialised() Then Return

        Dim connected As Boolean = Await Task.Run(Function() _swAutomation.IsConnected())
        If connected Then
            Dim ver = Await Task.Run(Function() _swAutomation.GetVersion())
            UpdateStatus($"SolidWorks connected — {ver}")
            MessageBox.Show($"SolidWorks is connected and running." &
                                Environment.NewLine & $"Version: {ver}",
                                "SolidWorks Status",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information)
        Else
            UpdateStatus("SolidWorks is not connected.")
            MessageBox.Show(
                    "SolidWorks is not currently connected." & Environment.NewLine &
                    "It will be launched automatically when you click ""Generate CAD"".",
                    "SolidWorks Status",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
        End If
    End Sub

    ' ------------------------------------------------------------------
    ' Private helpers
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' Shows a FolderBrowserDialog and returns the chosen path.
    ''' Returns Nothing if the user cancels.
    ''' </summary>
    Private Function PickOutputDirectory() As String
        Using dlg As New FolderBrowserDialog()
            dlg.Description = "Select the folder where SolidWorks parts will be saved:"
            dlg.ShowNewFolderButton = True

            ' Default to last used directory, or Desktop
            If Not String.IsNullOrEmpty(_lastOutputDirectory) AndAlso
                   Directory.Exists(_lastOutputDirectory) Then
                dlg.SelectedPath = _lastOutputDirectory
            Else
                dlg.SelectedPath = Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop)
            End If

            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Return dlg.SelectedPath
            End If
        End Using
        Return Nothing
    End Function

    ''' <summary>
    ''' Lazy-initialises <see cref="_swAutomation"/> and <see cref="_orchestrator"/>.
    ''' Returns False and shows an error dialog if initialisation fails.
    ''' </summary>
    Private Function EnsureOrchestratorInitialised() As Boolean
        Try
            If _swAutomation Is Nothing Then
                _swAutomation = New SolidWorksAutomation()
            End If
            If _selectionEngine Is Nothing Then
                Dim repo As New ComponentRepository()
                _selectionEngine = New ComponentSelectionEngine(repo)
            End If

            If _orchestrator Is Nothing Then

                ' Connect first so _swAutomation.Application is live
                ' before MacroRunner is constructed.
                Dim connectError As String = String.Empty
                If Not _swAutomation.Connect(connectError) Then
                    Throw New Exception("Could not connect to SolidWorks: " & connectError)
                End If

                Dim macroRunner As New MacroRunner(_swAutomation.Application)
                _orchestrator = New PipelineOrchestrator(
            _selectionEngine,
            _swAutomation,
            macroRunner,
            logger:=AddressOf LogToStatusStrip)
            End If

            Return True

            Catch ex As Exception
                MessageBox.Show(
                    "Failed to initialise the SolidWorks pipeline." &
                    Environment.NewLine & Environment.NewLine &
                    ex.Message,
                    "Initialisation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Reads form controls into a MissionSpecs object for manifest writing.
        ''' Delegates to the existing BuildMissionSpecs method in MainForm.Logic.vb.
        ''' Returns Nothing on validation failure.
        ''' </summary>
        Private Function TryBuildMissionSpecs() As MissionSpecs
            Try
                ' BuildMissionSpecs is defined in MainForm.Logic.vb (Task 11).
                Return BuildMissionSpecs()
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Enables/disables the Generate CAD button based on current application state.
        ''' </summary>
        Private Sub UpdateCADButtonState()
        Dim hasSelection As Boolean = _lastResult IsNot Nothing AndAlso
                                          _lastResult.SelectedMotors IsNot Nothing AndAlso
                                          _lastResult.SelectedMotors.Count > 0

        btnSendToSolidWorks.Enabled = hasSelection AndAlso Not _pipelineRunning

            If _pipelineRunning Then
                btnSendToSolidWorks.Text = "⏳ Generating…"
            ElseIf hasSelection Then
                btnSendToSolidWorks.Text = "Generate CAD ▶"
            Else
                btnSendToSolidWorks.Text = "Generate CAD (select components first)"
            End If
        End Sub

        ''' <summary>
        ''' Locks or unlocks the UI action buttons during pipeline execution.
        ''' </summary>
        Private Sub SetPipelineRunning(running As Boolean)
            _pipelineRunning = running
            UpdateCADButtonState()

            ' Also disable the Select Components button to prevent a mid-run
            ' re-selection from invalidating the data the pipeline is processing.
            If btnSelectComponents IsNot Nothing Then
                btnSelectComponents.Enabled = Not running
            End If

            ' Update cursor
            Me.Cursor = If(running, Cursors.WaitCursor, Cursors.Default)
        End Sub

        ''' <summary>
        ''' Thin logging delegate passed to PipelineOrchestrator.
        ''' Appends messages to the status strip — useful during development.
        ''' </summary>
        Private Sub LogToStatusStrip(message As String)
            ' Marshal to UI thread if called from background
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of String)(AddressOf LogToStatusStrip), message)
                Return
            End If
            System.Diagnostics.Debug.WriteLine($"[Pipeline] {message}")
            ' Optionally also update the status strip:
            ' UpdateStatus(message)
        End Sub

    End Class
