# Preparing A Release Package

Use this workflow when you are assembling the ZIP-style MVP release artifact rather than validating a source checkout for local development.

The user-facing installation guide assumes this packaging work has already been done correctly. That makes release preparation its own maintainer workflow, not just a side note in testing.

Read this page together with:

- [Configuration And Runtime Dependencies](../../reference/configuration-and-runtime-dependencies.md)
- [Testing And Validation](../../reference/testing-and-validation.md)
- [Installation And Prerequisites](../../user/installation-and-prerequisites.md)

## Read First

Before packaging a release, read:

1. [`Drone Designer.vbproj`](../../Drone%20Designer.vbproj) to see which runtime assets are actually copied into the build output.
2. [`docs/reference/configuration-and-runtime-dependencies.md`](../../reference/configuration-and-runtime-dependencies.md) to review the runtime-path assumptions.
3. [`docs/reference/testing-and-validation.md`](../../reference/testing-and-validation.md) for the package-level validation checks.

If the release claims CAD support, also read [`docs/maintainers/workflows/changing-cad-generation.md`](changing-cad-generation.md) so you do not ship a ZIP that omits required macros or templates.

## Release-Package Workflow

Use this order:

1. Build the intended configuration.
2. Inspect the actual output directory, not just the source tree.
3. Verify the required runtime asset layout beside the executable.
4. Smoke-test the package from an extracted folder.
5. Zip the release only after the runtime layout and smoke test pass.

Practical rule: package from the built output, not by hand-copying arbitrary source-tree files into a ZIP at the last minute.

## Runtime Assets To Verify

For the normal main-app path, verify that the release artifact keeps these together:

- `Drone Designer.exe`
- `Newtonsoft.Json.dll`
- `DroneDesigner.config.json`, if pre-seeded, or a writable location where the app can create it
- `Resources\AppData\components.json`

If the release includes optional CAD support, also verify the relevant runtime assets under `Resources\SolidWorks\`.

Current repo reality matters here: the source tree contains more SolidWorks assets than the project file necessarily copies into the runtime output. Always verify the built output folder before packaging.

## Main-App Versus CAD-Capable Release

Be explicit about which release you are preparing.

Main-app-only release:

- must launch
- must load the component database
- must support selection and Excel export
- does not require SolidWorks

CAD-capable release:

- must satisfy all main-app checks
- must include the required templates and compiled `.swp` macros
- should only be claimed when validated on a SolidWorks-capable machine

Do not blur those release modes in the packaging notes or release announcement.

## Packaging Validation

Minimum release validation:

1. extract the candidate package into a normal writable folder
2. launch the app from the extracted location
3. confirm startup creates or reads config successfully
4. confirm `components.json` loads from the packaged path
5. run one manual design flow
6. verify Excel export

Add CAD validation when the release claims CAD support.

Use [Testing And Validation](../../reference/testing-and-validation.md) as the central checklist; this page is the packaging workflow that leads into those checks.

## Common Release Mistakes

Watch for:

- validating from a source checkout instead of the packaged output
- assuming every source-tree `Resources\SolidWorks\` asset was copied into the build
- shipping a ZIP that cannot create `DroneDesigner.config.json` because of the extraction location
- packaging a CAD-capable release without confirming the compiled `.swp` files are present
- treating a successful local build as proof that the ZIP artifact is correct

These are packaging failures, not necessarily code failures.

## Primary Related Files

- `Drone Designer.vbproj`
- `Utilities/ConfigManager.vb`
- `docs/reference/configuration-and-runtime-dependencies.md`
- `docs/reference/testing-and-validation.md`
- `docs/user/installation-and-prerequisites.md`

## Secondary Related Files

- `Resources/AppData/components.json`
- `Resources/SolidWorks/`
- `UI/Forms/MainForm.CAD.vb`
- `Core/Services/PipelineOrchestrator.vb`
- `docs/maintainers/workflows/changing-cad-generation.md`
