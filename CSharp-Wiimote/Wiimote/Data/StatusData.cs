using System.Collections.ObjectModel;

namespace Wiimote.Data;

public class StatusData : WiimoteData
{
    /// Size: 4.  An array of what Player LEDs are on as reported by
    /// the Wii Remote.  This is only updated when the Wii Remote sends status reports.
    public ReadOnlyCollection<bool> Led => Array.AsReadOnly(_led);

    private bool[] _led;

    /// \brief True if the Wii Remote's batteries are low, as reported by the Wii Remote.
    ///        This is only updated when the Wii Remote sends status reports.
    /// \sa battery_level
    public bool BatteryLow { get; private set; }

    /// True if an extension controller is connected, as reported by the Wii Remote.
    /// This is only updated when the Wii Remote sends status reports.
    public bool ExtConnected { get; private set; }

    /// True if the speaker is currently enabled, as reported by the Wii Remote.
    /// This is only updated when the Wii Remote sends status reports.
    public bool SpeakerEnabled { get; private set; }

    /// True if IR is currently enabled, as reported by the Wii Remote.
    /// This is only updated when the Wii Remote sends status reports.
    public bool IrEnabled { get; private set; }

    /// \brief The current battery level, as reported by the Wii Remote.
    ///        This is only updated when the Wii Remote sends status reports.
    /// \sa battery_low
    public byte BatteryLevel { get; private set; }

    public StatusData(Wiimote owner)
        : base(owner)
    {
        _led = new bool[4];
    }

    public override bool InterpretData(byte[] data)
    {
        if (data.Length != 2) return false;

        byte flags = data[0];
        BatteryLow = (flags & 0x01) == 0x01;
        ExtConnected = (flags & 0x02) == 0x02;
        SpeakerEnabled = (flags & 0x04) == 0x04;
        IrEnabled = (flags & 0x08) == 0x08;
        _led[0] = (flags & 0x10) == 0x10;
        _led[1] = (flags & 0x20) == 0x20;
        _led[2] = (flags & 0x40) == 0x40;
        _led[3] = (flags & 0x80) == 0x80;

        BatteryLevel = data[1];

        return true;
    }
}