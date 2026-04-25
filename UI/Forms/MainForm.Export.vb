' =============================================================================
' File:    UI/Forms/MainForm.Export.vb
' Project: Drone Designer
'
' Purpose: Excel (.xlsx) export for the Selected Components result.
'          Produces a two-sheet workbook:
'            Sheet 1 — Mission Parameters  (all form inputs + engine summary)
'            Sheet 2 — Selected Components (the 10-column component grid,
'                                           recommended rows highlighted green)
'
'          Implemented with no NuGet packages. Uses System.IO.Compression
'          to write a valid OOXML (.xlsx) ZIP archive directly.
' =============================================================================

Imports System.Diagnostics
Imports System.IO
Imports System.IO.Compression
Imports System.Text
Imports Drone_Designer.Core.Models

Partial Class MainForm

    ' =====================================================================
    '  BUTTON HANDLER — wired in MainForm.Logic.vb OnLoad
    ' =====================================================================

    Friend Sub OnExportExcel(sender As Object, e As EventArgs)
        If _lastResult Is Nothing Then
            MessageBox.Show(
                "Run component selection first — there is nothing to export yet.",
                "Nothing to Export",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information)
            Return
        End If

        Using dlg As New SaveFileDialog()
            dlg.Filter = "Excel Workbook (*.xlsx)|*.xlsx"
            dlg.FileName = $"DroneDesign_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            dlg.Title = "Export Component Selection"
            dlg.DefaultExt = "xlsx"
            If dlg.ShowDialog() <> DialogResult.OK Then Return

            Try
                Dim sheets As New List(Of XlSheet)()
                sheets.Add(BuildMissionParamsSheet())
                sheets.Add(BuildComponentsSheet())
                XlsxWriter.Write(sheets, dlg.FileName)
                UpdateStatus($"✔  Exported: {Path.GetFileName(dlg.FileName)}")

                If MessageBox.Show(
                        $"Export complete.{Environment.NewLine}{dlg.FileName}" &
                        $"{Environment.NewLine}{Environment.NewLine}Open the file now?",
                        "Export Complete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information) = DialogResult.Yes Then
                    Process.Start(dlg.FileName)
                End If

            Catch ex As Exception
                UpdateStatus("⚠  Export failed.")
                MessageBox.Show(
                    $"Export failed:{Environment.NewLine}{ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            End Try
        End Using
    End Sub

    ' =====================================================================
    '  SHEET 1 — MISSION PARAMETERS
    ' =====================================================================

    Private Function BuildMissionParamsSheet() As XlSheet
        Dim s As New XlSheet("Mission Parameters")

        s.AddRow(XlRowStyle.Title,
                 "Mission Parameters — " & DateTime.Now.ToString("yyyy-MM-dd HH:mm"))
        s.AddBlankRow()

        s.AddRow(XlRowStyle.Section, "Flight Parameters")
        s.AddRow(XlRowStyle.Normal, "Endurance", $"{nudEndurance.Value:0.#} h")
        s.AddRow(XlRowStyle.Normal, "Range", $"{nudRange.Value:0} km")
        s.AddRow(XlRowStyle.Normal, "Cruise Speed", $"{nudCruiseSpeed.Value:0} km/h")
        s.AddRow(XlRowStyle.Normal, "Max Altitude", $"{nudMaxAltitude.Value:0} m AGL")
        s.AddRow(XlRowStyle.Normal, "Max Wind Speed", $"{nudMaxWindSpeed.Value:0} km/h")
        s.AddRow(XlRowStyle.Normal, "Max Takeoff Weight", $"{nudMaxTakeoffWeight.Value:0} g")
        s.AddBlankRow()

        s.AddRow(XlRowStyle.Section, "Payload")
        s.AddRow(XlRowStyle.Normal, "Payload Weight", $"{nudPayloadWeight.Value:0} g")
        s.AddRow(XlRowStyle.Normal, "Payload Type", cboPayloadType.Text)
        s.AddRow(XlRowStyle.Normal, "Camera Resolution", $"{nudCameraResolution.Value:0} MP")
        s.AddRow(XlRowStyle.Normal, "Payload Dimensions (W × H × D)",
                 $"{nudPayloadWidth.Value:0} × {nudPayloadHeight.Value:0} × {nudPayloadDepth.Value:0} mm")
        s.AddBlankRow()

        s.AddRow(XlRowStyle.Section, "Environment")
        s.AddRow(XlRowStyle.Normal, "Operating Environment", cboOperatingEnvironment.Text)
        s.AddRow(XlRowStyle.Normal, "Min Temperature", $"{nudTempMin.Value:0} °C")
        s.AddRow(XlRowStyle.Normal, "Max Temperature", $"{nudTempMax.Value:0} °C")
        s.AddRow(XlRowStyle.Normal, "IP Rating", cboIPRating.Text)
        s.AddRow(XlRowStyle.Normal, "Max Humidity", $"{nudHumidity.Value:0}%")
        s.AddBlankRow()

        s.AddRow(XlRowStyle.Section, "Mission Profile")
        s.AddRow(XlRowStyle.Normal, "Mission Profile", cboMissionProfile.Text)
        s.AddRow(XlRowStyle.Normal, "Airframe Type", cboFrameType.Text)
        s.AddRow(XlRowStyle.Normal, "Motor Count", cboMotorCount.Text)
        s.AddRow(XlRowStyle.Normal, "Autonomy Level", cboAutonomyLevel.Text)
        s.AddRow(XlRowStyle.Normal, "Motor Redundancy", If(chkMotorRedundancy.Checked, "Yes", "No"))
        s.AddRow(XlRowStyle.Normal, "GPS Redundancy", If(chkGPSRedundancy.Checked, "Yes", "No"))
        s.AddRow(XlRowStyle.Normal, "Battery Redundancy", If(chkBatteryRedundancy.Checked, "Yes", "No"))
        If txtNotes.Text.Trim().Length > 0 Then
            s.AddRow(XlRowStyle.Normal, "Notes", txtNotes.Text.Trim())
        End If
        s.AddBlankRow()

        s.AddRow(XlRowStyle.Section, "Selection Results")
        s.AddRow(XlRowStyle.Normal, "Estimated MTOW", $"{_lastResult.EstimatedMtowGrams:N0} g")
        s.AddRow(XlRowStyle.Normal, "Required Thrust / Motor",
                 $"{_lastResult.RequiredThrustPerMotorGf:N0} gf")
        If _lastResult.PowerBudget IsNot Nothing Then
            s.AddRow(XlRowStyle.Normal, "Battery Cell Count",
                     _lastResult.PowerBudget.CellCount.ToString())
            s.AddRow(XlRowStyle.Normal, "Battery Nominal Voltage",
                     $"{_lastResult.PowerBudget.NominalVoltageV:0.#} V")
            s.AddRow(XlRowStyle.Normal, "Required Battery Capacity",
                     $"{_lastResult.PowerBudget.RequiredCapacityMah:N0} mAh")
            s.AddRow(XlRowStyle.Normal, "Required C-Rating",
                     $"{_lastResult.PowerBudget.RequiredCRating:0.#}C")
            s.AddRow(XlRowStyle.Normal, "Peak System Power",
                     $"{_lastResult.PowerBudget.PeakSystemPowerW:N0} W")
            s.AddRow(XlRowStyle.Normal, "Hover System Power",
                     $"{_lastResult.PowerBudget.HoverSystemPowerW:N0} W")
        End If

        Return s
    End Function

    ' =====================================================================
    '  SHEET 2 — SELECTED COMPONENTS
    ' =====================================================================

    Private Function BuildComponentsSheet() As XlSheet
        Dim s As New XlSheet("Selected Components")

        s.AddRow(XlRowStyle.ColumnHeader,
                 "Category", "Manufacturer", "Model / Part No.",
                 "Mass (g)", "Voltage (V)", "Max Power (W)",
                 "Dimensions (mm)", "Interface / Protocol",
                 "Temp Range (°C)", "Selection Notes")

        For Each row As DataGridViewRow In dgvComponents.Rows
            Dim item As ComponentDisplayRow = TryCast(row.DataBoundItem, ComponentDisplayRow)
            If item Is Nothing Then Continue For

            Dim style As XlRowStyle = If(item.IsRecommended,
                                         XlRowStyle.Recommended,
                                         XlRowStyle.Normal)
            s.AddRow(style,
                     item.Category,
                     item.Manufacturer,
                     item.ModelName,
                     If(item.MassGrams > 0, item.MassGrams.ToString("0.#"), "—"),
                     If(item.NominalVoltage > 0, item.NominalVoltage.ToString("0.#"), "—"),
                     If(item.MaxPowerWatts > 0, item.MaxPowerWatts.ToString("0.#"), "—"),
                     item.Dimensions,
                     item.[Interface],
                     item.TempRating,
                     item.SelectionNotes)
        Next

        Return s
    End Function

End Class

' =============================================================================
'  XLSX DATA MODEL — sheet / row containers used by XlsxWriter
' =============================================================================

Friend Enum XlRowStyle
    Normal = 0
    ColumnHeader = 1   ' dark-blue background, white bold text
    Recommended = 2    ' light-green background, bold text
    Section = 3        ' bold blue text (section label rows)
    Title = 4          ' large bold blue, light-blue background
End Enum

Friend Class XlSheet
    Public ReadOnly Name As String
    Public ReadOnly Rows As New List(Of XlRow)()

    Public Sub New(name As String)
        Me.Name = name
    End Sub

    Public Sub AddRow(style As XlRowStyle, ParamArray cells As String())
        Dim r As New XlRow(style)
        If cells IsNot Nothing Then
            For Each c In cells
                r.Cells.Add(If(c, ""))
            Next
        End If
        Rows.Add(r)
    End Sub

    Public Sub AddBlankRow()
        Rows.Add(New XlRow(XlRowStyle.Normal))
    End Sub
End Class

Friend Class XlRow
    Public ReadOnly Style As XlRowStyle
    Public ReadOnly Cells As New List(Of String)()

    Public Sub New(style As XlRowStyle)
        Me.Style = style
    End Sub
End Class

' =============================================================================
'  XLSX WRITER — produces a valid .xlsx (OOXML) using System.IO.Compression
'
'  No NuGet packages required. The Office Open XML format is a ZIP archive
'  containing XML files. All strings are stored in a shared string table;
'  cell references are A1-style (A1, B2, AA1, etc.).
' =============================================================================

Friend NotInheritable Class XlsxWriter

    ' cellXfs style indices — MUST stay in sync with BuildStyles()
    Private Const STYLE_NORMAL As Integer = 0
    Private Const STYLE_HEADER As Integer = 1
    Private Const STYLE_RECOMMENDED As Integer = 2
    Private Const STYLE_SECTION As Integer = 3
    Private Const STYLE_TITLE As Integer = 4

    Public Shared Sub Write(sheets As List(Of XlSheet), filePath As String)
        Dim ss As New List(Of String)()

        ' Pre-build worksheet XML while populating the shared string table
        Dim wsXmls As New List(Of String)()
        For Each sheet In sheets
            wsXmls.Add(BuildWorksheetXml(sheet, ss))
        Next

        If File.Exists(filePath) Then File.Delete(filePath)

        Using fs As New FileStream(filePath, FileMode.Create, FileAccess.Write)
            Using zip As New ZipArchive(fs, ZipArchiveMode.Create, True, Encoding.UTF8)
                WriteEntry(zip, "[Content_Types].xml", BuildContentTypes(sheets.Count))
                WriteEntry(zip, "_rels/.rels", BuildRels())
                WriteEntry(zip, "xl/workbook.xml", BuildWorkbook(sheets))
                WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRels(sheets.Count))
                WriteEntry(zip, "xl/styles.xml", BuildStyles())
                WriteEntry(zip, "xl/sharedStrings.xml", BuildSharedStrings(ss))
                For i As Integer = 0 To wsXmls.Count - 1
                    WriteEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", wsXmls(i))
                Next
            End Using
        End Using
    End Sub

    Private Shared Sub WriteEntry(zip As ZipArchive, entryName As String, content As String)
        Dim entry As ZipArchiveEntry = zip.CreateEntry(entryName, CompressionLevel.Optimal)
        Using sw As New StreamWriter(entry.Open(), New UTF8Encoding(False))
            sw.Write(content)
        End Using
    End Sub

    Private Shared Function AddOrGetSS(ss As List(Of String), value As String) As Integer
        Dim idx As Integer = ss.IndexOf(value)
        If idx >= 0 Then Return idx
        ss.Add(value)
        Return ss.Count - 1
    End Function

    Private Shared Function ColRef(zeroBasedIdx As Integer) As String
        Dim n As Integer = zeroBasedIdx + 1
        Dim result As String = ""
        While n > 0
            result = Chr(Asc("A"c) + (n - 1) Mod 26) & result
            n = (n - 1) \ 26
        End While
        Return result
    End Function

    Private Shared Function XmlEsc(s As String) As String
        If String.IsNullOrEmpty(s) Then Return ""
        Return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("""", "&quot;")
    End Function

    Private Shared Function StyleFor(style As XlRowStyle) As Integer
        Select Case style
            Case XlRowStyle.ColumnHeader : Return STYLE_HEADER
            Case XlRowStyle.Recommended : Return STYLE_RECOMMENDED
            Case XlRowStyle.Section : Return STYLE_SECTION
            Case XlRowStyle.Title : Return STYLE_TITLE
            Case Else : Return STYLE_NORMAL
        End Select
    End Function

    ' ── Worksheet ─────────────────────────────────────────────────────────
    Private Shared Function BuildWorksheetXml(sheet As XlSheet,
                                               ss As List(Of String)) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<worksheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">")
        sb.Append("<sheetData>")

        For rowIdx As Integer = 0 To sheet.Rows.Count - 1
            Dim row As XlRow = sheet.Rows(rowIdx)
            Dim rowNum As Integer = rowIdx + 1

            If row.Cells.Count = 0 Then
                sb.Append($"<row r=""{rowNum}""/>")
                Continue For
            End If

            Dim sIdx As Integer = StyleFor(row.Style)
            sb.Append($"<row r=""{rowNum}"">")
            For colIdx As Integer = 0 To row.Cells.Count - 1
                Dim cellRef As String = ColRef(colIdx) & rowNum.ToString()
                Dim ssIdx As Integer = AddOrGetSS(ss, row.Cells(colIdx))
                sb.Append($"<c r=""{cellRef}"" t=""s"" s=""{sIdx}""><v>{ssIdx}</v></c>")
            Next
            sb.Append("</row>")
        Next

        sb.Append("</sheetData>")
        sb.Append("</worksheet>")
        Return sb.ToString()
    End Function

    ' ── Styles ────────────────────────────────────────────────────────────
    Private Shared Function BuildStyles() As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">")

        ' fonts:
        '   0 = normal (Calibri 10)
        '   1 = bold white         → column headers
        '   2 = bold               → recommended rows
        '   3 = bold blue          → section labels
        '   4 = large bold blue    → title row
        sb.Append("<fonts count=""5"">")
        sb.Append("<font><sz val=""10""/><name val=""Calibri""/></font>")
        sb.Append("<font><b/><sz val=""10""/><name val=""Calibri""/><color rgb=""FFFFFFFF""/></font>")
        sb.Append("<font><b/><sz val=""10""/><name val=""Calibri""/></font>")
        sb.Append("<font><b/><sz val=""10""/><name val=""Calibri""/><color rgb=""FF1E5AAA""/></font>")
        sb.Append("<font><b/><sz val=""11""/><name val=""Calibri""/><color rgb=""FF1E5AAA""/></font>")
        sb.Append("</fonts>")

        ' fills:
        '   0 = none     (required by Excel)
        '   1 = gray125  (required by Excel)
        '   2 = dark blue  → column header background
        '   3 = light green → recommended row background
        '   4 = light blue  → title row background
        sb.Append("<fills count=""5"">")
        sb.Append("<fill><patternFill patternType=""none""/></fill>")
        sb.Append("<fill><patternFill patternType=""gray125""/></fill>")
        sb.Append("<fill><patternFill patternType=""solid""><fgColor rgb=""FF1E5AAA""/><bgColor indexed=""64""/></patternFill></fill>")
        sb.Append("<fill><patternFill patternType=""solid""><fgColor rgb=""FFE4F8E4""/><bgColor indexed=""64""/></patternFill></fill>")
        sb.Append("<fill><patternFill patternType=""solid""><fgColor rgb=""FFEEF4FF""/><bgColor indexed=""64""/></patternFill></fill>")
        sb.Append("</fills>")

        sb.Append("<borders count=""1"">")
        sb.Append("<border><left/><right/><top/><bottom/><diagonal/></border>")
        sb.Append("</borders>")

        sb.Append("<cellStyleXfs count=""1"">")
        sb.Append("<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>")
        sb.Append("</cellStyleXfs>")

        ' cellXfs (style index → font + fill combination):
        '   0 = STYLE_NORMAL
        '   1 = STYLE_HEADER      bold-white on dark-blue
        '   2 = STYLE_RECOMMENDED bold on light-green
        '   3 = STYLE_SECTION     bold-blue on white
        '   4 = STYLE_TITLE       large-bold-blue on light-blue
        sb.Append("<cellXfs count=""5"">")
        sb.Append("<xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0"" xfId=""0""/>")
        sb.Append("<xf numFmtId=""0"" fontId=""1"" fillId=""2"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>")
        sb.Append("<xf numFmtId=""0"" fontId=""2"" fillId=""3"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>")
        sb.Append("<xf numFmtId=""0"" fontId=""3"" fillId=""0"" borderId=""0"" xfId=""0"" applyFont=""1""/>")
        sb.Append("<xf numFmtId=""0"" fontId=""4"" fillId=""4"" borderId=""0"" xfId=""0"" applyFont=""1"" applyFill=""1""/>")
        sb.Append("</cellXfs>")

        sb.Append("</styleSheet>")
        Return sb.ToString()
    End Function

    ' ── Workbook ──────────────────────────────────────────────────────────
    Private Shared Function BuildWorkbook(sheets As List(Of XlSheet)) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<workbook xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""")
        sb.Append(" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">")
        sb.Append("<sheets>")
        For i As Integer = 0 To sheets.Count - 1
            sb.Append($"<sheet name=""{XmlEsc(sheets(i).Name)}"" sheetId=""{i + 1}"" r:id=""rId{i + 1}""/>")
        Next
        sb.Append("</sheets>")
        sb.Append("</workbook>")
        Return sb.ToString()
    End Function

    ' ── Workbook rels ─────────────────────────────────────────────────────
    Private Shared Function BuildWorkbookRels(sheetCount As Integer) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">")
        For i As Integer = 1 To sheetCount
            sb.Append($"<Relationship Id=""rId{i}""")
            sb.Append(" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet""")
            sb.Append($" Target=""worksheets/sheet{i}.xml""/>")
        Next
        sb.Append($"<Relationship Id=""rId{sheetCount + 1}""")
        sb.Append(" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings""")
        sb.Append(" Target=""sharedStrings.xml""/>")
        sb.Append($"<Relationship Id=""rId{sheetCount + 2}""")
        sb.Append(" Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles""")
        sb.Append(" Target=""styles.xml""/>")
        sb.Append("</Relationships>")
        Return sb.ToString()
    End Function

    ' ── Package rels ──────────────────────────────────────────────────────
    Private Shared Function BuildRels() As String
        Return "<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>" &
               "<Relationships xmlns=""http://schemas.openxmlformats.org/package/2006/relationships"">" &
               "<Relationship Id=""rId1""" &
               " Type=""http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument""" &
               " Target=""xl/workbook.xml""/>" &
               "</Relationships>"
    End Function

    ' ── Content types ─────────────────────────────────────────────────────
    Private Shared Function BuildContentTypes(sheetCount As Integer) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append("<Types xmlns=""http://schemas.openxmlformats.org/package/2006/content-types"">")
        sb.Append("<Default Extension=""rels"" ContentType=""application/vnd.openxmlformats-package.relationships+xml""/>")
        sb.Append("<Default Extension=""xml"" ContentType=""application/xml""/>")
        sb.Append("<Override PartName=""/xl/workbook.xml""")
        sb.Append(" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml""/>")
        For i As Integer = 1 To sheetCount
            sb.Append($"<Override PartName=""/xl/worksheets/sheet{i}.xml""")
            sb.Append(" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml""/>")
        Next
        sb.Append("<Override PartName=""/xl/sharedStrings.xml""")
        sb.Append(" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml""/>")
        sb.Append("<Override PartName=""/xl/styles.xml""")
        sb.Append(" ContentType=""application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml""/>")
        sb.Append("</Types>")
        Return sb.ToString()
    End Function

    ' ── Shared strings ────────────────────────────────────────────────────
    Private Shared Function BuildSharedStrings(ss As List(Of String)) As String
        Dim sb As New StringBuilder()
        sb.Append("<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>")
        sb.Append($"<sst xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main""")
        sb.Append($" count=""{ss.Count}"" uniqueCount=""{ss.Count}"">")
        For Each s In ss
            sb.Append($"<si><t xml:space=""preserve"">{XmlEsc(s)}</t></si>")
        Next
        sb.Append("</sst>")
        Return sb.ToString()
    End Function

End Class
