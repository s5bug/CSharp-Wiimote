namespace Wiimote.Data;

public class ButtonData : WiimoteData
{
    /// Button: D-Pad Left
    public bool DLeft { get; private set; }

    /// Button: D-Pad Right
    public bool DRight { get; private set; }

    /// Button: D-Pad Up
    public bool DUp { get; private set; }

    /// Button: D-Pad Down
    public bool DDown { get; private set; }

    /// Button: A
    public bool A { get; private set; }

    /// Button: B
    public bool B { get; private set; }

    /// Button: 1 (one)
    public bool One { get; private set; }

    /// Button: 2 (two)
    public bool Two { get; private set; }

    /// Button: + (plus)
    public bool Plus { get; private set; }

    /// Button: - (minus)
    public bool Minus { get; private set; }

    /// Button: Home
    public bool Home { get; private set; }

    public ButtonData(Wiimote owner) : base(owner) { }

    public override bool InterpretData(byte[] data)
    {
        if (data.Length != 2) return false;

        DLeft = (data[0] & 0x01) == 0x01;
        DRight = (data[0] & 0x02) == 0x02;
        DDown = (data[0] & 0x04) == 0x04;
        DUp = (data[0] & 0x08) == 0x08;
        Plus = (data[0] & 0x10) == 0x10;

        Two = (data[1] & 0x01) == 0x01;
        One = (data[1] & 0x02) == 0x02;
        B = (data[1] & 0x04) == 0x04;
        A = (data[1] & 0x08) == 0x08;
        Minus = (data[1] & 0x10) == 0x10;

        Home = (data[1] & 0x80) == 0x80;

        return true;
    }
}