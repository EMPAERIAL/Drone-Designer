' ===================================================================
' MODULE 2 INTEGRATION EXAMPLE
' File: SolidWorks/Module2UsageExample.vb
'
' This file is NOT part of the build — it is a reference snippet
' showing exactly how MainForm.vb (or a dedicated Module2 controller)
' should wire up the SolidWorksAutomation class after Module 1 has
' selected a motor component.
'
' Delete this file once the actual UI integration is written (Task 15+).
' ===================================================================

' Imports DroneDesigner.Core.Models
' Imports DroneDesigner.SolidWorks

' ---------------------------------------------------------------
' Called from the "Generate CAD" button click handler in MainForm.
' selectedMotor is the ComponentSpecs object produced by Module 1.
' ---------------------------------------------------------------
'
' Private Async Sub btnGenerateCAD_Click(sender As Object, e As EventArgs) _
'         Handles btnGenerateCAD.Click
'
'     btnGenerateCAD.Enabled = False
'     lblStatus.Text = "Connecting to SolidWorks…"
'
'     ' Resolve macro directory relative to the running executable
'     Dim macroDir As String = Path.Combine(
'         AppDomain.CurrentDomain.BaseDirectory, "Resources", "Macros")
'
'     Dim outputDir As String = Path.Combine(
'         Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
'         "DroneDesigner", "GeneratedParts")
'
'     Try
'         Using sw As New SolidWorksAutomation(macroDir)
'
'             Await Task.Run(Sub()
'                 sw.Connect()                  ' may launch SW — can be slow
'             End Sub)
'
'             lblStatus.Text = "Building motor mount…"
'
'             Dim partPath As String = Await Task.Run(Function()
'                 Return sw.GenerateMotorMount(selectedMotor, outputDir)
'             End Function)
'
'             lblStatus.Text = $"Done: {Path.GetFileName(partPath)}"
'             MessageBox.Show($"Motor mount saved:{Environment.NewLine}{partPath}",
'                             "DroneDesigner", MessageBoxButtons.OK,
'                             MessageBoxIcon.Information)
'         End Using
'
'     Catch ex As SolidWorksConnectionException
'         MessageBox.Show($"Could not connect to SolidWorks:{Environment.NewLine}{ex.Message}",
'                         "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
'         lblStatus.Text = "SolidWorks connection failed."
'
'     Catch ex As SolidWorksMacroException
'         MessageBox.Show($"Macro error:{Environment.NewLine}{ex.Message}",
'                         "CAD Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
'         lblStatus.Text = "CAD generation failed."
'
'     Catch ex As Exception
'         MessageBox.Show($"Unexpected error:{Environment.NewLine}{ex.Message}",
'                         "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
'         lblStatus.Text = "Error."
'
'     Finally
'         btnGenerateCAD.Enabled = True
'     End Try
'
' End Sub

' ---------------------------------------------------------------
' WHAT ComponentSpecs.MechanicalSpecs SHOULD CONTAIN FOR A MOTOR
'
' The ComponentSelectionEngine (Module 1) should populate these keys
' when it selects a motor.  All values are in millimetres.
'
' Key                   Example     Notes
' ---                   -------     -----
' OuterDiameter         28.0        Motor can OD (e.g. 2208 → 22 mm stator, ~28 mm can)
' StatorDiameter        22.0        Stator diameter (used as fallback for OuterDiameter)
' BoltCircleDiameter    16.0        Motor mounting bolt circle Ø
' BoltHoleDiameter       3.2        M3 clearance = 3.2 mm
' NumBoltHoles           4          Standard UAV motor: 4 bolts
' MountPlateThickness    3.0        Carbon-fibre / aluminium plate thickness
'
' If any key is absent, SolidWorksAutomation.DeriveMotorMountParams
' applies the fallback values shown above.
' ---------------------------------------------------------------

' ---------------------------------------------------------------
' DEPLOYMENT CHECKLIST (run before first use)
'
'  [1] Copy MotorMount.swb to <AppDir>\Resources\SolidWorks\Macros\
'  [2] Ensure SolidWorks is installed and licensed on the same machine
'  [3] Set the default part template in SolidWorks:
'        Tools > Options > File Locations > Document Templates
'  [4] Confirm SolidWorks COM registration:
'        HKEY_LOCAL_MACHINE\SOFTWARE\SolidWorks\AddIns  (should exist)
'  [5] .NET target framework: 4.7.2 or 4.8
'        (SolidWorks 2020+ ships with interop DLLs for these versions)
'
' OUTPUT
'   %USERPROFILE%\Documents\DroneDesigner\GeneratedParts\
'     MotorMount_<ComponentName>_<timestamp>.sldprt
' ---------------------------------------------------------------
