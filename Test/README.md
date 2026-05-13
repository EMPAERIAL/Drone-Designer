# Test Directory

- `Scenarios/` contains maintained CSV inputs for the in-app harness (`Ctrl+Shift+L`).
- `Scenarios/multirotor_regression.csv` is the blocking regression track.
- `Scenarios/fixedwing_exploratory.csv` is the non-blocking exploratory track.
- `Logs/` contains generated test-run output and is ignored by git.
- `Components/` contains generated component export artifacts and is ignored by git.

## Interpretation Policy

- Blocking failures (multirotor track) are regression failures and require action.
- Exploratory failures (fixed-wing track) are expected while fixed-wing selection matures; track them, but do not treat them as blocking.
- Harness logs now label each scenario with `[blocking]` or `[exploratory]` and print per-track pass/fail totals.
