using System.Collections.ObjectModel;

namespace Wiimote.Data;

public class ClassicControllerData : WiimoteData
{

	/// Classic Controller left stick analog values.  This is a size-2 array [X,Y]
	/// of RAW (unprocessed) stick data.  These values are in the range 0-63
	/// in both X and Y.
	///
	/// \sa GetLeftStick01()
	public ReadOnlyCollection<byte> LStick => Array.AsReadOnly(_lstick);
	private byte[] _lstick;

	/// Classic Controller right stick analog values.  This is a size-2 array [X,Y]
	/// of RAW (unprocessed) stick data.  These values are in the range 0-31
	/// in both X and Y.
	/// 
	/// \note The Right analog stick reports one less bit of precision than the left
	///       stick (the left stick is in the range 0-63 while the right is 0-31).
	///
	/// \sa GetRightStick01()
	public ReadOnlyCollection<byte> RStick => Array.AsReadOnly(_rstick);
	private byte[] _rstick;

	/// Classic Controller left trigger analog value.  This is RAW (unprocessed) analog
	/// data.  It is in the range 0-31 (with 0 being unpressed and 31 being fully pressed).
	///
	/// \sa rtrigger_range, ltrigger_switch, ltrigger_switch
	public byte LTriggerRange { get; private set; }

	/// Classic Controller right trigger analog value.  This is RAW (unprocessed) analog
	/// data.  It is in the range 0-31 (with 0 being unpressed and 31 being fully pressed).
	///
	/// \sa ltrigger_range, rtrigger_switch, rtrigger_switch
	public byte RTriggerRange { get; private set; }

	/// Button: Left trigger (bottom out switch)
	/// \sa rtrigger_switch, rtrigger_range, ltrigger_range
	public bool LTriggerSwitch { get; private set; }

	/// Button: Right trigger (button out switch)
	/// \sa ltrigger_switch, ltrigger_range, rtrigger_range
	public bool RTriggerSwitch { get; private set; }

	/// Button: A
	public bool A { get; private set; }

	/// Button: B
	public bool B { get; private set; }

	/// Button: X
	public bool X { get; private set; }

	/// Button: Y
	public bool Y { get; private set; }

	/// Button: + (plus)
	public bool Plus { get; private set; }

	/// Button: - (minus)
	public bool Minus { get; private set; }

	/// Button: home
	public bool Home { get; private set; }

	/// Button:  ZL
	public bool ZL { get; private set; }

	/// Button: ZR
	public bool ZR { get; private set; }

	/// Button: D-Pad Up
	public bool DPadUp { get; private set; }

	/// Button: D-Pad Down
	public bool DPadDown { get; private set; }

	/// Button: D-Pad Left
	public bool DPadLeft { get; private set; }

	/// Button: D-Pad Right
	public bool DPadRight { get; private set; }

	public ClassicControllerData(global::Wiimote.Wiimote owner) : base(owner) {
		_lstick = new byte[2];

		_rstick = new byte[2];
	}

	public override bool InterpretData(byte[] data) {
		if(data.Length < 6)
			return false;

		_lstick[0] = (byte)(data[0] & 0x3f);
		_lstick[1] = (byte)(data[1] & 0x3f);

		_rstick[0] = (byte)(((data[0] & 0xc0) >> 3) |
		                    ((data[1] & 0xc0) >> 5) |
		                    ((data[2] & 0x80) >> 7));
		_rstick[1] = (byte)(data[2] & 0x1f);

		LTriggerRange = (byte)(((data[2] & 0x60) >> 2) |
		                         ((data[3] & 0xe0) >> 5));

		RTriggerRange = (byte)(data[3] & 0x1f);

		// Bit is zero when pressed, one when up.  This is really weird so I reverse
		// the bit with !=
		DPadRight 	 = (data[4] & 0x80) != 0x80;
		DPadDown  	 = (data[4] & 0x40) != 0x40;
		LTriggerSwitch = (data[4] & 0x20) != 0x20;
		Minus 			 = (data[4] & 0x10) != 0x10;
		Home 			 = (data[4] & 0x08) != 0x08;
		Plus			 = (data[4] & 0x04) != 0x04;
		RTriggerSwitch = (data[4] & 0x02) != 0x02;

		ZL 			 = (data[5] & 0x80) != 0x80;
		B 				 = (data[5] & 0x40) != 0x40;
		Y 				 = (data[5] & 0x20) != 0x20;
		A 				 = (data[5] & 0x10) != 0x10;
		X 				 = (data[5] & 0x08) != 0x08;
		ZR 			 = (data[5] & 0x04) != 0x04;
		DPadLeft		 = (data[5] & 0x02) != 0x02;
		DPadUp		 = (data[5] & 0x01) != 0x01;

		return true;
	}

	/// Returns the left stick analog values in the range 0-1.
	///
	/// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
	///			 is the zero point.  You must do these calibrations yourself.
	public float[] GetLeftStick01() {
		float[] ret = new float[2];
		for(int x=0;x<2;x++) {
			ret[x] = LStick[x];
			ret[x] /= 63;
		}
		return ret;
	}

	/// Returns the right stick analog values in the range 0-1.
	///
	/// \note The Right stick has half of the precision of the left stick due to how the Wiimote reports data.  The
	/// 	  right stick is therefore better for less precise input.
	///
	/// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
	///			 is the zero point.  You must do these calibrations yourself.
	public float[] GetRightStick01() {
		float[] ret = new float[2];
		for(int x=0;x<2;x++) {
			ret[x] = RStick[x];
			ret[x] /= 31;
		}
		return ret;
	}
}