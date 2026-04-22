' =============================================================================
' File:    Core/Models/ComponentDisplayRow.vb
' Project: Drone Designer
' Task:    11 — Wire UI to Engine
'
' Purpose: Lightweight display DTO that maps a ComponentSpecs instance (plus a
'          category label and selection note) into the exact property names that
'          the MainForm output DataGridView expects.
'
'          Why a separate class?
'            The DataGridView column DataPropertyNames were defined in Task 10
'            (MainForm.vb) before ComponentSpecs.vb was available. Some of
'            those names differ from the property names the engine confirmed:
'              DGV column "MaxPowerWatts"  ↔  ComponentSpecs.MaxPowerW
'              DGV column "NominalVoltage" ↔  ComponentSpecs.NominalVoltageV (assumed)
'            This class performs the name bridging so neither the grid definition
'            nor the engine class needs to change.
'
'          DataGridView column DataPropertyNames (from MainForm.vb Task 10):
'            Category | Manufacturer | ModelName | MassGrams | NominalVoltage |
'            MaxPowerWatts | Dimensions | Interface | TempRating | SelectionNotes
'
'          IsRecommended is NOT a grid column; it is used by MainForm.Logic.vb
'          to apply a green highlight to the top-ranked candidate per category.
'
' ComponentSpecs property name assumptions (verify against ComponentSpecs.vb):
'   Confirmed by engine code: MassGrams, MaxPowerW, MaxCurrentA, OperatingTempMinC
'   Assumed: Manufacturer, ModelName, NominalVoltageV, Dimensions, Interface,
'            OperatingTempMaxC, LengthMm, WidthMm, HeightMm, DiameterMm
'   If a property is missing a compile error will surface — add the property to
'   ComponentSpecs.vb or update the mapping in FromComponentSpecs() below.
'
' Author:  [Solo Dev] — Task 11
' Target:  .NET Framework 4.7.2 / VB.NET
' =============================================================================

Namespace Core.Models

    ''' <summary>
    ''' Display-only row object bound to the output DataGridView in MainForm.
    ''' Every public property name matches a DataGridView column DataPropertyName
    ''' defined in MainForm.vb so WinForms data binding resolves without any
    ''' additional column configuration.
    ''' </summary>
    Public Class ComponentDisplayRow

        ' ── Properties matching DataGridView column DataPropertyNames ────────

        ''' <summary>
        ''' Component category label, e.g. "Motor", "Battery (LiPo)", "GPS / GNSS Module".
        ''' Supplied by the presenter (not read from ComponentSpecs).
        ''' </summary>
        Public Property Category As String = String.Empty

        ''' <summary>Manufacturer / brand name (e.g. "T-Motor", "Holybro").</summary>
        Public Property Manufacturer As String = String.Empty

        ''' <summary>Model name or part number (e.g. "F60 Pro IV", "Here3+").</summary>
        Public Property ModelName As String = String.Empty

        ''' <summary>Component mass in grams. Included in the summary total (recommended rows only).</summary>
        Public Property MassGrams As Double = 0.0

        ''' <summary>
        ''' Nominal operating voltage in volts.
        ''' For batteries: pack nominal voltage (cell count × 3.7 V).
        ''' For motors/ESCs: mid-point of operating range.
        ''' For BEC-powered boards: typically 5 V.
        ''' </summary>
        Public Property NominalVoltage As Double = 0.0

        ''' <summary>
        ''' Maximum rated power consumption or output in watts.
        ''' Sourced from ComponentSpecs.MaxPowerW (engine-confirmed name).
        ''' </summary>
        Public Property MaxPowerWatts As Double = 0.0

        ''' <summary>
        ''' Physical dimensions as a formatted string, e.g. "38 × 38 × 7 mm"
        ''' or "⌀ 229 mm" for round/circular components.
        ''' </summary>
        Public Property Dimensions As String = String.Empty

        ''' <summary>
        ''' Communication interface or electrical protocol, e.g. "UART", "DSHOT600",
        ''' "I²C / UART", "PPM / SBUS".
        ''' </summary>
        Public Property [Interface] As String = String.Empty

        ''' <summary>
        ''' Operating temperature range formatted as "−20 to +85 °C".
        ''' "—" when temperature ratings are not populated in the database.
        ''' </summary>
        Public Property TempRating As String = String.Empty

        ''' <summary>
        ''' Selection rank within the category:
        '''   "Recommended"   — top-ranked candidate (index 0 from engine).
        '''   "Alternative 2" / "Alternative 3" — lower-ranked options.
        ''' </summary>
        Public Property SelectionNotes As String = String.Empty

        ' ── Non-column metadata ──────────────────────────────────────────────

        ''' <summary>
        ''' True for the first (highest-ranked) entry in each component category.
        ''' Used by DisplaySelectionResult() in MainForm.Logic.vb to highlight
        ''' the row green. Not bound to any DataGridView column.
        ''' </summary>
        Public Property IsRecommended As Boolean = False

        ' =====================================================================
        '  FACTORY METHOD
        ' =====================================================================

        ''' <summary>
        ''' Creates a <see cref="ComponentDisplayRow"/> from a <see cref="ComponentSpecs"/>
        ''' object, adding the category label, selection rank note, and recommendation flag.
        '''
        ''' Property mapping (ComponentSpecs → ComponentDisplayRow):
        '''   Manufacturer        ← comp.Manufacturer          (assumed)
        '''   ModelName           ← comp.ModelName             (assumed)
        '''   MassGrams           ← comp.MassGrams             (confirmed)
        '''   NominalVoltage      ← comp.NominalVoltageV       (assumed; see TODO below)
        '''   MaxPowerWatts       ← comp.MaxPowerW             (confirmed via engine)
        '''   Dimensions          ← comp.Dimensions string OR computed from L×W×H / diameter
        '''   Interface           ← comp.Interface             (assumed)
        '''   TempRating          ← formatted from OperatingTempMinC + OperatingTempMaxC
        '''
        ''' TODOs (verify against ComponentSpecs.vb):
        '''   • If NominalVoltageV doesn't exist, replace with the correct property name
        '''     or compute as (OperatingVoltageMinV + OperatingVoltageMaxV) / 2.
        '''   • If OperatingTempMaxC doesn't exist, use OperatingTempMinC only.
        '''   • If LengthMm / WidthMm / HeightMm / DiameterMm don't exist, remove
        '''     BuildDimensionsString() or use the Dimensions string property directly.
        ''' </summary>
        Public Shared Function FromComponentSpecs(comp As ComponentSpecs, category As String, selectionNote As String, isRecommended As Boolean) As ComponentDisplayRow

            ' Guard: return a skeleton row for a null component rather than throwing.
            If comp Is Nothing Then
                Return New ComponentDisplayRow With {
                    .Category = category,
                    .Manufacturer = "—",
                    .ModelName = "—",
                    .SelectionNotes = selectionNote,
                    .IsRecommended = isRecommended
                }
            End If

            Return New ComponentDisplayRow With {
                .Category = category,
                .Manufacturer = NullOrDash(comp.Manufacturer),
                .ModelName = NullOrDash(comp.ModelName),
                .MassGrams = comp.MassGrams,
                .NominalVoltage = comp.NominalVoltageV,
                .MaxPowerWatts = comp.MaxPowerW,
                .Dimensions = BuildDimensionsString(comp),
                .Interface = NullOrDash(comp._Interface),
                .TempRating = FormatTempRange(comp),
                .SelectionNotes = selectionNote,
                .IsRecommended = isRecommended
            }
            ' TODO: verify property name in ComponentSpecs.vb.
            '       Fallback: (comp.OperatingVoltageMinV + comp.OperatingVoltageMaxV) / 2


            ' Engine code confirms MaxPowerW as the property name.

        End Function

        ' ─────────────────────────────────────────────────────────────────────
        '  PRIVATE HELPERS
        ' ─────────────────────────────────────────────────────────────────────

        ''' <summary>Returns <paramref name="value"/> or "—" when null / whitespace.</summary>
        Private Shared Function NullOrDash(value As String) As String
            Return If(String.IsNullOrWhiteSpace(value), "—", value)
        End Function

        ''' <summary>
        ''' Formats an operating temperature range for display.
        '''   • Both limits populated → "−20 to +85 °C"
        '''   • Min only (OperatingTempMaxC missing or zero) → "−20 °C min"
        '''   • Neither → "—"
        '''
        ''' TODO: if ComponentSpecs does not have OperatingTempMaxC, remove the
        '''       branch that uses it and return only the minimum string.
        ''' </summary>
        Private Shared Function FormatTempRange(comp As ComponentSpecs) As String
            Dim hasMin As Boolean = (comp.MinOperatingTempC <> 0.0)
            Dim hasMax As Boolean = (comp.MaxOperatingTempC <> 0.0)   ' TODO: verify property exists

            If Not hasMin AndAlso Not hasMax Then Return "—"
            If hasMin AndAlso hasMax Then
                Return $"{comp.MinOperatingTempC:+0;−0;0} to {comp.MaxOperatingTempC:+0;−0;0} °C"
            End If
            Return $"{comp.MinOperatingTempC:+0;−0;0} °C min"
        End Function

        ''' <summary>
        ''' Builds a compact dimension string from ComponentSpecs.
        ''' Priority order:
        '''   1. comp.Dimensions (pre-formatted string, if populated)
        '''   2. L × W × H from LengthMm / WidthMm / HeightMm
        '''   3. Diameter from DiameterMm (motors, propellers)
        '''   4. "—" fallback
        '''
        ''' TODO: verify property names (LengthMm, WidthMm, HeightMm, DiameterMm)
        '''       against ComponentSpecs.vb; rename or remove unused branches.
        ''' </summary>
        Private Shared Function BuildDimensionsString(comp As ComponentSpecs) As String
            ' Prefer a pre-formatted string property if available

            ' Three-axis box (PCBs, housings)
            If comp.Dimensions.Length > 0 AndAlso comp.Dimensions.Width > 0 AndAlso comp.Dimensions.Height > 0 Then   ' TODO: verify
                Return $"{comp.Dimensions.Length:0.#} × {comp.Dimensions.Width:0.#} × {comp.Dimensions.Height:0.#} mm"
            End If

            ' Circular / cylindrical (motors, propellers, antennas)
            If comp.Dimensions.Diameter > 0 Then   ' TODO: verify
                Return $"⌀ {comp.Dimensions.Diameter:0.#} mm"
            End If

            Return "—"
        End Function

    End Class

End Namespace
