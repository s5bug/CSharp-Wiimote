using System.Runtime.InteropServices;
using Device.Net;
using Hid.Net.Windows;
using Microsoft.Extensions.Logging;

namespace Wiimote;

public class WiimoteManager
{
    private const ushort VendorIdWiimote = 0x057e;
    private const ushort ProductIdWiimote = 0x0306;
    private const ushort ProductIdWiimotePlus = 0x0330;

    /// A list of all currently connected Wii Remotes.
    public static List<Wiimote> Wiimotes { get; } = new();

    // ------------- RAW HIDAPI INTERFACE ------------- //

    /// \brief Attempts to find connected Wii Remotes, Wii Remote Pluses or Wii U Pro Controllers
    /// \return If any new remotes were found.
    public static async Task<bool> FindWiimotes(ILoggerFactory loggerFactory)
    {
        IDeviceFactory wiimoteFactory =
            new FilterDeviceDefinition(vendorId: VendorIdWiimote, productId: ProductIdWiimote)
                .CreateWindowsHidDeviceFactory(loggerFactory);

        bool wiimotesFound = await _FindWiimotes(WiimoteType.Wiimote, wiimoteFactory);
        
        IDeviceFactory wiimotePlusFactory =
            new FilterDeviceDefinition(vendorId: VendorIdWiimote, productId: ProductIdWiimotePlus)
                .CreateWindowsHidDeviceFactory(loggerFactory);

        bool wiimotePlusFound = await _FindWiimotes(WiimoteType.WiimotePlus, wiimotePlusFactory);

        return wiimotesFound || wiimotePlusFound;
    }

    private static async Task<bool> _FindWiimotes(WiimoteType type, IDeviceFactory factory)
    {
        IEnumerable<ConnectedDeviceDefinition> devices = await factory.GetConnectedDeviceDefinitionsAsync().ConfigureAwait(false);

        bool found = false;
        
        foreach (ConnectedDeviceDefinition definition in devices)
        {
            if(Wiimotes.Exists(connected => connected.Serial == definition.SerialNumber))
                continue;

            IDevice remote = await factory.GetDeviceAsync(definition).ConfigureAwait(false);

            await remote.InitializeAsync().ConfigureAwait(false);
            
            Wiimotes.Add(new Wiimote(type, definition.SerialNumber, remote));
        }

        return found;
    }

    /// \brief Disables the given \c Wiimote by closing its bluetooth HID connection.  Also removes the remote from Wiimotes
    /// \param remote The remote to cleanup
    public static void Cleanup(Wiimote remote)
    {
        remote.Device.Dispose();
        
        Wiimotes.Remove(remote);
    }

    /// \return If any Wii Remotes are connected and found by FindWiimote
    public static bool HasWiimote()
    {
        return Wiimotes.Count > 0;
    }
}
