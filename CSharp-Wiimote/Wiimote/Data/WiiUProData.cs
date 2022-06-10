using System.Collections.ObjectModel;

namespace Wiimote.Data;

public class WiiUProData : WiimoteData
{

	/// Pro Controller left stick analog values.  This is a size-2 array [X,Y]
	/// of RAW (unprocessed) stick data.  These values are in the range 803-3225
	/// in the X direction and 843-3291 in the Y direction.
	///
	/// \note Min/Max values may vary between controllers (untested).  One way to calibrate
	///		  is to prompt the user to spin the control sticks in circles and record the min/max values.
	///
	/// \sa GetLeftStick01()
	public ReadOnlyCollection<ushort> LStick => Array.AsReadOnly(_lstick);
	private ushort[] _lstick;

	/// Pro Controller right stick analog values.  This is a size-2 array [X,Y]
	/// of RAW (unprocessed) stick data.  These values are in the range 852-3169
	/// in the X direction and 810-3315 in the Y direction.
	///
	/// \note Min/Max values may vary between controllers (untested).  One way to calibrate
	///		  is to prompt the user to spin the control sticks in circles and record the min/max values.
	///
	/// \sa GetRightStick01()
	public ReadOnlyCollection<ushort> RStick => Array.AsReadOnly(_rstick);
	private ushort[] _rstick;

	/// Button: Left Stick Button (push down switch)
	public bool LStickButton { get; private set; }

	/// Button: Right Stick Button (push down switch)
	public bool RStickButton { get; private set; }

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

	/// Button:  L
	public bool L { get; private set; }

	/// Button: R
	public bool R { get; private set; }

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

	private ushort[] lmax = {3225,3291};
	private ushort[] lmin = {803,843};
	private ushort[] rmax = {3169,3315};
	private ushort[] rmin = {852,810};

	public WiiUProData(Wiimote owner) : base(owner) {
		_lstick = new ushort[2];

		_rstick = new ushort[2];
	}

	public override bool InterpretData(byte[] data)
	{
		if(data.Length < 11)
			return false;

		_lstick[0] = (ushort)((ushort)data[0] | ((ushort)(data[1] & 0x0f) << 8));
		_lstick[1] = (ushort)((ushort)data[4] | ((ushort)(data[5] & 0x0f) << 8));

		_rstick[0] = (ushort)((ushort)data[2] | ((ushort)(data[3] & 0x0f) << 8));
		_rstick[1] = (ushort)((ushort)data[6] | ((ushort)(data[7] & 0x0f) << 8));

		DPadRight	= (data[8] & 0x80) != 0x80;
		DPadDown 	= (data[8] & 0x40) != 0x40;
		L 			= (data[8] & 0x20) != 0x20;
		Minus	 	= (data[8] & 0x10) != 0x10;
		Home	 	= (data[8] & 0x08) != 0x08;
		Plus	 	= (data[8] & 0x04) != 0x04;
		R		 	= (data[8] & 0x02) != 0x02;

		ZL 		= (data[9] & 0x80) != 0x80;
		B	 		= (data[9] & 0x40) != 0x40;
		Y 			= (data[9] & 0x20) != 0x20;
		A	 		= (data[9] & 0x10) != 0x10;
		X		 	= (data[9] & 0x08) != 0x08;
		ZR		 	= (data[9] & 0x04) != 0x04;
		DPadLeft 	= (data[9] & 0x02) != 0x02;
		DPadUp 	= (data[9] & 0x01) != 0x01;

		LStickButton = (data[10] & 0x02) != 0x02;
		RStickButton = (data[10] & 0x01) != 0x01;

		return true;
	}

	/// Returns the left stick analog values in the range 0-1.
	///
	/// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
	///			 is the zero point.  You must do these calibrations yourself.
	public float[] GetLeftStick01() {
		float[] ret = new float[2];
		for(int x=0;x<2;x++) {
			ret[x] = _lstick[x];
			ret[x] -= lmin[x];
			ret[x] /= lmax[x]-lmin[x];
		}
		return ret;
	}

	/// Returns the right stick analog values in the range 0-1.
	///
	/// \warning This does not take into account zero points or deadzones.  Likewise it does not guaruntee that 0.5f
	///			 is the zero point.  You must do these calibrations yourself.
	public float[] GetRightStick01() {
		float[] ret = new float[2];
		for(int x=0;x<2;x++) {
			ret[x] = _rstick[x];
			ret[x] -= rmin[x];
			ret[x] /= rmax[x]-rmin[x];
		}
		return ret;
	}
}
