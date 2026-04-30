"""Generate MTOW Iterator & Selection Logic reference PDF."""
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle,
    HRFlowable, KeepTogether
)
from reportlab.lib.enums import TA_LEFT, TA_CENTER

OUT = r"C:\Users\Ahmed Osman\Desktop\Teknofest\THE DRONE DESIGNER\Drone Designer\MTOW_Selection_Logic.pdf"

W, H = A4
MARGIN = 2.0 * cm

doc = SimpleDocTemplate(
    OUT, pagesize=A4,
    leftMargin=MARGIN, rightMargin=MARGIN,
    topMargin=MARGIN, bottomMargin=MARGIN
)

BASE = getSampleStyleSheet()

def style(name, parent="Normal", **kw):
    s = ParagraphStyle(name, parent=BASE[parent], **kw)
    return s

TITLE   = style("TITLE",   "Title",   fontSize=18, spaceAfter=4, textColor=colors.HexColor("#1a1a2e"))
H1      = style("H1",      "Heading1", fontSize=13, spaceBefore=14, spaceAfter=4,
                textColor=colors.HexColor("#16213e"), borderPad=2)
H2      = style("H2",      "Heading2", fontSize=10.5, spaceBefore=10, spaceAfter=3,
                textColor=colors.HexColor("#0f3460"))
BODY    = style("BODY",    "Normal",   fontSize=9.5, leading=14, spaceAfter=4)
EQ      = style("EQ",      "Normal",   fontSize=9.5, leading=15, spaceAfter=3,
                fontName="Courier", leftIndent=28, textColor=colors.HexColor("#1a1a2e"),
                backColor=colors.HexColor("#f4f6f9"), borderPad=4)
NOTE    = style("NOTE",    "Normal",   fontSize=8.5, leading=12, spaceAfter=3,
                textColor=colors.HexColor("#555555"), leftIndent=14)
CAPTION = style("CAPTION", "Normal",   fontSize=8.5, leading=11, spaceAfter=6,
                textColor=colors.HexColor("#333333"), alignment=TA_CENTER)
WARN    = style("WARN",    "Normal",   fontSize=9, leading=13, spaceAfter=5,
                textColor=colors.HexColor("#8b0000"), leftIndent=14)

def h1(txt):    return Paragraph(txt, H1)
def h2(txt):    return Paragraph(txt, H2)
def body(txt):  return Paragraph(txt, BODY)
def eq(txt):    return Paragraph(txt, EQ)
def note(txt):  return Paragraph(txt, NOTE)
def warn(txt):  return Paragraph(txt, WARN)
def sp(n=6):    return Spacer(1, n)
def hr():       return HRFlowable(width="100%", thickness=0.5, color=colors.HexColor("#cccccc"), spaceAfter=6)

# ── Table helper ──────────────────────────────────────────────────────────────
def make_table(rows, col_widths, header_row=True):
    t = Table(rows, colWidths=col_widths, hAlign="LEFT")
    style_cmds = [
        ("FONTNAME",  (0,0), (-1,-1), "Helvetica"),
        ("FONTSIZE",  (0,0), (-1,-1), 8.5),
        ("LEADING",   (0,0), (-1,-1), 12),
        ("ROWBACKGROUNDS", (0,0), (-1,-1),
         [colors.HexColor("#f0f4fa"), colors.white]),
        ("GRID",      (0,0), (-1,-1), 0.35, colors.HexColor("#cccccc")),
        ("LEFTPADDING",  (0,0), (-1,-1), 6),
        ("RIGHTPADDING", (0,0), (-1,-1), 6),
        ("TOPPADDING",   (0,0), (-1,-1), 4),
        ("BOTTOMPADDING",(0,0), (-1,-1), 4),
    ]
    if header_row:
        style_cmds += [
            ("BACKGROUND",  (0,0), (-1,0), colors.HexColor("#16213e")),
            ("FONTNAME",    (0,0), (-1,0), "Helvetica-Bold"),
            ("TEXTCOLOR",   (0,0), (-1,0), colors.white),
            ("FONTSIZE",    (0,0), (-1,0), 9),
        ]
    t.setStyle(TableStyle(style_cmds))
    return t

# =============================================================================
story = []

story.append(Paragraph("Drone Designer — Component Selection Engine", TITLE))
story.append(Paragraph("MTOW Iterator &amp; Selection Logic Reference", style("SUB","Normal",
    fontSize=11, spaceAfter=2, textColor=colors.HexColor("#555555"))))
story.append(Paragraph("Teknofest 2025  ·  Core/Services/ComponentSelectionEngine.vb", NOTE))
story.append(sp(10))
story.append(hr())

# =============================================================================
story.append(h1("1  MTOW Fixed-Point Iterator"))
story.append(body(
    "Battery mass and MTOW are mutually dependent — a heavier drone needs more power, "
    "which requires a larger battery, which increases MTOW. The solver resolves this "
    "circular dependency by iterating until the MTOW delta falls below 1 g."
))

# ── 1.1 Structural mass ───────────────────────────────────────────────────────
story.append(h2("1.1  Structural Mass (constant across all iterations)"))
story.append(eq("m_struct  =  m_airframe  +  35 (FC stack)  +  10 (Rx)  +  20 (GPS)  +  30 (wiring)  +  m_payload"))
story.append(sp(4))
story.append(make_table(
    [["Motor count", "3", "4", "6", "8", "12"],
     ["Airframe mass (g)", "350", "250", "420", "680", "1 200"]],
    [3.5*cm, 2*cm, 2*cm, 2*cm, 2*cm, 2*cm]
))
story.append(sp(4))

# ── 1.2 Battery seed ─────────────────────────────────────────────────────────
story.append(h2("1.2  Initial Battery Mass Seed"))
story.append(body(
    "The seed fraction is chosen by endurance bracket, then scaled inversely with "
    "battery pack specific energy so that higher-density chemistries start with a "
    "smaller seed:"
))
story.append(eq("f_base  =  0.25   if  t < 15 min"))
story.append(eq("f_base  =  0.35   if  15 ≤ t ≤ 45 min"))
story.append(eq("f_base  =  0.45   if  t > 45 min"))
story.append(sp(2))
story.append(eq("f_seed  =  clamp( f_base × (130 / E_pack_Wh_kg),  0.10,  0.60 )"))
story.append(eq("b_0     =  m_struct  ×  f_seed                             [grams]"))
story.append(sp(4))
story.append(make_table(
    [["Chemistry", "Default E_pack (Wh/kg)", "f_seed  @ 24 min (base 0.35)"],
     ["LiPo",                "130",  "0.35"],
     ["Li-Ion",              "180",  "0.25"],
     ["H₂ Fuel Cell",        "350",  "0.13  (clamped → 0.13)"],
     ["Hybrid FC+LiPo",      "240",  "0.19"],
     ["Tethered",            "  0",  "0.00  (no battery)"]],
    [4*cm, 4.5*cm, 5.5*cm]
))
story.append(sp(6))

# ── 1.3 Iteration loop ────────────────────────────────────────────────────────
story.append(h2("1.3  Iteration Loop"))
story.append(body("Each pass i recomputes MTOW, hover power, total energy, and the next battery mass:"))
story.append(sp(3))
story.append(eq("MTOW_i      =  ( m_struct + b_i )  ×  1.10                       [+10 % safety]"))
story.append(eq("P_hover     =  MTOW_i  ×  10  [W / 100 g]                        [empirical for 5\"–7\" quads]"))
story.append(eq("E_total     =  P_hover  ×  t_h  ×  1.20                          [+20 % non-hover overhead]"))
story.append(eq("E_usable    =  E_pack_Wh_kg  ×  DoD                              [104 Wh/kg for LiPo @ 80 % DoD]"))
story.append(eq("b_{i+1}     =  ( E_total / E_usable )  ×  1000                   [Wh → grams]"))
story.append(sp(4))
story.append(body("<b>Convergence criterion:</b>  | MTOW_i − MTOW_{i-1} | ≤ 1 g"))
story.append(sp(2))

# ── 1.4 Guards ────────────────────────────────────────────────────────────────
story.append(h2("1.4  Divergence &amp; Non-Convergence Guards"))
story.append(warn(
    "Divergence throw:  if battery mass has increased for 3 consecutive passes "
    "AND  b_i > 1.5 × b_0  → mission is physically infeasible."
))
story.append(warn(
    "Non-convergence throw:  if 10 iterations complete without reaching the "
    "1 g tolerance → mission is at the edge of feasibility."
))
story.append(sp(4))

# ── 1.5 Worked example ────────────────────────────────────────────────────────
story.append(h2("1.5  Worked Example — Default UI Values (quad, 0.4 h, 0 g payload, LiPo)"))
story.append(body("Inputs: N=4, t=0.4 h (24 min), m_payload=0 g, LiPo 130 Wh/kg"))
story.append(sp(3))
story.append(make_table(
    [["Pass", "b_i (g)", "MTOW (g)", "P_hover (W)", "E_total (Wh)", "b_{i+1} (g)", "Δ MTOW (g)"],
     ["seed", "121",   "—",    "—",    "—",    "—",   "—"],
     ["1",    "121",  "512",  "51.2", "24.6", "236",  "512"],
     ["2",    "236",  "639",  "63.9", "30.7", "295",  "127"],
     ["3",    "295",  "704",  "70.4", "33.8", "325",   "65"],
     ["4",    "325",  "737",  "73.7", "35.4", "341",   "33"],
     ["5",    "341",  "754",  "75.4", "36.2", "348",   "17"],
     ["6",    "348",  "762",  "76.2", "36.6", "352",    "8"],
     ["7",    "352",  "766",  "76.6", "36.8", "354",    "4"],
     ["8",    "354",  "768",  "76.8", "36.9", "355",    "2"],
     ["9",    "355",  "769",  "76.9", "37.0", "356",  "≈1  ✓"]],
    [1.5*cm, 1.8*cm, 2*cm, 2.3*cm, 2.5*cm, 2.3*cm, 2.3*cm]
))
story.append(note(
    "* b_0 = 121 g.  Divergence threshold = 1.5 × 121 = 181 g.  "
    "b grows past 181 g at pass 2 (236 g) — guard fires if 3 consecutive increases "
    "have already occurred.  Passes 1–3 are all increases → guard triggers at end of pass 3.  "
    "This is the current bug: the guard is too aggressive for small drones where the seed "
    "is far below the true battery mass."
))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("2  Thrust Requirement  (Step 2)"))
story.append(eq("F_total  =  MTOW  ×  TWR                   TWR = 2.0  (all non-racing missions)"))
story.append(eq("F_motor  =  F_total  /  N                  [gram-force per motor]"))
story.append(body(
    "Units are gram-force (gf) throughout. Motor datasheets rate static thrust in gf, "
    "avoiding unit conversion during filtering."
))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("3  Propeller-First Selection Pipeline"))

story.append(h2("3.1  Frame Geometry → Target Propeller Diameter  (Step 3a)"))
story.append(eq("arm_length        =  diagonal / √2"))
story.append(eq("max_prop_radius   =  arm_length  /  2.0           [FrameArmToPropRadiusRatio = 2.0]"))
story.append(eq("max_prop_diameter =  2 × max_prop_radius          [mm → inches: ÷ 25.4]"))
story.append(eq("target_diameter   =  max_prop_diameter − 0.5 in   [inset for catalog fit]"))
story.append(sp(4))
story.append(make_table(
    [["Motor count", "Diagonal (mm)", "Arm (mm)", "Max prop diam (in)", "Target (in)"],
     ["3",  "220", "155.6", "12.2", "11.7"],
     ["4",  "250", "176.8", "13.9", "13.4"],
     ["6",  "380", "268.7", "21.2", "20.7"],
     ["8",  "500", "353.6", "27.8", "27.3"],
     ["12", "700", "494.9", "38.9", "38.4"]],
    [2.8*cm, 3*cm, 2.8*cm, 3.5*cm, 2.8*cm]
))
story.append(warn(
    "Note: these target diameters (13–38 in) are computed correctly from the geometry "
    "but appear large relative to typical catalog props (5–10 in for small quads). "
    "The FrameArmToPropRadiusRatio=2.0 and diagonal table are calibrated for "
    "commercial/inspection class frames, not 250 mm racing frames."
))
story.append(sp(6))

story.append(h2("3.2  Propeller Selection  (Step 3b)"))
story.append(body("<b>Hard filters:</b>"))
story.append(eq("prop.diameter  ≤  max_prop_diameter            [frame clearance — hard ceiling]"))
story.append(eq("| prop.diameter − target_diameter |  ≤  1.5 in [tolerance band]"))
story.append(sp(3))
story.append(body("<b>Ranking:</b>  distance from target ASC,  then static thrust DESC.  Take top 5."))
story.append(body(
    "<b>Fallback:</b>  if zero candidates pass the tolerance band, drop the ±1.5 in filter "
    "and keep only the frame-clearance constraint. If still zero, throw."
))
story.append(sp(6))

story.append(h2("3.3  Propeller Hover RPM  (momentum theory)"))
story.append(eq("RPM_hover  =  RPM_test  ×  √( F_motor / F_static_at_test )    [T ∝ RPM²]"))
story.append(note("Fallback when no test data: RPM_hover = prop.MaxRPM × 0.70"))
story.append(sp(6))

story.append(h2("3.4  Motor Selection  (Step 3c — propeller-first filters)"))
story.append(body("<b>Hard filters (all must pass):</b>"))
story.append(eq("motor.PropDiameterMin  ≤  prop.diameter  ≤  motor.PropDiameterMax"))
story.append(eq("motor.V_min  ≤  V_nominal  ≤  motor.V_max"))
story.append(eq("motor.MaxThrust  ≥  F_motor"))
story.append(eq("motor.KV × V_nominal  ≥  RPM_hover × 1.4               [RPM headroom factor]"))
story.append(sp(3))
story.append(body("<b>Ranking:</b>  efficiency (gf/W) DESC,  then mass ASC.  Take top 5."))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("4  Power Budget  (Step 4)"))

story.append(h2("4.1  Motor Current"))
story.append(eq("I_peak  =  motor.MaxCurrentA                           [datasheet — preferred]"))
story.append(eq("         =  motor.MaxPowerW / V_nominal                 [fallback 1: P = VI]"))
story.append(eq("         =  F_motor / 10                               [fallback 2: ~1 A per 10 gf]"))
story.append(sp(4))
story.append(eq("r        =  clamp( F_motor / motor.MaxThrust,  0.05,  1.0 )"))
story.append(eq("I_hover  =  I_peak × r^1.5                             [momentum theory: I ∝ T^1.5]"))
story.append(note(
    "Fallback if motor.MaxThrust = 0:  I_hover = I_peak × 0.35  (legacy flat fraction)."
))
story.append(sp(6))

story.append(h2("4.2  System Current"))
story.append(eq("I_sys_peak  =  I_peak  × N  +  3.0 A     [3.0 A fixed avionics estimate]"))
story.append(eq("I_sys_avg   =  I_hover × N  +  3.0 A"))
story.append(sp(6))

story.append(h2("4.3  Battery Capacity"))
story.append(eq("C_min  =  ( I_sys_avg × t_h × 1000 ) / 0.80         [mAh, 80 % DoD]"))
story.append(eq("C_req  =  C_min × 1.20                               [+20 % margin]"))
story.append(sp(6))

story.append(h2("4.4  C-Rating"))
story.append(eq("C_rate_required  =  ( I_sys_peak / C_req_Ah ) × 1.15  [+15 % headroom]"))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("5  Battery Selection  (Step 5)"))
story.append(body("<b>Hard filters (all must pass):</b>"))
story.append(eq("battery.cells          =  power.CellCount               [exact match]"))
story.append(eq("battery.capacity_mAh  ≥  C_req"))
story.append(eq("battery.C_rating      ≥  C_rate_required"))
story.append(eq("battery.mass_g        ≤  MTOW_battery_estimate × 1.15   [+15 % mass tolerance]"))
story.append(sp(3))
story.append(body(
    "<b>Ranking:</b>  |capacity − C_req| ASC (closest fit),  C_rating DESC,  mass ASC.  Take top 3."
))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("6  ESC &amp; PDB Sizing  (Steps 6–7)"))
story.append(eq("I_ESC_required  =  I_peak_motor × 1.25                  [+25 % thermal margin]"))
story.append(eq("I_PDB_required  =  I_sys_peak   × 1.30                  [+30 % wiring margin]"))
story.append(sp(4))
story.append(body(
    "ESC filter: continuous rating ≥ I_ESC_required, max voltage ≥ V_nominal, not all-in-one.  "
    "PDB filter: continuous rating ≥ I_PDB_required, max voltage ≥ V_nominal, ESC pad count ≥ N."
))
story.append(sp(8))

story.append(hr())

# =============================================================================
story.append(h1("7  Diagnosis — Current Failure at Default UI Values"))
story.append(body(
    "With defaults of 0.4 h endurance, 4-motor frame, 0 g payload, LiPo chemistry:"
))
story.append(sp(3))
story.append(make_table(
    [["Parameter", "Value"],
     ["m_struct",                     "345 g  (250 airframe + 95 electronics)"],
     ["b_0  (seed @ 24 min bracket)", "121 g  (345 × 0.35)"],
     ["Divergence threshold",          "182 g  (121 × 1.5)"],
     ["b after pass 1",               "236 g  — exceeds 182 g"],
     ["Consecutive increases at pass 3", "3  → guard fires"],
     ["Guard verdict",                "THROW — 'MTOW diverging'"]],
    [6*cm, 9.5*cm]
))
story.append(sp(4))
story.append(body(
    "<b>Root cause:</b> The seed fraction (0.35 × m_struct = 121 g) severely underestimates "
    "the battery needed for a 0.4 h quad, which actually converges to ~355 g. "
    "The divergence guard interprets the expected growth as divergence and throws prematurely. "
    "The iterator is correct — the guard threshold (1.5 × seed) is too tight when "
    "the seed is far from the true fixed point."
))
story.append(sp(8))

story.append(hr())
story.append(Paragraph(
    "Generated automatically from ComponentSelectionEngine.vb — Teknofest Drone Designer 2025",
    style("FOOT","Normal", fontSize=7.5, textColor=colors.HexColor("#888888"), alignment=TA_CENTER)
))

doc.build(story)
print(f"PDF written to: {OUT}")
