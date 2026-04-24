Imports System
Imports System.IO
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Xml.Serialization
Imports Drone_Designer.Core.Models
Imports System.Linq


' ============================================================
' FILE:    SolidWorks/MacroRunner.vb
' PURPOSE: Bridge between ComponentSpecs output and SolidWorks
'          automation. Handles template opening, parameter
'          injection, macro execution, and output saving.
'
' DEPENDENCIES:
'   - SolidWorks type library (SldWorks.Interop.SldWorks.dll,
'     SldWorks.Interop.SwConst.dll) — add via COM reference:
'     "SldWorks 20XX Type Library" in project references.
'   - Core/Models/ComponentSpecs.vb  (DroneDesigner.Core.Models)
'
' SOLIDWORKS VERSION TARGET: 2021+ (API v29+)
'   RunMacro2 signature and swDocumentTypes_e values are stable
'   across SW2018–2024. Confirm interop DLL version matches the
'   installed SolidWorks on the target machine.
'
' PARAMETER PASSING STRATEGY:
'   SolidWorks RunMacro2 does not support direct argument
'   passing to VBA procedures. This class uses two mechanisms:
'
'   1. CUSTOM DOCUMENT PROPERTIES (primary):
'      Parameters are written as Custom Properties on the open
'      document before the macro runs. The VBA macro reads them
'      via ActiveDoc.CustomInfo2("", "ParamKey").
'      Properties are cleaned up after the macro completes.
'
'   2. TEMP PARAMETER FILE (fallback / supplementary):
'      A key=value parameter file is written to %TEMP% before
'      the macro runs, at a well-known path. Macros that need
'      richer data (arrays, nested specs) read this file.
'      File is deleted after the macro completes.
'
' USAGE EXAMPLE:
'   Dim runner As New MacroRunner(swApp)
'   Dim p = MacroParameters.FromComponentSpec(motorSpec)
'   Dim result = runner.RunMacroOnTemplate(
'       "C:\Templates\MotorMount.SLDPRT",
'       "C:\Macros\BuildMotorMount.swp",
'       "MacroModule", "BuildMount",
'       p,
'       "C:\Output\MotorMount_Result.SLDPRT")
' ============================================================

Namespace Drone_Designer.SolidWorks

    ' ----------------------------------------------------------
    ' MacroParameters
    ' Carries all key-value pairs that will be injected into
    ' SolidWorks custom properties and the temp parameter file
    ' before a macro executes.
    ' ----------------------------------------------------------
    Public Class MacroParameters

        ''' <summary>Flat key→value store passed to the macro.</summary>
        Public ReadOnly Property Values As Dictionary(Of String, String)

        ''' <summary>
        ''' Optional: path to the source component spec file that
        ''' generated these parameters, for traceability in logs.
        ''' </summary>
        Public Property SourceDescription As String = String.Empty

        Public Sub New()
            Values = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        End Sub

        ''' <summary>Add or overwrite a parameter.</summary>
        Public Sub Add(key As String, value As String)
            Values(key) = value
        End Sub

        ''' <summary>Add a numeric value, formatted invariant-culture.</summary>
        Public Sub Add(key As String, value As Double)
            Values(key) = value.ToString("G", System.Globalization.CultureInfo.InvariantCulture)
        End Sub

        Public Sub Add(key As String, value As Integer)
            Values(key) = value.ToString(System.Globalization.CultureInfo.InvariantCulture)
        End Sub

        ' ----------------------------------------------------------
        ' Factory helpers — build MacroParameters from the domain
        ' models in Core/Models/ComponentSpecs.vb so callers in
        ' Module 2 don't have to manually unpack specs each time.
        ' Extend with more component types as needed.
        ' ----------------------------------------------------------

        ''' <summary>
        ''' Build parameters from a generic ComponentSpec.
        ''' Keys mirror property names in ComponentSpec so VBA
        ''' macros can use consistent, predictable names.
        ''' </summary>
        Public Shared Function FromComponentSpec(spec As ComponentSpecs) As MacroParameters
            Dim p As New MacroParameters()
            p.SourceDescription = $"{spec.Category} — {spec.ModelName}"

            p.Add("COMPONENT_NAME", spec.ModelName)
            p.Add("COMPONENT_CATEGORY", spec.Category.ToString())
            p.Add("MASS_G", spec.MassGrams)
            p.Add("LENGTH_MM", spec.Dimensions.Length)
            p.Add("WIDTH_MM", spec.Dimensions.Width)
            p.Add("HEIGHT_MM", spec.Dimensions.Height)
            p.Add("VOLTAGE_V", spec.NominalVoltageV)
            p.Add("POWER_W", spec.MaxPowerW)

            ' Append any extra spec entries stored in the flexible
            ' AdditionalSpecs dictionary on ComponentSpec
            'If spec.additionalSpecs IsNot Nothing Then
            'For Each kvp In spec.AdditionalSpecs
            '' Prefix with SPEC_ to avoid collisions
            'p.Add("SPEC_" & kvp.Key.ToUpperInvariant(), kvp.Value)
            'Next
            'End If

            Return p
        End Function

        ''' <summary>
        ''' Build parameters for a motor-specific spec, adding
        ''' motor fields on top of the generic ones.
        ''' </summary>
        Public Shared Function FromMotorSpec(spec As MotorSpec) As MacroParameters
            Dim p = FromComponentSpec(spec)
            p.Add("MOTOR_KV", spec.KV)
            p.Add("MOTOR_STATOR_DIAMETER_MM", spec.StatorDiameterMm)
            p.Add("MOTOR_STATOR_HEIGHT_MM", spec.StatorHeightMm)
            p.Add("MOTOR_SHAFT_DIAMETER_MM", spec.ShaftDiameterMm)
            p.Add("MOTOR_MOUNT_PATTERN_MM", spec.MountingBoltCircleMm)
            p.Add("MOTOR_MOUNT_HOLE_DIAMETER_MM", spec.MountingBoltCount)
            p.Add("MOTOR_THRUST_MAX_G", spec.MaxThrustGrams)
            Return p
        End Function

        ''' <summary>
        ''' Build parameters for a battery spec.
        ''' </summary>
        Public Shared Function FromBatterySpec(spec As BatterySpec) As MacroParameters
            Dim p = FromComponentSpec(spec)
            p.Add("BATTERY_CELLS", spec.CellCount)
            p.Add("BATTERY_CAPACITY_MAH", spec.CapacityMAh)
            p.Add("BATTERY_CHEMISTRY", spec.Chemistry.ToString())
            Return p
        End Function

        ''' <summary>
        ''' Build parameters for a propeller spec.
        ''' </summary>
        Public Shared Function FromPropellerSpec(spec As PropellerSpec) As MacroParameters
            Dim p = FromComponentSpec(spec)
            p.Add("PROP_DIAMETER_IN", spec.DiameterInches)
            p.Add("PROP_PITCH_IN", spec.PitchInches)
            p.Add("PROP_BLADE_COUNT", spec.BladeCount)
            p.Add("PROP_HUB_BORE_MM", spec.BoreDiameterMm)
            Return p
        End Function

        ''' <summary>
        ''' Serialise all parameters to key=value lines for the
        ''' temp file. VBA macros read this with Open/Line Input.
        ''' </summary>
        Public Function ToParameterFileContent() As String
            Dim sb As New StringBuilder()
            sb.AppendLine("# DroneDesigner macro parameter file")
            sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            sb.AppendLine($"# Source: {SourceDescription}")
            sb.AppendLine()
            For Each kvp In Values
                sb.AppendLine($"{kvp.Key}={kvp.Value}")
            Next
            Return sb.ToString()
        End Function

    End Class

    ' ----------------------------------------------------------
    ' MacroRunResult
    ' Returned by every RunMacro* method so callers can check
    ' success/failure and get diagnostics without catching
    ' exceptions for normal flow-control.
    ' ----------------------------------------------------------
    Public Class MacroRunResult

        Public ReadOnly Property Success As Boolean
        Public ReadOnly Property OutputFilePath As String
        Public ReadOnly Property ErrorMessage As String
        Public ReadOnly Property MacroPath As String
        Public ReadOnly Property TemplatePath As String
        Public ReadOnly Property ElapsedMs As Long

        ' SolidWorks-specific error code from RunMacro2
        Public ReadOnly Property SwErrorCode As Long

        Public Sub New(success As Boolean,
                       macroPath As String,
                       templatePath As String,
                       outputFilePath As String,
                       errorMessage As String,
                       swErrorCode As Long,
                       elapsedMs As Long)
            Me.Success = success
            Me.MacroPath = macroPath
            Me.TemplatePath = templatePath
            Me.OutputFilePath = outputFilePath
            Me.ErrorMessage = errorMessage
            Me.SwErrorCode = swErrorCode
            Me.ElapsedMs = elapsedMs
        End Sub

        Public Shared Function Ok(macroPath As String,
                                  templatePath As String,
                                  outputPath As String,
                                  elapsedMs As Long) As MacroRunResult
            Return New MacroRunResult(True, macroPath, templatePath, outputPath, String.Empty, 0, elapsedMs)
        End Function

        Public Shared Function Fail(macroPath As String,
                                    templatePath As String,
                                    message As String,
                                    Optional swErr As Long = 0,
                                    Optional elapsedMs As Long = 0) As MacroRunResult
            Return New MacroRunResult(False, macroPath, templatePath, String.Empty, message, swErr, elapsedMs)
        End Function

        Public Overrides Function ToString() As String
            If Success Then
                Return $"OK  [{ElapsedMs}ms]  →  {OutputFilePath}"
            Else
                Return $"ERR [{ElapsedMs}ms]  SW={SwErrorCode}  {ErrorMessage}"
            End If
        End Function

    End Class

    ' ----------------------------------------------------------
    ' MacroRunnerOptions
    ' Tune runtime behaviour without changing call sites.
    ' ----------------------------------------------------------
    Public Class MacroRunnerOptions

        ''' <summary>
        ''' Close the opened template document after saving the
        ''' output. Set False to leave it open for inspection.
        ''' Default: True.
        ''' </summary>
        Public Property CloseDocumentAfterRun As Boolean = True

        ''' <summary>
        ''' Open the template as a copy so the original is never
        ''' modified. Internally uses OpenDocOptions_e.swOpenDocOptions_Silent
        ''' + copies the file to a temp location first.
        ''' Default: True.
        ''' </summary>
        Public Property OpenTemplateAsCopy As Boolean = True

        ''' <summary>
        ''' If True, suppress all SolidWorks dialog boxes during
        ''' the run (recommended for automation).
        ''' Default: True.
        ''' </summary>
        Public Property SilentMode As Boolean = True

        ''' <summary>
        ''' Maximum time in milliseconds to wait for a macro to
        ''' complete before treating it as hung. 0 = no timeout.
        ''' Default: 60 000 (1 minute).
        ''' </summary>
        Public Property TimeoutMs As Integer = 60_000

        ''' <summary>
        ''' Directory for the temp parameter file.
        ''' Defaults to System.IO.Path.GetTempPath().
        ''' </summary>
        Public Property TempDirectory As String = Path.GetTempPath()

        ''' <summary>
        ''' If True, write parameters as custom document properties
        ''' on the open document before the macro runs.
        ''' Default: True.
        ''' </summary>
        Public Property InjectAsCustomProperties As Boolean = True

        ''' <summary>
        ''' If True, write parameters to a key=value temp file.
        ''' Default: True.
        ''' </summary>
        Public Property InjectAsTempFile As Boolean = True

        ''' <summary>
        ''' Well-known file name for the temp parameter file.
        ''' The VBA macro should open exactly this path.
        ''' Default: "DroneDesigner_MacroParams.txt"
        ''' </summary>
        Public Property TempParameterFileName As String = "DroneDesigner_MacroParams.txt"

        ''' <summary>
        ''' SolidWorks save format for the output file.
        ''' Uses the swSaveAsVersion_e value. 0 = same version.
        ''' </summary>
        Public Property SaveAsVersion As Integer = 0

    End Class

    ' ----------------------------------------------------------
    ' MacroRunner — main class
    ' ----------------------------------------------------------

    ''' <summary>
    ''' Executes SolidWorks VBA macros against template documents,
    ''' injecting ComponentSpec parameters before the macro runs
    ''' and saving the result to a specified output path.
    ''' </summary>
    ''' <remarks>
    ''' Requires an active SolidWorks COM instance. Obtain one from
    ''' SolidWorksAutomation.vb before constructing this class.
    ''' Thread-affinity: call from the UI thread or a dedicated STA
    ''' thread — SolidWorks COM is apartment-threaded.
    ''' </remarks>
    Public Class MacroRunner

        ' ---- private state -----------------------------------

        ''' <summary>Live SolidWorks application COM object.</summary>
        Private ReadOnly _swApp As Object   ' SldWorks.SldWorks

        ''' <summary>Options governing runtime behaviour.</summary>
        Private ReadOnly _options As MacroRunnerOptions

        ''' <summary>Accumulated log entries for this session.</summary>
        Private ReadOnly _log As List(Of String)

        ' SolidWorks constant: save as same version
        Private Const SW_SAVE_AS_CURRENT_VERSION As Integer = 0

        ' swDocumentTypes_e
        Private Const swDocPART As Integer = 1
        Private Const swDocASSEMBLY As Integer = 2
        Private Const swDocDRAWING As Integer = 3

        ' swOpenDocOptions_e (bit flags)
        Private Const swOpenDocOptions_Silent As Integer = 1
        Private Const swOpenDocOptions_ReadOnly As Integer = 2

        ' swRunMacroOption_e
        Private Const swRunMacroUnloadAfterRun As Integer = 1

        ' swFileSaveTypes_e  (used in SaveAs)
        Private Const swSaveAsPart As Integer = 20
        Private Const swSaveAsAssembly As Integer = 21

        ' ---- constructor -------------------------------------

        ''' <summary>
        ''' Initialise MacroRunner with an existing SolidWorks instance.
        ''' </summary>
        ''' <param name="swApp">
        ''' A live SldWorks.SldWorks COM object. Typically obtained
        ''' from SolidWorksAutomation.Connect().
        ''' </param>
        ''' <param name="options">
        ''' Optional runtime options. Null → defaults are used.
        ''' </param>
        Public Sub New(swApp As Object, Optional options As MacroRunnerOptions = Nothing)
            If swApp Is Nothing Then
                Throw New ArgumentNullException(NameOf(swApp), "SolidWorks application instance must not be null.")
            End If
            _swApp = swApp
            _options = If(options, New MacroRunnerOptions())
            _log = New List(Of String)()
        End Sub

        ' ---- public surface ----------------------------------

        ''' <summary>
        ''' Returns a copy of the log entries accumulated since
        ''' this MacroRunner was constructed. Useful for surfacing
        ''' run details in the UI or writing to a log file.
        ''' </summary>
        Public Function GetLog() As IReadOnlyList(Of String)
            Return _log.AsReadOnly()
        End Function

        ''' <summary>Clears the internal log buffer.</summary>
        Public Sub ClearLog()
            _log.Clear()
        End Sub

        ''' <summary>
        ''' Full pipeline: open template → inject parameters →
        ''' run macro → save output → clean up.
        ''' </summary>
        ''' <param name="templatePath">
        ''' Absolute path to the .SLDPRT or .SLDASM template file.
        ''' </param>
        ''' <param name="macroPath">
        ''' Absolute path to the .swp VBA macro file.
        ''' </param>
        ''' <param name="macroModule">
        ''' VBA module name inside the macro (e.g. "MacroModule1").
        ''' </param>
        ''' <param name="macroProcedure">
        ''' VBA Sub name to call (e.g. "BuildMotorMount").
        ''' </param>
        ''' <param name="parameters">
        ''' Parameters derived from ComponentSpec. Pass Nothing
        ''' to run the macro without parameter injection.
        ''' </param>
        ''' <param name="outputPath">
        ''' Where to save the finished document. If the directory
        ''' does not exist it will be created.
        ''' </param>
        Public Function RunMacroOnTemplate(
                templatePath As String,
                macroPath As String,
                macroModule As String,
                macroProcedure As String,
                parameters As MacroParameters,
                outputPath As String) As MacroRunResult

            Dim sw As New System.Diagnostics.Stopwatch()
            sw.Start()

            Log($"=== MacroRunner.RunMacroOnTemplate ===")
            Log($"  Template : {templatePath}")
            Log($"  Macro    : {macroPath}")
            Log($"  Entry    : {macroModule}.{macroProcedure}")
            Log($"  Output   : {outputPath}")

            ' --- 1. Validate inputs ---------------------------
            Dim validErr = ValidateInputs(templatePath, macroPath, outputPath)
            If validErr IsNot Nothing Then
                Return MacroRunResult.Fail(macroPath, templatePath, validErr, 0, sw.ElapsedMilliseconds)
            End If

            ' --- 2. Ensure output directory exists ------------
            Try
                Dim outDir = Path.GetDirectoryName(outputPath)
                If Not String.IsNullOrEmpty(outDir) AndAlso Not Directory.Exists(outDir) Then
                    Directory.CreateDirectory(outDir)
                    Log($"  Created output directory: {outDir}")
                End If
            Catch ex As Exception
                Return MacroRunResult.Fail(macroPath, templatePath,
                    $"Could not create output directory: {ex.Message}", 0, sw.ElapsedMilliseconds)
            End Try

            ' --- 3. Determine working copy path ---------------
            Dim workingPath As String = templatePath
            If _options.OpenTemplateAsCopy Then
                workingPath = BuildWorkingCopyPath(templatePath)
                Try
                    File.Copy(templatePath, workingPath, overwrite:=True)
                    Log($"  Working copy: {workingPath}")
                Catch ex As Exception
                    Return MacroRunResult.Fail(macroPath, templatePath,
                        $"Could not copy template to working path: {ex.Message}", 0, sw.ElapsedMilliseconds)
                End Try
            End If

            ' --- 4. Write temp parameter file -----------------
            Dim tempParamPath As String = Nothing
            If parameters IsNot Nothing AndAlso _options.InjectAsTempFile Then
                tempParamPath = WriteTempParameterFile(parameters)
                If tempParamPath Is Nothing Then
                    Log("  WARNING: Failed to write temp parameter file; continuing without it.")
                End If
            End If

            ' --- 5. Open the working copy in SolidWorks ------
            Dim doc As Object = Nothing   ' IModelDoc2
            Try
                doc = OpenDocument(workingPath)
            Catch ex As Exception
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail(macroPath, templatePath,
                    $"Failed to open template in SolidWorks: {ex.Message}", 0, sw.ElapsedMilliseconds)
            End Try

            If doc Is Nothing Then
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail(macroPath, templatePath,
                    "SolidWorks returned a null document. The template may be corrupt or in an unsupported format.",
                    0, sw.ElapsedMilliseconds)
            End If

            ' --- 6. Inject parameters as custom properties ----
            If parameters IsNot Nothing AndAlso _options.InjectAsCustomProperties Then
                Try
                    InjectCustomProperties(doc, parameters)
                Catch ex As Exception
                    Log($"  WARNING: Custom property injection failed — {ex.Message}")
                End Try
            End If

            ' --- 7. Run the macro -----------------------------
            Dim swErr As Integer = 0
            Dim macroOk As Boolean = False
            Try
                macroOk = RunMacro(macroPath, macroModule, macroProcedure, swErr)
            Catch ex As Exception
                CloseDocument(doc, False)
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail(macroPath, templatePath,
                    $"Exception during macro execution: {ex.Message}", swErr, sw.ElapsedMilliseconds)
            End Try

            If Not macroOk Then
                Log($"  Macro returned failure. SW error code: {swErr} ({TranslateSwError(swErr)})")
                CloseDocument(doc, False)
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail(macroPath, templatePath,
                    $"Macro execution failed. SW error {swErr}: {TranslateSwError(swErr)}",
                    swErr, sw.ElapsedMilliseconds)
            End If

            Log($"  Macro completed successfully.")

            ' --- 8. Remove injected custom properties ---------
            ' (keep document clean before saving as output)
            If parameters IsNot Nothing AndAlso _options.InjectAsCustomProperties Then
                Try
                    RemoveInjectedProperties(doc, parameters)
                Catch ex As Exception
                    Log($"  WARNING: Could not remove injected properties — {ex.Message}")
                End Try
            End If

            ' --- 9. Save output -------------------------------
            Dim saveOk = False
            Try
                saveOk = SaveDocumentAs(doc, outputPath)
            Catch ex As Exception
                Log($"  ERROR saving output: {ex.Message}")
            End Try

            ' --- 10. Close document ---------------------------
            If _options.CloseDocumentAfterRun Then
                CloseDocument(doc, False)   ' False = don't save again
            End If

            ' --- 11. Cleanup ----------------------------------
            CleanupWorkingCopy(workingPath, templatePath)
            CleanupTempFile(tempParamPath)

            sw.Stop()
            Log($"  Elapsed: {sw.ElapsedMilliseconds} ms")

            If saveOk AndAlso File.Exists(outputPath) Then
                Log($"  Output saved: {outputPath}")
                Return MacroRunResult.Ok(macroPath, templatePath, outputPath, sw.ElapsedMilliseconds)
            Else
                Return MacroRunResult.Fail(macroPath, templatePath,
                    $"Macro ran but output file was not found at: {outputPath}",
                    0, sw.ElapsedMilliseconds)
            End If
        End Function

        ''' <summary>
        ''' Convenience overload: builds MacroParameters from any
        ''' ComponentSpec automatically before calling the full pipeline.
        ''' </summary>
        Public Function RunMacroOnTemplate(
                templatePath As String,
                macroPath As String,
                macroModule As String,
                macroProcedure As String,
                componentSpec As ComponentSpecs,
                outputPath As String) As MacroRunResult

            Dim p = MacroParameters.FromComponentSpec(componentSpec)
            Return RunMacroOnTemplate(templatePath, macroPath, macroModule, macroProcedure, p, outputPath)
        End Function

        ''' <summary>
        ''' Run a macro against the document that is currently
        ''' active in SolidWorks (no template open/close). Useful
        ''' when SolidWorksAutomation.vb has already opened a doc.
        ''' </summary>
        Public Function RunMacroOnActiveDocument(
                macroPath As String,
                macroModule As String,
                macroProcedure As String,
                parameters As MacroParameters,
                outputPath As String) As MacroRunResult

            Dim sw As New System.Diagnostics.Stopwatch()
            sw.Start()

            Log($"=== MacroRunner.RunMacroOnActiveDocument ===")
            Log($"  Macro  : {macroPath}")
            Log($"  Entry  : {macroModule}.{macroProcedure}")
            Log($"  Output : {outputPath}")

            If Not File.Exists(macroPath) Then
                Return MacroRunResult.Fail(macroPath, "(active doc)",
                    $"Macro file not found: {macroPath}", 0, sw.ElapsedMilliseconds)
            End If

            Dim doc As Object
            Try
                doc = _swApp.ActiveDoc
            Catch ex As Exception
                Return MacroRunResult.Fail(macroPath, "(active doc)",
                    $"Could not access ActiveDoc: {ex.Message}", 0, sw.ElapsedMilliseconds)
            End Try

            If doc Is Nothing Then
                Return MacroRunResult.Fail(macroPath, "(active doc)",
                    "No active document in SolidWorks.", 0, sw.ElapsedMilliseconds)
            End If

            ' Inject parameters
            Dim tempParamPath As String = Nothing
            If parameters IsNot Nothing Then
                If _options.InjectAsCustomProperties Then
                    Try
                        InjectCustomProperties(doc, parameters)
                    Catch ex As Exception
                        Log($"  WARNING: Custom property injection failed — {ex.Message}")
                    End Try
                End If
                If _options.InjectAsTempFile Then
                    tempParamPath = WriteTempParameterFile(parameters)
                End If
            End If

            ' Run macro
            Dim swErr As Integer = 0
            Dim macroOk = RunMacro(macroPath, macroModule, macroProcedure, swErr)

            ' Clean up properties
            If parameters IsNot Nothing AndAlso _options.InjectAsCustomProperties Then
                Try
                    RemoveInjectedProperties(doc, parameters)
                Catch
                End Try
            End If
            CleanupTempFile(tempParamPath)

            If Not macroOk Then
                Return MacroRunResult.Fail(macroPath, "(active doc)",
                    $"Macro execution failed. SW error {swErr}: {TranslateSwError(swErr)}",
                    swErr, sw.ElapsedMilliseconds)
            End If

            ' Save
            Dim saveOk = False
            If Not String.IsNullOrEmpty(outputPath) Then
                Try
                    Dim outDir = Path.GetDirectoryName(outputPath)
                    If Not String.IsNullOrEmpty(outDir) Then Directory.CreateDirectory(outDir)
                    saveOk = SaveDocumentAs(doc, outputPath)
                Catch ex As Exception
                    Log($"  ERROR saving: {ex.Message}")
                End Try
            End If

            sw.Stop()
            Return If(saveOk,
                MacroRunResult.Ok(macroPath, "(active doc)", outputPath, sw.ElapsedMilliseconds),
                MacroRunResult.Fail(macroPath, "(active doc)",
                    "Macro ran but output save failed.", 0, sw.ElapsedMilliseconds))
        End Function

        ''' <summary>
        ''' Run multiple macros in sequence on a single template
        ''' document (e.g. BuildFrame → AddMountHoles → AddCutouts).
        ''' Each macro sees the state left by the previous one.
        ''' </summary>
        ''' <param name="macroSteps">
        ''' Ordered list of (macroPath, module, procedure) tuples.
        ''' </param>
        Public Function RunMacroSequenceOnTemplate(
                templatePath As String,
                macroSteps As IList(Of (MacroPath As String, [Module] As String, Procedure As String)),
                parameters As MacroParameters,
                outputPath As String) As MacroRunResult

            Log($"=== MacroRunner.RunMacroSequenceOnTemplate ({macroSteps.Count} steps) ===")

            If macroSteps Is Nothing OrElse macroSteps.Count = 0 Then
                Return MacroRunResult.Fail("(sequence)", templatePath, "No macro steps provided.")
            End If

            Dim sw As New System.Diagnostics.Stopwatch()
            sw.Start()

            Dim validErr = ValidateInputs(templatePath, macroSteps(0).MacroPath, outputPath)
            If validErr IsNot Nothing Then
                Return MacroRunResult.Fail("(sequence)", templatePath, validErr)
            End If

            ' Ensure output directory
            Try
                Dim outDir = Path.GetDirectoryName(outputPath)
                If Not String.IsNullOrEmpty(outDir) Then Directory.CreateDirectory(outDir)
            Catch ex As Exception
                Return MacroRunResult.Fail("(sequence)", templatePath,
                    $"Could not create output directory: {ex.Message}")
            End Try

            ' Working copy
            Dim workingPath = templatePath
            If _options.OpenTemplateAsCopy Then
                workingPath = BuildWorkingCopyPath(templatePath)
                File.Copy(templatePath, workingPath, overwrite:=True)
            End If

            ' Temp file (written once, reused by all steps)
            Dim tempParamPath As String = Nothing
            If parameters IsNot Nothing AndAlso _options.InjectAsTempFile Then
                tempParamPath = WriteTempParameterFile(parameters)
            End If

            ' Open document once
            Dim doc As Object = Nothing
            Try
                doc = OpenDocument(workingPath)
            Catch ex As Exception
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail("(sequence)", templatePath,
                    $"Failed to open template: {ex.Message}")
            End Try

            If doc Is Nothing Then
                CleanupWorkingCopy(workingPath, templatePath)
                CleanupTempFile(tempParamPath)
                Return MacroRunResult.Fail("(sequence)", templatePath, "SolidWorks returned null document.")
            End If

            ' Inject properties once
            If parameters IsNot Nothing AndAlso _options.InjectAsCustomProperties Then
                Try
                    InjectCustomProperties(doc, parameters)
                Catch ex As Exception
                    Log($"  WARNING: Property injection failed — {ex.Message}")
                End Try
            End If

            ' Run each macro step in order
            For i = 0 To macroSteps.Count - 1
                Dim _step = macroSteps(i)
                Log($"  Step {i + 1}/{macroSteps.Count}: {_step.MacroPath} → {_step.[Module]}.{_step.Procedure}")

                If Not File.Exists(_step.MacroPath) Then
                    CloseDocument(doc, False)
                    CleanupWorkingCopy(workingPath, templatePath)
                    CleanupTempFile(tempParamPath)
                    Return MacroRunResult.Fail(_step.MacroPath, templatePath,
                        $"Step {i + 1}: macro file not found: {_step.MacroPath}")
                End If

                Dim swErr As Integer = 0
                Dim ok = RunMacro(_step.MacroPath, _step.[Module], _step.Procedure, swErr)
                If Not ok Then
                    CloseDocument(doc, False)
                    CleanupWorkingCopy(workingPath, templatePath)
                    CleanupTempFile(tempParamPath)
                    Return MacroRunResult.Fail(_step.MacroPath, templatePath,
                        $"Step {i + 1} failed. SW error {swErr}: {TranslateSwError(swErr)}", swErr)
                End If
            Next

            ' Clean up properties
            If parameters IsNot Nothing AndAlso _options.InjectAsCustomProperties Then
                Try : RemoveInjectedProperties(doc, parameters) : Catch : End Try
            End If

            ' Save output
            Dim saveOk = False
            Try
                saveOk = SaveDocumentAs(doc, outputPath)
            Catch ex As Exception
                Log($"  ERROR saving sequence output: {ex.Message}")
            End Try

            If _options.CloseDocumentAfterRun Then CloseDocument(doc, False)
            CleanupWorkingCopy(workingPath, templatePath)
            CleanupTempFile(tempParamPath)

            sw.Stop()
            Return If(saveOk,
                MacroRunResult.Ok("(sequence)", templatePath, outputPath, sw.ElapsedMilliseconds),
                MacroRunResult.Fail("(sequence)", templatePath,
                    "Sequence ran but output save failed.", 0, sw.ElapsedMilliseconds))
        End Function

        ' ---- private helpers ---------------------------------

        ''' <summary>Opens a SolidWorks document, returning IModelDoc2.</summary>
        Private Function OpenDocument(filePath As String) As Object
            Dim docType = InferDocumentType(filePath)
            Dim errors As Integer = 0
            Dim warnings As Integer = 0

            ' Build options bitmask
            Dim openOptions As Integer = 0
            If _options.SilentMode Then openOptions = openOptions Or swOpenDocOptions_Silent

            Log($"  Opening document (type={docType}): {Path.GetFileName(filePath)}")

            ' OpenDoc6: (FileName, Type, Options, Configuration, Errors, Warnings) → IModelDoc2
            Dim doc As Object = _swApp.OpenDoc6(
                filePath,
                docType,
                openOptions,
                "",
                errors,
                warnings)

            If errors <> 0 Then
                Log($"  OpenDoc6 warnings: {warnings}, errors: {errors}")
            End If

            ' ── Critical: explicitly activate the opened document ─────────────────
            ' OpenDoc6 does not guarantee the new document becomes swApp.ActiveDoc,
            ' especially when SolidWorks was already running with another doc open.
            ' The VBA macro uses swApp.ActiveDoc — without this call it will modify
            ' the wrong document and the working copy will be saved unchanged.
            If doc IsNot Nothing Then
                Try
                    Dim activateErrors As Integer = 0
                    _swApp.ActivateDoc2(filePath, False, activateErrors)
                    Log($"  ActivateDoc2: errors={activateErrors}")
                Catch ex As Exception
                    Log($"  WARNING: ActivateDoc2 failed — {ex.Message}")
                End Try
            End If

            Return doc
        End Function

        ''' <summary>
        ''' Infer swDocumentTypes_e from file extension.
        ''' </summary>
        Private Shared Function InferDocumentType(filePath As String) As Integer
            Select Case Path.GetExtension(filePath).ToUpperInvariant()
                Case ".SLDPRT" : Return swDocPART
                Case ".SLDASM" : Return swDocASSEMBLY
                Case ".SLDDRW" : Return swDocDRAWING
                Case Else
                    Throw New NotSupportedException(
                        $"Unsupported SolidWorks file extension: {Path.GetExtension(filePath)}")
            End Select
        End Function

        ''' <summary>
        ''' Calls SldWorks.RunMacro2. Returns True on success.
        ''' swErr receives the SolidWorks error code on failure.
        ''' </summary>
        Private Function RunMacro(macroPath As String,
                                   [module] As String,
                                   procedure As String,
                                   ByRef swErr As Integer) As Boolean
            Log($"  RunMacro2: {Path.GetFileName(macroPath)} → {[module]}.{procedure}")
            ' RunMacro2(MacroFile, ModuleName, ProcedureName, Options, Errors) → Boolean
            Dim result As Boolean = _swApp.RunMacro2(
                macroPath,
                [module],
                procedure,
                swRunMacroUnloadAfterRun,
                swErr)
            Log($"  RunMacro2 result={result}, swErr={swErr}")
            Return result
        End Function

        ''' <summary>
        ''' Writes parameter key=value pairs as Custom Properties
        ''' on the SolidWorks document's root configuration.
        ''' VBA reads them with: ActiveDoc.CustomInfo2("", "KEY")
        ''' </summary>
        Private Sub InjectCustomProperties(doc As Object, parameters As MacroParameters)
            ' IModelDoc2.Extension.CustomPropertyManager provides
            ' a per-configuration custom property set.
            ' Using "" as config name targets the document-level
            ' (non-config-specific) custom properties.
            Dim custMgr As Object = Nothing
            Try
                custMgr = doc.Extension.CustomPropertyManager("")
            Catch ex As Exception
                Log($"  WARNING: Could not get CustomPropertyManager — {ex.Message}")
                Return
            End Try

            If custMgr Is Nothing Then
                Log("  WARNING: CustomPropertyManager returned null — skipping property injection.")
                Return
            End If

            Dim injected As Integer = 0
            For Each kvp In parameters.Values
                Try
                    ' Add2(FieldName, FieldType, FieldValue, OverwriteExisting)
                    ' swCustomInfoType_e.swCustomInfoText = 30
                    custMgr.Add3(kvp.Key, 30, kvp.Value, True)
                    injected += 1
                Catch ex As Exception
                    Log($"  WARNING: Could not inject property '{kvp.Key}' — {ex.Message}")
                End Try
            Next

            Log($"  Injected {injected}/{parameters.Values.Count} custom properties.")
        End Sub

        ''' <summary>
        ''' Removes all injected parameter properties from the
        ''' document to avoid polluting the saved output file.
        ''' </summary>
        Private Sub RemoveInjectedProperties(doc As Object, parameters As MacroParameters)
            Dim custMgr As Object
            Try
                custMgr = doc.Extension.CustomPropertyManager("")
            Catch
                Return
            End Try
            If custMgr Is Nothing Then Return

            For Each key In parameters.Values.Keys
                Try
                    custMgr.Delete(key)
                Catch
                    ' Property may not exist if injection failed; ignore
                End Try
            Next
        End Sub

        ''' <summary>
        ''' Writes the parameter file to the temp directory.
        ''' Returns the full path of the written file, or Nothing
        ''' if the write fails.
        ''' </summary>
        Private Function WriteTempParameterFile(parameters As MacroParameters) As String
            Try
                Dim _path = Path.Combine(_options.TempDirectory, _options.TempParameterFileName)
                File.WriteAllText(_path, parameters.ToParameterFileContent(), Encoding.UTF8)
                Log($"  Temp parameter file: {_path} ({parameters.Values.Count} entries)")
                Return _path
            Catch ex As Exception
                Log($"  WARNING: Could not write temp parameter file — {ex.Message}")
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Saves the active document as a new file at outputPath.
        ''' Preserves the SolidWorks file type (part/assembly).
        ''' Returns True on success.
        ''' </summary>
        Private Function SaveDocumentAs(doc As Object, outputPath As String) As Boolean
            Log($"  Saving output: {Path.GetFileName(outputPath)}")
            Try
                ' IModelDoc2.SaveAs3(FileName, Version, Options) → Boolean
                ' Options: 0 = default
                Dim ok As Boolean = doc.SaveAs3(outputPath, SW_SAVE_AS_CURRENT_VERSION, 0)
                If ok Then
                    Log($"  Save successful.")
                Else
                    ' Fallback: try SaveAs (older API)
                    ok = doc.SaveAs(outputPath)
                    Log($"  SaveAs fallback result: {ok}")
                End If
                Return ok
            Catch ex As Exception
                Log($"  ERROR in SaveDocumentAs: {ex.Message}")
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Closes a SolidWorks document. saveFirst=True performs
        ''' a save before close (use False when output already saved).
        ''' </summary>
        Private Sub CloseDocument(doc As Object, saveFirst As Boolean)
            Try
                If saveFirst Then
                    doc.Save3(1, 0, 0)  ' swSaveAsOptions_e.swSaveAsOptions_Silent = 1
                End If
                ' CloseDoc takes the full document path
                _swApp.CloseDoc(doc.GetPathName())
                Log($"  Document closed.")
            Catch ex As Exception
                Log($"  WARNING: CloseDoc failed — {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Builds a temp path for the working copy of a template.
        ''' Placed next to the original with a _work suffix.
        ''' </summary>
        Private Shared Function BuildWorkingCopyPath(templatePath As String) As String
            Dim dir = Path.GetDirectoryName(templatePath)
            Dim nameNoExt = Path.GetFileNameWithoutExtension(templatePath)
            Dim ext = Path.GetExtension(templatePath)
            Return Path.Combine(dir, $"{nameNoExt}_work{ext}")
        End Function

        ''' <summary>
        ''' Deletes the working copy if it differs from the original
        ''' (i.e., we actually made a copy and should clean it up).
        ''' </summary>
        Private Sub CleanupWorkingCopy(workingPath As String, originalPath As String)
            If _options.OpenTemplateAsCopy AndAlso
               Not String.Equals(workingPath, originalPath, StringComparison.OrdinalIgnoreCase) Then
                Try
                    If File.Exists(workingPath) Then
                        File.Delete(workingPath)
                        Log($"  Working copy deleted: {Path.GetFileName(workingPath)}")
                    End If
                Catch ex As Exception
                    Log($"  WARNING: Could not delete working copy — {ex.Message}")
                End Try
            End If
        End Sub

        ''' <summary>Deletes the temp parameter file if it exists.</summary>
        Private Sub CleanupTempFile(tempPath As String)
            If String.IsNullOrEmpty(tempPath) Then Return
            Try
                If File.Exists(tempPath) Then File.Delete(tempPath)
            Catch
            End Try
        End Sub

        ''' <summary>Validates that required files exist and paths are absolute.</summary>
        Private Shared Function ValidateInputs(templatePath As String,
                                                macroPath As String,
                                                outputPath As String) As String
            If String.IsNullOrWhiteSpace(templatePath) Then Return "Template path is empty."
            If Not Path.IsPathRooted(templatePath) Then Return $"Template path must be absolute: {templatePath}"
            If Not File.Exists(templatePath) Then Return $"Template file not found: {templatePath}"

            If String.IsNullOrWhiteSpace(macroPath) Then Return "Macro path is empty."
            If Not Path.IsPathRooted(macroPath) Then Return $"Macro path must be absolute: {macroPath}"
            If Not File.Exists(macroPath) Then Return $"Macro file not found: {macroPath}"

            If String.IsNullOrWhiteSpace(outputPath) Then Return "Output path is empty."
            If Not Path.IsPathRooted(outputPath) Then Return $"Output path must be absolute: {outputPath}"

            Return Nothing  ' All good
        End Function

        ''' <summary>Human-readable label for common SW error codes.</summary>
        Private Shared Function TranslateSwError(code As Long) As String
            Select Case code
                Case 0 : Return "No error"
                Case 1 : Return "File not found"
                Case 2 : Return "Access denied"
                Case 3 : Return "Internal SolidWorks error"
                Case 4 : Return "Macro module not found"
                Case 5 : Return "Macro procedure not found"
                Case 6 : Return "Document is read-only"
                Case 7 : Return "Out of memory"
                Case Else : Return $"Unknown error code {code}"
            End Select
        End Function

        ''' <summary>Appends a timestamped entry to the internal log.</summary>
        Private Sub Log(message As String)
            Dim entry = $"[{DateTime.Now:HH:mm:ss.fff}] {message}"
            _log.Add(entry)
            System.Diagnostics.Debug.WriteLine($"[MacroRunner] {entry}")
        End Sub

    End Class

    ' ----------------------------------------------------------
    ' MacroBatchRunner
    ' Runs a collection of (template, macro, output) jobs in
    ' sequence, accumulating results. Suitable for processing
    ' a full ComponentList from Module 1 in one call.
    ' ----------------------------------------------------------

    ''' <summary>
    ''' Describes a single job in a batch run.
    ''' </summary>
    Public Class MacroBatchJob

        Public Property TemplatePath As String
        Public Property MacroPath As String
        Public Property MacroModule As String
        Public Property MacroProcedure As String
        Public Property Parameters As MacroParameters
        Public Property OutputPath As String

        ''' <summary>Friendly label used in logs and result reporting.</summary>
        Public Property Label As String = String.Empty

        Public Sub New(templatePath As String,
                       macroPath As String,
                       macroModule As String,
                       macroProcedure As String,
                       parameters As MacroParameters,
                       outputPath As String,
                       Optional label As String = "")
            Me.TemplatePath = templatePath
            Me.MacroPath = macroPath
            Me.MacroModule = macroModule
            Me.MacroProcedure = macroProcedure
            Me.Parameters = parameters
            Me.OutputPath = outputPath
            Me.Label = If(String.IsNullOrEmpty(label), Path.GetFileNameWithoutExtension(macroPath), label)
        End Sub
    End Class

    ''' <summary>
    ''' Result of running a MacroBatchJob.
    ''' </summary>
    Public Class MacroBatchResult

        Public ReadOnly Property Job As MacroBatchJob
        Public ReadOnly Property RunResult As MacroRunResult

        Public Sub New(job As MacroBatchJob, runResult As MacroRunResult)
            Me.Job = job
            Me.RunResult = runResult
        End Sub

        Public ReadOnly Property Label As String
            Get
                Return Job.Label
            End Get
        End Property

        Public ReadOnly Property Success As Boolean
            Get
                Return RunResult.Success
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Orchestrates multiple macro jobs against a single SolidWorks
    ''' instance, processing all selected components from Module 1.
    ''' </summary>
    Public Class MacroBatchRunner

        Private ReadOnly _runner As MacroRunner
        Private ReadOnly _log As List(Of String)

        Public Sub New(swApp As Object, Optional options As MacroRunnerOptions = Nothing)
            _runner = New MacroRunner(swApp, options)
            _log = New List(Of String)()
        End Sub

        ''' <summary>
        ''' Execute a list of batch jobs sequentially.
        ''' Continues on individual failures unless stopOnFirstFailure=True.
        ''' </summary>
        ''' <returns>
        ''' List of results — one per job, in submission order.
        ''' </returns>
        Public Function RunBatch(jobs As IList(Of MacroBatchJob),
                                 Optional stopOnFirstFailure As Boolean = False,
                                 Optional progress As IProgress(Of (Current As Integer, Total As Integer, Label As String)) = Nothing
                                 ) As List(Of MacroBatchResult)

            Dim _results As New List(Of MacroBatchResult)()

            _log.Add($"[{DateTime.Now:HH:mm:ss}] Batch start — {jobs.Count} jobs")

            For i = 0 To jobs.Count - 1
                Dim job = jobs(i)
                _log.Add($"  [{i + 1}/{jobs.Count}] {job.Label}")

                progress?.Report((i + 1, jobs.Count, job.Label))

                Dim _result = _runner.RunMacroOnTemplate(
                    job.TemplatePath,
                    job.MacroPath,
                    job.MacroModule,
                    job.MacroProcedure,
                    job.Parameters,
                    job.OutputPath)

                _results.Add(New MacroBatchResult(job, _result))

                _log.Add($"  → {_result}")

                If Not _result.Success AndAlso stopOnFirstFailure Then
                    _log.Add("  STOPPING batch due to failure (stopOnFirstFailure=True).")
                    Exit For
                End If
            Next

            Dim successCount As Integer = 0
            For Each r In _results
                If r.Success Then successCount += 1
            Next
            _log.Add($"[{DateTime.Now:HH:mm:ss}] Batch complete — {successCount}/{_results.Count} succeeded.")

            Return _results
        End Function

        ''' <summary>Returns the combined log from all runs in this batch session.</summary>
        Public Function GetLog() As IReadOnlyList(Of String)
            Dim combined = New List(Of String)(_log)
            combined.AddRange(_runner.GetLog())
            Return combined.AsReadOnly()
        End Function

    End Class

End Namespace
