# Cleanup Handoff

## Scope
Resume codebase cleanup and file organization work without changing app behavior unless a move requires path fixes.

## Current branch
- `master`

## Last relevant commits
- `745c627` Reorganize Resources into appdata solidworks and submodules
- `e09a11d` Move external repos under Resources/Submodules
- `e3b3f6b` Fix report script output paths after root cleanup
- `20ad622` Reorganize root files into docs scripts and tools
- `babe13f` Track Web-Scrapper as a submodule
- `80217c6` Add ignore rules and untrack generated files

## What has already been cleaned
- Root folder cleanup:
  - moved docs to `docs/`
  - moved reports to `docs/reports/`
  - moved helper scripts to `scripts/`
  - moved `probe.exe` to `tools/`
- Ignore/index cleanup:
  - stopped tracking generated/editor/local files such as `.vs/`, `bin/`, `obj/`, local settings, temp test outputs
  - kept `packages/` tracked intentionally for small-team convenience
- Submodule cleanup:
  - `Resources/Submodules/FAST-UAV`
  - `Resources/Submodules/Web-Scrapper`
- `Resources/` cleanup:
  - `Resources/AppData/`
  - `Resources/SolidWorks/`
  - `Resources/Submodules/`

## Important current structure
- `Resources/AppData/components.json`
- `Resources/AppData/DroneDesigner_Components.xlsx`
- `Resources/SolidWorks/Macros/...`
- `Resources/SolidWorks/Templates/...`
- `Resources/Submodules/FAST-UAV`
- `Resources/Submodules/Web-Scrapper`

## Important path owners already updated
- `Drone Designer.vbproj`
- `Utilities/ConfigManager.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `UI/Forms/MainForm.Logic.vb`
- `scripts/generate_doc.py`
- `scripts/generate_summary_pdf.py`
- selected docs under `docs/`

## Leave these alone
- Untracked research artifacts:
  - `Research/testParameter_U8 Lite Efficiency Type Multirotor UAV Motor KV150.xls`
  - `Research/testParameter_U8 Lite Efficiency Type Multirotor UAV Motor KV150_files/`
- Local untracked content inside submodule:
  - `Resources/Submodules/FAST-UAV`
  - currently shows as `?` in parent repo because of untracked files inside the submodule worktree

## Current working tree notes
- Parent repo should be clean except for:
  - untracked research files above
  - local untracked content inside `Resources/Submodules/FAST-UAV`

## Recommended next task
Clean up `Test/` without changing behavior.

Suggested direction:
- separate stable test inputs from generated outputs/manual artifacts
- likely target shape:
  - `Test/Scenarios/` for `test_scenarios.csv` and `test_scenarios.xlsx`
  - `Test/Artifacts/` or `Test/Fixtures/` for ad hoc files if they are intentionally kept
  - keep generated logs/components out of tracking if they reappear
- inspect references before moving anything

## Constraints to preserve
- Do not revert unrelated user files.
- Do not touch `packages/` tracking.
- Do not disturb submodule internals unless explicitly cleaning those repos themselves.
- Prefer path-only organization changes first; patch code only when moved files require it.

## Suggested skills for next session
- `handoff` only if another fresh summary is needed later
- no special skill required for the next `Test/` cleanup pass
