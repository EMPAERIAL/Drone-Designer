Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports Drone_Designer.Core.Services

Namespace UI.Forms

    ''' <summary>
    ''' Non-modal popup showing the MTOW fixed-point iteration history as an animated line chart.
    '''
    ''' Blue series  : battery mass (g) per iteration.
    ''' Orange series: total mass / MTOW (g) per iteration.
    '''
    ''' Points appear one per 180 ms frame so the convergence path
    ''' animates in after the selection completes.
    ''' </summary>
    Friend Class ConvergenceForm
        Inherits Form

        ' ── data ──────────────────────────────────────────────────────────────
        Private ReadOnly _history     As List(Of MtowIterationPoint)
        Private ReadOnly _finalMtowG  As Double
        Private ReadOnly _finalBatG   As Double
        Private ReadOnly _converged   As Boolean

        ' ── animation ─────────────────────────────────────────────────────────
        Private ReadOnly _pnlChart    As Panel
        Private ReadOnly _animTimer   As Timer
        Private _displayedPoints      As Integer = 1

        ' ── palette ───────────────────────────────────────────────────────────
        Private Shared ReadOnly ClrBattery As Color = Color.FromArgb(30,  120, 200)
        Private Shared ReadOnly ClrMtow    As Color = Color.FromArgb(210,  90,  20)
        Private Shared ReadOnly ClrGrid    As Color = Color.FromArgb(218, 224, 236)
        Private Shared ReadOnly ClrAxes    As Color = Color.FromArgb( 90, 100, 125)

        ' ── layout constants ──────────────────────────────────────────────────
        Private Const ML As Integer = 74    ' chart left margin  (Y labels)
        Private Const MR As Integer = 20    ' chart right margin
        Private Const MT As Integer = 32    ' chart top margin   (title)
        Private Const MB As Integer = 54    ' chart bottom margin (X labels)

        ' =======================================================================
        ' CONSTRUCTOR
        ' =======================================================================

        ''' <param name="converged">
        ''' True when the iterator completed normally.
        ''' False marks the chart title red and adds a "Diverged / Failed" label.
        ''' </param>
        Public Sub New(history As List(Of MtowIterationPoint),
                       finalMtowG As Double,
                       Optional converged As Boolean = True)

            _history    = If(history, New List(Of MtowIterationPoint)())
            _finalMtowG = If(finalMtowG > 0, finalMtowG,
                             If(_history.Count > 0, _history.Last().MtowG, 0.0))
            _finalBatG  = If(_history.Count > 0, _history.Last().BatteryMassG, 0.0)
            _converged  = converged

            ' ── form ────────────────────────────────────────────────────────
            Me.Text            = "MTOW Convergence"
            Me.Size            = New Size(700, 480)
            Me.MinimumSize     = New Size(520, 380)
            Me.StartPosition   = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.SizableToolWindow
            Me.BackColor       = Color.White
            Me.Font            = New Font("Segoe UI", 9)

            ' ── top stats bar ───────────────────────────────────────────────
            Dim statsBack As Color = If(_converged,
                                        Color.FromArgb(235, 242, 255),
                                        Color.FromArgb(255, 235, 230))
            Dim statsFore As Color = If(_converged,
                                        Color.FromArgb(35, 45, 90),
                                        Color.FromArgb(140, 30, 10))

            Dim pnlStats As New Panel With {
                .Dock      = DockStyle.Top,
                .Height    = 44,
                .BackColor = statsBack,
                .Padding   = New Padding(14, 0, 14, 0)
            }
            Dim lblStats As New Label With {
                .AutoSize  = False,
                .Dock      = DockStyle.Fill,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Font      = New Font("Segoe UI", 8.5F),
                .ForeColor = statsFore,
                .BackColor = Color.Transparent
            }
            If _history.Count > 0 Then
                Dim seed   As Double  = _history(0).BatteryMassG
                Dim iters  As Integer = _history.Count - 1
                Dim status As String  = If(_converged, "Converged battery:", "Battery at failure:")
                lblStats.Text =
                    $"Seed battery: {seed:N0} g    │    " &
                    $"{status}  {_finalBatG:N0} g    │    " &
                    $"Final MTOW: {_finalMtowG:N0} g    │    " &
                    $"Iterations: {iters}" &
                    If(_converged, "", "    │    ⚠ DIVERGED")
            End If
            pnlStats.Controls.Add(lblStats)

            ' ── chart panel ─────────────────────────────────────────────────
            _pnlChart = New Panel With {
                .Dock      = DockStyle.Fill,
                .BackColor = Color.FromArgb(252, 253, 255)
            }
            AddHandler _pnlChart.Paint,  AddressOf OnChartPaint
            AddHandler _pnlChart.Resize, Sub(s, ev) _pnlChart.Invalidate()

            ' ── close button ────────────────────────────────────────────────
            Dim btnClose As New Button With {
                .Text      = "Close",
                .Dock      = DockStyle.Bottom,
                .Height    = 34,
                .FlatStyle = FlatStyle.Flat,
                .BackColor = Color.FromArgb(30, 90, 170),
                .ForeColor = Color.White,
                .Font      = New Font("Segoe UI", 9, FontStyle.Bold),
                .Cursor    = Cursors.Hand
            }
            btnClose.FlatAppearance.BorderSize = 0
            AddHandler btnClose.Click, Sub(s, ev) Me.Close()

            ' Controls are added in reverse DockStyle.Fill order
            Me.Controls.Add(_pnlChart)
            Me.Controls.Add(pnlStats)
            Me.Controls.Add(btnClose)

            ' ── animation timer ─────────────────────────────────────────────
            _animTimer = New Timer With {.Interval = 180}
            AddHandler _animTimer.Tick, AddressOf OnAnimTick
            If _history.Count > 1 Then _animTimer.Start()
        End Sub

        ' =======================================================================
        ' ANIMATION
        ' =======================================================================

        Private Sub OnAnimTick(sender As Object, e As EventArgs)
            _displayedPoints += 1
            _pnlChart.Invalidate()
            If _displayedPoints >= _history.Count Then _animTimer.Stop()
        End Sub

        Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
            _animTimer.Stop()
            _animTimer.Dispose()
            MyBase.OnFormClosed(e)
        End Sub

        ' =======================================================================
        ' CHART RENDERING
        ' =======================================================================

        Private Sub OnChartPaint(sender As Object, e As PaintEventArgs)
            If _history.Count < 2 Then Return

            Dim g  As Graphics = e.Graphics
            g.SmoothingMode    = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit

            Dim cw As Integer = _pnlChart.ClientSize.Width  - ML - MR
            Dim ch As Integer = _pnlChart.ClientSize.Height - MT - MB
            If cw < 30 OrElse ch < 30 Then Return

            ' Y scale: largest value across all history points, with 18 % headroom
            Dim maxY As Double = _history.Max(Function(p) Math.Max(p.BatteryMassG, p.MtowG)) * 1.18
            If maxY <= 0 Then maxY = 1000.0

            Dim xSteps As Integer = _history.Count - 1   ' total x intervals

            ' coordinate mappers (closures over cw, ch, maxY, xSteps)
            Dim ToX As Func(Of Integer, Single) =
                Function(iter As Integer) As Single
                    If xSteps = 0 Then Return ML
                    Return ML + CSng(iter) / xSteps * cw
                End Function

            Dim ToY As Func(Of Double, Single) =
                Function(mass As Double) As Single
                    Return MT + ch - CSng(mass / maxY) * ch
                End Function

            ' ── plot area ─────────────────────────────────────────────────
            g.FillRectangle(Brushes.White, ML, MT, cw, ch)

            ' ── grid + Y labels ───────────────────────────────────────────
            Const YGRID As Integer = 5
            Using gridPen As New Pen(ClrGrid, 1)
                Using axBrush As New SolidBrush(ClrAxes)
                    For i As Integer = 0 To YGRID
                        Dim yVal As Double = maxY * i / YGRID
                        Dim yy   As Single = ToY(yVal)
                        g.DrawLine(gridPen, ML, yy, ML + cw, yy)
                        Dim lbl As String = $"{yVal:N0} g"
                        Dim sz  As SizeF  = g.MeasureString(lbl, Me.Font)
                        g.DrawString(lbl, Me.Font, axBrush,
                                     ML - sz.Width - 5, yy - sz.Height / 2)
                    Next
                End Using
            End Using

            ' ── vertical grid + X labels ──────────────────────────────────
            Using gridPen As New Pen(ClrGrid, 1)
                Using axBrush As New SolidBrush(ClrAxes)
                    For i As Integer = 0 To xSteps
                        Dim xx  As Single = ToX(i)
                        g.DrawLine(gridPen, xx, MT, xx, MT + ch)
                        Dim lbl As String = If(i = 0, "seed", i.ToString())
                        Dim sz  As SizeF  = g.MeasureString(lbl, Me.Font)
                        g.DrawString(lbl, Me.Font, axBrush,
                                     xx - sz.Width / 2, MT + ch + 7)
                    Next
                End Using
            End Using

            ' ── converged dashes (appear once animation is complete) ───────
            If _displayedPoints >= _history.Count Then
                DrawDash(g, ToY(_finalBatG), ML, ML + cw, ClrBattery)
                DrawDash(g, ToY(_finalMtowG), ML, ML + cw, ClrMtow)
            End If

            ' ── axis border ───────────────────────────────────────────────
            Using bp As New Pen(Color.FromArgb(165, 172, 195), 1)
                g.DrawRectangle(bp, ML, MT, cw, ch)
            End Using

            ' ── chart title ───────────────────────────────────────────────
            Dim titleFont  As New Font("Segoe UI", 10, FontStyle.Bold)
            Dim titleStr   As String = If(_converged,
                                          "MTOW Fixed-Point Convergence",
                                          "MTOW Iteration — Diverged / Failed")
            Dim titleColor As Color  = If(_converged,
                                          Color.FromArgb(25, 40, 90),
                                          Color.FromArgb(180, 30, 10))
            Dim tsz As SizeF = g.MeasureString(titleStr, titleFont)
            Using tb As New SolidBrush(titleColor)
                g.DrawString(titleStr, titleFont, tb,
                             ML + cw / 2 - tsz.Width / 2, 6)
            End Using

            ' ── X axis label ──────────────────────────────────────────────
            Dim xFont  As New Font("Segoe UI", 8.5F)
            Dim xLabel As String = "Iteration"
            Dim xsz    As SizeF  = g.MeasureString(xLabel, xFont)
            Using axBrush As New SolidBrush(ClrAxes)
                g.DrawString(xLabel, xFont, axBrush,
                             ML + cw / 2 - xsz.Width / 2, MT + ch + 32)
            End Using

            ' ── data series ───────────────────────────────────────────────
            Dim visible = _history.Take(_displayedPoints).ToList()

            ' Battery mass
            Dim batPts = visible _
                .Select(Function(p) New PointF(ToX(p.Iteration), ToY(p.BatteryMassG))) _
                .ToArray()
            DrawSeries(g, batPts, ClrBattery)

            ' MTOW (seed has MtowG = 0 — skip it)
            Dim mtowPts = visible _
                .Where(Function(p) p.MtowG > 0) _
                .Select(Function(p) New PointF(ToX(p.Iteration), ToY(p.MtowG))) _
                .ToArray()
            DrawSeries(g, mtowPts, ClrMtow)

            ' ── legend ────────────────────────────────────────────────────
            DrawLegend(g, ML + cw - 158, MT + 10)
        End Sub

        Private Sub DrawSeries(g As Graphics, pts() As PointF, clr As Color)
            If pts.Length = 0 Then Return
            Using lp As New Pen(clr, 2.2F)
                lp.LineJoin = LineJoin.Round
                If pts.Length >= 2 Then g.DrawLines(lp, pts)
            End Using
            Using fb As New SolidBrush(clr)
                For Each pt As PointF In pts
                    g.FillEllipse(fb, pt.X - 5F, pt.Y - 5F, 10F, 10F)
                Next
            End Using
        End Sub

        Private Sub DrawDash(g As Graphics, yPx As Single,
                             x1 As Integer, x2 As Integer, clr As Color)
            Using dp As New Pen(Color.FromArgb(110, clr), 1.2F)
                dp.DashStyle = DashStyle.Dash
                g.DrawLine(dp, x1, yPx, x2, yPx)
            End Using
        End Sub

        Private Sub DrawLegend(g As Graphics, x As Integer, y As Integer)
            Dim colours() As Color  = {ClrBattery,       ClrMtow}
            Dim labels()  As String = {"Battery mass (g)", "MTOW (g)"}
            Using axBrush As New SolidBrush(ClrAxes)
                For i As Integer = 0 To 1
                    Using sb As New SolidBrush(colours(i))
                        g.FillRectangle(sb, x, y + i * 22 + 4, 16, 10)
                    End Using
                    g.DrawString(labels(i), Me.Font, axBrush, x + 22, y + i * 22)
                Next
            End Using
        End Sub

    End Class

End Namespace
