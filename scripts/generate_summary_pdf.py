"""Generate a PDF summary of the Drone Designer project."""

from pathlib import Path

from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.colors import HexColor
from reportlab.lib.units import mm, cm
from reportlab.lib.enums import TA_LEFT, TA_CENTER
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    PageBreak, HRFlowable, KeepTogether
)

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = str(ROOT / "docs" / "reports" / "Drone_Designer_Project_Summary.pdf")

BLUE = HexColor("#1a56db")
DARK = HexColor("#1e293b")
GRAY = HexColor("#64748b")
LIGHT_BG = HexColor("#f1f5f9")
WHITE = HexColor("#ffffff")
GREEN = HexColor("#16a34a")
BORDER = HexColor("#cbd5e1")

styles = getSampleStyleSheet()

sTitle = ParagraphStyle("DocTitle", parent=styles["Title"],
    fontSize=26, leading=32, textColor=BLUE, spaceAfter=4, alignment=TA_CENTER)
sSubtitle = ParagraphStyle("Subtitle", parent=styles["Normal"],
    fontSize=11, leading=14, textColor=GRAY, alignment=TA_CENTER, spaceAfter=20)
sH1 = ParagraphStyle("H1", parent=styles["Heading1"],
    fontSize=16, leading=20, textColor=BLUE, spaceBefore=18, spaceAfter=8,
    borderWidth=0, borderPadding=0)
sH2 = ParagraphStyle("H2", parent=styles["Heading2"],
    fontSize=13, leading=16, textColor=DARK, spaceBefore=14, spaceAfter=6)
sBody = ParagraphStyle("Body", parent=styles["Normal"],
    fontSize=9.5, leading=13, textColor=DARK, spaceAfter=4)
sBullet = ParagraphStyle("Bullet", parent=sBody,
    leftIndent=16, bulletIndent=6, spaceBefore=1, spaceAfter=1)
sCode = ParagraphStyle("Code", parent=styles["Code"],
    fontSize=7.5, leading=10, textColor=DARK, backColor=LIGHT_BG,
    borderWidth=0.5, borderColor=BORDER, borderPadding=6,
    leftIndent=8, spaceAfter=8, fontName="Courier")
sTableHead = ParagraphStyle("TH", parent=sBody,
    fontSize=8.5, leading=11, textColor=WHITE, fontName="Helvetica-Bold")
sTableCell = ParagraphStyle("TD", parent=sBody,
    fontSize=8, leading=10, textColor=DARK)
sTableCellBold = ParagraphStyle("TDBold", parent=sTableCell,
    fontName="Helvetica-Bold")


def hr():
    return HRFlowable(width="100%", thickness=0.5, color=BORDER,
                      spaceBefore=6, spaceAfter=6)

def make_table(headers, rows, col_widths=None):
    """Build a styled table from header list and row lists."""
    data = [[Paragraph(h, sTableHead) for h in headers]]
    for row in rows:
        data.append([Paragraph(str(c), sTableCell) for c in row])
    w = col_widths or [None] * len(headers)
    t = Table(data, colWidths=w, repeatRows=1)
    style_cmds = [
        ("BACKGROUND", (0, 0), (-1, 0), BLUE),
        ("TEXTCOLOR", (0, 0), (-1, 0), WHITE),
        ("FONTNAME", (0, 0), (-1, 0), "Helvetica-Bold"),
        ("FONTSIZE", (0, 0), (-1, 0), 8.5),
        ("BOTTOMPADDING", (0, 0), (-1, 0), 6),
        ("TOPPADDING", (0, 0), (-1, 0), 6),
        ("BACKGROUND", (0, 1), (-1, -1), WHITE),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [WHITE, LIGHT_BG]),
        ("GRID", (0, 0), (-1, -1), 0.4, BORDER),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 1), (-1, -1), 4),
        ("BOTTOMPADDING", (0, 1), (-1, -1), 4),
    ]
    t.setStyle(TableStyle(style_cmds))
    return t


# Use non-breaking spaces for indentation in the tree
S = "\u00a0"  # non-breaking space

def indent(level, last=False):
    """Return tree prefix characters for a given nesting level."""
    return S * (level * 4)


def build_pdf():
    doc = SimpleDocTemplate(
        OUTPUT, pagesize=A4,
        leftMargin=2*cm, rightMargin=2*cm,
        topMargin=2*cm, bottomMargin=2*cm,
        title="Drone Designer - Project Summary",
        author="Claude Code",
    )
    story = []
    usable = doc.width

    # ── Title page ──
    story.append(Spacer(1, 40))
    story.append(Paragraph("Drone Designer", sTitle))
    story.append(Paragraph("Project Summary &amp; Architecture Guide", sSubtitle))
    story.append(hr())
    story.append(Spacer(1, 8))
    story.append(Paragraph(
        "A <b>VB.NET WinForms</b> desktop application (.NET Framework 4.7.2) that takes "
        "UAV mission requirements as input and recommends optimal drone hardware components "
        "(motors, batteries, ESCs, flight controllers, cameras, GPS, and more).",
        sBody))
    story.append(Spacer(1, 6))
    story.append(Paragraph(
        "The application implements a <b>three-stage selection pipeline</b> (Module 1) and "
        "a <b>SolidWorks CAD generation pipeline</b> (Module 2), backed by a JSON component "
        "database of real-world drone hardware.", sBody))

    # ── File Tree ──
    story.append(Spacer(1, 14))
    story.append(Paragraph("1. Project File Tree", sH1))
    story.append(hr())

    # Build a properly indented tree using non-breaking spaces
    I = S * 4  # one indent level
    tree_lines = [
        "Drone Designer/",
        I + "Drone Designer.slnx",
        I + "Drone Designer.vbproj",
        I + "App.config",
        I + "packages.config",
        "",
        I + "Core/",
        I + I + "Data/",
        I + I + I + "ComponentRepository.vb",
        I + I + "Interfaces/",
        I + I + I + "IComponentSelector.vb",
        I + I + "Models/",
        I + I + I + "ComponentSpecs.vb",
        I + I + I + "MissionSpecs.vb",
        I + I + I + "ComponentDisplayRow.vb",
        I + I + I + "PipelineResult.vb",
        I + I + "Services/",
        I + I + I + "ComponentSelectionEngine.vb",
        I + I + I + "PipelineOrchestrator.vb",
        "",
        I + "UI/",
        I + I + "Forms/",
        I + I + I + "MainForm.vb",
        I + I + I + "MainForm.Logic.vb",
        I + I + I + "MainForm.CAD.vb",
        I + I + I + "MainForm.Designer.vb",
        I + I + I + "CadProgressForm.vb",
        I + I + "Controls/",
        "",
        I + "Solidworks/",
        I + I + "SolidWorksAutomation.vb",
        I + I + "MacroRunner.vb",
        I + I + "Module2UsageExample.vb",
        "",
        I + "Utilities/",
        I + I + "ConfigManager.vb",
        "",
        I + "Resources/",
        I + I + "AppData/",
        I + I + I + "components.json",
        I + I + "SolidWorks/",
        I + I + I + "Macros/",
        I + I + I + I + "MotorMount.swb",
        I + I + I + "Templates/",
        I + I + "Submodules/",
        "",
        I + "packages/",
        I + I + "Newtonsoft.Json.13.0.4/",
        "",
        I + "My Project/",
        I + "bin/ &amp; obj/",
    ]
    tree = "<br/>".join(tree_lines)
    story.append(Paragraph(tree, sCode))

    # ── Architecture ──
    story.append(Paragraph("2. Architecture Overview", sH1))
    story.append(hr())
    story.append(Paragraph(
        "The project follows a <b>layered architecture</b> with four tiers:", sBody))

    layers = [
        ["Layer", "Folder", "Responsibility"],
        ["Core (Business Logic)", "Core/", "Models, selection engine, pipeline orchestrator, data access"],
        ["UI (Presentation)", "UI/", "WinForms main window, CAD progress dialog, input/output controls"],
        ["SolidWorks (CAD)", "Solidworks/", "COM automation, macro execution, part generation"],
        ["Utilities", "Utilities/", "App-wide configuration, path resolution, logging"],
    ]
    data = [[Paragraph(c, sTableHead) for c in layers[0]]]
    for row in layers[1:]:
        data.append([
            Paragraph(row[0], sTableCellBold),
            Paragraph(row[1], sTableCell),
            Paragraph(row[2], sTableCell),
        ])
    t = Table(data, colWidths=[usable*0.28, usable*0.15, usable*0.57])
    t.setStyle(TableStyle([
        ("BACKGROUND", (0, 0), (-1, 0), BLUE),
        ("TEXTCOLOR", (0, 0), (-1, 0), WHITE),
        ("GRID", (0, 0), (-1, -1), 0.4, BORDER),
        ("ROWBACKGROUNDS", (0, 1), (-1, -1), [WHITE, LIGHT_BG]),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 6),
        ("RIGHTPADDING", (0, 0), (-1, -1), 6),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]))
    story.append(t)
    story.append(Spacer(1, 10))

    # Data flow
    story.append(Paragraph("Data Flow", sH2))
    flow_lines = [
        "User Input (MainForm.vb)",
        I + "|",
        I + "v",
        "MainForm.Logic.vb",
        I + "ValidateFormInputs()  ->  checks input sanity",
        I + "BuildMissionSpecs()   ->  maps UI controls to MissionSpecs",
        I + "|",
        I + "v",
        "ComponentSelectionEngine.vb  (implements IComponentSelector)",
        I + "Task 7: MTOW + thrust   ->  select motors &amp; propellers",
        I + "Task 8: Power budget    ->  select batteries, ESCs, PDB",
        I + "Task 9: Avionics        ->  select FC, GPS, telemetry, receiver, camera",
        I + I + "reads from: ComponentRepository.vb  &lt;--  components.json",
        I + "|",
        I + "v",
        "SelectionResult  (up to 5 candidates per category)",
        I + "|",
        I + "+-----------------------------+",
        I + "|" + S*29 + "|",
        I + "v" + S*29 + "v",
        "MainForm.Logic.vb" + S*8 + "MainForm.CAD.vb",
        I + "DisplaySelectionResult()" + S*4 + "btnGenerateCAD_Click()",
        I + "Bind to DataGridView" + S*8 + "|",
        S*33 + "v",
        S*29 + "PipelineOrchestrator.RunFromSelectionAsync()",
        S*33 + "|",
        S*33 + "v",
        S*29 + "SolidWorksAutomation.Connect()",
        S*29 + "MacroRunner  ->  MotorMount.swb",
        S*33 + "|",
        S*33 + "v",
        S*29 + "Generated .SLDPRT files + manifest",
    ]
    flow = "<br/>".join(flow_lines)
    story.append(Paragraph(flow, sCode))

    # ── File-by-file ──
    story.append(PageBreak())
    story.append(Paragraph("3. File-by-File Breakdown", sH1))
    story.append(hr())

    files = [
        {
            "name": "ComponentSpecs.vb",
            "path": "Core/Models/ComponentSpecs.vb",
            "lines": "~1,314 lines",
            "purpose": "Data model backbone. Defines 11 typed component subclasses and all supporting enumerations.",
            "details": [
                "Abstract base class <b>ComponentSpecs</b> with universal properties: Id, Name, Manufacturer, MassGrams, Dimensions, Voltage, Temperature range, Price.",
                "Concrete subclasses: <b>MotorSpec</b>, <b>ESCSpec</b>, <b>PropellerSpec</b>, <b>FlightControllerSpec</b>, <b>BatterySpec</b>, <b>GPSModuleSpec</b>, <b>TelemetryRadioSpec</b>, <b>CameraSpec</b>, <b>ServoSpec</b>, <b>ReceiverSpec</b>, <b>PowerDistributionBoardSpec</b>.",
                "Key enums: ComponentCategory, MotorType, BatteryCellChemistry, GNSSConstellation, ESCProtocol, FlightControllerFirmware, CameraOutputInterface, PowerConnectorType.",
                "Computed read-only properties (e.g., BatterySpec.NominalPackVoltageV, MaxContinuousCurrentAmps).",
            ],
            "connects": "Consumed by ComponentRepository (deserialization target), ComponentSelectionEngine (selection logic), ComponentDisplayRow (display mapping), and MacroRunner (parameter extraction)."
        },
        {
            "name": "MissionSpecs.vb",
            "path": "Core/Models/MissionSpecs.vb",
            "lines": "~458 lines",
            "purpose": "Complete set of UAV mission parameters. Primary input to the selection engine.",
            "details": [
                "Flight performance: EnduranceMinutes, MaxRangeKm, CruiseSpeedMs, MaxAltitudeMeters, MaxWindSpeedMs, MaxTakeoffMassGrams.",
                "Payload: PayloadMassGrams, PayloadDimensions (L x W x H).",
                "Environment: MinOperatingTempCelsius, MaxOperatingTempCelsius, RequiresWaterproofing, RequiredIPRating.",
                "Mission profile &amp; config: MissionProfileCategory, UAVConfiguration, PowerSourceType, RegulatoryClass.",
                "Dual enum sets for backward compatibility (legacy + engine-facing enums).",
            ],
            "connects": "Built by MainForm.Logic.BuildMissionSpecs(). Passed to ComponentSelectionEngine and PipelineOrchestrator."
        },
        {
            "name": "PipelineResult.vb",
            "path": "Core/Models/PipelineResult.vb",
            "lines": "~368 lines",
            "purpose": "Immutable result types for the end-to-end pipeline (Task 15).",
            "details": [
                "<b>PipelineStage</b> enum: Validating, SelectingComponents, ConnectingToSolidWorks, GeneratingPart, SavingFile, WritingManifest, Finalising, Failed.",
                "<b>PipelineProgressReport</b>: immutable snapshot with Stage, PercentComplete, StatusMessage, DetailMessage, ErrorMessage. Used via IProgress(Of T).",
                "<b>GeneratedPartRecord</b>: describes a single CAD file produced (ComponentId, PartType, FilePath, Success/ErrorDetail).",
                "<b>PipelineResult</b>: final outcome with Success flag, GeneratedParts list, OutputDirectory, ManifestPath, Duration, ToSummaryString() for display.",
            ],
            "connects": "Produced by PipelineOrchestrator. Consumed by MainForm.CAD.vb and CadProgressForm."
        },
        {
            "name": "ComponentSelectionEngine.vb",
            "path": "Core/Services/ComponentSelectionEngine.vb",
            "lines": "~1,544 lines",
            "purpose": "Core selection algorithm. Implements the three-stage pipeline (Tasks 7, 8, 9).",
            "details": [
                "<b>Task 7 (Weight &amp; Thrust):</b> EstimateMtow() uses fixed-point iteration. SelectMotors() and SelectPropellers() filter and rank candidates.",
                "<b>Task 8 (Power System):</b> CalculatePowerBudget() determines cell count, currents, capacity. SelectBatteries(), SelectEscs(), SelectPdb().",
                "<b>Task 9 (Avionics):</b> SelectFlightController(), SelectGpsModule(), SelectTelemetryRadio(), SelectReceiver(), SelectCamera().",
                "Returns <b>SelectionResult</b> with up to 5 candidates per category plus intermediate sizing data.",
            ],
            "connects": "Implements IComponentSelector. Reads from ComponentRepository. Called by MainForm.Logic and PipelineOrchestrator."
        },
        {
            "name": "PipelineOrchestrator.vb",
            "path": "Core/Services/PipelineOrchestrator.vb",
            "lines": "~559 lines",
            "purpose": "Coordinates the full end-to-end workflow: MissionSpecs -> Selection -> SolidWorks -> CAD parts (Task 15).",
            "details": [
                "RunAsync(): 6-stage async pipeline: Validate, Select Components, Connect SW, Generate Parts, Write Manifest, Finalise.",
                "RunFromSelectionAsync(): skips re-running selection when user already has a SelectionResult from Module 1.",
                "Partial-failure tolerant: if one motor mount fails, remaining motors are still attempted.",
                "CancellationToken supported: UI Cancel button hooks in. IProgress for all status updates.",
                "Writes a plain-text component_manifest.txt alongside generated CAD files.",
            ],
            "connects": "Consumes IComponentSelector, SolidWorksAutomation, MacroRunner, ConfigManager. Called by MainForm.CAD.vb."
        },
        {
            "name": "ComponentRepository.vb",
            "path": "Core/Data/ComponentRepository.vb",
            "lines": "~304 lines",
            "purpose": "Read-only data access layer. Loads and indexes components from JSON.",
            "details": [
                "Loads Resources/AppData/components.json on construction; builds category and ID indexes.",
                "Typed accessors: GetMotors(), GetBatteries(), GetESCs(), GetFlightControllers(), etc.",
                "Filter methods: GetMotorsWithMinThrust(), GetBatteriesWithMinCapacity(), GetComponentsUnderMass().",
                "Uses Newtonsoft.Json for flexible deserialization. Warns on duplicate IDs without crashing.",
            ],
            "connects": "Reads components.json. Injected into ComponentSelectionEngine constructor."
        },
        {
            "name": "IComponentSelector.vb",
            "path": "Core/Interfaces/IComponentSelector.vb",
            "lines": "~31 lines",
            "purpose": "Contract interface for the selection engine.",
            "details": [
                "Single method: SelectComponents(specs As MissionSpecs) As SelectionResult.",
                "Enables dependency injection, mocking for tests, and future web-service migration.",
            ],
            "connects": "Implemented by ComponentSelectionEngine. Used by PipelineOrchestrator and MainForm.Logic."
        },
        {
            "name": "ComponentDisplayRow.vb",
            "path": "Core/Models/ComponentDisplayRow.vb",
            "lines": "~236 lines",
            "purpose": "Display-only DTO bridging ComponentSpecs properties to DataGridView column names.",
            "details": [
                "Factory method FromComponentSpecs() creates display rows from engine output.",
                "Helpers: FormatTempRange(), BuildDimensionsString() for human-readable formatting.",
            ],
            "connects": "Created by MainForm.Logic.DisplaySelectionResult(). Bound to DataGridView in MainForm."
        },
        {
            "name": "MainForm.vb",
            "path": "UI/Forms/MainForm.vb",
            "lines": "~1,023 lines",
            "purpose": "Root application window. Two-tab layout built entirely in code.",
            "details": [
                "Tab 1 (Input): GroupBoxes for Flight, Payload, Environment, Mission parameters.",
                "Tab 2 (Output): DataGridView for results + summary panel + Generate CAD button.",
                "All controls created programmatically. Form size: 1180 x 760.",
            ],
            "connects": "Partial class with MainForm.Logic.vb, MainForm.CAD.vb, and MainForm.Designer.vb."
        },
        {
            "name": "MainForm.Logic.vb",
            "path": "UI/Forms/MainForm.Logic.vb",
            "lines": "~492 lines",
            "purpose": "Event handlers, validation, and Module 1 engine wiring.",
            "details": [
                "OnLoad(): Initializes ComponentRepository and ComponentSelectionEngine.",
                "ValidateFormInputs(), BuildMissionSpecs(), DisplaySelectionResult().",
                "Enum mapping helpers: ParseMotorCount(), MapMissionProfileType(), MapEnvironmentType().",
            ],
            "connects": "Calls ComponentSelectionEngine.SelectComponents(). Updates DataGridView."
        },
        {
            "name": "MainForm.CAD.vb",
            "path": "UI/Forms/MainForm.CAD.vb",
            "lines": "~410 lines",
            "purpose": "Partial class for MainForm - Module 2 (SolidWorks CAD) button wiring (Task 15).",
            "details": [
                "WireCADControls(): hooks the Generate CAD button click handler.",
                "btnGenerateCAD_Click(): validates selection exists, picks output folder, runs PipelineOrchestrator async.",
                "Shows CadProgressForm (modeless) with real-time progress. Offers to open output folder on success.",
                "Lazy-initialises SolidWorksAutomation and PipelineOrchestrator on first use.",
            ],
            "connects": "Uses PipelineOrchestrator.RunFromSelectionAsync(). Shows CadProgressForm. Reads _lastSelectionResult from Logic."
        },
        {
            "name": "MainForm.Designer.vb",
            "path": "UI/Forms/MainForm.Designer.vb",
            "lines": "~31 lines",
            "purpose": "Auto-generated form designer stub.",
            "details": [],
            "connects": "Partial class with MainForm.vb."
        },
        {
            "name": "CadProgressForm.vb",
            "path": "UI/Forms/CadProgressForm.vb",
            "lines": "~397 lines",
            "purpose": "Modal progress dialog shown while the SolidWorks pipeline runs (Task 15).",
            "details": [
                "Dark-themed form with stage label, progress bar, detail label, scrolling activity log.",
                "Cancel button signals CancellationTokenSource; swaps to Close button on completion.",
                "Exposes IProgress(Of PipelineProgressReport) for the orchestrator to report into.",
                "PipelineCompleted event raised when user dismisses the dialog.",
                "Thread-safe: all public methods use Me.Invoke for cross-thread marshalling.",
            ],
            "connects": "Created by MainForm.CAD.vb. Receives progress from PipelineOrchestrator. Returns PipelineResult via event."
        },
        {
            "name": "SolidWorksAutomation.vb",
            "path": "Solidworks/SolidWorksAutomation.vb",
            "lines": "~547 lines",
            "purpose": "Manages the COM connection lifecycle to SolidWorks (Task 12/14).",
            "details": [
                "Connect(): tries GetActiveObject first (attach), then CreateObject (launch new instance).",
                "Disconnect(): closes SW only if we launched it and no docs are open.",
                "IsConnected(): liveness ping via COM round-trip. GetVersion(): maps API revision to year.",
                "Late binding (Object) so the project compiles without SolidWorks installed.",
                "IDisposable for guaranteed cleanup. COMErrorCodes module for friendly HRESULT messages.",
            ],
            "connects": "Used by PipelineOrchestrator and MainForm.CAD.vb. Provides Application property to MacroRunner."
        },
        {
            "name": "MacroRunner.vb",
            "path": "Solidworks/MacroRunner.vb",
            "lines": "~500+ lines",
            "purpose": "Bridge between ComponentSpecs and SolidWorks macro execution (Task 13).",
            "details": [
                "MacroParameters class: flat key-value store with FromComponentSpec() factory.",
                "RunMacroOnTemplate(): opens template part, injects parameters as custom properties, runs macro via RunMacro2, saves output.",
                "Parameter passing via custom document properties + temp INI file fallback.",
                "Cleans up temporary properties and files after macro completion.",
            ],
            "connects": "Uses SolidWorksAutomation.Application COM object. Called by PipelineOrchestrator. Reads ComponentSpecs."
        },
        {
            "name": "Module2UsageExample.vb",
            "path": "Solidworks/Module2UsageExample.vb",
            "lines": "~110 lines",
            "purpose": "Reference snippet (not compiled) showing how to wire SolidWorks automation from the UI.",
            "details": [
                "Documents expected MechanicalSpecs keys for motor components (OuterDiameter, BoltCircleDiameter, etc.).",
                "Includes deployment checklist for SolidWorks + macro setup.",
            ],
            "connects": "Documentation only. To be deleted once actual UI integration is complete."
        },
        {
            "name": "MotorMount.swb",
            "path": "Resources/SolidWorks/Macros/MotorMount.swb",
            "lines": "~399 lines",
            "purpose": "SolidWorks VBA macro that generates a parametric motor-mount part.",
            "details": [
                "Reads parameters from %TEMP%\\SW_MotorMountParams.ini (KEY=VALUE format).",
                "Builds geometry: base disc extrusion, central shaft hole, motor bolt holes on bolt circle, arm attachment holes.",
                "Stamps custom properties (DD_* prefix) on the part for traceability.",
                "Writes status to %TEMP%\\SW_MacroStatus.ini for the VB.NET host to poll.",
            ],
            "connects": "Executed by MacroRunner via swApp.RunMacro2. Parameters written by SolidWorksAutomation."
        },
        {
            "name": "ConfigManager.vb",
            "path": "Utilities/ConfigManager.vb",
            "lines": "~313 lines",
            "purpose": "Application-wide configuration manager (singleton pattern).",
            "details": [
                "Loads/saves DroneDesigner.config.json (auto-creates with defaults if missing).",
                "AppSettings: ComponentsDatabasePath, SolidWorksInstallPath, TemplatePartsDirectory, OutputDirectory, LogFilePath, LogLevel.",
                "Resolved properties convert relative paths to absolute. Uses DataContractJsonSerializer.",
            ],
            "connects": "Used by ComponentRepository, PipelineOrchestrator, and MainForm.CAD.vb."
        },
        {
            "name": "components.json",
            "path": "Resources/AppData/components.json",
            "lines": "~1,829 lines",
            "purpose": "Component database. JSON array of real-world drone hardware.",
            "details": [
                "Categories: motor, esc, propeller, flight_controller, battery, gps, telemetry, camera, servo, receiver, pdb.",
                "Includes real hardware: T-Motor, Pixhawk, u-blox, EMAX, Matek, etc.",
            ],
            "connects": "Loaded by ComponentRepository.vb on startup."
        },
        {
            "name": "packages.config",
            "path": "packages.config",
            "lines": "~4 lines",
            "purpose": "NuGet package manifest. Lists Newtonsoft.Json 13.0.4 targeting net472.",
            "details": [],
            "connects": "Consumed by NuGet restore. Newtonsoft.Json used by ComponentRepository for JSON deserialization."
        },
        {
            "name": "App.config",
            "path": "App.config",
            "lines": "~6 lines",
            "purpose": ".NET Framework startup configuration. Targets .NET 4.7.2.",
            "details": [],
            "connects": "Read by .NET runtime at startup."
        },
        {
            "name": "Drone Designer.vbproj",
            "path": "Drone Designer.vbproj",
            "lines": "~135 lines",
            "purpose": "MSBuild project file. Defines compilation targets, references, and resources.",
            "details": [
                "Target: .NET Framework 4.7.2, WinExe output.",
                "References: System, System.Drawing, System.Windows.Forms, Newtonsoft.Json.",
            ],
            "connects": "Defines the build for the entire project."
        },
    ]

    for f in files:
        block = []
        block.append(Paragraph(f"<b>{f['name']}</b>", sH2))
        block.append(Paragraph(
            f"<font color='#64748b'>{f['path']}  |  {f['lines']}</font>", sBody))
        block.append(Spacer(1, 3))
        block.append(Paragraph(f"<b>Purpose:</b> {f['purpose']}", sBody))
        if f["details"]:
            for d in f["details"]:
                block.append(Paragraph(d, sBullet, bulletText="\u2022"))
        block.append(Spacer(1, 2))
        block.append(Paragraph(
            f"<font color='#1a56db'><b>Connections:</b></font> {f['connects']}", sBody))
        block.append(Spacer(1, 6))
        story.append(KeepTogether(block))

    # ── Summary Table ──
    story.append(PageBreak())
    story.append(Paragraph("4. Summary Table", sH1))
    story.append(hr())

    summary_rows = [
        ["ComponentSpecs.vb", "1,314", "11 component subclasses + enums"],
        ["MissionSpecs.vb", "458", "Mission input parameters"],
        ["PipelineResult.vb", "368", "Pipeline progress/result types (Task 15)"],
        ["ComponentSelectionEngine.vb", "1,544", "3-stage selection pipeline (Module 1)"],
        ["PipelineOrchestrator.vb", "559", "End-to-end pipeline coordinator (Task 15)"],
        ["ComponentRepository.vb", "304", "JSON data access &amp; indexing"],
        ["IComponentSelector.vb", "31", "Engine interface contract"],
        ["ComponentDisplayRow.vb", "236", "Display DTO for DataGridView"],
        ["MainForm.vb", "1,023", "Root window, two-tab layout"],
        ["MainForm.Logic.vb", "492", "Module 1 validation, wiring, display"],
        ["MainForm.CAD.vb", "410", "Module 2 (Generate CAD) button wiring"],
        ["MainForm.Designer.vb", "31", "Auto-generated stub"],
        ["CadProgressForm.vb", "397", "CAD generation progress dialog"],
        ["SolidWorksAutomation.vb", "547", "SolidWorks COM connection manager"],
        ["MacroRunner.vb", "500+", "Macro parameter injection &amp; execution"],
        ["Module2UsageExample.vb", "110", "Reference snippet (not compiled)"],
        ["MotorMount.swb", "399", "VBA macro: parametric motor mount geometry"],
        ["ConfigManager.vb", "313", "App config &amp; path resolution"],
        ["components.json", "1,829", "Hardware database (JSON)"],
        ["packages.config", "4", "NuGet manifest (Newtonsoft.Json)"],
        ["App.config", "6", ".NET Framework 4.7.2 startup"],
        ["Drone Designer.vbproj", "135", "MSBuild project definition"],
    ]
    story.append(make_table(
        ["File", "Lines", "Role"],
        summary_rows,
        col_widths=[usable*0.35, usable*0.10, usable*0.55],
    ))

    # ── Key Design Patterns ──
    story.append(Spacer(1, 14))
    story.append(Paragraph("5. Key Design Patterns", sH1))
    story.append(hr())

    patterns = [
        "<b>Repository Pattern</b> - ComponentRepository abstracts the JSON data source behind typed query methods.",
        "<b>Dependency Injection</b> - ComponentSelectionEngine and PipelineOrchestrator accept dependencies in constructors.",
        "<b>Interface Segregation</b> - IComponentSelector decouples UI from the concrete engine implementation.",
        "<b>Enum-Over-Strings</b> - ComponentCategory, MotorType, BatteryCellChemistry, etc. prevent magic strings throughout.",
        "<b>Immutable Intermediate Types</b> - MtowEstimate, PowerBudget, PipelineProgressReport expose transparent sizing breakdown.",
        "<b>Partial Classes</b> - MainForm split across .vb (layout), .Logic.vb (Module 1), .CAD.vb (Module 2), .Designer.vb (scaffold).",
        "<b>Async Pipeline</b> - PipelineOrchestrator uses Async/Await + IProgress(Of T) for non-blocking UI during CAD generation.",
        "<b>Late Binding</b> - SolidWorksAutomation uses Object (not early-bound COM types) so the project compiles without SolidWorks installed.",
    ]
    for p in patterns:
        story.append(Paragraph(p, sBullet, bulletText="\u2022"))
    story.append(Spacer(1, 10))

    # ── Extension Points ──
    story.append(Paragraph("6. Extension Points", sH1))
    story.append(hr())
    extensions = [
        "<b>Additional CAD Parts</b> - New .swb macros can be added to Resources/SolidWorks/Macros/ and wired into PipelineOrchestrator for propeller mounts, battery trays, etc.",
        "<b>Module 3 (Cost / Supply Chain)</b> - SelectionResult exposes up to 5 candidates per category for trade-off analysis.",
        "<b>Web Migration</b> - IComponentSelector interface and plain data types enable a REST API without UI changes.",
    ]
    for e in extensions:
        story.append(Paragraph(e, sBullet, bulletText="\u2022"))

    # ── Footer ──
    story.append(Spacer(1, 30))
    story.append(hr())
    story.append(Paragraph(
        "<font color='#94a3b8'>Generated by Claude Code  |  April 2026</font>",
        ParagraphStyle("Footer", parent=sBody, alignment=TA_CENTER, fontSize=8)))

    doc.build(story)
    print(f"PDF saved to: {OUTPUT}")


if __name__ == "__main__":
    build_pdf()
