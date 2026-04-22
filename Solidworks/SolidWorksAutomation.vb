' =============================================================================
' File:        SolidWorks/SolidWorksAutomation.vb
' Project:     Drone Designer
' Description: Manages the COM connection to a SolidWorks instance.
'              Supports both attaching to a running instance and launching
'              a fresh one. All SolidWorks interaction in Module 2 goes
'              through this class.
'
' Target SolidWorks Version: 2021 SP0 and later (API version 29.0+)
'   - Uses late binding (Object) so the assembly compiles without a direct
'     reference to a specific SldWorks.Interop.sldworks.dll version.
'   - To switch to early binding for IntelliSense: add a COM reference to
'     "SOLIDWORKS 20XX Type Library" and replace every `As Object` with the
'     concrete SldWorks types (ISldWorks, IModelDoc2, etc.).
'
' .NET Target: .NET Framework 4.7.2
'   SolidWorks 2021+ ships interop assemblies built against .NET 4.x.
'   Do NOT use .NET 5/6/7 — SolidWorks COM interop is not supported there.
'
' Dependencies:
'   - SolidWorks must be installed on the same machine.
'   - The process must run as the same Windows user that holds the SW licence.
'   - No additional NuGet packages required; uses built-in System.Runtime
'     .InteropServices and Microsoft.Win32 for registry checks.
'
' Usage (from Module 2 entry point):
'   Dim sw As New SolidWorksAutomation()
'   If sw.Connect() Then
'       Console.WriteLine(sw.GetVersion())
'       ' ... drive macros ...
'       sw.Disconnect()
'   End If
' =============================================================================

Imports System
Imports System.Runtime.InteropServices
Imports Microsoft.Win32

Namespace SolidWorks

    ''' <summary>
    ''' Manages the lifecycle of a SolidWorks COM connection.
    ''' Provides <see cref="Connect"/>, <see cref="Disconnect"/>,
    ''' <see cref="IsConnected"/>, and <see cref="GetVersion"/> as the
    ''' public surface used by the rest of Module 2.
    ''' </summary>
    ''' <remarks>
    ''' Design notes:
    ''' • Late binding is used so the project compiles on any machine, even
    '''   without SolidWorks installed (useful for CI or Module-1-only work).
    ''' • <see cref="Connect"/> tries GetActiveObject first (attach to a
    '''   running SolidWorks window) before falling back to CreateObject
    '''   (launch a new invisible instance). This mirrors the behaviour of the
    '''   built-in SolidWorks macro recorder.
    ''' • The class implements IDisposable so a Using block can guarantee
    '''   cleanup even if an unhandled exception occurs mid-macro.
    ''' </remarks>
    Public Class SolidWorksAutomation
        Implements IDisposable

        ' ------------------------------------------------------------------ '
        ' Constants                                                            '
        ' ------------------------------------------------------------------ '

        ''' <summary>COM ProgID registered by the SolidWorks installer.</summary>
        Private Const SW_PROG_ID As String = "SldWorks.Application"

        ''' <summary>
        ''' Minimum API version we are willing to work with.
        ''' SolidWorks 2021 = API 29.0.  Adjust if targeting an older release.
        ''' </summary>
        Private Const MIN_API_VERSION As Integer = 29

        ''' <summary>
        ''' Registry key written by the SolidWorks installer (64-bit hive).
        ''' Used to confirm installation before attempting COM activation.
        ''' </summary>
        Private Const SW_REGISTRY_KEY As String =
            "SOFTWARE\SolidWorks\SOLIDWORKS"

        ' ------------------------------------------------------------------ '
        ' Private state                                                        '
        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' The live COM object.  Typed as Object for late-binding portability.
        ''' Cast to ISldWorks if you add the early-binding interop reference.
        ''' </summary>
        Private _swApp As Object = Nothing

        ''' <summary>True once <see cref="Connect"/> succeeds.</summary>
        Private _isConnected As Boolean = False

        ''' <summary>
        ''' True when *we* launched SolidWorks (as opposed to attaching to an
        ''' already-running instance).  Used in <see cref="Disconnect"/> to
        ''' decide whether to call ExitApp.
        ''' </summary>
        Private _weOwnTheProcess As Boolean = False

        ''' <summary>IDisposable bookkeeping.</summary>
        Private _disposed As Boolean = False

        ' ------------------------------------------------------------------ '
        ' Public interface                                                      '
        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Establishes a COM connection to SolidWorks.
        ''' Tries to attach to an already-running instance first; if none is
        ''' found, launches a new (initially invisible) instance.
        ''' </summary>
        ''' <returns>
        ''' <c>True</c> on success; <c>False</c> if connection could not be
        ''' established.  Detailed failure information is surfaced through
        ''' <paramref name="errorMessage"/>.
        ''' </returns>
        ''' <param name="errorMessage">
        ''' Human-readable error description when the method returns False.
        ''' Empty string on success.
        ''' </param>
        Public Function Connect(Optional ByRef errorMessage As String = "") As Boolean

            errorMessage = String.Empty

            ' Guard: already connected — nothing to do.
            If _isConnected Then
                Return True
            End If

            ' ── Step 1: Verify SolidWorks is installed ───────────────────── '
            If Not IsSolidWorksInstalled() Then
                errorMessage = "SolidWorks does not appear to be installed on this machine. " &
                               "Check that SOLIDWORKS is present under " &
                               $"HKLM\{SW_REGISTRY_KEY}."
                Return False
            End If

            ' ── Step 2: Try attaching to a running instance ───────────────── '
            Try
                _swApp = Marshal.GetActiveObject(SW_PROG_ID)
                _weOwnTheProcess = False
            Catch ex As COMException
                ' HRESULT 0x800401E3 = MK_E_UNAVAILABLE — nothing running yet.
                ' Any other COM error is unexpected; surface it.
                If ex.ErrorCode = COMErrorCodes.MK_E_UNAVAILABLE Then
                    _swApp = Nothing   ' Will try CreateObject below.
                Else
                    errorMessage = BuildCOMErrorMessage(ex)
                    Return False
                End If
            Catch ex As Exception
                errorMessage = $"Unexpected error while checking for a running SolidWorks instance: {ex.Message}"
                Return False
            End Try

            ' ── Step 3: Launch a new instance if none was running ─────────── '
            If _swApp Is Nothing Then
                Try
                    _swApp = CreateObject(SW_PROG_ID)
                    _weOwnTheProcess = True
                Catch ex As COMException
                    errorMessage = BuildCOMErrorMessage(ex)
                    Return False
                Catch ex As Exception
                    errorMessage = $"Failed to launch SolidWorks via CreateObject: {ex.Message}"
                    Return False
                End Try
            End If

            ' ── Step 4: Confirm the object is alive ───────────────────────── '
            If _swApp Is Nothing Then
                errorMessage = "COM object was created but is Nothing — COM activation failed silently."
                Return False
            End If

            ' ── Step 5: Make the application visible (if we launched it) ──── '
            '    This is optional but helpful during MVP development so the
            '    developer can see what the automation is doing in real time.
            If _weOwnTheProcess Then
                Try
                    _swApp.Visible = True
                Catch
                    ' Non-fatal — some versions require UserControl = True first.
                    Try
                        _swApp.UserControl = True
                    Catch
                        ' Ignore; visibility is cosmetic during MVP.
                    End Try
                End Try
            End If

            ' ── Step 6: Version compatibility check ───────────────────────── '
            Dim versionOk As Boolean = False
            Dim versionMessage As String = String.Empty

            versionOk = CheckVersionCompatibility(versionMessage)
            If Not versionOk Then
                ' Release the object — we won't use an incompatible version.
                ReleaseComObject()
                errorMessage = versionMessage
                Return False
            End If

            _isConnected = True
            Return True

        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Releases the COM connection.
        ''' If we launched SolidWorks ourselves and no documents are open,
        ''' the application is closed.  If the user had SolidWorks open before
        ''' we connected, it is left running.
        ''' </summary>
        Public Sub Disconnect()

            If Not _isConnected OrElse _swApp Is Nothing Then
                Return
            End If

            Try
                If _weOwnTheProcess Then
                    ' Only close the application if it has no open documents —
                    ' avoids silently discarding work the user may have open.
                    Dim openDocCount As Integer = 0
                    Try
                        openDocCount = _swApp.GetDocumentCount()
                    Catch
                        ' If we cannot query, err on the side of caution and
                        ' leave SolidWorks running.
                        openDocCount = 1
                    End Try

                    If openDocCount = 0 Then
                        _swApp.ExitApp()
                    End If
                End If
            Catch ex As Exception
                ' Log but do not throw — Disconnect must always succeed.
                System.Diagnostics.Debug.WriteLine(
                    $"[SolidWorksAutomation] Warning during Disconnect: {ex.Message}")
            Finally
                ReleaseComObject()
                _isConnected = False
                _weOwnTheProcess = False
            End Try

        End Sub

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Returns <c>True</c> if the COM object is held and SolidWorks
        ''' is still responding.
        ''' </summary>
        ''' <remarks>
        ''' A quick liveness ping is performed (reading the Visible property)
        ''' rather than relying solely on the cached flag, so the caller gets
        ''' an accurate answer even if SolidWorks crashed after Connect().
        ''' </remarks>
        Public Function IsConnected() As Boolean

            If Not _isConnected OrElse _swApp Is Nothing Then
                Return False
            End If

            Try
                ' Reading any property forces a COM round-trip.
                ' If SolidWorks has crashed, this will throw.
                Dim dummy As Boolean = _swApp.Visible
                Return True
            Catch
                ' SolidWorks is gone — update our state to reflect reality.
                _isConnected = False
                _swApp = Nothing
                Return False
            End Try

        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Returns a human-readable version string, e.g. "SOLIDWORKS 2023 SP2".
        ''' Returns an empty string if not connected.
        ''' </summary>
        Public Function GetVersion() As String

            If Not IsConnected() Then
                Return String.Empty
            End If

            Try
                ' RevisionNumber returns a string like "31.2.0" (SW 2023 SP2).
                ' GetBuildNumbers2 would give the full build, but RevisionNumber
                ' is simpler for display purposes.
                Dim revision As String = _swApp.RevisionNumber()

                ' Map major revision digit to marketing year.
                ' SolidWorks 2021 = 29, 2022 = 30, 2023 = 31, 2024 = 32, etc.
                Dim yearLabel As String = RevisionToYear(revision)

                Return $"SOLIDWORKS {yearLabel} (API {revision})"

            Catch ex As Exception
                Return $"[Version query failed: {ex.Message}]"
            End Try

        End Function

        ' ------------------------------------------------------------------ '
        ' Package-internal accessor                                            '
        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Provides the raw COM application object to other classes in the
        ''' SolidWorks namespace (e.g. MacroRunner, PartBuilder).
        ''' Returns Nothing if not connected.
        ''' </summary>
        ''' <remarks>
        ''' Callers must handle the case where this returns Nothing and must
        ''' not cache the reference across a Disconnect/Connect cycle.
        ''' </remarks>
        Friend ReadOnly Property Application As Object
            Get
                Return If(_isConnected, _swApp, Nothing)
            End Get
        End Property

        ' ------------------------------------------------------------------ '
        ' Private helpers                                                       '
        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Checks the registry for a SolidWorks installation entry.
        ''' This is a fast pre-flight check before attempting COM activation,
        ''' which produces a less informative error on an unmachined machine.
        ''' </summary>
        Private Function IsSolidWorksInstalled() As Boolean
            Try
                Using key As RegistryKey = Registry.LocalMachine.OpenSubKey(SW_REGISTRY_KEY)
                    Return key IsNot Nothing
                End Using
            Catch
                ' If we cannot read the registry, optimistically proceed and
                ' let COM activation surface the real error.
                Return True
            End Try
        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Verifies that the connected SolidWorks instance meets the minimum
        ''' API version requirement defined by <see cref="MIN_API_VERSION"/>.
        ''' </summary>
        Private Function CheckVersionCompatibility(ByRef message As String) As Boolean

            message = String.Empty

            Try
                Dim revision As String = _swApp.RevisionNumber()
                If String.IsNullOrWhiteSpace(revision) Then
                    message = "Could not retrieve SolidWorks revision number."
                    Return False
                End If

                ' RevisionNumber format: "MajorVersion.SPNumber.HotfixNumber"
                ' e.g. "29.0.0" for SolidWorks 2021 SP0
                Dim parts() As String = revision.Split("."c)
                Dim majorVersion As Integer = 0

                If parts.Length < 1 OrElse Not Integer.TryParse(parts(0), majorVersion) Then
                    ' Cannot parse — warn but allow, rather than blocking.
                    message = $"Could not parse major version from revision '{revision}'. Proceeding anyway."
                    Return True
                End If

                If majorVersion < MIN_API_VERSION Then
                    message = $"SolidWorks API version {revision} is below the minimum required " &
                              $"version {MIN_API_VERSION}.x (SolidWorks 2021). " &
                              "Please upgrade SolidWorks or adjust MIN_API_VERSION in SolidWorksAutomation.vb."
                    Return False
                End If

                Return True

            Catch ex As Exception
                ' Version check is best-effort; do not block connection on it.
                message = $"Version check error (non-fatal): {ex.Message}"
                Return True
            End Try

        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Maps a SolidWorks API major revision number to its marketing year.
        ''' Kept as a simple lookup table — extend as new versions ship.
        ''' </summary>
        Private Function RevisionToYear(revision As String) As String

            Dim parts() As String = revision.Split("."c)
            Dim major As Integer = 0

            If parts.Length < 1 OrElse Not Integer.TryParse(parts(0), major) Then
                Return "Unknown"
            End If

            ' SolidWorks major revision → calendar year offset from 1992.
            ' SW 1.0 shipped in 1995; by convention SW 2021 = revision 29.
            ' Formula: Year = 1992 + major  (holds from SW 2018 onwards).
            If major >= 26 Then   ' SW 2018 = revision 26
                Return (1992 + major).ToString()
            End If

            Return $"(rev {major})"

        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Produces a user-friendly error message for common COM HRESULTs
        ''' encountered when activating SolidWorks.
        ''' </summary>
        Private Function BuildCOMErrorMessage(ex As COMException) As String

            Select Case ex.ErrorCode

                Case COMErrorCodes.REGDB_E_CLASSNOTREG
                    Return "SolidWorks is not registered as a COM server. " &
                           "Reinstalling SolidWorks usually fixes this. " &
                           $"(HRESULT: 0x{ex.ErrorCode:X8})"

                Case COMErrorCodes.CO_E_SERVER_EXEC_FAILURE
                    Return "SolidWorks process failed to start (CO_E_SERVER_EXEC_FAILURE). " &
                           "This can happen when a SolidWorks licence is not available, " &
                           "or when the process is already running but unresponsive. " &
                           $"(HRESULT: 0x{ex.ErrorCode:X8})"

                Case COMErrorCodes.E_ACCESSDENIED
                    Return "Access denied when connecting to SolidWorks. " &
                           "Ensure this application runs as the same Windows user " &
                           "that holds the SolidWorks licence. " &
                           $"(HRESULT: 0x{ex.ErrorCode:X8})"

                Case COMErrorCodes.RPC_E_DISCONNECTED
                    Return "The SolidWorks COM server was disconnected unexpectedly. " &
                           "SolidWorks may have crashed. Try restarting SolidWorks. " &
                           $"(HRESULT: 0x{ex.ErrorCode:X8})"

                Case Else
                    Return $"COM error connecting to SolidWorks: {ex.Message} " &
                           $"(HRESULT: 0x{ex.ErrorCode:X8})"

            End Select

        End Function

        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Releases the COM reference and nulls the field.
        ''' Safe to call multiple times.
        ''' </summary>
        Private Sub ReleaseComObject()
            If _swApp IsNot Nothing Then
                Try
                    Marshal.FinalReleaseComObject(_swApp)
                Catch
                    ' Suppress — object may already be dead.
                End Try
                _swApp = Nothing
            End If
        End Sub

        ' ------------------------------------------------------------------ '
        ' IDisposable                                                           '
        ' ------------------------------------------------------------------ '

        ''' <summary>
        ''' Ensures the COM connection is released when the object is garbage
        ''' collected or falls out of a Using block.
        ''' </summary>
        Public Sub Dispose() Implements IDisposable.Dispose
            If Not _disposed Then
                Disconnect()
                _disposed = True
            End If
            GC.SuppressFinalize(Me)
        End Sub

        Protected Overrides Sub Finalize()
            If Not _disposed Then
                ReleaseComObject()
            End If
            MyBase.Finalize()
        End Sub

    End Class

    ' ======================================================================= '
    ' Supporting type: well-known COM HRESULTs                                 '
    ' ======================================================================= '

    ''' <summary>
    ''' Named constants for COM HRESULTs that can appear when activating or
    ''' communicating with SolidWorks.  Centralised here so the error-handling
    ''' switch is readable without magic numbers.
    ''' </summary>
    Friend Module COMErrorCodes

        ''' <summary>
        ''' 0x800401E3 — GetActiveObject found no running instance with that ProgID.
        ''' </summary>
        Public Const MK_E_UNAVAILABLE As Integer = CInt(&H800401E3)

        ''' <summary>
        ''' 0x80040154 — ProgID is not in the registry; SolidWorks not installed
        ''' or COM registration is broken.
        ''' </summary>
        Public Const REGDB_E_CLASSNOTREG As Integer = CInt(&H80040154)

        ''' <summary>
        ''' 0x80080005 — Out-of-process COM server failed to start; often a
        ''' licence or permission issue.
        ''' </summary>
        Public Const CO_E_SERVER_EXEC_FAILURE As Integer = CInt(&H80080005)

        ''' <summary>0x80070005 — Access denied.</summary>
        Public Const E_ACCESSDENIED As Integer = CInt(&H80070005)

        ''' <summary>
        ''' 0x80010004 — The COM server disconnected while a call was in flight;
        ''' typically means SolidWorks crashed.
        ''' </summary>
        Public Const RPC_E_DISCONNECTED As Integer = CInt(&H80010004)

    End Module

End Namespace
