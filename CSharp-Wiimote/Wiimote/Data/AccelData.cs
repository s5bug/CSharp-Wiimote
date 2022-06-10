using System.Collections.ObjectModel;

namespace Wiimote.Data;

public class AccelData : WiimoteData
{
    /// <summary>
    /// Current remote-space acceleration, in the Wii Remote's coordinate system.
    /// These are RAW values, so they are not with respect to a zero point.  See <see cref="CalibrateAccel"/>.
    /// This is only updated if the Wii Remote has a report mode that supports
    /// the Accelerometer.
    /// </summary>
    ///
    /// <remarks>
    /// This should not be used unless if you want to calibrate the accelerometer manually.  Use
    /// <see cref="CalibrateAccel"/> instead.
    /// </remarks>
    ///
    /// <remarks>
    /// Range:            0 - 1024\n
    /// *The sign of the directions below are with respect to the zero point of the accelerometer:*\n
    /// Up/Down:          +Z/-Z\n
    /// Left/Right:       +X/-X\n
    /// Forward/Backward: -Y/+Y\n
    /// </remarks>
    public ReadOnlyCollection<int> Accel => Array.AsReadOnly(_accel);

    private readonly int[] _accel;

    /// <summary>
    /// Size: 3x3. Calibration data for the accelerometer. This is not reported
    /// by the Wii Remote directly - it is instead collected from normal
    /// Wii Remote accelerometer data.
    /// </summary>
    /// <seealso cref="AccelCalibrationStep"/>, <seealso cref="CalibrateAccel(AccelCalibrationStep)"/>
    ///
    /// <remarks>
    /// Here are the 3 calibration steps:
    /// 1. Horizontal with the A button facing up
    /// 2. IR sensor down on the table so the expansion port is facing up
    /// 3. Laying on its side, so the left side is facing up
    /// 
    /// By default this is set to experimental calibration data.
    /// </remarks>
    ///
    /// <remarks>
    /// int[calibration step,calibration data] (size 3x3)
    /// </remarks>
    public readonly int[,] AccelCalib =
    {
        {479, 478, 569},
        {472, 568, 476},
        {569, 469, 476}
    };

    public AccelData(Wiimote owner)
        : base(owner)
    {
        _accel = new int[3];
    }

    public override bool InterpretData(byte[] data)
    {
        if (data.Length != 5) return false;

        // Note: data[0 - 1] is the buttons data.  data[2 - 4] is the accel data.
        // Accel data and buttons data is interleaved to reduce packet size.
        _accel[0] = (data[2] << 2) | ((data[0] >> 5) & 0x03);
        _accel[1] = (data[3] << 2) | ((data[1] >> 5) & 0x01);
        _accel[2] = (data[4] << 2) | ((data[1] >> 6) & 0x01);

        //for (int x = 0; x < 3; x++) _accel[x] -= 0x200; // center around zero.

        return true;
    }

    /// \brief Interprets raw byte data reported by the Wii Remote when in interleaved data reporting mode.
    ///        The format of the actual bytes passed to this depends on the Wii Remote's current data report
    ///        mode and the type of data being passed.
    /// 
    /// \sa Wiimote::ReadWiimoteData()
    public bool InterpretDataInterleaved(byte[] data1, byte[] data2)
    {
        if (data1.Length != 21 || data2.Length != 21)
            return false;

        _accel[0] = data1[2] << 2;
        _accel[1] = data2[2] << 2;
        _accel[2] = (((data1[0] & 0x60) >> 1) |
                     ((data1[1] & 0x60) << 1) |
                     ((data2[0] & 0x60) >> 5) |
                     ((data2[1] & 0x60) >> 3)) << 2;

        //for (int x = 0; x < 3; x++) _accel[x] -= 0x200; // center around zero.

        return true;
    }

    /// \brief Use current accelerometer values to update calibration data.  Use this when
    ///        the user reports that the Wii Remote is in a calibration position.
    /// \param step The calibration step to perform.
    /// \sa  accel_calib,  AccelCalibrationStep
    public void CalibrateAccel(AccelCalibrationStep step)
    {
        for (int x = 0; x < 3; x++)
            AccelCalib[(int) step, x] = Accel[x];
    }

    public float[] GetAccelZeroPoints()
    {
        float[] ret = new float[3];
        // For each axis, find the two steps that are not affected by gravity on that axis.
        // average these values together to get a final zero point.
        ret[0] = ((float) AccelCalib[0, 0] + (float) AccelCalib[1, 0]) / 2f;
        ret[1] = ((float) AccelCalib[0, 1] + (float) AccelCalib[2, 1]) / 2f;
        ret[2] = ((float) AccelCalib[1, 2] + (float) AccelCalib[2, 2]) / 2f;
        return ret;
    }

    /// \brief Calibrated Accelerometer Data using experimental calibration points.
    ///        These values are in Wii Remote coordinates (in the direction of gravity)
    /// \sa  CalibrateAccel(),  GetAccelZeroPoints(),  accel,  accel_calib
    ///
    /// Range: -1 to 1\n
    /// Up/Down:          +Z/-Z\n
    /// Left/Right:       +X/-X\n
    /// Forward/Backward: -Y/+Y
    public float[] GetCalibratedAccelData()
    {
        float[] o = GetAccelZeroPoints();

        float xRaw = Accel[0];
        float yRaw = Accel[1];
        float zRaw = Accel[2];

        float[] ret = new float[3];
        ret[0] = (xRaw - o[0]) / (AccelCalib[2, 0] - o[0]);
        ret[1] = (yRaw - o[1]) / (AccelCalib[1, 1] - o[1]);
        ret[2] = (zRaw - o[2]) / (AccelCalib[0, 2] - o[2]);
        return ret;
    }
}