Namespace Core.Models

    ''' <summary>
    ''' Safety-margin multipliers (k-factors) applied during component sizing.
    ''' Each factor adds headroom above the minimum calculated requirement.
    '''
    ''' Defaults match FAST-UAV reference values for a general-purpose multirotor.
    ''' Increase margins for harsh environments or safety-critical missions.
    ''' Decrease for minimum-weight racing builds.
    ''' </summary>
    Public Class SizingPolicy

        ''' <summary>
        ''' Motor must produce at least (k Ã— propeller hover torque) as max continuous torque.
        ''' Range: 1.5 (racing, short bursts) to 3.0 (heavy-lift, sustained climb).
        ''' Default 1.5 - calibrated baseline (motor uses about 67% of rated continuous torque at hover).
        ''' </summary>
        Public Property KMotorTorque As Double = 1.5

        ''' <summary>
        ''' Battery pack voltage = motor nominal operating voltage Ã— k.
        ''' Range: 1.1 to 1.5.
        ''' Default 1.3 â€” ensures cell sag under load does not drop below motor minimum.
        ''' </summary>
        Public Property KBatteryVoltage As Double = 1.3

        ''' <summary>
        ''' Battery usable capacity = calculated mission energy requirement Ã— k.
        ''' Range: 1.1 (calm conditions, known route) to 1.5 (wind, unknowns).
        ''' Default 1.2 â€” 20% reserve covers throttle spikes, headwind, and capacity aging.
        ''' </summary>
        Public Property KBatteryCapacity As Double = 1.2

        ''' <summary>
        ''' ESC continuous current rating â‰¥ motor peak current Ã— k.
        ''' Range: 1.1 to 1.5.
        ''' Default 1.25 â€” 25% thermal headroom for sustained full-throttle flight.
        ''' </summary>
        Public Property KEscCurrent As Double = 1.25

        ''' <summary>Returns a SizingPolicy preset tuned for minimum-weight racing builds.</summary>
        Public Shared Function RacingPreset() As SizingPolicy
            Return New SizingPolicy With {
                .KMotorTorque = 1.5,
                .KBatteryVoltage = 1.1,
                .KBatteryCapacity = 1.1,
                .KEscCurrent = 1.15
            }
        End Function

        ''' <summary>Returns a SizingPolicy preset tuned for harsh-environment / safety-critical missions.</summary>
        Public Shared Function HarshEnvironmentPreset() As SizingPolicy
            Return New SizingPolicy With {
                .KMotorTorque = 3.0,
                .KBatteryVoltage = 1.4,
                .KBatteryCapacity = 1.4,
                .KEscCurrent = 1.5
            }
        End Function

    End Class

End Namespace


