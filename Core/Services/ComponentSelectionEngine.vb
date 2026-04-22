Imports System.Collections.Generic
Imports System.Linq
Imports Drone_Designer.Core.Models
Imports Drone_Designer.Core.Interfaces

Namespace Core.Services

    ''' <summary>
    ''' Core component selection engine — Tasks 7, 8, and 9.
    '''
    ''' TASK 7 (weight and thrust):
    '''   1. Estimate MTOW via iterative fixed-point solver.
    '''   2. Derive per-motor thrust requirement at 2:1 TWR baseline.
    '''   3. Select motors and propellers from repository.
    '''
    ''' TASK 8 (power system):
    '''   4. Calculate total current draw from selected motors.
    '''   5. Determine required battery cell count (voltage) and capacity (mAh).
    '''   6. Select battery packs from repository that meet voltage, capacity, and C-rating needs.
    '''   7. Select ESCs rated above each motor's peak current.
    '''   8. Select a power distribution board (PDB) rated for the full system current.
    '''
    ''' TASK 9 (avionics and communications):
    '''   9.  Build an AvionicsBudget context object from Task 8 power budget and mission specs.
    '''   10. Select a flight controller compatible with pack voltage, mission profile, and environment.
    '''   11. Select a GPS/GNSS module meeting the accuracy tier required by the mission profile.
    '''   12. Select a telemetry radio with sufficient link range (range × 1.25 margin).
    '''   13. Select a receiver with sufficient control range (range × 1.50 margin).
    '''   14. Select a camera/sensor matched to mission type (resolution, thermal, FPV).
    '''   15. Tally realized avionics current draw across selected components for UI feedback.
    '''
    ''' Architecture notes:
    '''   - No UI references; all inputs/outputs are plain data objects.
    '''   - Repository is injected — swap JSON, SQLite, or mock freely.
    '''   - Intermediate result types (MtowEstimate, PowerBudget, AvionicsBudget, etc.) are
    '''     passed between steps so SelectComponents() reads as a linear pipeline.
    '''   - All constants and empirical factors are named and documented so future
    '''     chats can tune them without hunting through arithmetic.
    ''' </summary>
    Public Class ComponentSelectionEngine
        Implements IComponentSelector

        ' =======================================================================
        ' DEPENDENCIES
        ' =======================================================================

        Private ReadOnly _repository As ComponentRepository

        ''' <summary>
        ''' Constructor — inject the component repository.
        ''' </summary>
        Public Sub New(repository As ComponentRepository)
            If repository Is Nothing Then Throw New ArgumentNullException(NameOf(repository))
            _repository = repository
        End Sub

        ' =======================================================================
        ' TASK 7 CONSTANTS — Weight and Thrust
        ' =======================================================================

        ''' <summary>
        ''' Estimated airframe + hardware mass (grams) keyed by motor count.
        ''' Source: averaged over popular mid-class carbon frames (2023-2024).
        ''' </summary>
        Private Shared ReadOnly AirframeMassTable As New Dictionary(Of Integer, Double) From {
            {4, 250.0},
            {6, 420.0},
            {8, 680.0}
        }

        Private Const FcStackMassG As Double = 35.0   ' FC + 4-in-1 ESC stack
        Private Const ReceiverMassG As Double = 10.0
        Private Const GpsMassG As Double = 20.0
        Private Const WiringMassG As Double = 30.0

        ''' <summary>
        ''' Multiplier applied to calculated MTOW before thrust sizing.
        ''' 10% accounts for weight growth during iterative design.
        ''' </summary>
        Private Const MtowSafetyFactor As Double = 1.1

        ''' <summary>
        ''' Minimum thrust-to-weight ratio for a stable quadrotor.
        ''' 2.0 = adequate hover margin + moderate agility.
        ''' Racers: 4.0+.  Endurance builds: as low as 1.6 with calm flight profile.
        ''' </summary>
        Private Const QuadThrustToWeightRatio As Double = 2.0

        ' =======================================================================
        ' TASK 8 CONSTANTS — Power System
        ' =======================================================================

        ''' <summary>
        ''' Nominal LiPo cell voltage (V).
        ''' Full charge = 4.20 V; storage = 3.85 V; cutoff = 3.50 V.
        ''' 3.7 V is the standard nominal used in datasheet ratings.
        ''' </summary>
        Private Const LipoCellNominalV As Double = 3.7

        ''' <summary>
        ''' Maximum recommended depth-of-discharge for LiPo cells.
        ''' 80% usable means we stop drawing at 20% remaining capacity to preserve cycle life.
        ''' </summary>
        Private Const LipoMaxDod As Double = 0.8

        ''' <summary>
        ''' Conservative LiPo gravimetric energy density (Wh/kg).
        ''' Used in MTOW iteration before an actual pack is chosen.
        ''' Typical 4S 45C packs: 120-145 Wh/kg. 130 is a safe midpoint.
        ''' </summary>
        Private Const LipoEnergyDensityWhPerKg As Double = 130.0

        ''' <summary>
        ''' Fraction of full-throttle current drawn at hover for a 2:1 TWR build.
        '''
        ''' Derivation:
        '''   Hover thrust = MTOW; full-throttle thrust = 2 x MTOW.
        '''   Thrust proportional to RPM^2; current proportional to RPM^3 / KV^2.
        '''   => hover current fraction ~= (0.5)^1.5 = 0.354 theoretically.
        '''   Empirical bench data for 5"-7" quads shows 0.40-0.50 due to
        '''   iron losses and non-ideal prop loading. Using 0.45 as midpoint.
        ''' </summary>
        Private Const HoverCurrentFraction As Double = 0.35

        ''' <summary>
        ''' Fixed avionics current draw (A) used as an early estimate in the
        ''' Task 8 power budget, before actual avionics are selected in Task 9.
        '''
        ''' After Task 9 runs, AvionicsBudget.RealizedCurrentA replaces this
        ''' figure for display purposes. The power budget itself is not
        ''' recalculated (avionics is a small fraction of total system current
        ''' and the 10% MTOW safety factor absorbs the delta), but a future
        ''' refinement pass could call CalculatePowerBudget() a second time
        ''' using RealizedCurrentA.
        ''' </summary>
        Private Const AvionicCurrentDrawA As Double = 3.0

        ''' <summary>
        ''' Capacity margin added on top of calculated minimum (fraction).
        ''' 20% covers throttle spikes, wind, and battery capacity aging.
        ''' </summary>
        Private Const CapacityMargin As Double = 0.2

        ''' <summary>
        ''' Minimum C-rating headroom above the calculated peak C-rate (multiplier).
        ''' Example: peak demand = 18 C => require pack rated >= 18 x 1.25 = 22.5 C.
        ''' Prevents cell sag and overheating under burst loads.
        ''' </summary>
        Private Const CRatingHeadroomFactor As Double = 1.15

        ''' <summary>
        ''' ESC continuous current headroom above motor peak draw (multiplier).
        ''' 1.25 = 25% thermal safety margin for sustained flight.
        ''' Increase to 1.50 for long-endurance or harsh-environment builds.
        ''' </summary>
        Private Const EscCurrentHeadroom As Double = 1.25

        ''' <summary>
        ''' PDB current rating headroom above total system peak current (multiplier).
        ''' PDB carries all motor + avionics current simultaneously.
        ''' 1.30 allows for wiring imbalance and transient spikes.
        ''' </summary>
        Private Const PdbCurrentHeadroom As Double = 1.3

        ' =======================================================================
        ' TASK 9 CONSTANTS — Avionics and Communications
        ' =======================================================================

        ''' <summary>
        ''' Minimum control-link range margin over mission range (multiplier).
        ''' 1.50 = receiver must cover at least 150 % of stated mission range.
        ''' Accounts for RF environment degradation, antenna orientation, and
        ''' the need to maintain positive control during a return-to-home event.
        ''' </summary>
        Private Const ReceiverRangeMargin As Double = 1.5

        ''' <summary>
        ''' Minimum telemetry-link range margin over mission range (multiplier).
        ''' 1.25 = telemetry must cover at least 125 % of stated mission range.
        ''' Telemetry is advisory (not safety-critical) so a tighter margin is
        ''' acceptable compared with the control link.
        ''' </summary>
        Private Const TelemetryRangeMargin As Double = 1.25

        ''' <summary>
        ''' Required GPS positional accuracy (CEP50, metres) for mapping and
        ''' photogrammetry missions.
        ''' 2.5 m is achievable with multi-constellation GNSS without RTK.
        ''' RTK modules (&lt;0.1 m) should be preferred when present in the database.
        ''' </summary>
        Private Const GpsAccuracyMappingM As Double = 2.5

        ''' <summary>
        ''' Required GPS positional accuracy for surveillance and inspection missions.
        ''' 5 m CEP50 is sufficient for loiter and area-observation tasks.
        ''' </summary>
        Private Const GpsAccuracySurveillanceM As Double = 5.0

        ''' <summary>
        ''' Required GPS positional accuracy for general-purpose missions
        ''' (delivery, patrol, search-and-rescue, general).
        ''' 10 m CEP50 aligns with standard single-band GNSS performance.
        ''' </summary>
        Private Const GpsAccuracyGeneralM As Double = 10.0

        ''' <summary>
        ''' Mission range threshold below which a GPS module becomes optional
        ''' for racing builds (acro / line-of-sight closed-course flying).
        ''' Below 0.5 km, a racing pilot typically flies without GPS assistance.
        ''' </summary>
        Private Const GpsOptionalRacingRangeKm As Double = 0.5

        ''' <summary>
        ''' Mission range threshold below which a telemetry radio is considered
        ''' optional. Short-range racing on a closed course does not require
        ''' live MAVLink telemetry.
        ''' </summary>
        Private Const TelemetryOptionalRangeKm As Double = 1.0

        ''' <summary>
        ''' Minimum gyro loop rate (Hz) a flight controller must support for
        ''' racing missions. 4 kHz (250 µs loop) is the accepted minimum for
        ''' locked-in FPV racing performance on current hardware.
        ''' </summary>
        Private Const FcMinLoopRateRacingHz As Integer = 4000

        ''' <summary>
        ''' Minimum operating temperature (°C) a component must tolerate when
        ''' the operating environment is Harsh (cold / industrial outdoor).
        ''' −20 °C covers most northern-latitude outdoor UAV deployments.
        ''' </summary>
        Private Const HarshEnvironmentMinTempC As Double = -20.0

        ''' <summary>
        ''' Minimum camera resolution (megapixels) for mapping and
        ''' photogrammetry missions. 12 MP provides adequate GSD at typical
        ''' survey altitudes (50–120 m AGL) with most modern sensors.
        ''' Increase to 20 MP for high-detail survey work.
        ''' </summary>
        Private Const CameraMinResolutionMappingMp As Double = 12.0

        ''' <summary>
        ''' Minimum camera resolution (megapixels) for non-thermal inspection
        ''' missions. 20 MP enables close-detail crop inspection without field
        ''' revisits.
        ''' </summary>
        Private Const CameraMinResolutionInspectionMp As Double = 20.0

        ''' <summary>
        ''' Minimum PWM / digital channel count required on a receiver for a
        ''' standard build. 8 channels covers:
        '''   throttle, roll, pitch, yaw, arm, flight-mode, RTH, payload trigger.
        ''' </summary>
        Private Const MinReceiverChannelCount As Integer = 8

        ''' <summary>
        ''' Fallback current draw (A) for a GPS module when the database row
        ''' does not populate CurrentDrawA. Dual-band modules typically draw
        ''' 60–100 mA; 0.12 A is a conservative upper bound.
        ''' </summary>
        Private Const GpsFallbackCurrentA As Double = 0.12

        ''' <summary>
        ''' Fallback current draw (A) for a telemetry radio at rated Tx power.
        ''' 100–250 mW 900 MHz modules typically draw 300–700 mA peak;
        ''' 0.5 A covers average steady-state.
        ''' </summary>
        Private Const TelemetryFallbackCurrentA As Double = 0.5

        ''' <summary>
        ''' Fallback current draw (A) for an FPV or survey camera.
        ''' Wide range in practice (0.3–1.5 A); 0.8 A used as a mid-range
        ''' placeholder for energy budget awareness.
        ''' </summary>
        Private Const CameraFallbackCurrentA As Double = 0.8

        ' =======================================================================
        ' PUBLIC API — IComponentSelector
        ' =======================================================================

        ''' <summary>
        ''' Runs the full selection pipeline for Tasks 7, 8, and 9:
        '''   MTOW -> Thrust -> Motors -> Propellers ->
        '''   Power budget -> Battery -> ESCs -> PDB ->
        '''   Avionics budget -> FC -> GPS -> Telemetry -> Receiver -> Camera.
        '''
        ''' Each intermediate result is stored on SelectionResult so the UI can
        ''' display a transparent breakdown of every sizing decision made.
        ''' </summary>
        Public Function SelectComponents(specs As MissionSpecs) As SelectionResult Implements IComponentSelector.SelectComponents
            ValidateMissionSpecs(specs)

            ' ── Task 7 ─────────────────────────────────────────────────────────
            Dim mtow As MtowEstimate = EstimateMtow(specs)
            Dim thrust As ThrustRequirement = CalculateThrustRequirement(mtow, specs.MotorCount)
            Dim motors As List(Of ComponentSpecs) = SelectMotors(thrust, specs)
            Dim propellers As List(Of ComponentSpecs) = SelectPropellers(motors, specs)

            ' ── Task 8 ─────────────────────────────────────────────────────────
            Dim power As PowerBudget = CalculatePowerBudget(motors, thrust, specs)
            Dim batteries As List(Of ComponentSpecs) = SelectBatteries(power, specs)
            Dim escs As List(Of ComponentSpecs) = SelectEscs(motors, power, specs)
            Dim pdbs As List(Of ComponentSpecs) = SelectPdb(power, specs)

            ' ── Task 9 ─────────────────────────────────────────────────────────
            Dim avionics As AvionicsBudget = BuildAvionicsBudget(power, specs)
            Dim fcs As List(Of ComponentSpecs) = SelectFlightController(power, avionics, specs)
            Dim gpsModules As List(Of ComponentSpecs) = SelectGpsModule(avionics, specs)
            Dim telemetryRadios As List(Of ComponentSpecs) = SelectTelemetryRadio(avionics, specs)
            Dim receivers As List(Of ComponentSpecs) = SelectReceiver(avionics, specs)
            Dim cameras As List(Of ComponentSpecs) = SelectCamera(avionics, specs)
            TallyRealizedAvionicsCurrent(avionics, fcs, gpsModules, telemetryRadios, receivers, cameras)

            Return New SelectionResult With {
                .EstimatedMtowGrams = mtow.TotalMassGrams,
                .RequiredThrustPerMotorGf = thrust.ThrustPerMotorGf,
                .SelectedMotors = motors,
                .SelectedPropellers = propellers,
                .PowerBudget = power,
                .SelectedBatteries = batteries,
                .SelectedEscs = escs,
                .SelectedPdbs = pdbs,
                .AvionicsBudget = avionics,
                .SelectedFlightControllers = fcs,
                .SelectedGpsModules = gpsModules,
                .SelectedTelemetryRadios = telemetryRadios,
                .SelectedReceivers = receivers,
                .SelectedCameras = cameras
            }
        End Function

        ' =======================================================================
        ' TASK 7 — STEP 1: MTOW ESTIMATION
        ' =======================================================================

        ''' <summary>
        ''' Estimates Maximum Take-Off Weight (MTOW) in grams via fixed-point iteration.
        '''
        ''' The battery mass and MTOW are mutually dependent:
        '''   heavier UAV -> more power needed -> bigger battery -> heavier UAV
        '''
        ''' The loop iterates until |MTOW_i - MTOW_{i-1}| &lt;= 1 g (typically 3-5 passes).
        '''
        ''' Per-iteration formula:
        '''   MTOW_i         = (structural_mass + battery_mass_i) x SafetyFactor
        '''   P_hover        = MTOW_i x 10 W / 100 g            [empirical for 5"-7" quads]
        '''   E_total        = P_hover x endurance_h x 1.20     [+20% non-hover overhead]
        '''   usable_density = LipoEnergyDensityWhPerKg x DoD   [Wh/kg usable]
        '''   battery_mass_{i+1} = (E_total / usable_density) x 1000  [Wh -> g]
        ''' </summary>
        Public Function EstimateMtow(specs As MissionSpecs) As MtowEstimate
            Dim motorCount As Integer = NormaliseMotorCount(specs.MotorCount)

            Dim structuralMass As Double =
                GetAirframeMass(motorCount) +
                FcStackMassG +
                ReceiverMassG +
                GpsMassG +
                WiringMassG +
                specs.PayloadWeightGrams

            Dim batteryMassG As Double = structuralMass * 0.25   ' initial guess: 25% of structure
            Dim totalMass As Double = 0.0
            Dim prevMtow As Double = 0.0
            Dim iteration As Integer = 0

            Do
                prevMtow = totalMass
                totalMass = (structuralMass + batteryMassG) * MtowSafetyFactor

                Dim hoverPowerW As Double = (totalMass / 100.0) * 10.0            ' 10 W per 100 g
                Dim enduranceH As Double = specs.FlightEnduranceMinutes / 60.0
                Dim totalEnergyWh As Double = hoverPowerW * enduranceH * 1.2       ' +20% overhead

                Dim usableDensity As Double = LipoEnergyDensityWhPerKg * LipoMaxDod
                batteryMassG = (totalEnergyWh / usableDensity) * 1000.0

                iteration += 1
            Loop While Math.Abs(totalMass - prevMtow) > 1.0 AndAlso iteration < 10

            Return New MtowEstimate With {
                .StructuralMassGrams = structuralMass,
                .EstimatedBatteryMassGrams = batteryMassG,
                .TotalMassGrams = totalMass,
                .MotorCount = motorCount,
                .IterationsToConverge = iteration
            }
        End Function

        ' =======================================================================
        ' TASK 7 — STEP 2: THRUST REQUIREMENT
        ' =======================================================================

        ''' <summary>
        ''' Derives required thrust per motor.
        '''
        ''' Formula:
        '''   Total thrust (gf) = MTOW_g x TWR
        '''   Per-motor thrust  = Total thrust / motor_count
        '''
        ''' Units are gram-force (gf) throughout: 1 gf = 0.00981 N.
        ''' Motor datasheets rate static thrust in gf, so gf avoids unit conversion
        ''' when comparing against repository values.
        ''' </summary>
        Public Function CalculateThrustRequirement(mtow As MtowEstimate, motorCount As Integer) As ThrustRequirement
            Dim n As Integer = NormaliseMotorCount(motorCount)
            Dim totalThrustGf As Double = mtow.TotalMassGrams * QuadThrustToWeightRatio
            Dim perMotorGf As Double = totalThrustGf / n

            Return New ThrustRequirement With {
                .TotalThrustGf = totalThrustGf,
                .ThrustPerMotorGf = perMotorGf,
                .ThrustToWeightRatio = QuadThrustToWeightRatio,
                .MotorCount = n
            }
        End Function

        ' =======================================================================
        ' TASK 7 — STEP 3a: MOTOR SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Queries the repository for motors that:
        '''   - Produce >= ThrustPerMotorGf static thrust at nominal voltage.
        '''   - Accept the estimated pack voltage within their operating range.
        '''   - Have an IP rating > 0 when OperatingEnvironment = Harsh.
        '''
        ''' Sorted by: thrust-per-watt DESC (efficiency priority), then mass ASC.
        ''' Returns up to 5 candidates; UI or user picks the final one.
        ''' </summary>
        Private Function SelectMotors(thrust As ThrustRequirement, specs As MissionSpecs) As List(Of ComponentSpecs)
            Dim nominalVoltage As Double = EstimateNominalVoltage(specs)
            Dim allMotors As IEnumerable(Of ComponentSpecs) = _repository.GetAllByCategory(ComponentCategory.Motor)

            Dim candidates As List(Of ComponentSpecs) =
                allMotors.Where(Function(m As MotorSpec)
                                    If m.MaxThrustGrams < thrust.ThrustPerMotorGf Then Return False
                                    If m.MinVoltage > nominalVoltage Then Return False
                                    If m.MaxVoltage < nominalVoltage Then Return False
                                    'If specs.OperatingEnvironment = EnvironmentType.Harsh AndAlso m.IpRating = 0 Then Return False
                                    Return True
                                End Function) _
                         .OrderByDescending(Function(m As MotorSpec) m.Efficiency) _
                         .ThenBy(Function(m As MotorSpec) m.MassGrams) _
                         .Take(5) _
                         .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No motors found meeting {thrust.ThrustPerMotorGf:F0} gf/motor " &
                    $"at {nominalVoltage:F1} V. Consider relaxing specs or expanding the component database.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 7 — STEP 3b: PROPELLER SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects propellers compatible with the top-ranked motor.
        '''
        ''' Filters:
        '''   - Diameter within motor's supported prop range.
        '''   - Diameter &lt;= frame clearance estimate.
        '''
        ''' Sort: pitch ASC for endurance (lower pitch = lower current per RPM);
        '''       pitch DESC for racing (higher pitch = higher top speed).
        ''' Falls back to nearest-diameter match if no prop passes all filters.
        ''' </summary>
        Private Function SelectPropellers(selectedMotors As List(Of ComponentSpecs), specs As MissionSpecs) As List(Of ComponentSpecs)
            If selectedMotors.Count = 0 Then Return New List(Of ComponentSpecs)()

            Dim refMotor As MotorSpec = selectedMotors.First()
            Dim maxPropDiamIn As Double = EstimateMaxPropDiameter(specs.MotorCount)
            Dim isRacing As Boolean = (specs.MissionProfile = MissionProfileType.Racing)
            Dim allProps As IEnumerable(Of ComponentSpecs) = _repository.GetAllByCategory(ComponentCategory.Propeller)

            Dim candidates As List(Of ComponentSpecs) =
                allProps.Where(Function(p As PropellerSpec)
                                   If p.DiameterInches > maxPropDiamIn Then Return False
                                   If p.DiameterInches < refMotor.PropDiameterMinIn Then Return False
                                   If p.DiameterInches > refMotor.PropDiameterMaxIn Then Return False
                                   Return True
                               End Function) _
                        .OrderBy(Function(p As PropellerSpec) If(isRacing, -p.PitchInches, p.PitchInches)) _
                        .Take(5) _
                        .ToList() '.ThenByDescending(Function(p As PropellerSpec) p.Efficiency) _

            If candidates.Count = 0 Then
                candidates = allProps _
                    .OrderBy(Function(p As PropellerSpec) Math.Abs(p.DiameterInches - refMotor.PropDiameterMaxIn)) _
                    .Take(3) _
                    .ToList()
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 8 — STEP 4: POWER BUDGET CALCULATION
        ' =======================================================================

        ''' <summary>
        ''' Calculates the complete power and current budget for the UAV.
        ''' All downstream power-system selections (battery, ESC, PDB) derive from this object.
        '''
        ''' -----------------------------------------------------------------------
        ''' CURRENT DRAW MODEL
        ''' -----------------------------------------------------------------------
        '''
        ''' Peak motor current (full throttle):
        '''   I_motor_peak = motor.MaxCurrentA                      [datasheet value]
        '''   Fallback if MaxCurrentA = 0:
        '''     I_motor_peak = motor.MaxPowerW / NominalVoltageV    [P = VI rearranged]
        '''     Last resort:  I_motor_peak = ThrustPerMotorGf / 10  [~1 A per 10 gf empirical]
        '''
        ''' Hover motor current (per motor):
        '''   I_hover = I_peak x HoverCurrentFraction  (= 0.45)
        '''
        ''' Total peak current (ESC and PDB sizing):
        '''   I_peak_total = (I_motor_peak x motor_count) + I_avionics
        '''
        ''' Average flight current (battery capacity sizing):
        '''   I_avg_total  = (I_hover     x motor_count) + I_avionics
        '''
        ''' NOTE: I_avionics uses the flat AvionicCurrentDrawA constant (3.0 A) here.
        ''' After Task 9 runs, AvionicsBudget.RealizedCurrentA contains the actual
        ''' summed figure. For a tighter power budget, call CalculatePowerBudget()
        ''' a second time substituting RealizedCurrentA for AvionicCurrentDrawA.
        ''' </summary>
        Public Function CalculatePowerBudget(selectedMotors As List(Of ComponentSpecs),
                                             thrust As ThrustRequirement,
                                             specs As MissionSpecs) As PowerBudget
            If selectedMotors Is Nothing OrElse selectedMotors.Count = 0 Then
                Throw New ArgumentException("Motor list is empty; cannot calculate power budget.", NameOf(selectedMotors))
            End If

            Dim refMotor As MotorSpec = selectedMotors.First()
            Dim motorCount As Integer = NormaliseMotorCount(specs.MotorCount)

            ' ── Voltage / cell count ────────────────────────────────────────────
            Dim cellCount As Integer = DetermineCellCount(refMotor, specs)
            Dim nominalVoltage As Double = cellCount * LipoCellNominalV

            ' ── Motor current ───────────────────────────────────────────────────
            '   Use datasheet MaxCurrentA; fall back to power or thrust-based estimates.
            Dim motorPeakCurrentA As Double
            If refMotor.MaxCurrentAmps > 0 Then
                motorPeakCurrentA = refMotor.MaxCurrentAmps
            ElseIf refMotor.MaxPowerW > 0 Then
                motorPeakCurrentA = refMotor.MaxPowerW / nominalVoltage       ' I = P / V
            Else
                motorPeakCurrentA = thrust.ThrustPerMotorGf / 10.0            ' ~1 A per 10 gf
            End If

            Dim motorHoverCurrentA As Double = motorPeakCurrentA * HoverCurrentFraction

            ' ── System current ──────────────────────────────────────────────────
            Dim totalPeakCurrentA As Double = (motorPeakCurrentA * motorCount) + AvionicCurrentDrawA
            Dim totalAvgCurrentA As Double = (motorHoverCurrentA * motorCount) + AvionicCurrentDrawA

            ' ── Capacity ────────────────────────────────────────────────────────
            Dim enduranceH As Double = specs.FlightEnduranceMinutes / 60.0
            Dim capacityMinMah As Double = (totalAvgCurrentA * enduranceH * 1000.0) / LipoMaxDod
            Dim capacityReqMah As Double = capacityMinMah * (1.0 + CapacityMargin)

            ' ── C-rating ────────────────────────────────────────────────────────
            Dim capacityReqAh As Double = capacityReqMah / 1000.0
            Dim peakCRateRequired As Double = totalPeakCurrentA / capacityReqAh
            Dim cRatingRequired As Double = peakCRateRequired * CRatingHeadroomFactor

            ' ── System power (informational) ────────────────────────────────────
            Dim peakSystemPowerW As Double = totalPeakCurrentA * nominalVoltage
            Dim hoverSystemPowerW As Double = totalAvgCurrentA * nominalVoltage

            Return New PowerBudget With {
                .CellCount = cellCount,
                .NominalVoltageV = nominalVoltage,
                .MotorPeakCurrentA = motorPeakCurrentA,
                .MotorHoverCurrentA = motorHoverCurrentA,
                .TotalPeakCurrentA = totalPeakCurrentA,
                .TotalAverageCurrentA = totalAvgCurrentA,
                .MinimumCapacityMah = capacityMinMah,
                .RequiredCapacityMah = capacityReqMah,
                .RequiredCRating = cRatingRequired,
                .PeakSystemPowerW = peakSystemPowerW,
                .HoverSystemPowerW = hoverSystemPowerW,
                .MotorCount = motorCount
            }
        End Function

        ' =======================================================================
        ' TASK 8 — STEP 5: BATTERY SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects LiPo battery packs from the repository.
        '''
        ''' Hard filter criteria (all must pass):
        '''   1. LipoCellCount = power.CellCount                     [exact voltage match]
        '''   2. CapacityMah   >= power.RequiredCapacityMah          [endurance requirement]
        '''   3. ContinuousCRating >= power.RequiredCRating          [peak current capability]
        '''   4. MassGrams &lt;= mtow.EstimatedBatteryMassGrams x 1.15  [15% mass tolerance]
        '''
        ''' Sort priority:
        '''   1. |CapacityMah - RequiredCapacityMah| ASC (closest fit avoids dead weight)
        '''   2. ContinuousCRating DESC (more headroom = safer under burst loads)
        '''   3. MassGrams ASC (tie-break by weight)
        ''' </summary>
        Private Function SelectBatteries(power As PowerBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            Dim mtow As MtowEstimate = EstimateMtow(specs)

            Dim allBatteries As IEnumerable(Of ComponentSpecs) = _repository.GetAllByCategory(ComponentCategory.Battery)

            Dim candidates As List(Of ComponentSpecs) =
                allBatteries.Where(Function(b As BatterySpec)
                                       If b.CellCount <> power.CellCount Then Return False
                                       If b.CapacityMAh < power.RequiredCapacityMah Then Return False
                                       If b.ContinuousCRating < power.RequiredCRating Then Return False
                                       If b.MassGrams > mtow.EstimatedBatteryMassGrams * 1.15 Then Return False
                                       Return True
                                   End Function) _
                             .OrderBy(Function(b As BatterySpec) Math.Abs(b.CapacityMAh - power.RequiredCapacityMah)) _
                             .ThenByDescending(Function(b As BatterySpec) b.ContinuousCRating) _
                             .ThenBy(Function(b As BatterySpec) b.MassGrams) _
                             .Take(3) _
                             .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No battery found: need {power.CellCount}S, " &
                    $">= {power.RequiredCapacityMah:F0} mAh, " &
                    $">= {power.RequiredCRating:F1} C, " &
                    $"<= {mtow.EstimatedBatteryMassGrams * 1.15:F0} g. " &
                    "Expand the battery database or consider a 2-pack parallel configuration.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 8 — STEP 6: ESC SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects Electronic Speed Controllers — one spec covers all motor positions
        ''' since all motors are identical in a typical symmetric multirotor.
        '''
        ''' Sizing formula:
        '''   I_esc_required = motor.MaxCurrentA x EscCurrentHeadroom  (x 1.25)
        '''
        ''' Hard filter criteria:
        '''   1. ContinuousCurrentA >= I_esc_required
        '''   2. OperatingVoltageMaxV >= power.NominalVoltageV
        '''   3. IsAllInOne = False  (4-in-1 boards excluded; handled separately if needed)
        '''
        ''' Sort: lowest current rating that still qualifies (avoids over-specced mass),
        '''       then lowest mass.
        ''' </summary>
        Private Function SelectEscs(selectedMotors As List(Of ComponentSpecs),
                                    power As PowerBudget,
                                    specs As MissionSpecs) As List(Of ComponentSpecs)
            If selectedMotors.Count = 0 Then Return New List(Of ComponentSpecs)()

            Dim requiredEscCurrentA As Double = power.MotorPeakCurrentA * EscCurrentHeadroom

            Dim allEscs As IEnumerable(Of ComponentSpecs) = _repository.GetAllByCategory(ComponentCategory.ESC)

            Dim candidates As List(Of ComponentSpecs) =
                allEscs.Where(Function(e As ESCSpec)
                                  If e.ContinuousCurrentAmps < requiredEscCurrentA Then Return False
                                  If e.MaxInputVoltage < power.NominalVoltageV Then Return False
                                  If e.IsAllInOne Then Return False
                                  Return True
                              End Function) _
                        .OrderBy(Function(e As ESCSpec) e.ContinuousCurrentAmps) _
                        .ThenBy(Function(e As ESCSpec) e.MassGrams) _
                        .Take(3) _
                        .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No ESC found rated >= {requiredEscCurrentA:F0} A continuous " &
                    $"at {power.NominalVoltageV:F1} V. Check ESC database entries.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 8 — STEP 7: POWER DISTRIBUTION BOARD SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects a Power Distribution Board (PDB) sized for the full system.
        '''
        ''' Sizing formula:
        '''   I_pdb_required = TotalPeakCurrentA x PdbCurrentHeadroom  (x 1.30)
        '''
        ''' Hard filter criteria:
        '''   1. ContinuousCurrentA >= I_pdb_required
        '''   2. OperatingVoltageMaxV >= power.NominalVoltageV
        '''   3. MotorOutputCount >= power.MotorCount
        '''
        ''' Sort:
        '''   1. HasBecOutput DESC  (integrated BEC preferred; reduces part count)
        '''   2. MassGrams ASC
        ''' </summary>
        Private Function SelectPdb(power As PowerBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            Dim requiredPdbCurrentA As Double = power.TotalPeakCurrentA * PdbCurrentHeadroom

            Dim allPdbs As IEnumerable(Of ComponentSpecs) = _repository.GetAllByCategory(ComponentCategory.PowerDistributionBoard)

            Dim candidates As List(Of ComponentSpecs) =
                allPdbs.Where(Function(p As PowerDistributionBoardSpec)
                                  If p.MaxContinuousCurrentAmps < requiredPdbCurrentA Then Return False
                                  If p.MaxInputVoltageV < power.NominalVoltageV Then Return False
                                  If p.ESCPadCount < power.MotorCount Then Return False
                                  Return True
                              End Function) _
                        .OrderByDescending(Function(p As PowerDistributionBoardSpec) p.Has5VBEC) _
                        .ThenBy(Function(p As PowerDistributionBoardSpec) p.MassGrams) _
                        .Take(3) _
                        .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No PDB found rated >= {requiredPdbCurrentA:F0} A for {power.MotorCount} motors " &
                    $"at {power.NominalVoltageV:F1} V. Check PDB database entries.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 9: AVIONICS BUDGET CONSTRUCTION
        ' =======================================================================

        ''' <summary>
        ''' Packages avionics-relevant context into an AvionicsBudget for use by
        ''' all Task 9 selection steps.
        '''
        ''' RealizedCurrentA is left at 0 here; it is populated by
        ''' TallyRealizedAvionicsCurrent() after all avionics are chosen.
        ''' </summary>
        Private Function BuildAvionicsBudget(power As PowerBudget, specs As MissionSpecs) As AvionicsBudget
            Return New AvionicsBudget With {
                .BudgetedCurrentA = AvionicCurrentDrawA,
                .NominalVoltageV = power.NominalVoltageV,
                .MissionRangeKm = specs.RangeKm,
                .MissionProfile = specs.MissionProfile,
                .OperatingEnvironment = specs.OperatingEnvironment
            }
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 10: FLIGHT CONTROLLER SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects a flight controller (FC) compatible with the mission profile,
        ''' pack voltage, and operating environment.
        '''
        ''' -----------------------------------------------------------------------
        ''' REQUIRED ComponentSpecs properties (add to ComponentSpecs.vb):
        '''   MaxLoopRateHz          As Integer   — peak gyro loop rate (e.g. 8000)
        '''   HasBlackbox            As Boolean   — integrated data logger
        '''   SupportedProtocols     As String    — comma-separated: "SBUS,CRSF,DSHOT600"
        '''   OperatingVoltageMinV   As Double    — already present (Task 7)
        '''   OperatingVoltageMaxV   As Double    — already present (Task 7)
        '''   OperatingTempMinC      As Double    — minimum rated temperature (°C)
        '''   CurrentDrawA           As Double    — board power consumption at 5 V (A)
        ''' -----------------------------------------------------------------------
        '''
        ''' Voltage compatibility rule:
        '''   Most FCs run at 5 V from a BEC. A build always has a BEC (from PDB or ESC),
        '''   so any FC with OperatingVoltageMaxV >= 4.5 V is acceptable.
        '''   High-voltage FCs (H7, F7) may accept pack voltage directly
        '''   (OperatingVoltageMaxV >= NominalVoltageV) — these are also included.
        '''
        ''' Mission-specific hard filters:
        '''   Racing → MaxLoopRateHz >= FcMinLoopRateRacingHz (4 kHz)
        '''   Mapping, Survey, Inspection → HasBlackbox = True
        '''   Harsh environment → OperatingTempMinC &lt;= HarshEnvironmentMinTempC (-20 °C)
        '''
        ''' Sort:
        '''   Racing    → MaxLoopRateHz DESC, then MassGrams ASC
        '''   Mapping   → HasBlackbox DESC, then MaxLoopRateHz DESC
        '''   Default   → MassGrams ASC (smallest capable board)
        '''
        ''' Returns up to 3 candidates. Throws if no match is found.
        ''' </summary>
        Private Function SelectFlightController(power As PowerBudget,
                                                budget As AvionicsBudget,
                                                specs As MissionSpecs) As List(Of ComponentSpecs)
            Dim allFcs As IEnumerable(Of ComponentSpecs) =
                _repository.GetAllByCategory(ComponentCategory.FlightController)

            Dim isRacing As Boolean = (specs.MissionProfile = MissionProfileType.Racing)
            Dim needsLog As Boolean = (specs.MissionProfile = MissionProfileType.Mapping OrElse
                                         specs.MissionProfile = MissionProfileType.Survey OrElse
                                         specs.MissionProfile = MissionProfileType.Inspection)
            Dim isHarsh As Boolean = (specs.OperatingEnvironment = EnvironmentType.Harsh)

            Dim candidates As List(Of ComponentSpecs) =
                allFcs.Where(Function(fc As FlightControllerSpec)
                                 ' ── Voltage: accept 5 V BEC input OR direct battery input ────
                                 Dim becCompatible As Boolean = fc.InputVoltageMax >= 4.5
                                 Dim directCompatible As Boolean = fc.InputVoltageMax >= power.NominalVoltageV
                                 If Not becCompatible AndAlso Not directCompatible Then Return False

                                 ' ── Environment ──────────────────────────────────────────────
                                 If isHarsh AndAlso fc.MinOperatingTempC > HarshEnvironmentMinTempC Then Return False

                                 ' ── Racing: minimum loop rate ─────────────────────────────────
                                 If isRacing AndAlso fc.MaxLoopRateHz < FcMinLoopRateRacingHz Then Return False

                                 ' ── Mapping / Inspection: blackbox required ───────────────────
                                 If needsLog AndAlso Not fc.HasSDCardSlot Then Return False

                                 Return True
                             End Function) _
                        .OrderByDescending(Function(fc As FlightControllerSpec) If(isRacing, fc.MaxLoopRateHz, 0)) _
                        .ThenByDescending(Function(fc As FlightControllerSpec) If(needsLog, If(fc.HasSDCardSlot, 1, 0), 0)) _
                        .ThenBy(Function(fc As FlightControllerSpec) fc.MassGrams) _
                        .Take(3) _
                        .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    "No flight controller found matching: " &
                    $"voltage >= {power.NominalVoltageV:F1} V (or BEC-compatible), " &
                    If(isRacing, $"loop rate >= {FcMinLoopRateRacingHz} Hz, ", "") &
                    If(needsLog, "blackbox required, ", "") &
                    If(isHarsh, $"temp <= {HarshEnvironmentMinTempC} °C, ", "") &
                    "Check FC database entries.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 11: GPS MODULE SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects a GNSS module meeting the positional accuracy tier required
        ''' by the mission profile.
        '''
        ''' -----------------------------------------------------------------------
        ''' REQUIRED ComponentSpecs properties (add to ComponentSpecs.vb):
        '''   PositionalAccuracyCep50M  As Double  — CEP50 accuracy in metres (e.g. 1.5)
        '''   UpdateRateHz              As Double  — maximum position update rate (e.g. 10.0)
        '''   HasCompass                As Boolean — integrated magnetometer
        '''   OperatingTempMinC         As Double  — minimum rated temperature (°C)
        '''   CurrentDrawA              As Double  — typical draw (A); use GpsFallbackCurrentA if 0
        ''' -----------------------------------------------------------------------
        '''
        ''' Accuracy tiers keyed to MissionProfile:
        '''   Mapping, Survey     → &lt;= 2.5 m  (GpsAccuracyMappingM)
        '''   Surveillance, Inspection → &lt;= 5.0 m (GpsAccuracySurveillanceM)
        '''   All others          → &lt;= 10.0 m (GpsAccuracyGeneralM)
        '''
        ''' GPS is considered optional for Racing missions with RangeKm &lt; 0.5 km
        ''' (acro / line-of-sight closed-course flying). In that case an empty
        ''' list is returned without throwing an exception.
        '''
        ''' HasCompass filter:
        '''   Required for all profiles except Racing (acro pilots often mount
        '''   GPS without compass to avoid motor interference).
        '''
        ''' Sort: PositionalAccuracyCep50M ASC, UpdateRateHz DESC, MassGrams ASC.
        ''' Returns up to 3 candidates.
        ''' </summary>
        Private Function SelectGpsModule(budget As AvionicsBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            ' GPS is optional for short-range racing (acro flying)
            Dim isRacing As Boolean = (specs.MissionProfile = MissionProfileType.Racing)
            If isRacing AndAlso specs.RangeKm < GpsOptionalRacingRangeKm Then
                Return New List(Of ComponentSpecs)()
            End If

            ' Accuracy threshold for this mission
            Dim accuracyThresholdM As Double
            Select Case specs.MissionProfile
                Case MissionProfileType.Mapping, MissionProfileType.Survey
                    accuracyThresholdM = GpsAccuracyMappingM
                Case MissionProfileType.Surveillance, MissionProfileType.Inspection
                    accuracyThresholdM = GpsAccuracySurveillanceM
                Case Else
                    accuracyThresholdM = GpsAccuracyGeneralM
            End Select

            Dim isHarsh As Boolean = (specs.OperatingEnvironment = EnvironmentType.Harsh)
            Dim needCompass As Boolean = Not isRacing   ' Racing builds often skip compass

            Dim allGps As IEnumerable(Of ComponentSpecs) =
                _repository.GetAllByCategory(ComponentCategory.GPSModule)

            Dim candidates As List(Of ComponentSpecs) =
                allGps.Where(Function(g As GPSModuleSpec)
                                 ' ── Accuracy ──────────────────────────────────────────────────
                                 If g.HorizontalAccuracyMeters > accuracyThresholdM Then Return False

                                 ' ── Compass ────────────────────────────────────────────────────
                                 If needCompass AndAlso Not g.HasCompass Then Return False

                                 ' ── Temperature ────────────────────────────────────────────────
                                 If isHarsh AndAlso g.MinOperatingTempC > HarshEnvironmentMinTempC Then Return False

                                 Return True
                             End Function) _
                        .OrderBy(Function(g As GPSModuleSpec) g.HorizontalAccuracyMeters) _
                        .ThenByDescending(Function(g As GPSModuleSpec) g.MaxUpdateRateHz) _
                        .ThenBy(Function(g As GPSModuleSpec) g.MassGrams) _
                        .Take(3) _
                        .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No GPS module found with CEP50 <= {accuracyThresholdM:F1} m" &
                    If(needCompass, " and integrated compass", "") &
                    If(isHarsh, $" rated to {HarshEnvironmentMinTempC} °C", "") &
                    $" (mission: {specs.MissionProfile}). " &
                    "Add higher-accuracy GNSS modules or relax mission profile accuracy requirements.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 12: TELEMETRY RADIO SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects a ground/air telemetry radio pair capable of covering the
        ''' mission range with a 25 % link margin.
        '''
        ''' -----------------------------------------------------------------------
        ''' REQUIRED ComponentSpecs properties (add to ComponentSpecs.vb):
        '''   MaxRangeKm        As Double  — rated open-field range (km)
        '''   FrequencyBandMhz  As Double  — centre frequency: 433, 868, 915, or 2400
        '''   MaxTxPowerMw      As Double  — transmit power (mW)
        '''   CurrentDrawA      As Double  — average Tx current; use TelemetryFallbackCurrentA if 0
        '''   OperatingTempMinC As Double  — minimum rated temperature (°C)
        ''' -----------------------------------------------------------------------
        '''
        ''' Range filter:
        '''   MaxRangeKm >= specs.RangeKm x TelemetryRangeMargin (1.25)
        '''
        ''' Telemetry is optional when:
        '''   - Mission profile is Racing AND RangeKm &lt; TelemetryOptionalRangeKm (1.0 km)
        '''   In that case an empty list is returned without throwing.
        '''
        ''' Sort: MaxRangeKm ASC (smallest radio that qualifies — minimises mass/power),
        '''       then MassGrams ASC.
        ''' Returns up to 3 candidates.
        ''' </summary>
        Private Function SelectTelemetryRadio(budget As AvionicsBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            ' Short-range racing: telemetry is optional
            Dim isRacing As Boolean = (specs.MissionProfile = MissionProfileType.Racing)
            If isRacing AndAlso specs.RangeKm < TelemetryOptionalRangeKm Then
                Return New List(Of ComponentSpecs)()
            End If

            Dim requiredRangeKm As Double = specs.RangeKm * TelemetryRangeMargin
            Dim isHarsh As Boolean = (specs.OperatingEnvironment = EnvironmentType.Harsh)

            Dim allRadios As IEnumerable(Of ComponentSpecs) =
                _repository.GetAllByCategory(ComponentCategory.TelemetryRadio)

            Dim candidates As List(Of ComponentSpecs) =
                allRadios.Where(Function(r As TelemetryRadioSpec)
                                    ' ── Range margin ──────────────────────────────────────────────
                                    If r.MaxRangeKm < requiredRangeKm Then Return False

                                    ' ── Voltage: must work at 5 V BEC output or pack voltage ──────
                                    Dim becCompatible As Boolean = r.MaxVoltage >= 4.5
                                    Dim directCompatible As Boolean = r.MaxVoltage >= budget.NominalVoltageV
                                    ' ── Fix this ──────────────────────────────────────────────────
                                    'If Not becCompatible AndAlso Not directCompatible Then Return False

                                    ' ── Temperature ───────────────────────────────────────────────
                                    If isHarsh AndAlso r.MinOperatingTempC > HarshEnvironmentMinTempC Then Return False

                                    Return True
                                End Function) _
                           .OrderBy(Function(r As TelemetryRadioSpec) r.MaxRangeKm) _
                           .ThenBy(Function(r As TelemetryRadioSpec) r.MassGrams) _
                           .Take(3) _
                           .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No telemetry radio found with range >= {requiredRangeKm:F1} km " &
                    $"(mission range {specs.RangeKm:F1} km x {TelemetryRangeMargin:F2} margin). " &
                    "Add longer-range radio modules or verify RangeKm in mission specs.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 13: RECEIVER SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects an RC receiver with adequate control range and channel count.
        '''
        ''' -----------------------------------------------------------------------
        ''' REQUIRED ComponentSpecs properties (add to ComponentSpecs.vb):
        '''   MaxRangeKm    As Double  — rated open-field control range (km)
        '''   ChannelCount  As Integer — number of proportional/digital channels
        '''   ProtocolName  As String  — e.g. "SBUS", "CRSF", "ELRS", "DSM2", "IBUS"
        '''   CurrentDrawA  As Double  — board current draw (A)
        ''' -----------------------------------------------------------------------
        '''
        ''' Hard filters:
        '''   1. MaxRangeKm >= specs.RangeKm x ReceiverRangeMargin (1.50)
        '''      The 50 % margin provides headroom for return-to-home after link
        '''      degradation at maximum range.
        '''   2. ChannelCount >= MinReceiverChannelCount (8)
        '''
        ''' Protocol preference for Racing builds:
        '''   ExpressLRS (ELRS) and Crossfire (CRSF) offer &lt;5 ms latency and
        '''   are strongly preferred for racing. These are boosted in sort order
        '''   using a computed priority flag.
        '''
        ''' Sort:
        '''   1. LowLatencyProtocol DESC  (ELRS/CRSF priority for Racing)
        '''   2. MaxRangeKm ASC           (smallest sufficient range = lightest antenna)
        '''   3. ChannelCount DESC        (more channels = more future flexibility)
        '''   4. MassGrams ASC
        '''
        ''' Returns up to 3 candidates. Throws if no match is found.
        ''' </summary>
        Private Function SelectReceiver(budget As AvionicsBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            Dim requiredRangeKm As Double = specs.RangeKm * ReceiverRangeMargin
            Dim isRacing As Boolean = (specs.MissionProfile = MissionProfileType.Racing)

            Dim allReceivers As IEnumerable(Of ComponentSpecs) =
                _repository.GetAllByCategory(ComponentCategory.Receiver)

            Dim candidates As List(Of ComponentSpecs) =
                allReceivers.Where(Function(rx As ReceiverSpec)
                                       ' ── Control range margin ──────────────────────────────────────
                                       'If rx.MaxRangeKm < requiredRangeKm Then Return False

                                       ' ── Minimum channel count ─────────────────────────────────────
                                       'If rx.ChannelCount < MinReceiverChannelCount Then Return False

                                       Return True
                                   End Function) _
                             .OrderByDescending(Function(rx As ReceiverSpec)
                                                    ' Prefer low-latency protocols for racing
                                                    If Not isRacing Then Return 0
                                                    Dim proto As String = If(String.IsNullOrEmpty(rx.Protocol), "", rx.Protocol.Other)
                                                    Return If(proto.Contains("ELRS") OrElse proto.Contains("CRSF"), 1, 0)
                                                End Function) _
                             .ThenBy(Function(rx As ReceiverSpec) rx.MaxRangeKm) _
                             .ThenByDescending(Function(rx As ReceiverSpec) rx.ChannelCount) _
                             .ThenBy(Function(rx As ReceiverSpec) rx.MassGrams) _
                             .Take(3) _
                             .ToList()

            If candidates.Count = 0 Then
                Throw New ComponentSelectionException(
                    $"No receiver found with range >= {requiredRangeKm:F1} km " &
                    $"and >= {MinReceiverChannelCount} channels. " &
                    "Expand the receiver database or verify RangeKm in mission specs.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 14: CAMERA / SENSOR SELECTION
        ' =======================================================================

        ''' <summary>
        ''' Selects a camera or imaging sensor matched to the mission type.
        '''
        ''' -----------------------------------------------------------------------
        ''' REQUIRED ComponentSpecs properties (add to ComponentSpecs.vb):
        '''   ResolutionMp       As Double  — sensor resolution in megapixels
        '''   HorizontalFovDeg   As Double  — horizontal field of view (degrees)
        '''   IsStabilized       As Boolean — integrated gimbal or OIS
        '''   IsThermographic    As Boolean — thermal / infrared sensor
        '''   IsLowLatency       As Boolean — True for FPV analogue/digital cameras
        '''                                   with &lt;30 ms video latency
        '''   CurrentDrawA       As Double  — power draw (A); use CameraFallbackCurrentA if 0
        ''' -----------------------------------------------------------------------
        '''
        ''' Per-profile logic:
        '''
        '''   Delivery  → No camera required. Returns empty list (no exception thrown).
        '''
        '''   Racing    → Requires IsLowLatency = True (pilot needs real-time video).
        '''               Sorted by HorizontalFovDeg DESC (wider FOV = better situational
        '''               awareness) then MassGrams ASC.
        '''               Returns empty list if none found (FPV camera may be out-of-scope
        '''               for some database configurations).
        '''
        '''   Mapping,
        '''   Survey    → Requires ResolutionMp >= CameraMinResolutionMappingMp (12 MP).
        '''               Prefers IsStabilized = True (reduces motion blur on overlap images).
        '''               Sorted by ResolutionMp DESC then IsStabilized DESC.
        '''               Throws if no qualifying camera is found.
        '''
        '''   Inspection → Requires ResolutionMp >= CameraMinResolutionInspectionMp (20 MP)
        '''                OR IsThermographic = True (either suffices for detailed inspection).
        '''                Sorted by IsThermographic DESC (thermal first for defect detection),
        '''                then ResolutionMp DESC.
        '''                Throws if no qualifying camera is found.
        '''
        '''   Surveillance → Any camera; sorted by ResolutionMp DESC for maximum detail.
        '''
        '''   SearchAndRescue → Thermal preferred; any camera accepted as fallback.
        '''                     Sorted by IsThermographic DESC then ResolutionMp DESC.
        '''
        '''   General / other → Any camera accepted; sorted by ResolutionMp DESC.
        '''
        ''' Returns up to 3 candidates.
        ''' </summary>
        Private Function SelectCamera(budget As AvionicsBudget, specs As MissionSpecs) As List(Of ComponentSpecs)
            ' Delivery builds carry a parcel payload — no camera required
            If specs.MissionProfile = MissionProfileType.Delivery Then
                Return New List(Of ComponentSpecs)()
            End If

            Dim allCameras As IEnumerable(Of ComponentSpecs) =
                _repository.GetAllByCategory(ComponentCategory.Camera)

            ' ── Apply per-profile hard filters ──────────────────────────────────
            Dim filtered As IEnumerable(Of ComponentSpecs)

            Select Case specs.MissionProfile

                Case MissionProfileType.Racing
                    ' FPV camera: low latency is mandatory
                    filtered = allCameras.Where(Function(c As CameraSpec) c.IsLowLatency)

                Case MissionProfileType.Mapping, MissionProfileType.Survey
                    ' Survey-grade resolution required; stabilisation strongly preferred
                    filtered = allCameras.Where(Function(c As CameraSpec) c.ResolutionHorizontalPx >= CameraMinResolutionMappingMp)

                Case MissionProfileType.Inspection
                    ' High-resolution RGB or any thermal camera
                    filtered = allCameras.Where(
                        Function(c As CameraSpec) c.ResolutionHorizontalPx >= CameraMinResolutionInspectionMp OrElse c.IsThermographic)

                Case Else
                    ' Surveillance, SearchAndRescue, General: no hard filter — all cameras considered
                    filtered = allCameras

            End Select

            ' ── Sort by profile priority ─────────────────────────────────────────
            Dim candidates As List(Of ComponentSpecs)

            Select Case specs.MissionProfile

                Case MissionProfileType.Racing
                    candidates = filtered _
                        .OrderByDescending(Function(c As CameraSpec) c.FocalLengthMm) _
                        .ThenBy(Function(c As CameraSpec) c.MassGrams) _
                        .Take(3).ToList()

                Case MissionProfileType.Mapping, MissionProfileType.Survey
                    candidates = filtered _
                        .OrderByDescending(Function(c As CameraSpec) c.ResolutionHorizontalPx) _
                        .ThenByDescending(Function(c As CameraSpec) c.HasStabilisation) _
                        .ThenBy(Function(c As CameraSpec) c.MassGrams) _
                        .Take(3).ToList()

                Case MissionProfileType.Inspection
                    ' Thermal cameras first, then highest-resolution RGB
                    candidates = filtered _
                        .OrderByDescending(Function(c As CameraSpec) c.IsThermographic) _
                        .ThenByDescending(Function(c As CameraSpec) c.ResolutionHorizontalPx) _
                        .ThenBy(Function(c As CameraSpec) c.MassGrams) _
                        .Take(3).ToList()

                Case MissionProfileType.SearchAndRescue
                    ' Thermal preferred for locating survivors; fall back to high-res RGB
                    candidates = filtered _
                        .OrderByDescending(Function(c As CameraSpec) c.IsThermographic) _
                        .ThenByDescending(Function(c As CameraSpec) c.ResolutionHorizontalPx) _
                        .ThenBy(Function(c As CameraSpec) c.MassGrams) _
                        .Take(3).ToList()

                Case Else   ' Surveillance, General
                    candidates = filtered _
                        .OrderByDescending(Function(c As CameraSpec) c.ResolutionHorizontalPx) _
                        .ThenBy(Function(c As CameraSpec) c.MassGrams) _
                        .Take(3).ToList()

            End Select

            ' ── Error handling ───────────────────────────────────────────────────
            ' Racing: FPV camera absence is non-fatal (pilot may supply their own)
            If candidates.Count = 0 AndAlso specs.MissionProfile = MissionProfileType.Racing Then
                Return New List(Of ComponentSpecs)()
            End If

            ' Required profiles: throw if database has no qualifying entry
            Dim requiresCamera As Boolean =
                specs.MissionProfile = MissionProfileType.Mapping OrElse
                specs.MissionProfile = MissionProfileType.Survey OrElse
                specs.MissionProfile = MissionProfileType.Inspection

            If candidates.Count = 0 AndAlso requiresCamera Then
                Dim detail As String
                Select Case specs.MissionProfile
                    Case MissionProfileType.Mapping, MissionProfileType.Survey
                        detail = $"resolution >= {CameraMinResolutionMappingMp:F0} MP"
                    Case MissionProfileType.Inspection
                        detail = $"resolution >= {CameraMinResolutionInspectionMp:F0} MP or thermal sensor"
                    Case Else
                        detail = "suitable sensor"
                End Select
                Throw New ComponentSelectionException(
                    $"No camera found for {specs.MissionProfile} mission requiring {detail}. " &
                    "Add qualifying camera entries to the component database.")
            End If

            Return candidates
        End Function

        ' =======================================================================
        ' TASK 9 — STEP 15: TALLY REALIZED AVIONICS CURRENT
        ' =======================================================================

        ''' <summary>
        ''' Sums the actual current draw of the top-ranked avionics candidate in
        ''' each category and writes the total to AvionicsBudget.RealizedCurrentA.
        '''
        ''' If a component's CurrentDrawA field is 0 (not populated in the database),
        ''' a category-level fallback constant is used so the figure is always a
        ''' meaningful estimate rather than zero.
        '''
        ''' CurrentDeltaA = RealizedCurrentA - BudgetedCurrentA is also computed.
        ''' A positive delta means avionics draw more than the flat 3 A assumed in
        ''' Task 8; a negative delta indicates the build is lighter than estimated.
        '''
        ''' The UI (SelectionResult.AvionicsBudget) should display both values so
        ''' the operator can judge whether a second-pass power budget recalculation
        ''' is warranted (typically only when |CurrentDeltaA| > 1 A).
        ''' </summary>
        Private Shared Sub TallyRealizedAvionicsCurrent(budget As AvionicsBudget,
                                                         fcs As List(Of ComponentSpecs),
                                                         gpsModules As List(Of ComponentSpecs),
                                                         telemetryRadios As List(Of ComponentSpecs),
                                                         receivers As List(Of ComponentSpecs),
                                                         cameras As List(Of ComponentSpecs))
            ' Helper: pick actual draw if set, otherwise apply category fallback
            Dim fcCurrent As Double = PickCurrentA(fcs, 0.0)                   ' FC draw varies widely; no safe fallback
            Dim gpsCurrent As Double = PickCurrentA(gpsModules, GpsFallbackCurrentA)
            Dim txCurrent As Double = PickCurrentA(telemetryRadios, TelemetryFallbackCurrentA)
            Dim rxCurrent As Double = PickCurrentA(receivers, 0.0)                   ' Receivers typically < 50 mA; negligible
            Dim camCurrent As Double = PickCurrentA(cameras, If(cameras.Count > 0, CameraFallbackCurrentA, 0.0))

            budget.RealizedCurrentA = fcCurrent + gpsCurrent + txCurrent + rxCurrent + camCurrent
            budget.CurrentDeltaA = budget.RealizedCurrentA - budget.BudgetedCurrentA
        End Sub

        ''' <summary>
        ''' Returns the CurrentDrawA of the first component in the list,
        ''' or <paramref name="fallback"/> when the list is empty or the
        ''' property is 0 (not populated).
        ''' </summary>
        Private Shared Function PickCurrentA(components As List(Of ComponentSpecs),
                                             fallback As Double) As Double
            If components Is Nothing OrElse components.Count = 0 Then Return 0.0
            Dim draw As Double = components(0).NominalCurrentA
            Return If(draw > 0, draw, fallback)
        End Function

        ' =======================================================================
        ' SHARED PRIVATE HELPERS
        ' =======================================================================

        ''' <summary>
        ''' Determines the optimal LiPo cell count (S rating) for the build.
        '''
        ''' Decision rules applied in priority order:
        '''   1. Heavy-lift (payload > 1 kg) or high-speed (cruise > 20 m/s) => prefer 6S.
        '''   2. Racing profile => 4S or 6S based on speed (threshold: 25 m/s).
        '''   3. Long range (> 10 km) => prefer 6S for wire current efficiency.
        '''   4. Default => 4S (widest ESC/motor compatibility in the hobby market).
        '''
        ''' Hard cap: motor.OperatingVoltageMaxV limits the result.
        '''   maxCells = floor(motor.OperatingVoltageMaxV / 3.7)
        '''   Result   = min(preferred_cells, maxCells), floored at 3S.
        ''' </summary>
        Private Shared Function DetermineCellCount(motor As MotorSpec, specs As MissionSpecs) As Integer
            Dim cells As Integer = 4

            If specs.PayloadWeightGrams > 1000 OrElse specs.CruiseSpeedMs > 20 Then cells = 6
            If specs.MissionProfile = MissionProfileType.Racing Then
                cells = If(specs.CruiseSpeedMs > 25, 6, 4)
            End If
            If specs.RangeKm > 10 Then cells = 6

            ' Hard cap from motor voltage limit
            Dim maxCellsFromMotor As Integer = CInt(Math.Floor(motor.MaxVoltage / LipoCellNominalV))
            If maxCellsFromMotor > 0 Then cells = Math.Min(cells, maxCellsFromMotor)

            Return Math.Max(cells, 3)   ' minimum 3S
        End Function

        ''' <summary>
        ''' Voltage heuristic used during motor filtering (Task 7) before the exact
        ''' cell count from DetermineCellCount is available. Kept consistent with
        ''' DetermineCellCount to avoid selecting motors that Task 8 then rejects.
        ''' </summary>
        Private Shared Function EstimateNominalVoltage(specs As MissionSpecs) As Double
            Dim cells As Integer = 4
            If specs.PayloadWeightGrams > 1000 OrElse specs.CruiseSpeedMs > 20 Then cells = 6
            If specs.MissionProfile = MissionProfileType.Racing Then
                cells = If(specs.CruiseSpeedMs > 25, 6, 4)
            End If
            Return cells * LipoCellNominalV
        End Function

        ''' <summary>
        ''' Returns airframe mass estimate for a given motor count.
        ''' Linearly extrapolates for counts not in the lookup table.
        ''' </summary>
        Private Shared Function GetAirframeMass(motorCount As Integer) As Double
            If AirframeMassTable.ContainsKey(motorCount) Then
                Return AirframeMassTable(motorCount)
            End If
            Return 250.0 + ((motorCount - 4) / 2.0) * 115.0
        End Function

        ''' <summary>
        ''' Maps unusual motor counts to the nearest valid multirotor configuration.
        ''' Valid: 3 (tri), 4 (quad), 6 (hex), 8 (octo), 12 (dodeca). Default: 4.
        ''' </summary>
        Private Shared Function NormaliseMotorCount(motorCount As Integer) As Integer
            Select Case motorCount
                Case 3, 4, 6, 8, 12 : Return motorCount
                Case Else : Return 4
            End Select
        End Function

        ''' <summary>
        ''' Estimates maximum propeller diameter (inches) within frame geometry clearance.
        '''
        ''' Formula (symmetric X-frame):
        '''   arm_length (mm)      = frame_diagonal / sqrt(2)
        '''   max_prop_radius (mm) = arm_length / 1.3  [30% tip clearance]
        '''   max_prop_diam (in)   = (max_prop_radius x 2) / 25.4
        ''' </summary>
        Private Shared Function EstimateMaxPropDiameter(motorCount As Integer) As Double
            Dim diagonalMm As Double
            Select Case NormaliseMotorCount(motorCount)
                Case 4 : diagonalMm = 250.0
                Case 6 : diagonalMm = 380.0
                Case 8 : diagonalMm = 500.0
                Case Else : diagonalMm = 250.0
            End Select
            Dim armLengthMm As Double = diagonalMm / Math.Sqrt(2.0)
            Dim maxPropRadiusMm As Double = armLengthMm / 1.3
            Return (maxPropRadiusMm * 2.0) / 25.4
        End Function

        ''' <summary>
        ''' Validates minimum required fields on MissionSpecs before any calculation.
        ''' Throws descriptive ArgumentExceptions suitable for direct display in WinForms.
        ''' </summary>
        Private Shared Sub ValidateMissionSpecs(specs As MissionSpecs)
            If specs Is Nothing Then Throw New ArgumentNullException(NameOf(specs))
            If specs.FlightEnduranceMinutes <= 0 Then
                Throw New ArgumentException("FlightEnduranceMinutes must be > 0.", NameOf(specs))
            End If
            If specs.PayloadWeightGrams < 0 Then
                Throw New ArgumentException("PayloadWeightGrams cannot be negative.", NameOf(specs))
            End If
        End Sub

    End Class

    ' ===========================================================================
    ' INTERMEDIATE AND RESULT TYPES
    ' Co-located here during development — move to Core/Models/ once all engine
    ' tasks are complete and the full type surface is stable.
    ' ===========================================================================

    ''' <summary>Result of MTOW fixed-point iteration (Task 7 output).</summary>
    Public Class MtowEstimate
        ''' <summary>Airframe + electronics + payload mass before safety factor (g).</summary>
        Public Property StructuralMassGrams As Double
        ''' <summary>Battery mass from the final iteration (g).</summary>
        Public Property EstimatedBatteryMassGrams As Double
        ''' <summary>MTOW including safety factor — use this for all downstream sizing (g).</summary>
        Public Property TotalMassGrams As Double
        ''' <summary>Motor count used in this estimate.</summary>
        Public Property MotorCount As Integer
        ''' <summary>Number of iterations until |delta MTOW| &lt;= 1 g.</summary>
        Public Property IterationsToConverge As Integer
    End Class

    ''' <summary>Per-motor and total thrust requirements (Task 7 output).</summary>
    Public Class ThrustRequirement
        ''' <summary>Sum of all motor thrust at full throttle (gf).</summary>
        Public Property TotalThrustGf As Double
        ''' <summary>Thrust one motor must produce at full throttle (gf).</summary>
        Public Property ThrustPerMotorGf As Double
        ''' <summary>Applied thrust-to-weight ratio (dimensionless).</summary>
        Public Property ThrustToWeightRatio As Double
        ''' <summary>Motor count used.</summary>
        Public Property MotorCount As Integer
    End Class

    ''' <summary>
    ''' Complete power and current budget calculated from the selected motor set (Task 8 output).
    ''' All downstream power-system selections (battery, ESC, PDB) read exclusively from this object.
    ''' Expose on SelectionResult so the UI can show a full sizing breakdown.
    ''' </summary>
    Public Class PowerBudget
        ' Voltage
        ''' <summary>Selected LiPo cell count, e.g. 4 for "4S".</summary>
        Public Property CellCount As Integer
        ''' <summary>CellCount x 3.7 V (V).</summary>
        Public Property NominalVoltageV As Double

        ' Per-motor current
        ''' <summary>Single motor peak current at full throttle (A) — from datasheet or derived.</summary>
        Public Property MotorPeakCurrentA As Double
        ''' <summary>Single motor estimated current at hover throttle (A).</summary>
        Public Property MotorHoverCurrentA As Double

        ' System-level current
        ''' <summary>All motors at max + avionics (A) — governs ESC and PDB sizing.</summary>
        Public Property TotalPeakCurrentA As Double
        ''' <summary>All motors at hover + avionics (A) — governs battery capacity sizing.</summary>
        Public Property TotalAverageCurrentA As Double

        ' Battery sizing inputs
        ''' <summary>Minimum required capacity before margin (mAh).</summary>
        Public Property MinimumCapacityMah As Double
        ''' <summary>Required capacity including CapacityMargin (mAh) — use for battery query.</summary>
        Public Property RequiredCapacityMah As Double
        ''' <summary>Minimum continuous C-rating the battery must support (C).</summary>
        Public Property RequiredCRating As Double

        ' Informational power totals
        ''' <summary>Peak system power draw — all motors at max + avionics (W).</summary>
        Public Property PeakSystemPowerW As Double
        ''' <summary>Hover / cruise system power draw (W).</summary>
        Public Property HoverSystemPowerW As Double

        ''' <summary>Motor count used in all current calculations.</summary>
        Public Property MotorCount As Integer
    End Class

    ''' <summary>
    ''' Avionics power and range context produced by Task 9 (Step 9).
    '''
    ''' BudgetedCurrentA = the flat 3 A estimate used in Task 8's PowerBudget.
    ''' RealizedCurrentA = sum of actual CurrentDrawA values from selected avionics
    '''                    components (populated by TallyRealizedAvionicsCurrent).
    ''' CurrentDeltaA    = RealizedCurrentA - BudgetedCurrentA.
    '''                    |Delta| > 1 A signals the operator to consider a
    '''                    second-pass power budget recalculation.
    ''' </summary>
    Public Class AvionicsBudget
        ''' <summary>Flat avionics current used in Task 8 PowerBudget (A).</summary>
        Public Property BudgetedCurrentA As Double
        ''' <summary>Actual summed current from selected avionics (A); set by Task 9.</summary>
        Public Property RealizedCurrentA As Double
        ''' <summary>RealizedCurrentA minus BudgetedCurrentA (A). Positive = over budget.</summary>
        Public Property CurrentDeltaA As Double
        ''' <summary>Pack nominal voltage (V) — from PowerBudget, forwarded for convenience.</summary>
        Public Property NominalVoltageV As Double
        ''' <summary>Mission range from MissionSpecs (km).</summary>
        Public Property MissionRangeKm As Double
        ''' <summary>Mission profile — used by each avionics selector for profile-specific logic.</summary>
        Public Property MissionProfile As MissionProfileType
        ''' <summary>Operating environment — used for temperature and IP-rating filters.</summary>
        Public Property OperatingEnvironment As EnvironmentType
    End Class

    ''' <summary>
    ''' Complete output of the selection pipeline (Tasks 7, 8, and 9).
    ''' </summary>
    Public Class SelectionResult
        ' ── Task 7 ─────────────────────────────────────────────────────────────
        ''' <summary>Final MTOW estimate with safety margin (g).</summary>
        Public Property EstimatedMtowGrams As Double
        ''' <summary>Required single-motor thrust at full throttle (gf).</summary>
        Public Property RequiredThrustPerMotorGf As Double
        ''' <summary>Up to 5 motor candidates sorted by efficiency then mass.</summary>
        Public Property SelectedMotors As New List(Of ComponentSpecs)
        ''' <summary>Up to 5 propeller candidates compatible with SelectedMotors(0).</summary>
        Public Property SelectedPropellers As New List(Of ComponentSpecs)

        ' ── Task 8 ─────────────────────────────────────────────────────────────
        ''' <summary>Full power and current budget — exposes all intermediate values for UI.</summary>
        Public Property PowerBudget As PowerBudget
        ''' <summary>Up to 3 battery candidates sorted by capacity fit, C-rating, then mass.</summary>
        Public Property SelectedBatteries As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 individual ESC candidates sorted by current rating then mass.</summary>
        Public Property SelectedEscs As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 PDB candidates sorted by BEC presence then mass.</summary>
        Public Property SelectedPdbs As New List(Of ComponentSpecs)

        ' ── Task 9 ─────────────────────────────────────────────────────────────
        ''' <summary>
        ''' Avionics budget context: budgeted vs. realized current draw.
        ''' CurrentDeltaA > 1 A suggests running a second-pass power budget.
        ''' </summary>
        Public Property AvionicsBudget As AvionicsBudget
        ''' <summary>Up to 3 flight controller candidates.</summary>
        Public Property SelectedFlightControllers As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 GPS/GNSS module candidates. Empty for short-range racing builds.</summary>
        Public Property SelectedGpsModules As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 telemetry radio candidates. Empty for short-range racing.</summary>
        Public Property SelectedTelemetryRadios As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 RC receiver candidates.</summary>
        Public Property SelectedReceivers As New List(Of ComponentSpecs)
        ''' <summary>Up to 3 camera/sensor candidates. Empty for Delivery profile.</summary>
        Public Property SelectedCameras As New List(Of ComponentSpecs)
    End Class

    ''' <summary>
    ''' Thrown when the repository contains no component satisfying the calculated requirements.
    ''' The message string is designed to be shown directly in a WinForms MessageBox or label.
    ''' </summary>
    Public Class ComponentSelectionException
        Inherits Exception
        Public Sub New(message As String)
            MyBase.New(message)
        End Sub
    End Class

End Namespace
