using System.Collections.ObjectModel;

namespace Wiimote.Data;

public class GuitarData : WiimoteData
{
    /// Guitar Analog Stick values.  This is a size 2 Array [X, Y] of
    /// RAW (unprocessed) stick data.  Generally the analog stick returns
    /// values in the range 4-61, the center being at 32.
    public ReadOnlyCollection<byte> Stick => Array.AsReadOnly(_stick);

    private byte[] _stick;

    /// Guitar frets where element 0 is green and element 4 is orange
    public ReadOnlyCollection<bool> Frets => Array.AsReadOnly(_frets);

    private bool[] _frets;

    /// Guitar slider (supported in some models) - the strip below the
    /// regular frets on Guitar Hero IV+ controllers. Analog data in
    /// range (4, 31), snaps to 15 when untouched.
    public byte Slider { get; private set; }

    /// True if the model has a slider
    public bool HasSlider { get; private set; }

    /// Get active green fret, ignoring slider value
    public bool GreenFret => _frets[0];

    /// Get active red fret, ignoring slider value
    public bool RedFret => _frets[1];

    /// Get active yellow fret, ignoring slider value
    public bool YellowFret => _frets[2];

    /// Get active blue fret, ignoring slider value
    public bool BlueFret => _frets[3];

    /// Get active orange fret, ignoring slider value
    public bool OrangeFret => _frets[4];

    /// True if player's finger is touching green segment of slider
    public bool GreenSlider => HasSlider && Slider > 0 && Slider < 0x08;

    /// True if player's finger is touching red segment of slider
    public bool RedSlider => HasSlider && Slider > 0x06 && Slider < 0x0E;

    /// True if player's finger is touching yellow segment of slider
    public bool YellowSlider => HasSlider && Slider > 0x0B && Slider < 0x16 && Slider != 0x0F;

    /// True if player's finger is touching blue segment of slider
    public bool BlueSlider => HasSlider && Slider > 0x13 && Slider < 0x1B;

    /// True if player's finger is touching orange segment of slider
    public bool OrangeSlider => HasSlider && Slider > 0x19 && Slider < 0x20;

    /// True if player is touching EITHER green fret or green slider (if supported)
    public bool Green => _frets[0] || GreenSlider;

    /// True if player is touching EITHER red fret or red slider (if supported)
    public bool Red => _frets[1] || RedSlider;

    /// True if player is touching EITHER yellow fret or yellow slider (if supported)
    public bool Yellow => _frets[2] || YellowSlider;

    /// True if player is touching EITHER blue fret or blue slider (if supported)
    public bool Blue => _frets[3] || BlueSlider;

    /// True if player is touching EITHER orange fret or orange slider (if supported)
    public bool Orange => _frets[4] || OrangeSlider;

    /// Button: Plus (start/pause)
    public bool Plus { get; private set; }

    /// Button: Minus (star power)
    public bool Minus { get; private set; }

    /// Strum Up
    public bool StrumUp { get; private set; }

    /// Strum Down
    public bool StrumDown { get; private set; }

    /// Strum Up OR Down
    public bool Strum => StrumDown || StrumUp;

    /// Whammy Bar, typically rests somewhere between 14-16
    /// and maxes out at 26.
    public byte Whammy { get; private set; }


    public GuitarData(Wiimote owner)
        : base(owner)
    {
        _stick = new byte[2];
        _frets = new bool[5];
    }

    public override bool InterpretData(byte[] data)
    {
        if (data.Length < 6)
        {
            _stick[0] = 32;
            _stick[1] = 32;
            Whammy = 0x10;
            for (int i = 0; i < _frets.Length; i++)
            {
                _frets[i] = false;
            }

            Slider = 0x0F;
            StrumUp = StrumDown = Minus = Plus = false;
            return false;
        }

        _stick[0] = (byte) (data[0] & 0x3F); // because the last 2 bits differ by model
        _stick[1] = (byte) (data[1] & 0x3F); // because the last 2 bits differ by model

        Whammy = (byte) (data[3] & 0x1F); // only first 5 bits used

        _frets[0] = (data[5] & 0x10) == 0;
        _frets[1] = (data[5] & 0x40) == 0;
        _frets[2] = (data[5] & 0x08) == 0;
        _frets[3] = (data[5] & 0x20) == 0;
        _frets[4] = (data[5] & 0x80) == 0;

        HasSlider = data[2] != 0xFF;
        Slider = (byte) (data[2] & 0x1F); // only first 5 bits used

        Minus = (data[4] & 0x10) == 0;
        Plus = (data[4] & 0x04) == 0;

        StrumUp = (data[5] & 0x01) == 0;
        StrumDown = (data[4] & 0x40) == 0;

        return true;
    }

    /// Returns a size 2 [X, Y] array of the analog stick's position, in the range
    /// (0, 1). The stick typically rests somewhere NEAR [0.5, 0.5], and the actual
    /// range ends up being somewhere in the neighborhood of (0.07, 0.93).
    public float[] GetStick01()
    {
        float[] ret = new float[2];
        for (int x = 0; x < 2; x++)
        {
            ret[x] = _stick[x] / 63f;
        }

        return ret;
    }

    /// Returns a the whammy bar's value in the range (0, 1), where 0 is resting 
    /// position and 1 is fully depressed
    public float GetWhammy01()
    {
        float ret = (Whammy - 16) / 10f;
        return ret < 0 ? 0 : ret > 1 ? 1 : ret;
    }

    /// Returns a the slider's value in the range (0, 1), where 0 is green and
    /// 1 is orange. If the slider is not supported or not actively being used,
    /// returns -1.
    public float GetSlider01()
    {
        if (!HasSlider || Slider == 0x0F || Slider == 0)
        {
            return -1f;
        }

        return (Slider - 4) / 27f;
    }
}