# Changing The Component Database And Schema

Use this workflow when you are editing `components.json`, changing component-model fields, or changing schema assumptions the runtime depends on.

In Drone Designer, data changes are code changes unless proven otherwise. Category strings, nested dimension fields, and derived-value conventions are all runtime-coupled.

## Read First

Before editing data or schema, read in this order:

1. [`docs/reference/component-database-and-schema.md`](../../reference/component-database-and-schema.md) for the stable contract.
2. [`Core/Data/ComponentRepository.vb`](../../Core/Data/ComponentRepository.vb) for deserialization and typed accessors.
3. [`Core/Models/ComponentSpecs.vb`](../../Core/Models/ComponentSpecs.vb) for `JsonProperty` names, derived values, and `OnDeserialized` behavior.
4. [`docs/reference/testing-and-validation.md`](../../reference/testing-and-validation.md) for the required validation path.

If the data change is motivated by engine behavior, also read [`docs/maintainers/workflows/changing-the-selection-engine.md`](changing-the-selection-engine.md) so the code and data updates stay aligned.

## Safe Data-Change Workflow

Use this order:

1. Decide whether the change is data-only or schema-coupled.
2. Make the smallest coherent edit to [`Resources/AppData/components.json`](../../Resources/AppData/components.json).
3. If field names, category names, or nested structure changed, update the relevant model and repository code in the same slice.
4. Update the reference doc if the stable schema contract changed.
5. Run the validation path before merging.

Do not treat a component-database edit as "just content" when:

- a category string changed
- a field moved between nested and top-level locations
- a derived fallback is now relied on differently
- a selector starts depending on a new field

## Schema-Impact Awareness

Schema-sensitive edits usually affect more than one layer:

- `components.json` data shape
- `ComponentSpecs` properties and `JsonProperty` bindings
- `ComponentRepository.DeserializeComponents()` category dispatch
- selection-engine logic that consumes the data
- reference docs that describe the stable contract

That coupling is why category renames and dimension-field edits are high-risk even when the JSON still parses.

## Validation Path

Minimum validation for a component-database or schema change:

1. Build the app.
2. Launch it and confirm the repository still loads without a startup error.
3. Run one manual design flow through `Select Components`.
4. Run the in-app scenario harness.
5. Check that the changed records still appear or participate where expected.

Add export validation if the changed fields are surfaced in the result grid or workbook. Add CAD validation only when the change affects parts, dimensions, or downstream geometry assumptions.

Use [Testing And Validation](../../reference/testing-and-validation.md) as the central checklist rather than inventing a one-off test story in the workflow doc.

## Common Failure Modes

Watch for:

- category renames that stop records from being indexed into the expected accessor
- field renames that bypass `JsonProperty` bindings
- nested `dimensions` edits that break top-level convenience properties
- data that still loads but causes selector drift because a derived fallback changed silently
- source-tree data edits that are not copied into the actual runtime output being tested

These failures often look like "fewer recommendations" or "odd warnings" instead of immediate crashes.

## Documentation Updates That Usually Belong With The Change

Update these docs when applicable:

- [Component Database And Schema](../../reference/component-database-and-schema.md) when the stable contract changed
- [Configuration And Runtime Dependencies](../../reference/configuration-and-runtime-dependencies.md) when runtime paths or packaged data assumptions changed
- [Changing The Selection Engine](changing-the-selection-engine.md) when the data change is tightly coupled to selector logic

## Primary Related Files

- `Resources/AppData/components.json`
- `Core/Models/ComponentSpecs.vb`
- `Core/Data/ComponentRepository.vb`
- `docs/reference/component-database-and-schema.md`
- `docs/reference/testing-and-validation.md`

## Secondary Related Files

- `Core/Services/ComponentSelectionEngine.vb`
- `Utilities/ConfigManager.vb`
- `UI/Forms/MainForm.Logic.vb`
- `docs/reference/configuration-and-runtime-dependencies.md`
- `docs/maintainers/workflows/changing-the-selection-engine.md`
