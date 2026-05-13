# Fix Plan For The 2026-05-13 Test Run Failures

## Summary

The `0 passed, 17 failed` run is a systemic propeller-motor selection collapse, not 17 unrelated bugs.

Fix in this order:

1. Repair runtime component normalization so valid JSON values are not overwritten by zeros.
2. Harden propeller aero evaluation so invalid data is rejected explicitly instead of producing `∞ RPM`.
3. Re-run multirotor scenarios, then retune torque policy only if the calibrated pass case still fails.
4. Separate fixed-wing scenarios from multirotor regression interpretation.

This follows:

- `docs/maintainers/workflows/changing-the-selection-engine.md`
- `docs/reference/testing-and-validation.md`

## Implementation Changes

### 1) Runtime normalization fix (`ComponentSpecs`)

Update `MotorSpec` and `PropellerSpec` `OnDeserialized` behavior:

- Preserve non-zero top-level values already deserialized.
- Copy from `Dimensions.*` only when nested values are positive.
- Never overwrite positive top-level values with zero.
- Apply derived fallbacks after merge (`Efficiency`, `KtNmPerA`, `MaxTorqueNm`, `CtStatic`, `CpStatic`).

Expected result:

- `mot_013..mot_015` keep shaft diameter at runtime.
- Prop records do not lose diameter, bore, or aero coefficients due to partial nested blocks.

### 2) Repository load diagnostics (`ComponentRepository`)

Add post-load non-fatal audits with warnings:

- Motors:
  - zero shaft diameter
  - invalid prop diameter range
  - invalid voltage range
- Propellers:
  - non-positive diameter
  - non-positive `CtStatic` or `CpStatic` after normalization
  - invalid bore when source suggests value should exist

Public API addition:

- `ComponentRepository.LoadWarnings As IReadOnlyList(Of String)`

### 3) Explicit aero input guard (`ComponentSelectionEngine`)

Before aero computation, reject prop candidates when any are non-positive:

- `DiameterInches`
- `CtStatic`
- `CpStatic`
- `rho`

Do not allow divide-by-zero path in `ComputePropellerAero`.
Log explicit rejection reason (invalid aero inputs) instead of `Required RPM ∞`.

Expected result:

- No `∞ RPM` rejection lines.
- Invalid catalogue data becomes diagnosable and deterministic.

### 4) Policy retune only after data-path fixes

After steps 1 to 3:

- Re-run maintained multirotor suite.
- If `Calibrated Survey Quad (Pass)` still fails primarily due to torque near-misses:
  - lower default `KMotorTorque` from `2.0` to `1.5`.
- Calibration outcome (2026-05): default `KMotorTorque` baseline was set to `1.5`; harsh preset remains `3.0`.
- Keep harsh preset high (`3.0`).
- Keep racing preset at `1.5`.

### 5) Split scenario interpretation tracks

Keep fixed-wing scenarios out of multirotor regression pass/fail accounting until a dedicated fixed-wing pipeline exists.

- Maintain separate scenario files:
  - multirotor regression set
  - fixed-wing exploratory set
- Update test README to clarify interpretation policy.

### 6) Documentation synchronization

Update docs in the same vertical slice:

- schema/reference doc: mixed top-level and nested normalization behavior
- pipeline doc: explicit invalid-aero rejection behavior
- testing reference: multirotor harness as primary regression signal for this selector path

## Test Plan

1. Repository sanity:
   verify runtime values for known problematic motors and props, then confirm `LoadWarnings` is empty or expected.
2. Engine guard verification:
   run a small multirotor scenario and confirm no `Required RPM ∞` appears.
3. Harness regression:
   run maintained multirotor CSV and require:
   - `Calibrated Survey Quad (Pass)` passes
   - at least one small-quad scenario passes
   - run is no longer `0 passed, 17 failed`
4. Manual flow check:
   launch app, run one realistic quad selection, verify motor/prop/battery/ESC populate and warnings are coherent.
5. If torque default changed:
   re-run same suite and confirm pass-rate improves without obvious under-torque pairings.

## Assumptions

- No dedicated fixed-wing selector is added in this change.
- Existing mixed-schema JSON remains; code becomes tolerant of mixed top-level and nested shapes.
- Runtime normalization and diagnostics are fixed before catalogue expansion.
- Docs are updated in the same PR if behavior changes.
