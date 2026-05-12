# Configuration And Runtime Dependencies

This page documents how Drone Designer finds its runtime files, how configuration is created and resolved, and which dependencies are required for the normal app path versus the optional CAD path.

Use this page when you need to answer questions like:

- what must exist beside the executable
- which paths come from `DroneDesigner.config.json`
- what a source checkout assumes that a release ZIP should already package
- whether SolidWorks is required for a given workflow

Use the other reference pages alongside this one when the change also affects the component database contract or the validation plan:

- [Component Database And Schema](component-database-and-schema.md) for the stable component-data contract
- [Testing And Validation](testing-and-validation.md) for the checks to run after config, packaging, or runtime-path changes

## Runtime Model

Drone Designer is a single Windows desktop executable targeting `.NET Framework 4.7.2`.

The normal runtime path is:

1. launch `Drone Designer.exe`
2. load or create `DroneDesigner.config.json` beside the executable
3. resolve runtime paths relative to the executable directory unless the config stores absolute paths
4. load `components.json`
5. run selection, export, and optionally CAD generation

There is no service layer, installer-managed config store, or external database in the default path.

## Required Runtime Dependencies For The Main App Path

These are the non-optional dependencies for launch, component selection, and Excel export:

- Windows desktop environment
- `.NET Framework 4.7.2`
- `Drone Designer.exe` and its managed dependencies, including `Newtonsoft.Json`
- `Resources\AppData\components.json` at the configured or default location
- a writable executable-adjacent folder structure if the app needs to create config, logs, or output folders

For the main app path, SolidWorks is not required.

## Configuration File Behavior

Configuration is managed by [`Utilities/ConfigManager.vb`](../../Utilities/ConfigManager.vb).

Stable behavior:

- config filename: `DroneDesigner.config.json`
- config location: next to the executable, via `AppDomain.CurrentDomain.BaseDirectory`
- first-run behavior: if the file is missing, `ConfigManager.Load()` creates an in-memory default `AppSettings` object and immediately saves it to disk
- serialization format: `DataContractJsonSerializer`

This is why the app should live in a normal writable folder for the user-facing ZIP flow. If the process cannot write beside the executable, first-run config creation can fail even if the binaries themselves are intact.

## AppSettings Fields That Matter At Runtime

The current config surface is small. These fields are the important stable ones:

| Setting | Default | What uses it |
|---|---|---|
| `componentsDatabasePath` | `Resources\AppData\components.json` | `ComponentRepository` |
| `solidWorksInstallPath` | `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS` | Informational today; not the main detection path |
| `templatePartsDirectory` | `Resources\SolidWorks\Templates` | `PipelineOrchestrator` |
| `outputDirectory` | `Output` | default output folder helper |
| `solidWorksTargetVersion` | `2026` | documentation and packaging signal; not the main runtime gate |
| `logFilePath` | `Logs\DroneDesigner.log` | log-path resolution |
| `logLevel` | `Info` | config surface only; not a deeply wired runtime feature yet |

Two important repo-state facts:

- `ComponentRepository` actively uses `ResolvedComponentsDatabasePath`.
- `PipelineOrchestrator` actively uses `ResolvedTemplatePartsDirectory`.

By contrast, `solidWorksInstallPath` and `solidWorksTargetVersion` are present in config but are not the main selectors for the current SolidWorks connection path. The actual COM check lives in [`Solidworks/SolidWorksAutomation.vb`](../../Solidworks/SolidWorksAutomation.vb).

## Path Resolution Rules

`AppSettings` resolves relative paths against the executable directory.

Important differences by property:

- `ResolvedComponentsDatabasePath`: resolves to an absolute path but does not create anything
- `ResolvedTemplatePartsDirectory`: resolves to an absolute path but does not create anything
- `ResolvedOutputDirectory`: resolves and creates the directory if missing
- `ResolvedLogFilePath`: resolves and creates the parent log directory if missing

This means missing output or log folders are usually self-healed, while missing component and template assets are not.

## Source Checkout Assumptions

A source checkout assumes more than an operator ZIP release does.

The current repo state expects:

- Windows development environment
- Visual Studio or equivalent MSBuild support for a VB.NET WinForms project
- the `packages\Newtonsoft.Json.13.0.4\lib\net45\Newtonsoft.Json.dll` restore path referenced by the project file
- the source-tree `Resources\...` folders to exist

The project file also controls which content files are copied into `bin\Debug` and `bin\Release`.

Currently, [`Drone Designer.vbproj`](../../Drone%20Designer.vbproj) explicitly copies:

- `Resources\AppData\components.json`
- `Resources\SolidWorks\Macros\MotorMount.swb`
- `Resources\SolidWorks\Macros\MotorMount.swp`
- `Resources\SolidWorks\Macros\MotorMount1.swb`
- `Resources\SolidWorks\MotorMount_Template.SLDPRT`
- `Resources\SolidWorks\Templates\MotorMount_Template.SLDPRT`

That is narrower than the full set of source-tree SolidWorks assets currently present under `Resources\SolidWorks\Macros\` and `Resources\SolidWorks\Templates\`.

## ZIP Release Assumptions

The operator-facing ZIP path assumes the release has already been packaged correctly.

In practice, a healthy ZIP release should keep these together beside the executable:

- `Drone Designer.exe`
- `DroneDesigner.config.json`, if pre-seeded, or a writable location for the app to create it
- `Resources\AppData\components.json`
- any SolidWorks templates and macros needed by the workflows the release claims to support

The user installation guide is intentionally simple because it assumes this packaging work has already happened.

## Optional SolidWorks Dependencies

SolidWorks is optional for:

- launching the app
- running component selection
- reviewing recommendations
- exporting Excel output

SolidWorks is required only for the `Generate CAD` path.

That distinction is important because many bug reports are really in one of two buckets:

- main-app runtime problem
- optional CAD-runtime problem

Do not treat those as the same dependency class.

## SolidWorks-Specific Runtime Dependencies

The optional CAD path depends on these additional conditions:

- SolidWorks installed on the same Windows machine
- the process running as the same Windows user that owns the SolidWorks license
- COM activation for `SldWorks.Application`
- a compatible installed SolidWorks version according to `SolidWorksAutomation`
- compiled `.swp` macros and matching template parts reachable from the resolved runtime paths

Current implementation details:

- `SolidWorksAutomation` checks the registry key `HKLM\SOFTWARE\SolidWorks\SOLIDWORKS 2026`
- the class uses late binding so the project can still compile without a direct SolidWorks interop reference
- `PipelineOrchestrator` builds macro paths under `Resources\SolidWorks\Macros`
- `PipelineOrchestrator` builds template paths from `ResolvedTemplatePartsDirectory`

The CAD path is therefore both code-dependent and asset-dependent.

## CAD Asset Expectations In The Current Repo

`PipelineOrchestrator` currently expects compiled `.swp` macros for:

- `MotorMount`
- `CarbonFiberArm`
- `ArmChassisConnection`
- `ChassisPlates`
- `LandingGearConnection`
- `LandingGearTube`

It also expects matching `.SLDPRT` templates under the configured template directory.

This matters because the current source tree contains a broader set of macro and template assets than the project file explicitly copies to the output directory. Maintainers should not assume that "present in `Resources\SolidWorks`" automatically means "present beside the built executable."

For source-build and release-prep work, verify the actual runtime output folder rather than only the source tree.

## Main Runtime Failure Modes To Expect

The most important configuration and dependency failures are:

- config file cannot be created or written beside the executable
- `components.json` missing from the resolved path
- template directory missing for CAD generation
- compiled `.swp` macro missing or stale for CAD generation
- SolidWorks COM connection fails because the installed version, user session, or registry state does not match expectations

The main app and the CAD pipeline fail in different ways, so diagnose them separately.

## Practical Guidance By Deployment Mode

### If You Are Running From A Source Build

Check:

- `bin\<Configuration>\Drone Designer.exe`
- whether `components.json` was copied into `bin\<Configuration>\Resources\AppData\`
- whether the required SolidWorks templates and `.swp` macros actually exist under the built output, not just under the source tree
- whether `DroneDesigner.config.json` is being created beside the built executable

### If You Are Running From A Release ZIP

Check:

- the app was extracted to a writable folder
- the `Resources\...` structure stayed intact
- the component database exists at the packaged path
- SolidWorks is installed only if the ZIP is expected to support CAD generation

## Primary Related Files

- `Utilities/ConfigManager.vb`
- `Core/Data/ComponentRepository.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `Solidworks/SolidWorksAutomation.vb`
- `Drone Designer.vbproj`
- `docs/maintainers/workflows/changing-the-component-database-and-schema.md`
- `docs/maintainers/workflows/preparing-a-release-package.md`

## Secondary Related Files

- `Solidworks/MacroRunner.vb`
- `UI/Forms/MainForm.CAD.vb`
- `docs/user/installation-and-prerequisites.md`
- `Resources/AppData/components.json`
- `Resources/SolidWorks/`
- `docs/reference/component-database-and-schema.md`
- `docs/reference/testing-and-validation.md`
