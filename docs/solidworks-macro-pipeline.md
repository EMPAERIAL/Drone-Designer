# SolidWorks Macro Pipeline — How We Made It Work

This document records every problem we hit getting `RunMacro2` to execute macros from the
Drone Designer pipeline, and the exact fix for each one. Follow the checklist at the bottom
whenever adding a new macro so you don't have to rediscover any of this.

---

## Problem 1 — SolidWorks COM calls were silently ignored

### What happened
The pipeline ran, SolidWorks opened, but the macro never executed. No error, no output file,
nothing. `RunMacro2` returned as if it succeeded but did nothing.

### Root cause
`PipelineOrchestrator.RunAsync` uses `Async/Await`. After this line:

```vb
Await Task.Run(Function() _selector.SelectComponents(specs), cancellationToken)
```

every line that follows resumes on a **.NET thread-pool thread**, which is **MTA
(Multi-Threaded Apartment)**. SolidWorks COM is **STA (Single-Threaded Apartment)**.
Calling `RunMacro2` from an MTA thread causes it to silently do nothing and return without
an error code — no exception, no `swErr`, just a blank result.

### What STA and MTA mean
COM (Component Object Model) is the system that lets .NET talk to SolidWorks across process
boundaries. SolidWorks was built to receive calls from exactly one thread at a time, in an
orderly queue — that is STA. .NET's thread pool hands work to many threads simultaneously
with no ordering guarantees — that is MTA. When an MTA thread calls a STA COM object,
the call either gets silently dropped or misbehaves.

### The fix
Extract all SolidWorks work — Connect, RunMacro2, Disconnect — into a single synchronous
method (`RunSolidWorksStages`), then invoke it via `RunOnStaAsync`: a helper that spawns
a dedicated STA thread and runs the entire block on it.

```vb
Return Await RunOnStaAsync(Of PipelineResult)(Function()
    Return RunSolidWorksStages(
        selectionResult, specs, outputDirectory,
        parts, swWasConnectedBeforeRun,
        progress, cancellationToken, startedAt)
End Function)
```

### Critical rule
**Connect and every COM call after it must all run on the same STA thread.**
Wrapping only `RunMacro2` in its own STA thread still fails because the `_swApp` COM object
was created on a different apartment. The fix is one lambda, one thread, the entire session.
Never split SolidWorks calls across separate `RunOnStaAsync` invocations.

---

## Problem 2 — `RunMacro2` does not accept `.swb` files

### What happened
Switching the pipeline to point at `MotorMount.swb` produced `result=False, swErr=0`
in about 39 ms. The macro body never ran.

### Root cause
`.swb` is the old plain-text VBA format. `RunMacro2` silently rejects it and returns
immediately with no error code. Running a `.swb` manually inside SolidWorks works because
SolidWorks' own **Tools → Macros → Run** menu handles `.swb` internally. `RunMacro2`
called from an external COM client requires the binary `.swp` (VBA project) format.

### The fix
Always use `.swp` as the macro file for `RunMacro2`. Keep `.swb` files as the
human-readable source you edit in a text editor; the `.swp` is the compiled artifact
that the pipeline actually runs.

---

## Problem 3 — The existing `.swp` had stale content and broken type library references

### What happened
The pipeline ran `MotorMount.swp` and returned `swErr=22` (VBA runtime error). SolidWorks
was visibly doing things — opening the template and running some API calls — but then
crashed out of the macro.

### Root cause
`MotorMount.swp` was created at an earlier point with different macro code and with
references to an older SolidWorks type library. When run on SolidWorks 2026 (API 34.0.0),
the old type library binding could not resolve, causing a VBA runtime error at startup.
The "few actions" visible in SolidWorks were the old macro code executing before it hit
the unresolvable reference.

### The fix
The `.swp` binary must be regenerated from the currently installed SolidWorks version.
You cannot fix this by editing the file — the type library references are baked into the
binary. Recreate it:

1. Open SolidWorks 2026
2. **Tools → Macros → Edit** → open `MotorMount.swp`
3. Delete all existing code in the module
4. Paste in the new macro code
5. Add the correct type library: **Tools → References → check "SOLIDWORKS 2026 Type Library"**
6. Save — SolidWorks rewrites the binary with correct 2026 references
7. Rebuild the Drone Designer project so the updated `.swp` is copied to `bin\Debug\Resources\SolidWorks\Macros\`

Any time the project moves to a new SolidWorks version, the `.swp` files must be
regenerated this way.

---

## Problem 4 — Module name conflict between VBA project and module

### What happened
Trying to rename the VBA module to `MotorMount` inside the `.swp` produced a naming
conflict error.

### Root cause
When SolidWorks saves a file as `MotorMount.swp`, it automatically names the **VBA
project** `MotorMount`. A module inside a VBA project cannot share the same name as
the project itself. So the module defaulted to `MotorMount1`.

The pipeline was passing `"MotorMount"` as the module name to `RunMacro2`, which could
not find it and failed.

### The fix
Use the actual module name that SolidWorks assigned. Check it in the VBA editor's
Project Explorer before wiring up the pipeline call:

```vb
_macroRunner.RunMacroOnTemplate(
    templatePath,
    macroPath,
    "MotorMount1",      ' must match the module name shown in the VBA Project Explorer
    "BuildMotorMount",
    ...)
```

To avoid this ambiguity on new macros: after creating the `.swp`, open it in the VBA
editor, note the exact module name in the Project Explorer, and use that string in the
pipeline. Alternatively, rename the VBA **project** (not the module) to something like
`MotorMountProject` via **Tools → [ProjectName] Properties**, which frees up `MotorMount`
as a module name.

---

## Checklist for adding a new macro

1. **Write the code as `.swb`** — plain text, easy to edit and version-control.
2. **Create a fresh `.swp` from SolidWorks:**
   - Tools → Macros → Edit → open the `.swp` (or create new via Tools → Macros → New)
   - Paste the code from your `.swb`
   - Add the type library: Tools → References → check "SOLIDWORKS [year] Type Library"
   - Save
3. **Check the module name** in the VBA Project Explorer — write it down.
4. **Rebuild the Drone Designer project** so the `.swp` is copied to `bin\Debug\Resources\SolidWorks\Macros\`.
5. **Wire up the pipeline call** using the exact module name from step 3 and the exact
   Sub name as the procedure name.
6. **All SolidWorks COM calls go inside `RunOnStaAsync` as one block** — Connect through
   Disconnect. Never split them across separate `RunOnStaAsync` calls.
7. **Test with the diagnostic probe first** (a macro that just writes a file to `C:\Temp\`)
   before adding real SolidWorks API calls. If the probe file appears, the pipeline is
   working end-to-end.
