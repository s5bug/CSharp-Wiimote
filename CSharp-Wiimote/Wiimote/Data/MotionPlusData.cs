namespace Wiimote.Data;

public class MotionPlusData : WiimoteData
{
    /// The rotational velocity in the Pitch direction of the Wii Remote, as
    /// reported by the Wii Motion Plus.  Measured in degrees per second.
    ///
    /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
    ///       of finding the change in degrees over the previous report is to divide
    ///       this value by 95.
    public float PitchSpeed { get; private set; }

    private int _pitchSpeedRaw;

    /// The rotational velocity in the Yaw direction of the Wii Remote, as
    /// reported by the Wii Motion Plus.  Measured in degrees per second.
    ///
    /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
    ///       of finding the change in degrees over the previous report is to divide
    ///       this value by 95.
    public float YawSpeed { get; private set; }

    private int _yawSpeedRaw;

    /// The rotational velocity in the Roll direction of the Wii Remote, as
    /// reported by the Wii Motion Plus.  Measured in degrees per second.
    ///
    /// \note The Wii Remote sends updates at a frequency of 95Hz.  So, one way
    ///       of finding the change in degrees over the previous report is to divide
    ///       this value by 95.
    public float RollSpeed { get; private set; }

    private int _rollSpeedRaw;

    /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
    /// Pitch direction.  This means that it is more precise as it doesn't have
    /// to report higher values.  If false often, it is more likely that the Wii Motion
    /// Plus will "fall out of sync" with the real world.
    public bool PitchSlow { get; private set; }

    /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
    /// Yaw direction.  This means that it is more precise as it doesn't have
    /// to report higher values.  If false often, it is more likely that the Wii Motion
    /// Plus will "fall out of sync" with the real world.
    public bool YawSlow { get; private set; }

    /// If true, the Wii Motion Plus reports that it is in "slow" mode in the
    /// Roll direction.  This means that it is more precise as it doesn't have
    /// to report higher values.  If false often, it is more likely that the Wii Motion
    /// Plus will "fall out of sync" with the real world.
    public bool RollSlow { get; private set; }

    /// If true, the Wii Motion Plus reports that an extension is connected in its
    /// extension port.
    public bool ExtensionConnected { get; private set; }

    private int _pitchZero = 8063;
    private int _yawZero = 8063;
    private int _rollZero = 8063;

    // I would like to say that this was calculated or something, but honestly this was created
    // simply through trial and error.  I am going to tweak this constant to see if I can get it
    // any better in the future.  Realistically this value is the result of the Analog/Digital converter
    // in the Wii Motion Plus along with the analog output of the gyros, but the documentation is so
    // shitty that I don't even care anymore.
    private const float MagicCalibrationConstant = 0.05f;

    public MotionPlusData(Wiimote owner) : base(owner)
    {
    }

    public override bool InterpretData(byte[] data)
    {
        if (data.Length < 6)
            return false;

        _yawSpeedRaw = data[0];
        _yawSpeedRaw |= (data[3] & 0xfc) << 6;
        _rollSpeedRaw = data[1];
        _rollSpeedRaw |= (data[4] & 0xfc) << 6;
        _pitchSpeedRaw = data[2];
        _pitchSpeedRaw |= (data[5] & 0xfc) << 6;

        YawSlow = (data[3] & 0x02) == 0x02;
        PitchSlow = (data[3] & 0x01) == 0x01;
        RollSlow = (data[4] & 0x02) == 0x02;
        ExtensionConnected = (data[4] & 0x01) == 0x01;

        PitchSpeed = (_pitchSpeedRaw - _pitchZero) * MagicCalibrationConstant;
        YawSpeed = (_yawSpeedRaw - _yawZero) * MagicCalibrationConstant;
        RollSpeed = (_rollSpeedRaw - _rollZero) * MagicCalibrationConstant;

        // At high speeds, the Wii Remote Reports with less precision to reach higher values.
        // The multiplier is 2000 / 440 when in fast mode.
        // http://wiibrew.org/wiki/Wiimote/Extension_Controllers/Wii_Motion_Plus
        if (!PitchSlow)
            PitchSpeed *= 2000f / 440f;
        if (!YawSlow)
            YawSpeed *= 2000f / 440f;
        if (!RollSlow)
            RollSpeed *= 2000f / 440f;

        return true;
    }

    /// Calibrates the zero values of the Wii Motion Plus in the Pitch, Yaw, and Roll directions.
    /// The Wii Remote should be in a flat, motionless position when calibrating (for example, face
    /// down on a flat surface).
    ///
    /// A good idea here is to reference the Accelerometer values of the Wii Remote to make sure that
    /// your simulated rotation is consistent with the actual rotation of the remote.
    public void SetZeroValues()
    {
        _pitchZero = _pitchSpeedRaw;
        _yawZero = _yawSpeedRaw;
        _rollZero = _rollSpeedRaw;

        _pitchSpeedRaw = 0;
        _yawSpeedRaw = 0;
        _rollSpeedRaw = 0;
        PitchSpeed = 0;
        YawSpeed = 0;
        RollSpeed = 0;
    }
}