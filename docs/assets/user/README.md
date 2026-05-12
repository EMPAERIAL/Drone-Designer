# User Screenshot Assets

This directory is the home for operator-facing screenshots used by the Phase 2 user docs.

Keep screenshots grouped by the document that references them:

- `installation/`
- `workflow-design-a-uav/`
- `workflow-export-and-generate-outputs/`
- `troubleshooting/`

## Capture Rules

- Capture the real Drone Designer window at full-window size.
- Prefer the packaged or built app over mocked layouts.
- Add simple red-box callouts only when they materially shorten the operator's path.
- Keep filenames stable and descriptive so later doc edits can link to them without guesswork.

## Planned Screenshot Set

Use these filenames unless a stronger repo-grounded reason appears to change them:

- `installation/main-window-first-launch.png`
- `workflow-design-a-uav/mission-inputs-overview.png`
- `workflow-design-a-uav/selected-components-and-mtow.png`
- `workflow-export-and-generate-outputs/export-to-excel.png`
- `workflow-export-and-generate-outputs/generate-cad.png`
- `troubleshooting/database-load-error.png`
- `troubleshooting/nothing-to-export.png`

## Current Batch Note

During the DOC-105 batch run on 2026-05-13, the app process could be started in this environment, but the session did not expose a usable desktop window for capture:

- shell-based screenshot capture failed with a Win32 `CopyFromScreen` invalid-handle error
- the launched `Drone Designer` process reported no main window handle
- an in-process helper attempt surfaced a `Database Load Error` dialog instead of a stable renderable main window

Treat this README as the organized destination and capture inventory for the next run in an interactive Windows session. Do not add fabricated screenshots to satisfy the folder structure.
