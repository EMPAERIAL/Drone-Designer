# Installation And Prerequisites

Use this guide when you have a packaged Drone Designer release and want to start using the app as an operator. It covers the normal ZIP-based setup path only.

If you are trying to change the code, rebuild the app, or troubleshoot internal file paths, use the maintainer docs instead of this page.

## Before You Start

You need:

- A Drone Designer release ZIP.
- A Windows machine that can run `.NET Framework 4.7.2` desktop apps.
- Permission to extract the ZIP to a normal writable folder such as your Desktop or Documents.

You do not need SolidWorks to run the main component-selection workflow. SolidWorks is only needed if you want to use the optional CAD-generation step later.

## Recommended Install Path

1. Download the Drone Designer release ZIP.
2. Extract the ZIP to a normal folder.
3. Open the extracted folder.
4. Launch `Drone Designer.exe`.

Recommended extraction locations:

- `Desktop\Drone Designer`
- `Documents\Drone Designer`
- Another normal user-writable folder

Avoid launching the app directly from inside the ZIP. Extract it first so the runtime files stay next to the executable.

## Files That Must Stay Together

Drone Designer depends on bundled runtime files that live beside the executable in the extracted release.

Do not separate:

- `Drone Designer.exe`
- `Resources\AppData\components.json`
- `Resources\SolidWorks\...` files included with the release
- other files and folders shipped in the ZIP

If `components.json` is missing or moved, the app can open but component selection will fail during startup.

## First Launch Check

After launch, confirm the app opens normally and reaches the main input form.

The normal operator starting point is the `Mission Specifications` tab. That is the screen used by the next workflow guide.

On a healthy first launch:

- the main window opens without a startup error
- mission input controls are available
- the app does not report that the component database failed to load

If the app shows a component-database load error, the extracted release is incomplete or the folder structure was changed after extraction.

## Optional Prerequisite For CAD Generation

SolidWorks is optional and only matters if you plan to use the `Generate CAD` step after selecting components.

You need SolidWorks only when you want to:

- send the selected design into the CAD-generation pipeline
- generate the SolidWorks output parts from inside Drone Designer

If you only need mission input, component selection, and Excel export, you can ignore SolidWorks completely.

## What Works Without Extra Setup

In the normal MVP release flow, you should be able to:

- launch the app
- enter mission and payload requirements
- run component selection
- review the recommended components
- export the result to Excel

These steps should work from the packaged release without manual config editing.

If the app launches and reaches the mission-input screen, continue directly to the design workflow instead of trying to configure anything else first.

## What This Guide Does Not Cover

This page does not cover:

- building from source
- editing project files or config files
- changing `components.json`
- preparing SolidWorks macros
- maintainer validation or release packaging

For the next operator step, continue to [Design A UAV From Requirements](workflow-design-a-uav-from-requirements.md).
