Imports System.IO
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Json
Imports System.Text

Namespace Utilities

    ''' <summary>
    ''' Manages application-wide configuration settings for the Drone Designer tool.
    ''' Reads from and writes to a JSON config file (DroneDesigner.config.json) located
    ''' next to the executable. Access settings via ConfigManager.Settings from any module.
    '''
    ''' Config file is auto-created with defaults on first run if missing.
    ''' </summary>
    Public NotInheritable Class ConfigManager

        ' -------------------------------------------------------------------------
        ' Singleton / Static access
        ' -------------------------------------------------------------------------

        Private Shared _settings As AppSettings
        Private Shared _configFilePath As String

        ''' <summary>
        ''' The name of the JSON config file on disk.
        ''' </summary>
        Private Const CONFIG_FILE_NAME As String = "DroneDesigner.config.json"

        ''' <summary>
        ''' Returns the loaded application settings. Loads from disk on first access.
        ''' Thread-safe for read scenarios typical of a WinForms desktop app.
        ''' </summary>
        Public Shared ReadOnly Property Settings As AppSettings
            Get
                If _settings Is Nothing Then
                    Load()
                End If
                Return _settings
            End Get
        End Property

        ''' <summary>
        ''' Full path to the config file being used (next to the executable).
        ''' </summary>
        Public Shared ReadOnly Property ConfigFilePath As String
            Get
                If String.IsNullOrEmpty(_configFilePath) Then
                    _configFilePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        CONFIG_FILE_NAME)
                End If
                Return _configFilePath
            End Get
        End Property

        ' -------------------------------------------------------------------------
        ' Load / Save
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Loads settings from the JSON config file.
        ''' If the file does not exist, creates it with default values.
        ''' Throws ConfigurationException if the file is present but malformed.
        ''' </summary>
        Public Shared Sub Load()
            If Not File.Exists(ConfigFilePath) Then
                _settings = AppSettings.CreateDefaults()
                _settings.NormalizeLegacyRelativePaths()
                Save() ' Persist defaults so the user can edit the file
            Else
                Try
                    Dim json As String = File.ReadAllText(ConfigFilePath, Encoding.UTF8)
                    _settings = DeserializeJson(json)
                    If _settings Is Nothing Then
                        _settings = AppSettings.CreateDefaults()
                    End If

                    Dim didNormalize As Boolean = _settings.NormalizeLegacyRelativePaths()
                    If didNormalize Then
                        Save()
                    End If
                Catch ex As Exception
                    Throw New ConfigurationException(
                        $"Failed to load config file '{ConfigFilePath}': {ex.Message}", ex)
                End Try
            End If
        End Sub

        ''' <summary>
        ''' Saves the current in-memory settings back to the JSON config file.
        ''' Call this after programmatically changing any Settings property.
        ''' </summary>
        Public Shared Sub Save()
            If _settings Is Nothing Then
                Throw New InvalidOperationException("Settings have not been loaded. Call Load() first.")
            End If

            Try
                Dim json As String = SerializeJson(_settings)
                File.WriteAllText(ConfigFilePath, json, Encoding.UTF8)
            Catch ex As Exception
                Throw New ConfigurationException(
                    $"Failed to save config file '{ConfigFilePath}': {ex.Message}", ex)
            End Try
        End Sub

        ''' <summary>
        ''' Forces a reload from disk, discarding any unsaved in-memory changes.
        ''' Useful if the user edits the config file while the application is running.
        ''' </summary>
        Public Shared Sub Reload()
            _settings = Nothing
            Load()
        End Sub

        ' -------------------------------------------------------------------------
        ' JSON helpers (uses built-in DataContractJsonSerializer — no NuGet needed)
        ' -------------------------------------------------------------------------

        Private Shared Function SerializeJson(obj As AppSettings) As String
            Dim serializer As New DataContractJsonSerializer(GetType(AppSettings))
            Using ms As New MemoryStream()
                ' Use indented writer for human-readable config file
                Dim writerSettings As New System.Xml.XmlWriterSettings() With {
                    .Indent = True
                }
                ' DataContractJsonSerializer doesn't natively indent, so we route through
                ' a JsonReaderWriterFactory writer to get indented output.
                Using writer = System.Runtime.Serialization.Json.JsonReaderWriterFactory.
                                   CreateJsonWriter(ms, Encoding.UTF8, True, True, "  ")
                    serializer.WriteObject(writer, obj)
                    writer.Flush()
                End Using
                Return Encoding.UTF8.GetString(ms.ToArray())
            End Using
        End Function

        Private Shared Function DeserializeJson(json As String) As AppSettings
            Dim serializer As New DataContractJsonSerializer(GetType(AppSettings))
            Dim bytes() As Byte = Encoding.UTF8.GetBytes(json)
            Using ms As New MemoryStream(bytes)
                Return DirectCast(serializer.ReadObject(ms), AppSettings)
            End Using
        End Function

        ' -------------------------------------------------------------------------
        ' Private constructor — prevent instantiation (all members are Shared)
        ' -------------------------------------------------------------------------
        Private Sub New()
        End Sub

    End Class


    ' =============================================================================
    ''' <summary>
    ''' Strongly-typed model for all application settings.
    ''' All paths may be absolute or relative to the executable directory.
    ''' Serialized to / deserialized from JSON by ConfigManager.
    ''' </summary>
    <DataContract(Name:="AppSettings", Namespace:="")>
    Public Class AppSettings

        ' -------------------------------------------------------------------------
        ' Paths
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Path to the components database file (JSON).
        ''' Example: "Resources\AppData\components.json"
        ''' </summary>
        <DataMember(Name:="componentsDatabasePath", Order:=1)>
        Public Property ComponentsDatabasePath As String = "Resources\AppData\components.json"

        ''' <summary>
        ''' Root installation directory of SolidWorks.
        ''' Example: "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"
        ''' Used by SolidWorksAutomation.vb when locating the COM server.
        ''' </summary>
        <DataMember(Name:="solidWorksInstallPath", Order:=2)>
        Public Property SolidWorksInstallPath As String = "C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS"

        ''' <summary>
        ''' Directory containing parametric SolidWorks template parts (.SLDPRT / .SLDASM).
        ''' Module 2 reads from here when editing pre-designed templates.
        ''' Example: "C:\DroneDesigner\Resources\SolidWorks\Templates"
        ''' </summary>
        <DataMember(Name:="templatePartsDirectory", Order:=3)>
        Public Property TemplatePartsDirectory As String = "Resources\SolidWorks\Templates"

        ''' <summary>
        ''' Directory where generated CAD files (parts and assemblies) are written.
        ''' Example: "C:\DroneDesigner\Output"
        ''' </summary>
        <DataMember(Name:="outputDirectory", Order:=4)>
        Public Property OutputDirectory As String = "Output"

        ' -------------------------------------------------------------------------
        ' SolidWorks version targeting
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Target SolidWorks version string used when selecting macros or API calls.
        ''' Example: "2023", "2024"
        ''' Macros are version-sensitive; keep this in sync with the installed version.
        ''' </summary>
        <DataMember(Name:="solidWorksTargetVersion", Order:=5)>
        Public Property SolidWorksTargetVersion As String = "2026"

        ' -------------------------------------------------------------------------
        ' Logging
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Path to the application log file.
        ''' Example: "Logs\DroneDesigner.log"
        ''' </summary>
        <DataMember(Name:="logFilePath", Order:=6)>
        Public Property LogFilePath As String = "Logs\DroneDesigner.log"

        ''' <summary>
        ''' Minimum log level written to file.
        ''' Accepted values: "Debug", "Info", "Warning", "Error"
        ''' </summary>
        <DataMember(Name:="logLevel", Order:=7)>
        Public Property LogLevel As String = "Info"

        ' -------------------------------------------------------------------------
        ' Factory
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Returns a new AppSettings instance populated with safe default values.
        ''' Called automatically by ConfigManager when no config file is found.
        ''' </summary>
        Public Shared Function CreateDefaults() As AppSettings
            Return New AppSettings()  ' All Property initializers above ARE the defaults
        End Function

        ' -------------------------------------------------------------------------
        ' Convenience helpers
        ' -------------------------------------------------------------------------

        ''' <summary>
        ''' Returns ComponentsDatabasePath resolved to an absolute path,
        ''' anchored to the executable directory if the stored path is relative.
        ''' </summary>
        Public ReadOnly Property ResolvedComponentsDatabasePath As String
            Get
                Return ResolvePath(ComponentsDatabasePath)
            End Get
        End Property

        ''' <summary>
        ''' Returns TemplatePartsDirectory resolved to an absolute path.
        ''' </summary>
        Public ReadOnly Property ResolvedTemplatePartsDirectory As String
            Get
                Return ResolvePath(TemplatePartsDirectory)
            End Get
        End Property

        ''' <summary>
        ''' Returns OutputDirectory resolved to an absolute path.
        ''' Creates the directory if it does not already exist.
        ''' </summary>
        Public ReadOnly Property ResolvedOutputDirectory As String
            Get
                Dim resolved As String = ResolvePath(OutputDirectory)
                If Not Directory.Exists(resolved) Then
                    Directory.CreateDirectory(resolved)
                End If
                Return resolved
            End Get
        End Property

        ''' <summary>
        ''' Returns LogFilePath resolved to an absolute path.
        ''' Creates the parent Logs directory if it does not already exist.
        ''' </summary>
        Public ReadOnly Property ResolvedLogFilePath As String
            Get
                Dim resolved As String = ResolvePath(LogFilePath)
                Dim dir As String = Path.GetDirectoryName(resolved)
                If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                    Directory.CreateDirectory(dir)
                End If
                Return resolved
            End Get
        End Property

        ''' <summary>
        ''' Converts a path that may be relative (to the exe directory) into a full absolute path.
        ''' Absolute paths pass through unchanged.
        ''' </summary>
        Private Shared Function ResolvePath(rawPath As String) As String
            If Path.IsPathRooted(rawPath) Then
                Return rawPath
            End If
            Return Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPath))
        End Function

        ''' <summary>
        ''' Upgrades legacy relative resource paths from older config files to the
        ''' current packaged layout.
        ''' </summary>
        Friend Function NormalizeLegacyRelativePaths() As Boolean
            Dim changed As Boolean = False

            If IsLegacyRelativePath(ComponentsDatabasePath, "Resources\components.json") Then
                ComponentsDatabasePath = "Resources\AppData\components.json"
                changed = True
            End If

            If IsLegacyRelativePath(TemplatePartsDirectory, "Resources\Templates") Then
                TemplatePartsDirectory = "Resources\SolidWorks\Templates"
                changed = True
            End If

            Return changed
        End Function

        Private Shared Function IsLegacyRelativePath(rawPath As String, legacyRelativePath As String) As Boolean
            If String.IsNullOrWhiteSpace(rawPath) Then
                Return False
            End If

            If Path.IsPathRooted(rawPath) Then
                Return False
            End If

            Return String.Equals(
                rawPath.Replace("/"c, "\"c).Trim(),
                legacyRelativePath,
                StringComparison.OrdinalIgnoreCase)
        End Function

    End Class


    ' =============================================================================
    ''' <summary>
    ''' Thrown when ConfigManager cannot read or write the configuration file.
    ''' Wraps the underlying IO/serialization exception for caller context.
    ''' </summary>
    Public Class ConfigurationException
        Inherits Exception

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

    End Class

End Namespace
