' =============================================================================
' File:    Core/Services/PipelineOrchestrator.vb
' Project: Drone Designer
' Task:    15 — End-to-End Pipeline
'
' Purpose: Coordinates the complete end-to-end workflow:
'             MissionSpecs → ComponentSelectionEngine
'                          → SolidWorksAutomation (connect)
'                          → MacroRunner (generate motor mounts)
'                          → Save parts + write manifest
'
' Design principles:
'   • Async throughout — UI thread never blocks.
'   • IProgress(Of PipelineProgressReport) for all status updates —
'     the orchestrator knows nothing about WinForms controls.
'   • CancellationToken supported — the UI Cancel button hooks in here.
'   • Partial-failure tolerant — if motor 2 of 4 fails the macro, the
'     remaining motors are still attempted and the result shows which
'     parts succeeded and which failed.
'   • No direct SolidWorks references — consumes SolidWorksAutomation
'     and MacroRunner through their public interfaces so this class can
'     be unit-tested without SolidWorks installed.
'
' Dependencies:
'   Core/Models/MissionSpecs.vb
'   Core/Models/ComponentSpecs.vb
'   Core/Models/PipelineResult.vb        (Task 15)
'   Core/Interfaces/IComponentSelector.vb
'   Core/Services/ComponentSelectionEngine.vb
'   SolidWorks/SolidWorksAutomation.vb   (Task 12 + Task 14)
'   SolidWorks/MacroRunner.vb            (Task 13)
'   Utilities/ConfigManager.vb
'
' .NET Framework 4.7.2 — required for SolidWorks COM interop compatibility.
' =============================================================================

Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Drone_Designer.Core.Interfaces
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Services
Imports Drone_Designer.Drone_Designer.Core.Models
Imports Drone_Designer.Drone_Designer.SolidWorks
Imports Drone_Designer.SolidWorks
Imports Drone_Designer.Utilities

Namespace Drone_Designer.Core.Services

    ''' <summary>
    ''' Orchestrates the full UAV design pipeline from mission specs through
    ''' to generated SolidWorks parts.
    ''' <para>
    ''' Instantiate once per application session and call
    ''' <see cref="RunAsync"/> for each design run.
    ''' </para>
    ''' </summary>
    Public Class PipelineOrchestrator

        ' ------------------------------------------------------------------
        ' Constants — stage weight table drives percentage calculation
        ' ------------------------------------------------------------------

        ' Each stage is assigned a weight (out of 100) that approximates its
        ' real-world duration share.  Must sum to 100.
        Private ReadOnly _stageWeights As New Dictionary(Of PipelineStage, Integer) From {
            {PipelineStage.Validating, 5},
            {PipelineStage.SelectingComponents, 10},
            {PipelineStage.ConnectingToSolidWorks, 15},
            {PipelineStage.GeneratingPart, 60},  ' divided across N parts
            {PipelineStage.SavingFile, 5},  ' rolled into GeneratingPart sub-steps
            {PipelineStage.WritingManifest, 3},
            {PipelineStage.Finalising, 2}
        }

        ' ------------------------------------------------------------------
        ' Fields
        ' ------------------------------------------------------------------

        Private ReadOnly _selector As IComponentSelector
        Private ReadOnly _swAutomation As SolidWorksAutomation
        Private ReadOnly _macroRunner As MacroRunner
        Private ReadOnly _config As ConfigManager
        Private ReadOnly _logger As Action(Of String)      ' thin logging hook

        ''' <summary>Path to the motor mount macro file (.swb).</summary>
        Private ReadOnly _motorMountMacroPath As String

        ' ------------------------------------------------------------------
        ' Constructor
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Initialises the orchestrator with all required services.
        ''' </summary>
        ''' <param name="selector">Component selection engine instance.</param>
        ''' <param name="swAutomation">SolidWorks connection manager (Task 12/14).</param>
        ''' <param name="macroRunner">Macro execution runner (Task 13).</param>
        ''' <param name="logger">
        ''' Optional delegate for log messages. Pass <c>AddressOf Debug.WriteLine</c>
        ''' during development, or wire to a real logger later.
        ''' </param>
        Public Sub New(selector As IComponentSelector,
                       swAutomation As SolidWorksAutomation,
                       macroRunner As MacroRunner,
                       Optional logger As Action(Of String) = Nothing)

            If selector Is Nothing Then Throw New ArgumentNullException(NameOf(selector))
            If swAutomation Is Nothing Then Throw New ArgumentNullException(NameOf(swAutomation))
            If macroRunner Is Nothing Then Throw New ArgumentNullException(NameOf(macroRunner))

            _selector = selector
            _swAutomation = swAutomation
            _macroRunner = macroRunner
            _logger = If(logger, Sub(msg) System.Diagnostics.Debug.WriteLine($"[Pipeline] {msg}"))

            ' Macro path read from config; fall back to a path relative to the executable.
            _motorMountMacroPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Resources", "Macros", "MotorMount.swb")
        End Sub

        ' ------------------------------------------------------------------
        ' Public API
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Runs the full pipeline asynchronously:
        ''' <list type="number">
        '''   <item>Validates inputs.</item>
        '''   <item>Runs component selection on the given specs.</item>
        '''   <item>Connects to SolidWorks.</item>
        '''   <item>Generates a motor-mount part for each selected motor.</item>
        '''   <item>Saves parts and writes a component manifest.</item>
        ''' </list>
        ''' </summary>
        ''' <param name="specs">Mission specifications from the UI input form.</param>
        ''' <param name="outputDirectory">
        ''' Folder where CAD parts and the manifest will be saved.
        ''' Created if it does not exist.
        ''' </param>
        ''' <param name="progress">
        ''' Receiver for incremental progress updates.  All callbacks are marshalled
        ''' to whichever thread owns this <see cref="IProgress(Of T)"/> — normally
        ''' the UI thread if constructed with <c>New Progress(Of ...)(callback)</c>
        ''' on the UI thread.
        ''' </param>
        ''' <param name="cancellationToken">
        ''' Token connected to a Cancel button on the progress form.
        ''' The pipeline checks this before each major step and after each part.
        ''' </param>
        ''' <returns>
        ''' A <see cref="PipelineResult"/> describing what was generated.
        ''' Never throws — errors are captured into the result object.
        ''' </returns>
        Public Async Function RunAsync(
            specs As MissionSpecs,
            outputDirectory As String,
            progress As IProgress(Of PipelineProgressReport),
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of PipelineResult)

            Dim startedAt As DateTime = DateTime.UtcNow
            Dim parts As New List(Of GeneratedPartRecord)
            Dim swWasConnectedBeforeRun As Boolean = _swAutomation.IsConnected()

            Try
                ' ----------------------------------------------------------------
                ' STAGE 1 — Validate
                ' ----------------------------------------------------------------
                Report(progress, PipelineStage.Validating, 2, "Validating inputs…")
                cancellationToken.ThrowIfCancellationRequested()

                Dim validationError As String = ValidateInputs(specs, outputDirectory)
                If validationError IsNot Nothing Then
                    Report(progress, PipelineStage.Failed, 2, "Validation failed", validationError)
                    Return New PipelineResult(validationError, parts, startedAt)
                End If

                ' Ensure output directory exists
                Directory.CreateDirectory(outputDirectory)
                _logger($"Output directory: {outputDirectory}")

                ' ----------------------------------------------------------------
                ' STAGE 2 — Component selection
                ' ----------------------------------------------------------------
                Report(progress, PipelineStage.SelectingComponents, 5,
                       "Running component selection engine…")
                cancellationToken.ThrowIfCancellationRequested()

                Dim selectionResult As SelectionResult = Await Task.Run(
                    Function() _selector.SelectComponents(specs), cancellationToken)

                If selectionResult Is Nothing OrElse selectionResult.SelectedMotors Is Nothing OrElse
                   selectionResult.SelectedMotors.Count = 0 Then
                    Const msg = "Component selection returned no motors. " &
                                "Adjust mission specs (payload, endurance, or altitude) and retry."
                    Report(progress, PipelineStage.Failed, 15, "Selection failed", msg)
                    Return New PipelineResult(msg, parts, startedAt)
                End If

                _logger($"Selection engine returned {selectionResult.SelectedMotors.Count} motor(s).")
                Report(progress, PipelineStage.SelectingComponents, 15,
                       "Components selected",
                       $"{selectionResult.SelectedMotors.Count} motor(s) identified")

                cancellationToken.ThrowIfCancellationRequested()

                ' ----------------------------------------------------------------
                ' STAGE 3 — Connect to SolidWorks
                ' ----------------------------------------------------------------
                Report(progress, PipelineStage.ConnectingToSolidWorks, 18,
                       "Connecting to SolidWorks…",
                       "This may take 20–30 seconds if SolidWorks needs to launch.")

                Dim swErrorMsg As String = Nothing
                Dim swConnected As Boolean = Await Task.Run(
                    Function() _swAutomation.Connect(swErrorMsg), cancellationToken)

                If Not swConnected Then
                    Const prefix = "Could not connect to SolidWorks: "
                    Dim msg = prefix & If(swErrorMsg, "Unknown error.")
                    Report(progress, PipelineStage.Failed, 18, "SolidWorks connection failed", msg)
                    Return New PipelineResult(msg, parts, startedAt)
                End If

                Dim swVersion As String = _swAutomation.GetVersion()
                _logger($"Connected to SolidWorks {swVersion}.")
                Report(progress, PipelineStage.ConnectingToSolidWorks, 30,
                       $"Connected to SolidWorks {swVersion}")
                cancellationToken.ThrowIfCancellationRequested()

                ' ----------------------------------------------------------------
                ' STAGE 4 — Generate motor mounts (one per selected motor)
                ' ----------------------------------------------------------------
                Dim motors As IReadOnlyList(Of ComponentSpecs) = selectionResult.SelectedMotors
                Dim motorCount As Integer = motors.Count

                ' Percentage window for all part generation: 30 → 90 = 60 points
                Dim partWindowStart As Integer = 30
                Dim partWindowEnd As Integer = 90
                Dim pointsPerPart As Double = (partWindowEnd - partWindowStart) /
                                               Math.Max(motorCount, 1)

                For i As Integer = 0 To motorCount - 1
                    cancellationToken.ThrowIfCancellationRequested()

                    Dim motor As ComponentSpecs = motors(i)
                    Dim partIndex As Integer = i + 1
                    Dim pctStart As Integer = partWindowStart + CInt(i * pointsPerPart)
                    Dim pctMid As Integer = partWindowStart + CInt((i + 0.5) * pointsPerPart)

                    Report(progress, PipelineStage.GeneratingPart, pctStart,
                           $"Generating motor mount {partIndex} of {motorCount}…",
                           GetMotorDisplayName(motor))

                    _logger($"Starting motor mount for {GetMotorDisplayName(motor)}")

                    Try
                        ' Derive parameters and call the Task 14 method on SolidWorksAutomation
                        Dim outputFilename As String =
                            SanitiseFilename($"MotorMount_{GetMotorDisplayName(motor)}.SLDPRT")
                        Dim outputPath As String = Path.Combine(outputDirectory, outputFilename)

                        ' RunMacroForMotor calls the macro via MacroRunner and saves the part.
                        ' This is the Task 14 method:  SolidWorksAutomation.GenerateMotorMount
                        ' AFTER — route through MacroRunner which already exists
                        Dim motorParams As New MacroParameters()
                        motorParams.Add("ShaftDiameterMm", motor.Dimensions.ShaftDiameterMm)
                        motorParams.Add("MountingBoltCircleMm", motor.Dimensions.MountingPatternMm)
                        motorParams.Add("OuterDiameterMm", motor.Dimensions.OuterDiameterMm)

                        Dim templatePath As String = Path.Combine(
    ConfigManager.Settings.ResolvedTemplatePartsDirectory,
    "MotorMount_Template.SLDPRT")

                        Dim macroResult As MacroRunResult = Await Task.Run(
    Function()
        Return _macroRunner.RunMacroOnTemplate(
            templatePath:=templatePath,
            macroPath:=_motorMountMacroPath,
            macroModule:="MotorMount",
            macroProcedure:="GenerateMotorMount",
            parameters:=motorParams,
            outputPath:=outputPath)
    End Function,
    cancellationToken)

                        Dim macroSuccess As Boolean = macroResult.Success

                        If macroSuccess Then
                            Report(progress, PipelineStage.SavingFile, pctMid,
                                   $"Saved motor mount {partIndex} of {motorCount}",
                                   outputFilename)
                            parts.Add(New GeneratedPartRecord(
                                          motor.Id,
                                          GetMotorDisplayName(motor),
                                          "Motor Mount",
                                          outputPath))
                            _logger($"  ✓ Saved: {outputPath}")
                        Else
                            Const detail = "Macro returned failure status (see SolidWorks log)."
                            parts.Add(GeneratedPartRecord.Failure(
                                          motor.Id,
                                          GetMotorDisplayName(motor),
                                          "Motor Mount",
                                          detail))
                            _logger($"  ✗ Macro failed for {GetMotorDisplayName(motor)}: {detail}")
                        End If

                    Catch ex As OperationCanceledException
                        Throw   ' propagate cancellation — do not swallow
                    Catch ex As Exception
                        ' Partial failure — log and continue with the remaining motors
                        Dim detail = ex.Message
                        parts.Add(GeneratedPartRecord.Failure(
                                      motor.Id,
                                      GetMotorDisplayName(motor),
                                      "Motor Mount",
                                      detail))
                        _logger($"  ✗ Exception for {GetMotorDisplayName(motor)}: {ex}")
                    End Try

                Next i ' end motor loop

                ' ----------------------------------------------------------------
                ' STAGE 5 — Write component manifest
                ' ----------------------------------------------------------------
                cancellationToken.ThrowIfCancellationRequested()
                Report(progress, PipelineStage.WritingManifest, 92, "Writing component manifest…")

                Dim manifestPath As String = Path.Combine(outputDirectory, "component_manifest.txt")
                WriteManifest(manifestPath, specs, selectionResult, parts)
                _logger($"Manifest written: {manifestPath}")

                ' ----------------------------------------------------------------
                ' STAGE 6 — Finalise
                ' ----------------------------------------------------------------
                Report(progress, PipelineStage.Finalising, 98, "Finalising…")

                ' Disconnect only if WE connected (don't close the user's existing session)
                If Not swWasConnectedBeforeRun Then
                    _swAutomation.Disconnect()
                    _logger("Disconnected from SolidWorks (we opened the session).")
                End If

                Dim successCount As Integer = 0
                For Each p In parts
                    If p.Success Then successCount += 1
                Next
                Report(progress, PipelineStage.Finalising, 100,
                       $"Complete — {successCount} of {motorCount} part(s) generated",
                       $"Output: {outputDirectory}")

                Return New PipelineResult(parts, outputDirectory, manifestPath, startedAt)

            Catch ex As OperationCanceledException
                _logger("Pipeline cancelled by user.")
                Report(progress, PipelineStage.Failed, -1, "Cancelled", "User cancelled the operation.")
                Return New PipelineResult("Operation was cancelled by the user.", parts, startedAt)

            Catch ex As Exception
                _logger($"Unhandled pipeline exception: {ex}")
                Dim msg = $"Unexpected error: {ex.Message}"
                Report(progress, PipelineStage.Failed, -1, "Pipeline error", msg)

                ' Attempt clean disconnect on unexpected error
                Try
                    If Not swWasConnectedBeforeRun AndAlso _swAutomation.IsConnected() Then
                        _swAutomation.Disconnect()
                    End If
                Catch
                    ' Swallow disconnect errors during error handling
                End Try

                Return New PipelineResult(msg, parts, startedAt)
            End Try

        End Function

        ''' <summary>
        ''' Overload that accepts an already-computed <see cref="SelectionResult"/>
        ''' (i.e. the user already ran Module 1 via the UI and this is a re-run
        ''' of Module 2 only, skipping component selection).
        ''' </summary>
        Public Async Function RunFromSelectionAsync(
            selectionResult As SelectionResult,
            specs As MissionSpecs,
            outputDirectory As String,
            progress As IProgress(Of PipelineProgressReport),
            Optional cancellationToken As CancellationToken = Nothing) As Task(Of PipelineResult)

            If selectionResult Is Nothing Then
                Throw New ArgumentNullException(NameOf(selectionResult))
            End If

            ' Wrap the existing result in a fake selector so RunAsync can
            ' reuse all its logic without duplicating it.
            Dim wrappedSelector As IComponentSelector =
                New PreloadedResultSelector(selectionResult)

            Dim tempOrchestrator As New PipelineOrchestrator(
                wrappedSelector,
                _swAutomation,
                _macroRunner,
                _logger)

            Return Await tempOrchestrator.RunAsync(
                specs, outputDirectory, progress, cancellationToken)

        End Function

        ' ------------------------------------------------------------------
        ' Private helpers
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Reports a progress update.  Safe to call from any thread — IProgress
        ''' handles marshalling to the capture thread (usually the UI thread).
        ''' </summary>
        Private Shared Sub Report(progress As IProgress(Of PipelineProgressReport),
                                   stage As PipelineStage,
                                   pct As Integer,
                                   message As String,
                                   Optional detail As String = Nothing)
            If progress Is Nothing Then Return
            progress.Report(New PipelineProgressReport(stage, pct, message, detail))
        End Sub

        ''' <summary>Validates inputs before any real work begins.</summary>
        Private Shared Function ValidateInputs(specs As MissionSpecs,
                                               outputDirectory As String) As String
            If specs Is Nothing Then Return "MissionSpecs cannot be Nothing."

            If specs.FlightEnduranceMinutes <= 0 Then
                Return "Flight endurance must be greater than 0 minutes."
            End If
            If specs.PayloadMassGrams < 0 Then
                Return "Payload mass cannot be negative."
            End If
            If String.IsNullOrWhiteSpace(outputDirectory) Then
                Return "No output directory selected."
            End If

            ' Check the output path is legal (doesn't need to exist — we create it)
            Try
                Dim fullPath = Path.GetFullPath(outputDirectory)   ' throws on bad chars
                If fullPath.Length > 240 Then
                    Return "Output path is too long (>240 chars). Choose a shorter path."
                End If
            Catch ex As Exception
                Return $"Invalid output path: {ex.Message}"
            End Try

            Return Nothing  ' Nothing = validation passed
        End Function

        ''' <summary>
        ''' Returns a display-safe name for a motor component for use in filenames
        ''' and progress messages.
        ''' </summary>
        Private Shared Function GetMotorDisplayName(motor As ComponentSpecs) As String
            If motor Is Nothing Then Return "Unknown Motor"
            Dim name = $"{motor.Manufacturer} {motor.ModelName}".Trim()
            Return If(String.IsNullOrWhiteSpace(name), motor.Id, name)
        End Function

        ''' <summary>
        ''' Strips characters that are illegal in Windows file names.
        ''' Replaces spaces with underscores for cleaner filenames.
        ''' </summary>
        Private Shared Function SanitiseFilename(name As String) As String
            Dim invalid As Char() = Path.GetInvalidFileNameChars()
            Dim sb As New StringBuilder(name.Length)
            For Each c As Char In name
                If Array.IndexOf(invalid, c) >= 0 Then
                    sb.Append("_")
                ElseIf c = " "c Then
                    sb.Append("_")
                Else
                    sb.Append(c)
                End If
            Next
            Return sb.ToString()
        End Function

        ''' <summary>
        ''' Writes a plain-text manifest listing all selected components and
        ''' which CAD files were generated.  Saved to the output directory so
        ''' the CAD folder is self-documenting.
        ''' </summary>
        Private Shared Sub WriteManifest(manifestPath As String,
                                          specs As MissionSpecs,
                                          result As SelectionResult,
                                          parts As List(Of GeneratedPartRecord))
            Dim sb As New StringBuilder
            sb.AppendLine("=================================================================")
            sb.AppendLine(" DRONE DESIGNER — Component & CAD Generation Manifest")
            sb.AppendLine($" Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            sb.AppendLine("=================================================================")
            sb.AppendLine()
            sb.AppendLine("MISSION PARAMETERS")
            sb.AppendLine("------------------")
            sb.AppendLine($"  Endurance:          {specs.FlightEnduranceMinutes} min")
            sb.AppendLine($"  Range:              {specs.MaxRangeKm} km")
            sb.AppendLine($"  Cruise speed:       {specs.CruiseSpeedMs} m/s")
            sb.AppendLine($"  Max altitude:       {specs.MaxAltitudeMeters} m")
            sb.AppendLine($"  Payload mass:       {specs.PayloadMassGrams} g")
            sb.AppendLine($"  Mission profile:    {specs.Profile}")
            sb.AppendLine($"  Frame type:         {specs.Configuration}")
            sb.AppendLine()
            sb.AppendLine("SELECTED COMPONENTS")
            sb.AppendLine("-------------------")

            Dim allComponents As New List(Of ComponentSpecs)
            If result.SelectedMotors IsNot Nothing Then allComponents.AddRange(result.SelectedMotors)
            If result.SelectedBatteries IsNot Nothing Then allComponents.AddRange(result.SelectedBatteries)
            If result.SelectedEscs IsNot Nothing Then allComponents.AddRange(result.SelectedEscs)
            If result.SelectedFlightControllers IsNot Nothing Then allComponents.AddRange(result.SelectedFlightControllers)
            If result.SelectedGpsModules IsNot Nothing Then allComponents.AddRange(result.SelectedGpsModules)
            If result.SelectedPropellers IsNot Nothing Then allComponents.AddRange(result.SelectedPropellers)

            For Each comp In allComponents
                sb.AppendLine($"  [{comp.Category,-20}] {comp.Manufacturer} {comp.ModelName}")
            Next

            sb.AppendLine()
            sb.AppendLine("GENERATED CAD FILES")
            sb.AppendLine("-------------------")
            If parts.Count = 0 Then
                sb.AppendLine("  (none)")
            Else
                For Each p In parts
                    Dim status = If(p.Success, "OK  ", "FAIL")
                    Dim fileInfo = If(p.Success,
                                     IO.Path.GetFileName(p.FilePath),
                                     $"— {p.ErrorDetail}")
                    sb.AppendLine($"  [{status}] {p.PartType} ({p.ComponentName}): {fileInfo}")
                Next
            End If

            sb.AppendLine()
            sb.AppendLine("=================================================================")

            File.WriteAllText(manifestPath, sb.ToString(), System.Text.Encoding.UTF8)
        End Sub

        ' ------------------------------------------------------------------
        ' Private inner class: _PreloadedResultSelector
        ' Wraps an existing SelectionResult so RunFromSelectionAsync can
        ' reuse the main RunAsync logic without duplicating it.
        ' ------------------------------------------------------------------

        Private Class PreloadedResultSelector
            Implements IComponentSelector

            Private ReadOnly _result As SelectionResult

            Public Sub New(result As SelectionResult)
                _result = result
            End Sub

            Public Function SelectComponents(specs As MissionSpecs) As SelectionResult _
                Implements IComponentSelector.SelectComponents
                Return _result   ' always return the pre-loaded result
            End Function

        End Class

    End Class

End Namespace
