' =============================================================================
' File:    Core/Models/PipelineResult.vb
' Project: Drone Designer
' Task:    15 — End-to-End Pipeline
'
' Purpose: Immutable result types produced by PipelineOrchestrator.
'          The UI layer consumes these without knowing anything about
'          SolidWorks or the selection engine internals.
'
' Used by:
'   - Core/Services/PipelineOrchestrator.vb  (produces these)
'   - UI/Forms/MainForm.CAD.vb               (consumes these)
'
' .NET Framework 4.7.2 / VB.NET
' =============================================================================

Imports System
Imports System.Collections.Generic

Namespace DroneDesigner.Core.Models

    ' =========================================================================
    ' Enum: PipelineStage
    ' =========================================================================

    ''' <summary>
    ''' Named stages that the pipeline reports as it progresses.
    ''' Used by <see cref="PipelineProgressReport"/> so the UI can display
    ''' a meaningful status string, not just a percentage.
    ''' </summary>
    Public Enum PipelineStage
        ''' <summary>Initial validation of inputs before any real work begins.</summary>
        Validating = 0

        ''' <summary>Running the component selection engine (Module 1).</summary>
        SelectingComponents = 1

        ''' <summary>Connecting to or launching SolidWorks via COM interop.</summary>
        ConnectingToSolidWorks = 2

        ''' <summary>Generating a single CAD part from a macro template.</summary>
        GeneratingPart = 3

        ''' <summary>Saving the generated part file to disk.</summary>
        SavingFile = 4

        ''' <summary>Writing the component manifest alongside the CAD output.</summary>
        WritingManifest = 5

        ''' <summary>All work is complete; releasing SolidWorks resources.</summary>
        Finalising = 6

        ''' <summary>
        ''' Terminal failure state. Check <see cref="PipelineProgressReport.ErrorMessage"/>
        ''' for details.
        ''' </summary>
        Failed = 99
    End Enum

    ' =========================================================================
    ' Class: PipelineProgressReport  (progress event payload)
    ' =========================================================================

    ''' <summary>
    ''' Immutable snapshot of pipeline progress reported via
    ''' <see cref="IProgress(Of PipelineProgressReport)"/>.
    ''' One instance is fired for each meaningful state change.
    ''' </summary>
    Public NotInheritable Class PipelineProgressReport

        ''' <summary>Current named stage of the pipeline.</summary>
        Public ReadOnly Property Stage As PipelineStage

        ''' <summary>
        ''' 0–100 overall completion percentage.
        ''' Not necessarily linear — stages have different weights.
        ''' </summary>
        Public ReadOnly Property PercentComplete As Integer

        ''' <summary>Human-readable description of the current operation.</summary>
        Public ReadOnly Property StatusMessage As String

        ''' <summary>
        ''' Optional detail line shown below the main status message.
        ''' Useful for "Generating motor mount for T-Motor U8 II (3 of 4)" style messages.
        ''' Nothing when there is no detail to show.
        ''' </summary>
        Public ReadOnly Property DetailMessage As String

        ''' <summary>
        ''' Non-Nothing when <see cref="Stage"/> is <see cref="PipelineStage.Failed"/>.
        ''' Contains the exception message (not the full stack trace — use logging for that).
        ''' </summary>
        Public ReadOnly Property ErrorMessage As String

        ''' <summary>True when the pipeline has ended (either success or failure).</summary>
        Public ReadOnly Property IsTerminal As Boolean
            Get
                Return Stage = PipelineStage.Finalising OrElse Stage = PipelineStage.Failed
            End Get
        End Property

        ''' <summary>Initialises a progress report for a normal in-progress stage.</summary>
        Public Sub New(stage As PipelineStage,
                       percentComplete As Integer,
                       statusMessage As String,
                       Optional detailMessage As String = Nothing)
            Me.Stage = stage
            Me.PercentComplete = Math.Max(0, Math.Min(100, percentComplete))
            Me.StatusMessage = statusMessage
            Me.DetailMessage = detailMessage
            Me.ErrorMessage = Nothing
        End Sub

        ''' <summary>Factory — creates a terminal failure report.</summary>
        Public Shared Function Failure(errorMessage As String,
                                       Optional percentAtFailure As Integer = -1) _
                                       As PipelineProgressReport
            Dim r As New PipelineProgressReport(PipelineStage.Failed,
                                                 If(percentAtFailure >= 0, percentAtFailure, 0),
                                                 "Pipeline failed",
                                                 errorMessage)
            Return r
        End Function

        ''' <summary>Returns a single-line summary suitable for status-bar display.</summary>
        Public Overrides Function ToString() As String
            If ErrorMessage IsNot Nothing Then
                Return $"[{Stage}] ERROR: {ErrorMessage}"
            End If
            If DetailMessage IsNot Nothing Then
                Return $"[{PercentComplete}%] {StatusMessage} — {DetailMessage}"
            End If
            Return $"[{PercentComplete}%] {StatusMessage}"
        End Function

    End Class

    ' =========================================================================
    ' Class: GeneratedPartRecord
    ' =========================================================================

    ''' <summary>
    ''' Describes a single CAD file produced during a pipeline run.
    ''' Collected into <see cref="PipelineResult.GeneratedParts"/>.
    ''' </summary>
    Public NotInheritable Class GeneratedPartRecord

        ''' <summary>Component ID from the database that drove this part.</summary>
        Public ReadOnly Property ComponentId As String

        ''' <summary>Human-readable component name, e.g. "T-Motor U8 II".</summary>
        Public ReadOnly Property ComponentName As String

        ''' <summary>Category of part generated, e.g. "Motor Mount".</summary>
        Public ReadOnly Property PartType As String

        ''' <summary>Absolute path of the saved SolidWorks part (.SLDPRT) file.</summary>
        Public ReadOnly Property FilePath As String

        ''' <summary>UTC timestamp when the file was saved.</summary>
        Public ReadOnly Property SavedAtUtc As DateTime

        ''' <summary>True if this part was generated successfully.</summary>
        Public ReadOnly Property Success As Boolean

        ''' <summary>Error message if <see cref="Success"/> is False; Nothing otherwise.</summary>
        Public ReadOnly Property ErrorDetail As String

        ''' <summary>Initialises a successful part record.</summary>
        Public Sub New(componentId As String,
                       componentName As String,
                       partType As String,
                       filePath As String)
            Me.ComponentId = componentId
            Me.ComponentName = componentName
            Me.PartType = partType
            Me.FilePath = filePath
            Me.SavedAtUtc = DateTime.UtcNow
            Me.Success = True
            Me.ErrorDetail = Nothing
        End Sub

        ''' <summary>Factory — creates a failed part record (no file path).</summary>
        Public Shared Function Failure(componentId As String,
                                       componentName As String,
                                       partType As String,
                                       errorDetail As String) As GeneratedPartRecord
            Dim r As New GeneratedPartRecord(componentId, componentName, partType, String.Empty)
            ' Use reflection-free approach: create a new instance via a hidden sub
            ' Because this class is NotInheritable we use a different constructor overload:
            Return New GeneratedPartRecord(componentId, componentName, partType,
                                           String.Empty, errorDetail)
        End Function

        ''' <summary>Internal constructor for failure records.</summary>
        Private Sub New(componentId As String,
                        componentName As String,
                        partType As String,
                        filePath As String,
                        errorDetail As String)
            Me.ComponentId = componentId
            Me.ComponentName = componentName
            Me.PartType = partType
            Me.FilePath = filePath
            Me.SavedAtUtc = DateTime.UtcNow
            Me.Success = False
            Me.ErrorDetail = errorDetail
        End Sub

    End Class

    ' =========================================================================
    ' Class: PipelineResult  (final outcome returned to the UI)
    ' =========================================================================

    ''' <summary>
    ''' Final result of a complete pipeline run.
    ''' Returned by <c>PipelineOrchestrator.RunAsync()</c> when the pipeline
    ''' finishes (regardless of success or failure).
    ''' </summary>
    Public NotInheritable Class PipelineResult

        ' ------------------------------------------------------------------
        ' Properties
        ' ------------------------------------------------------------------

        ''' <summary>True if the pipeline completed without a fatal error.</summary>
        Public ReadOnly Property Success As Boolean

        ''' <summary>
        ''' Top-level error message when <see cref="Success"/> is False.
        ''' Nothing on success.
        ''' </summary>
        Public ReadOnly Property ErrorMessage As String

        ''' <summary>
        ''' Individual records for each CAD part that was attempted.
        ''' Always populated (even on partial failure) so the UI can report
        ''' which parts succeeded and which failed.
        ''' </summary>
        Public ReadOnly Property GeneratedParts As IReadOnlyList(Of GeneratedPartRecord)

        ''' <summary>
        ''' Directory where all generated files were saved.
        ''' Nothing if the pipeline failed before any files were written.
        ''' </summary>
        Public ReadOnly Property OutputDirectory As String

        ''' <summary>
        ''' Absolute path of the component manifest (.txt) written alongside
        ''' the CAD files. Nothing if manifest was not written.
        ''' </summary>
        Public ReadOnly Property ManifestPath As String

        ''' <summary>UTC time the pipeline run started.</summary>
        Public ReadOnly Property StartedAtUtc As DateTime

        ''' <summary>UTC time the pipeline run ended.</summary>
        Public ReadOnly Property FinishedAtUtc As DateTime

        ''' <summary>Total wall-clock duration of the run.</summary>
        Public ReadOnly Property Duration As TimeSpan
            Get
                Return FinishedAtUtc - StartedAtUtc
            End Get
        End Property

        ''' <summary>Count of parts generated successfully.</summary>
        Public ReadOnly Property SuccessfulPartCount As Integer
            Get
                Dim count As Integer = 0
                For Each p In GeneratedParts
                    If p.Success Then count += 1
                Next
                Return count
            End Get
        End Property

        ''' <summary>Count of parts that failed during generation.</summary>
        Public ReadOnly Property FailedPartCount As Integer
            Get
                Dim count As Integer = 0
                For Each p In GeneratedParts
                    If Not p.Success Then count += 1
                Next
                Return count
            End Get
        End Property

        ' ------------------------------------------------------------------
        ' Constructors
        ' ------------------------------------------------------------------

        ''' <summary>Initialises a successful pipeline result.</summary>
        Public Sub New(generatedParts As List(Of GeneratedPartRecord),
                       outputDirectory As String,
                       manifestPath As String,
                       startedAtUtc As DateTime)
            Me.Success = True
            Me.ErrorMessage = Nothing
            Me.GeneratedParts = generatedParts.AsReadOnly()
            Me.OutputDirectory = outputDirectory
            Me.ManifestPath = manifestPath
            Me.StartedAtUtc = startedAtUtc
            Me.FinishedAtUtc = DateTime.UtcNow
        End Sub

        ''' <summary>
        ''' Initialises a failed pipeline result.
        ''' <paramref name="partsSoFar"/> can be empty or partially populated.
        ''' </summary>
        Public Sub New(errorMessage As String,
                       partsSoFar As List(Of GeneratedPartRecord),
                       startedAtUtc As DateTime)
            Me.Success = False
            Me.ErrorMessage = errorMessage
            Me.GeneratedParts = If(partsSoFar IsNot Nothing,
                                   CType(partsSoFar.AsReadOnly(),
                                         IReadOnlyList(Of GeneratedPartRecord)),
                                   New List(Of GeneratedPartRecord)().AsReadOnly())
            Me.OutputDirectory = Nothing
            Me.ManifestPath = Nothing
            Me.StartedAtUtc = startedAtUtc
            Me.FinishedAtUtc = DateTime.UtcNow
        End Sub

        ' ------------------------------------------------------------------
        ' Helpers
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Returns a multi-line summary suitable for display in a MessageBox
        ''' or log file.
        ''' </summary>
        Public Function ToSummaryString() As String
            Dim sb As New System.Text.StringBuilder
            sb.AppendLine($"Pipeline run finished at {FinishedAtUtc:HH:mm:ss UTC}")
            sb.AppendLine($"Duration: {Duration.TotalSeconds:N1}s")
            sb.AppendLine()

            If Success Then
                sb.AppendLine($"Status:  SUCCESS")
                sb.AppendLine($"Parts generated: {SuccessfulPartCount}")
                If FailedPartCount > 0 Then
                    sb.AppendLine($"Parts failed:    {FailedPartCount}  ← check log for details")
                End If
                If OutputDirectory IsNot Nothing Then
                    sb.AppendLine($"Output folder:   {OutputDirectory}")
                End If
            Else
                sb.AppendLine($"Status:  FAILED")
                sb.AppendLine($"Reason:  {ErrorMessage}")
                If SuccessfulPartCount > 0 Then
                    sb.AppendLine($"Parts saved before failure: {SuccessfulPartCount}")
                End If
            End If

            If GeneratedParts.Count > 0 Then
                sb.AppendLine()
                sb.AppendLine("Generated parts:")
                For Each p In GeneratedParts
                    Dim icon = If(p.Success, "✓", "✗")
                    Dim detail = If(p.Success,
                                    IO.Path.GetFileName(p.FilePath),
                                    $"FAILED — {p.ErrorDetail}")
                    sb.AppendLine($"  {icon} {p.PartType} ({p.ComponentName}): {detail}")
                Next
            End If

            Return sb.ToString().TrimEnd()
        End Function

    End Class

End Namespace
