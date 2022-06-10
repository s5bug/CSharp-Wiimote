namespace Wiimote.Data;

public abstract class WiimoteData
{
    protected global::Wiimote.Wiimote Owner;

    public WiimoteData(global::Wiimote.Wiimote Owner)
    {
        this.Owner = Owner;
    }

    /// \brief Interprets raw byte data reported by the Wii Remote.  The format of the actual bytes
    ///        passed to this depends on the Wii Remote's current data report mode and the type
    ///        of data being passed.
    /// \sa Wiimote::ReadWiimoteData()
    public abstract bool InterpretData(byte[] data);
        
}