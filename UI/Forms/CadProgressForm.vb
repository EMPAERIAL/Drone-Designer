' =============================================================================
' File:    UI/Forms/CadProgressForm.vb
' Project: Drone Designer
' Task:    15 — End-to-End Pipeline
'
' Purpose: Modal dialog shown while the SolidWorks generation pipeline runs.
'          Displays stage labels, an animated progress bar, and a scrolling
'          log of status messages.  Has a Cancel button that signals the
'          CancellationTokenSource owned by the caller.
'
' Usage (from MainForm.CAD.vb):
'
'   Using cts As New CancellationTokenSource()
'   Using frm As New CadProgressForm(cts)
'       AddHandler frm.PipelineCompleted, AddressOf OnPipelineCompleted
'       frm.Show(Me)
'       Dim result = Await orchestrator.RunAsync(specs, outDir, frm.Progress, cts.Token)
'       frm.MarkComplete(result)
'   End Using
'
' Threading:
'   All public methods on this form are thread-safe — they use Me.Invoke
'   when called from background threads.
'
' .NET Framework 4.7.2 / WinForms
' =============================================================================

Imports System
Imports System.Drawing
Imports System.Threading
Imports System.Windows.Forms
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Services
Imports Drone_Designer.Drone_Designer.Core.Models

Namespace Drone_Designer.UI.Forms

    ''' <summary>
    ''' Modal progress window for the CAD generation pipeline.
    ''' Owned by MainForm; created fresh for each pipeline run.
    ''' </summary>
    Public Class CadProgressForm
        Inherits Form

        ' ------------------------------------------------------------------
        ' Events
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Raised on the UI thread when the pipeline has finished
        ''' (either success, failure, or cancel) and the user dismisses
        ''' the dialog.
        ''' </summary>
        Public Event PipelineCompleted(sender As Object, e As PipelineCompletedEventArgs)

        ' ------------------------------------------------------------------
        ' Controls (created in code — no .Designer.vb)
        ' ------------------------------------------------------------------

        Private WithEvents _btnCancel As Button
        Private WithEvents _btnClose As Button
        Private _lblStage As Label
        Private _progressBar As ProgressBar
        Private _lblDetail As Label
        Private _txtLog As TextBox
        Private _lblResultIcon As Label
        Private _pnlResult As Panel

        ' ------------------------------------------------------------------
        ' Fields
        ' ------------------------------------------------------------------

        Private ReadOnly _cts As CancellationTokenSource
        Private _result As PipelineResult   ' set by MarkComplete
        Private _completed As Boolean = False

        ' IProgress implementation — this is what the orchestrator reports into.
        ' Constructed on the UI thread; callbacks are automatically marshalled back.
        Private ReadOnly _progress As IProgress(Of PipelineProgressReport)

        ''' <summary>
        ''' The <see cref="IProgress(Of PipelineProgressReport)"/> to pass to
        ''' <c>PipelineOrchestrator.RunAsync()</c>.
        ''' </summary>
        Public ReadOnly Property Progress As IProgress(Of PipelineProgressReport)
            Get
                Return _progress
            End Get
        End Property

        ' ------------------------------------------------------------------
        ' Constructor
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Creates the progress form.  Must be called on the UI thread so the
        ''' <see cref="System.Progress(Of T)"/> captures the correct
        ''' synchronisation context.
        ''' </summary>
        ''' <param name="cts">
        ''' The <see cref="CancellationTokenSource"/> shared with the pipeline.
        ''' Clicking Cancel calls <c>cts.Cancel()</c>.
        ''' </param>
        Public Sub New(cts As CancellationTokenSource)
            If cts Is Nothing Then Throw New ArgumentNullException(NameOf(cts))
            _cts = cts

            ' Progress(Of T) captures SynchronizationContext.Current at construction
            ' time, which is the UI sync context when called from a WinForms event handler.
            ' Callbacks will therefore run on the UI thread automatically.
            _progress = New Progress(Of PipelineProgressReport)(AddressOf HandleProgressReport)

            InitialiseControls()
        End Sub

        ' ------------------------------------------------------------------
        ' Control initialisation (all in code — no Designer)
        ' ------------------------------------------------------------------

        Private Sub InitialiseControls()
            ' --- Form ---
            Me.Text = "Generating CAD Models…"
            Me.Size = New Size(580, 460)
            Me.MinimumSize = New Size(500, 380)
            Me.FormBorderStyle = FormBorderStyle.Sizable
            Me.StartPosition = FormStartPosition.CenterParent
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = Color.FromArgb(30, 30, 30)
            Me.ForeColor = Color.WhiteSmoke

            ' --- Stage label ---
            _lblStage = New Label With {
                .Text = "Initialising…",
                .Font = New Font("Segoe UI", 12, FontStyle.Bold),
                .ForeColor = Color.FromArgb(100, 200, 255),
                .Location = New Point(16, 16),
                .Size = New Size(540, 28),
                .AutoEllipsis = True
            }

            ' --- Progress bar ---
            _progressBar = New ProgressBar With {
                .Location = New Point(16, 52),
                .Size = New Size(540, 22),
                .Minimum = 0,
                .Maximum = 100,
                .Value = 0,
                .Style = ProgressBarStyle.Continuous
            }

            ' --- Detail label ---
            _lblDetail = New Label With {
                .Text = String.Empty,
                .Font = New Font("Segoe UI", 9, FontStyle.Regular),
                .ForeColor = Color.Silver,
                .Location = New Point(16, 80),
                .Size = New Size(540, 18),
                .AutoEllipsis = True
            }

            ' --- Scrolling log ---
            Dim lblLog As New Label With {
                .Text = "Activity log:",
                .Font = New Font("Segoe UI", 8, FontStyle.Regular),
                .ForeColor = Color.Gray,
                .Location = New Point(16, 108),
                .Size = New Size(100, 16)
            }

            _txtLog = New TextBox With {
                .Location = New Point(16, 126),
                .Size = New Size(540, 230),
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Vertical,
                .BackColor = Color.FromArgb(20, 20, 20),
                .ForeColor = Color.FromArgb(180, 220, 180),
                .Font = New Font("Consolas", 8.5),
                .BorderStyle = BorderStyle.FixedSingle,
                .WordWrap = True
            }

            ' --- Result panel (hidden until complete) ---
            _pnlResult = New Panel With {
                .Location = New Point(16, 362),
                .Size = New Size(340, 32),
                .Visible = False
            }

            _lblResultIcon = New Label With {
                .Text = String.Empty,
                .Font = New Font("Segoe UI", 10, FontStyle.Bold),
                .Location = New Point(0, 0),
                .Size = New Size(340, 32),
                .AutoEllipsis = True
            }
            _pnlResult.Controls.Add(_lblResultIcon)

            ' --- Cancel button ---
            _btnCancel = New Button With {
                .Text = "Cancel",
                .Size = New Size(90, 32),
                .Location = New Point(454, 362),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = Color.FromArgb(80, 40, 40),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 9)
            }
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 60, 60)

            ' --- Close button (hidden until pipeline finishes) ---
            _btnClose = New Button With {
                .Text = "Close",
                .Size = New Size(90, 32),
                .Location = New Point(454, 362),
                .FlatStyle = FlatStyle.Flat,
                .BackColor = Color.FromArgb(40, 80, 40),
                .ForeColor = Color.White,
                .Font = New Font("Segoe UI", 9),
                .Visible = False
            }
            _btnClose.FlatAppearance.BorderColor = Color.FromArgb(60, 180, 60)

            ' --- Add controls ---
            Me.Controls.AddRange(New Control() {
                _lblStage,
                _progressBar,
                _lblDetail,
                lblLog,
                _txtLog,
                _pnlResult,
                _btnCancel,
                _btnClose
            })

            Me.ResumeLayout(False)
        End Sub

        ' ------------------------------------------------------------------
        ' Progress handler — always called on UI thread (via Progress(Of T))
        ' ------------------------------------------------------------------

        Private Sub HandleProgressReport(report As PipelineProgressReport)
            ' Guard: form might be disposed if user closed the window somehow
            If Me.IsDisposed Then Return

            ' Update stage label
            _lblStage.Text = report.StatusMessage

            ' Update progress bar (clamp to valid range)
            If report.PercentComplete >= 0 Then
                _progressBar.Value = Math.Max(0, Math.Min(100, report.PercentComplete))
            End If

            ' Update detail label
            _lblDetail.Text = If(report.DetailMessage, String.Empty)

            ' Append to log
            Dim timestamp = DateTime.Now.ToString("HH:mm:ss")
            Dim logLine As String
            If report.ErrorMessage IsNot Nothing Then
                logLine = $"[{timestamp}] ✗ {report.StatusMessage}: {report.ErrorMessage}"
            ElseIf report.DetailMessage IsNot Nothing Then
                logLine = $"[{timestamp}] {report.StatusMessage} — {report.DetailMessage}"
            Else
                logLine = $"[{timestamp}] {report.StatusMessage}"
            End If

            AppendLog(logLine)

            ' If the pipeline flagged a failure stage, tint the stage label red
            If report.Stage = PipelineStage.Failed Then
                _lblStage.ForeColor = Color.FromArgb(255, 120, 120)
            End If
        End Sub

        ''' <summary>
        ''' Appends a line to the log text box and auto-scrolls to the bottom.
        ''' Thread-safe.
        ''' </summary>
        Private Sub AppendLog(line As String)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of String)(AddressOf AppendLog), line)
                Return
            End If
            _txtLog.AppendText(line & Environment.NewLine)
        End Sub

        ' ------------------------------------------------------------------
        ' Public methods called by MainForm after the pipeline completes
        ' ------------------------------------------------------------------

        ''' <summary>
        ''' Called by MainForm after <c>RunAsync</c> returns to update the
        ''' form into its completed state (success or failure).
        ''' Thread-safe.
        ''' </summary>
        Public Sub MarkComplete(result As PipelineResult)
            If Me.InvokeRequired Then
                Me.Invoke(New Action(Of PipelineResult)(AddressOf MarkComplete), result)
                Return
            End If

            _result = result
            _completed = True

            ' Swap Cancel → Close button
            _btnCancel.Visible = False
            _btnClose.Visible = True

            ' Show result summary
            If result.Success Then
                _lblResultIcon.Text =
                    $"✓  {result.SuccessfulPartCount} part(s) saved — {result.OutputDirectory}"
                _lblResultIcon.ForeColor = Color.FromArgb(80, 220, 80)
                _lblStage.Text = "Complete"
                _progressBar.Value = 100
                Me.Text = "CAD Generation Complete"
            Else
                _lblResultIcon.Text = $"✗  {result.ErrorMessage}"
                _lblResultIcon.ForeColor = Color.FromArgb(255, 100, 100)
                _lblStage.Text = "Failed"
                Me.Text = "CAD Generation Failed"
            End If
            _pnlResult.Visible = True

            ' Log the full summary
            AppendLog(String.Empty)
            AppendLog("--- SUMMARY ---")
            For Each line In result.ToSummaryString().Split(
                New String() {Environment.NewLine}, StringSplitOptions.None)
                AppendLog(line)
            Next
        End Sub

        ' ------------------------------------------------------------------
        ' Button events
        ' ------------------------------------------------------------------

        Private Sub _btnCancel_Click(sender As Object, e As EventArgs) _
            Handles _btnCancel.Click

            If _completed Then Return

            _btnCancel.Enabled = False
            _btnCancel.Text = "Cancelling…"
            AppendLog("[" & DateTime.Now.ToString("HH:mm:ss") & "] Cancellation requested.")

            Try
                _cts.Cancel()
            Catch ex As Exception
                ' Token already cancelled — safe to ignore
            End Try
        End Sub

        Private Sub _btnClose_Click(sender As Object, e As EventArgs) _
            Handles _btnClose.Click

            If _result IsNot Nothing Then
                RaiseEvent PipelineCompleted(Me, New PipelineCompletedEventArgs(_result))
            End If
            Me.Close()
        End Sub

        ''' <summary>Prevent accidental close via the X button while running.</summary>
        Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
            If Not _completed AndAlso e.CloseReason = CloseReason.UserClosing Then
                ' Treat X-button click as Cancel
                _btnCancel_Click(Me, EventArgs.Empty)
                e.Cancel = True   ' don't close yet — wait for pipeline to react to cancel
            Else
                If _result IsNot Nothing AndAlso Not _completed Then
                    RaiseEvent PipelineCompleted(Me, New PipelineCompletedEventArgs(_result))
                End If
                MyBase.OnFormClosing(e)
            End If
        End Sub

    End Class

    ' =========================================================================
    ' EventArgs for PipelineCompleted event
    ' =========================================================================

    ''' <summary>Event arguments carrying the final pipeline result.</summary>
    Public Class PipelineCompletedEventArgs
        Inherits EventArgs

        ''' <summary>The final result of the pipeline run.</summary>
        Public ReadOnly Property Result As PipelineResult

        Public Sub New(result As PipelineResult)
            Me.Result = result
        End Sub
    End Class

End Namespace
