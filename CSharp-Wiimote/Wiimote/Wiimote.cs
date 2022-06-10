using System.Collections.ObjectModel;
using System.Diagnostics;
using Device.Net;
using Wiimote.Data;

namespace Wiimote;

public delegate void ReadResponder(byte[] data);

public class Wiimote
{
    /// Represents whether or not to turn on rumble when sending reports to
    /// the Wii Remote.  This will only be applied when a data report is sent.
    /// That is, simply setting this flag will not instantly enable rumble.
    public bool RumbleOn = false;

    /// Accelerometer data component
    public AccelData Accel { get; }

    /// If a Nunchuck is currently connected to the Wii Remote's extension port,
    /// this contains all relevant Nunchuck controller data as it is reported by
    /// the Wiimote.  If no Nunchuck is connected, this is \c null.
    ///
    /// \sa current_ext
    public NunchuckData? Nunchuck
    {
        get
        {
            if (CurrentExt == ExtensionController.Nunchuck)
                return (NunchuckData) _extension!;
            return null;
        }
    }

    /// If a Classic Controller is currently connected to the Wii Remote's extension port,
    /// this contains all relevant Classic Controller data as it is reported by
    /// the Wiimote.  If no Classic Controller is connected, this is \c null.
    ///
    /// \sa current_ext
    public ClassicControllerData? ClassicController
    {
        get
        {
            if (CurrentExt == ExtensionController.Classic)
                return (ClassicControllerData) _extension!;
            return null;
        }
    }

    /// If a Wii Motion Plus is currently connected to the Wii Remote's extension port,
    /// and has been activated by ActivateWiiMotionPlus(), this contains all relevant 
    /// Wii Motion Plus controller data as it is reported by the Wiimote.  If no
    /// WMP is connected, this is \c null.
    ///
    /// \sa current_ext, wmp_attached, ActivateWiiMotionPlus()
    public MotionPlusData? MotionPlus
    {
        get
        {
            if (CurrentExt == ExtensionController.MotionPlus)
                return (MotionPlusData) _extension!;
            return null;
        }
    }

    /// If this Wiimote is a Wii U Pro Controller,
    /// this contains all relevant Pro Controller data as it is reported by
    /// the Controller.  If this Wiimote is not a Wii U Pro Controller, this is \c null.
    ///
    /// \sa current_ext
    public WiiUProData? WiiUPro
    {
        get
        {
            if (CurrentExt == ExtensionController.WiiUPro)
                return (WiiUProData) _extension!;
            return null;
        }
    }

    /// If this Wiimote is a Guitar Hero Guitar Controller,
    /// this contains all relevant Guitar data as it is reported by
    /// the Controller.  If this Wiimote is not a Guitar Controller, this is \c null.
    ///
    /// \sa current_ext
    public GuitarData? Guitar
    {
        get
        {
            if (CurrentExt == ExtensionController.Guitar)
                return (GuitarData) _extension!;
            return null;
        }
    }

    private WiimoteData? _extension;

    /// Button data component.
    public ButtonData Button { get; }

    /// IR data component.
    public IrData Ir { get; }

    /// Status info data component.
    public StatusData Status { get; }

    /// The RAW (unprocessesed) extension data reported by the Wii Remote.  This could
    /// be used for debugging new / undocumented extension controllers.
    public ReadOnlyCollection<byte>? RawExtension { get; private set; }

    public WiimoteType Type { get; private set; }

    /// <summary>
    /// The serial number of the Wiimote device
    /// </summary>
    public string Serial { get; }

    /// <summary>
    /// The HID device handle, used to send and receive commands
    /// </summary>
    public IDevice Device { get; }

    private RegisterReadData? _currentReadData;

    private InputDataType _lastReportType = InputDataType.ReportButtons;
    private bool _expectingStatusReport = false;

    /// True if a Wii Motion Plus is attached to the Wii Remote, and it
    /// has NOT BEEN ACTIVATED.  When the WMP is activated this value is
    /// false.  This is only updated when WMP state is requested from
    /// Wii Remote registers (see: RequestIdentifyWiiMotionPlus())
    public bool WmpAttached { get; private set; }

    /// The current extension connected to the Wii Remote.  This is only updated
    /// when the Wii Remote reports an extension change (this should update
    /// automatically).
    public ExtensionController CurrentExt { get; private set; } = ExtensionController.None;

    private byte[] _interleavedDataBuffer = new byte[18];
    private bool _expectingSecondInterleavedPacket;

    private bool _expectingWiiMotionPlusSwitch;

    public Wiimote(WiimoteType type, string serial, IDevice device)
    {
        Type = type;
        Serial = serial;
        Device = device;

        Accel = new AccelData(this);
        Button = new ButtonData(this);
        Ir = new IrData(this);
        Status = new StatusData(this);
        _extension = null;
        _expectingWiiMotionPlusSwitch = false;

        //RequestIdentifyWiiMotionPlus(); // why not?
    }

    private static byte[] ID_InactiveMotionPlus = new byte[] {0x00, 0x00, 0xA6, 0x20, 0x00, 0x05};

    private void RespondIdentifyWiiMotionPlus(byte[] data)
    {
        if (data.Length != ID_InactiveMotionPlus.Length)
        {
            WmpAttached = false;
            return;
        }

        if (data[0] == 0x01) // This is a weird inconsistency with some Wii Remote Pluses.  They don't have the -TR suffix
            Type = WiimoteType
                .WiimotePlus; // or a different PID as an identifier.  Instead they have a different WMP extension identifier.
        // It occurs on some of the oldest Wii Remote Pluses available (pre-2012).

        for (int x = 0; x < data.Length; x++)
        {
            // [x != 4] is necessary because byte 5 of the identifier changes based on the state of the remote
            // It is 0x00 on startup, 0x04 when deactivated, 0x05 when deactivated nunchuck passthrough,
            // and 0x07 when deactivated classic passthrough
            //
            // [x != 0] is necessary due to the inconsistency noted above.
            if (x != 4 && x != 0 && data[x] != ID_InactiveMotionPlus[x])
            {
                WmpAttached = false;
                return;
            }
        }

        WmpAttached = true;
    }

    private const long IdActiveMotionPlus = 0x0000A4200405;
    private const long IdActiveMotionPlusNunchuck = 0x0000A4200505;
    private const long IdActiveMotionPlusClassic = 0x0000A4200705;
    private const long IdNunchuck = 0x0000A4200000;
    private const long IdNunchuck2 = 0xFF00A4200000;
    private const long IdClassic = 0x0000A4200101;
    private const long IdClassicPro = 0x0100A4200101;
    private const long IdWiiUPro = 0x0000A4200120;
    private const long IdGuitar = 0x0000A4200103;


    private void RespondIdentifyExtension(byte[] data)
    {
        if (data.Length != 6)
            return;

        byte[] resized = new byte[8];
        for (int x = 0; x < 6; x++) resized[x] = data[5 - x];
        long val = BitConverter.ToInt64(resized, 0);

        // Disregard bytes 0 and 5 - see RespondIdentifyWiiMotionPlus()
        if ((val | 0xff000000ff00) == (IdActiveMotionPlus | 0xff000000ff00))
        {
            CurrentExt = ExtensionController.MotionPlus;
            if (_extension == null || _extension.GetType() != typeof(MotionPlusData))
                _extension = new MotionPlusData(this);
        }
        else if (val == IdActiveMotionPlusNunchuck)
        {
            CurrentExt = ExtensionController.MotionPlusNunchuck;
            _extension = null;
        }
        else if (val == IdActiveMotionPlusClassic)
        {
            CurrentExt = ExtensionController.MotionPlusClassic;
            _extension = null;
        }
        else if (val == IdClassicPro)
        {
            CurrentExt = ExtensionController.ClassicPro;
            _extension = null;
        }
        else if (val == IdNunchuck || val == IdNunchuck2)
        {
            CurrentExt = ExtensionController.Nunchuck;
            if (_extension == null || _extension.GetType() != typeof(NunchuckData))
                _extension = new NunchuckData(this);
        }
        else if (val == IdClassic)
        {
            CurrentExt = ExtensionController.Classic;
            if (_extension == null || _extension.GetType() != typeof(ClassicControllerData))
                _extension = new ClassicControllerData(this);
        }
        else if (val == IdWiiUPro)
        {
            CurrentExt = ExtensionController.WiiUPro;
            Type = WiimoteType.ProController;
            if (_extension == null || _extension.GetType() != typeof(WiiUProData))
                _extension = new WiiUProData(this);
        }
        else if (val == IdGuitar)
        {
            CurrentExt = ExtensionController.Guitar;
            if (_extension == null || _extension.GetType() != typeof(GuitarData))
                _extension = new GuitarData(this);
        }
        else
        {
            CurrentExt = ExtensionController.None;
            _extension = null;
        }
    }

    #region Setups

    /// \brief Performs a series of coperations to initialize the IR camera.
    /// \param type The IR Report type you want to use.
    /// \return If all IR setup commands were successfully sent to the Wii Remote.
    /// 
    /// This performs the following steps in order to set up the IR camera:
    /// 1. Enable IR Camera (Send \c 0x04 to Output Report \c 0x13)
    /// 2. Enable IR Camera 2 (Send \c 0x04 to Output Report \c 0x1a)
    /// 3. Write 0x08 to register \c 0xb00030
    /// 4. Write Sensitivity Block 1 to registers at \c 0xb00000
    /// 5. Write Sensitivity Block 2 to registers at \c 0xb0001a
    /// 6. Write Mode Number to register \c 0xb00033
    /// 7. Write 0x08 to register \c 0xb00030 (again)
    /// 8. Update the Wii Remote's data reporting mode based on \c type
    public async Task<bool> SetupIrCamera(IrDataType type = IrDataType.Extended)
    {
        // 1. Enable IR Camera (Send 0x04 to Output Report 0x13)
        // 2. Enable IR Camera 2 (Send 0x04 to Output Report 0x1a)
        await SendIrCameraEnable(true);
        // 3. Write 0x08 to register 0xb00030
        await SendRegisterWriteRequest(RegisterType.Control, 0xb00030, new byte[] {0x08});
        // 4. Write Sensitivity Block 1 to registers at 0xb00000
        // Wii sensitivity level 3:
        // 02 00 00 71 01 00 aa 00 64
        // High Sensitivity:
        // 00 00 00 00 00 00 90 00 41
        await SendRegisterWriteRequest(RegisterType.Control, 0xb00000,
            new byte[] {0x02, 0x00, 0x00, 0x71, 0x01, 0x00, 0xaa, 0x00, 0x64});
        //new byte[] {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x90, 0x00, 0x41});
        // 5. Write Sensitivity Block 2 to registers at 0xb0001a
        // Wii sensitivity level 3: 
        // 63 03
        // High Sensitivity:
        // 40 00
        await SendRegisterWriteRequest(RegisterType.Control, 0xb0001a, new byte[] {0x63, 0x03});
        // 6. Write Mode Number to register 0xb00033
        await SendRegisterWriteRequest(RegisterType.Control, 0xb00033, new byte[] {(byte) type});
        // 7. Write 0x08 to register 0xb00030 (again)
        await SendRegisterWriteRequest(RegisterType.Control, 0xb00030, new byte[] {0x08});

        switch (type)
        {
            case IrDataType.Basic:
                await SendDataReportMode(InputDataType.ReportButtonsAccelIr10Ext6);
                break;
            case IrDataType.Extended:
                await SendDataReportMode(InputDataType.ReportButtonsAccelIr12);
                break;
            case IrDataType.Full:
                await SendDataReportMode(InputDataType.ReportInterleaved);
                break;
        }

        return true;
    }

    /// \brief Attempts to identify whether or not a Wii Motion Plus is connected, but NOT activated.
    /// \sa RequestIdentifyExtension(), ActivateWiiMotionPlus()
    /// \return If the Identification request was successfully sent to the Wii Remote.
    ///
    /// When the Wii Remote reports back if a Wii Motion Plus is connected, wmp_attached will be updated.
    /// \note If the Wii Motion Plus is activated (using ActivateWiiMotionPlus()) the Wii Remote will report false
    public async Task<bool> RequestIdentifyWiiMotionPlus()
    {
        uint res;
        res = await SendRegisterReadRequest(RegisterType.Control, 0xA600FA, 6, RespondIdentifyWiiMotionPlus);
        return res > 0;
    }

    /// \brief Attempts to identify what (if any) ACTIVE extension is currently connected to the Wii Remote.
    /// \sa RequestIdentifyWiiMotionPlus(), ActivateExtension()
    /// \return If the identification request was successfully sent to the Wii Remote.
    ///
    /// When the Wii Remote reports back what extension is connected, current_ext will be updated.
    /// \note If the Extension has not been activated yet (using ActivateExtension) the Wii Remote will report ExtensionController::NONE.
    private async Task<bool> RequestIdentifyExtension()
    {
        uint res = await SendRegisterReadRequest(RegisterType.Control, 0xA400FA, 6, RespondIdentifyExtension);
        return res > 0;
    }

    /// \brief Attempts to activate the Wii Motion Plus.
    /// \sa RequestIdentifyWiiMotionPlus(), wmp_attached
    /// \return If the activation request was successfully sent to the Wii Remote.
    ///
    /// When the Wii Remote reports that the Wii Motion Plus has been activated, current_ext will be updated to ExtensionController::MOTIONPLUS
    /// If there is no Wii Motion Plus connected, undefined behavior may occur on the Wii Remote.
    public async Task<bool> ActivateWiiMotionPlus()
    {
        // if (!WmpAttached)
        // Debug.LogWarning("There is a request to activate the Wii Motion Plus even though it has not been confirmed to exist!  Trying anyway.");

        // Initialize the Wii Motion Plus by writing 0x55 to register 0xA600F0
        await SendRegisterWriteRequest(RegisterType.Control, 0xA600F0, new byte[] {0x55});

        // Activate the Wii Motion Plus as the active extension by writing 0x04 to register 0xA600FE
        // This does 3 things:
        // 1. A status report (0x20) will be sent, which indicates that an extension has been
        //    plugged in - IF there is no extension plugged into the passthrough port.
        // 2. The standard extension identifier at 0xA400FA now reads 00 00 A4 20 04 05
        // 3. Extension reports now contain Wii Motion Plus data.
        await SendRegisterWriteRequest(RegisterType.Control, 0xA600FE, new byte[] {0x04});

        CurrentExt = ExtensionController.MotionPlus;
        if (_extension == null || _extension.GetType() != typeof(MotionPlusData))
            _extension = new MotionPlusData(this);
        _expectingWiiMotionPlusSwitch = true;

        return true;
    }

    public async Task<bool> DeactivateWiiMotionPlus()
    {
        // if (CurrentExt != ExtensionController.MotionPlus && CurrentExt != ExtensionController.MotionPlusClassic && CurrentExt != ExtensionController.MotionPlusNunchuck)
        // Debug.LogWarning("There is a request to deactivate the Wii Motion Plus even though it has not been activated!  Trying anyway.");
        uint res = await SendRegisterWriteRequest(RegisterType.Control, 0xA400F0, new byte[] {0x55});
        return res > 0;
    }

    /// \brief Attempts to activate any connected extension controller
    /// \sa RequestIdentifyExtension(), StatusData::ext_connected
    /// \return If the activation request was successfully sent to the Wii Remote.
    ///
    /// If there is no extension connected, undefined behavior may occur on the Wii Remote.
    private async Task<bool> ActivateExtension()
    {
        // if (!Status.ExtConnected)
        // Debug.LogWarning("There is a request to activate an Extension controller even though it has not been confirmed to exist!  Trying anyway.");

        // 1. Initialize the Extension by writing 0x55 to register 0xA400F0
        await SendRegisterWriteRequest(RegisterType.Control, 0xA400F0, new byte[] {0x55});

        // 2. Activate the Extension by writing 0x00 to register 0xA400FB
        await SendRegisterWriteRequest(RegisterType.Control, 0xA400FB, new byte[] {0x00});
        return true;
    }

    #endregion

    #region Write

    /// \brief Sends a generic block of data to the Wii Remote using the specified Output Report.
    /// \param type The output report type you would like to send
    /// \param data The raw data you would like to send using the specified \c type.
    /// \return On success, the total size of the data written, -1 if HIDApi reports an error, or &lt; -1 if there is an invalid input.
    /// 
    /// This should only be used to send custom data to the Wii Remote that is currently unimplemented by WiimoteApi.
    /// In any average use case you can use any of the higher-level output functions provided by WiimoteApi.
    ///
    /// The Wii Remote rumble settings are also updated based on RumbleOn.
    public async Task<uint> SendWithType(OutputDataType type, byte[] data)
    {
        byte[] final = new byte[data.Length + 1];
        final[0] = (byte) type;

        for (int x = 0; x < data.Length; x++)
            final[x + 1] = data[x];

        if (RumbleOn)
            final[1] |= 0x01;

        return await Device.WriteAsync(final).ConfigureAwait(false);
    }

    /// \brief Updates the Player LEDs on the bottom of the Wii Remote
    /// \param led1,led2,led3,led4 If this LED should be turned on
    /// \return On success, the total size of the data written (> 0), &lt;= 0 on failure.
    /// \sa SendWithType(OutputDataType, byte[])
    /// \note More than one LED can be on at a time, but this may confuse players.  Use this with caution.
    ///
    /// If you are willing to use up a lot of bluetooth bandwith, pulse-width modulation (PWM) is also possible
    /// to lower the intensity of the LEDs.
    public async Task<uint> SendPlayerLed(bool led1, bool led2, bool led3, bool led4)
    {
        byte mask = 0;
        if (led1) mask |= 0x10;
        if (led2) mask |= 0x20;
        if (led3) mask |= 0x40;
        if (led4) mask |= 0x80;

        return await SendWithType(OutputDataType.Led, new byte[] {mask});
    }

    /// \brief Sets the Data Reporting mode of the Wii Remote.
    /// \param mode The data reporting mode desired.  This can be any InputDataType except for
    ///         InputDataType::STATUS_INFO, InputDataType::READ_MEMORY_REGISTERS, or InputDataType::ACKNOWLEDGE_OUTPUT_REPORT.
    ///         Said data types are not data reporting modes so it doesn't make sense to use them here.
    /// \return On success, the total size of the data written (> 0), &lt;= 0 on failure.
    /// \sa SendWithType(OutputDataType, byte[])
    public async Task<uint> SendDataReportMode(InputDataType mode)
    {
        if (mode is InputDataType.StatusInfo or InputDataType.ReadMemoryRegisters
            or InputDataType.AcknowledgeOutputReport)
        {
            // Debug.LogError("Passed " + mode.ToString() + " to SendDataReportMode!");
            throw new ArgumentException("Passed " + mode + " to SendDataReportMode!");
        }

        _lastReportType = mode;

        _expectingSecondInterleavedPacket = false;

        return await SendWithType(OutputDataType.DataReportMode, new byte[] {0x00, (byte) mode});
    }

    private async Task<uint> SendIrCameraEnable(bool enabled)
    {
        byte[] mask = {(byte) (enabled ? 0x04 : 0x00)};

        uint first = await SendWithType(OutputDataType.IrCameraEnable, mask);

        uint second = await SendWithType(OutputDataType.IrCameraEnable2, mask);

        return first + second; // success
    }

    private async Task<uint> SendSpeakerEnabled(bool enabled)
    {
        byte[] mask = {(byte) (enabled ? 0x04 : 0x00)};

        return await SendWithType(OutputDataType.SpeakerEnable, mask);
    }

    private async Task<uint> SendSpeakerMuted(bool muted)
    {
        byte[] mask = {(byte) (muted ? 0x04 : 0x00)};

        return await SendWithType(OutputDataType.SpeakerMute, mask);
    }

    /// \brief Request a Wii Remote Status update.
    /// \return On success > 0, &lt;= 0 on failure.
    /// \sa Status, StatusData
    ///
    /// This will update the data in Status when the Wii Remote reports back.
    public async Task<uint> SendStatusInfoRequest()
    {
        _expectingStatusReport = true;
        return await SendWithType(OutputDataType.StatusInfoRequest, new byte[] {0x00});
    }

    /// \brief Requests the Wii Remote to report data from its internal registers.
    /// \param type The type of register you would like to read from
    /// \param offset The starting offset of the block of data you would like to read
    /// \param size The size of the block of data you would like to read
    /// \param Responder This will be called when the Wii Remote finishes reporting the requested data.
    /// \return On success, > 0, &lt;= 0 on failure.
    /// \sa SendRegisterWriteRequest(RegisterType, int, byte[])
    ///
    /// \warning Do not attempt to read from the registers when another read is pending (that is, data is being
    ///          recieved by the Wii Remote).  If you attempt to do this, the new read request will be ignored.
    /// 
    /// Reading from the Wii Remote's internal registers can give important data not available through normal output reports.
    /// This can, for example, be used to read saved Mii data from the Wii Remote's EEPROM registers.  It is also used by some
    /// of WiimoteApi's setup functions.
    /// 
    /// If you use this incorrectly (for example, if you attempt to read from an invalid block of data), \c Responder will not be called.
    public async Task<uint> SendRegisterReadRequest(RegisterType type, int offset, int size, ReadResponder responder)
    {
        if (_currentReadData != null)
        {
            // Debug.LogWarning("Aborting read request; There is already a read request pending!");
            throw new InvalidOperationException("Aborting read request; There is already a read request pending!");
        }


        _currentReadData = new RegisterReadData(offset, size, responder);

        byte addressSelect = (byte) type;
        byte[] offsetArr = IntToBigEndian(offset, 3);
        byte[] sizeArr = IntToBigEndian(size, 2);

        byte[] total =
        {
            addressSelect, offsetArr[0], offsetArr[1], offsetArr[2],
            sizeArr[0], sizeArr[1]
        };

        return await SendWithType(OutputDataType.ReadMemoryRegisters, total);
    }

    /// \brief Attempts to write a block of data to the Wii Remote's internal registers.
    /// \param type The type of register you would like to write to
    /// \param offset The starting offset of the block of data you would like to write
    /// \param data Data to write to registers at \c offset.  This must have a maximum length of 16.
    /// \return On success, > 0, &lt;= 0 on failure.
    /// \warning If data.Length > 16 the write request will be ignored.
    /// 
    /// Writing to the Wii Remote's internal registers allows you to access advanced functions of the remote, such as
    /// the speakers or the IR camera.  It is used by some of WiimoteApi's setup functions (SetupIRCamera()
    /// for example).
    /// 
    /// If you use this incorrectly (for example, if you attempt to write to a read-only register) the Wii Remote handles this gracefully
    /// and nothing happens.
    public async Task<uint> SendRegisterWriteRequest(RegisterType type, int offset, byte[] data)
    {
        if (data.Length > 16) throw new ArgumentException("Length is greater than 16");

        byte addressSelect = (byte) type;
        byte[] offsetArr = IntToBigEndian(offset, 3);

        byte[] total = new byte[21];
        total[0] = addressSelect;
        for (int x = 0; x < 3; x++) total[x + 1] = offsetArr[x];
        total[4] = (byte) data.Length;
        for (int x = 0; x < data.Length; x++) total[x + 5] = data[x];

        return await SendWithType(OutputDataType.WriteMemoryRegisters, total);
    }

    #endregion

    #region Read

    /// \brief Reads and interprets data reported by the Wii Remote.
    /// \return On success, > 0, &lt; 0 on failure, 0 if nothing has been recieved.
    /// 
    /// Wii Remote reads function similarly to a Queue, in FIFO (first in, first out) order.
    /// For example, if two reports were sent since the last \c ReadWiimoteData() call,
    /// this call will only read and interpret the first of those two (and "pop" it off
    /// of the queue).  So, in order to make sure you don't fall behind the Wiimote's update
    /// frequency, you can do something like this (in a game loop for example):
    ///
    /// \code
    /// Wii Remote wiimote;
    /// int ret;
    /// do
    /// {
    ///     ret = wiimote.ReadWiimoteData();
    /// } while (ret > 0);
    /// \endcode
    public async Task<uint> ReadWiimoteData()
    {
        TransferResult result = await Device.ReadAsync().ConfigureAwait(false);

        int typesize = GetInputDataTypeSize((InputDataType) result.Data[0]);
        byte[] data = new byte[typesize];
        for (int x = 0; x < data.Length; x++)
            data[x] = result.Data[x + 1];

        // Variable names used throughout the switch/case block
        byte[] buttons;
        byte[] accel;
        byte[]? ext = null;
        byte[] ir;

        switch ((InputDataType) result.Data[0]) // buf[0] is the output ID byte
        {
            case InputDataType.StatusInfo: // done.
                buttons = new byte[] {data[0], data[1]};
                byte flags = data[2];
                byte batteryLevel = data[5];

                Button.InterpretData(buttons);

                bool oldExtConnected = Status.ExtConnected;

                byte[] total = new byte[] {flags, batteryLevel};
                Status.InterpretData(total);

                if (_expectingStatusReport)
                {
                    _expectingStatusReport = false;
                }
                else // We haven't requested any data report type, meaning a controller has connected.
                {
                    await SendDataReportMode(
                        _lastReportType); // If we don't update the data report mode, no updates will be sent
                }

                if (Status.ExtConnected != oldExtConnected && Type != WiimoteType.ProController)
                {
                    if (Status.ExtConnected) // The Wii Remote doesn't allow reading from the extension identifier
                    {
                        // when nothing is connected.
                        // Debug.Log("An extension has been connected.");
                        if (CurrentExt != ExtensionController.MotionPlus)
                        {
                            await ActivateExtension();
                            await RequestIdentifyExtension(); // Identify what extension was connected.
                        }
                        else
                            _expectingWiiMotionPlusSwitch = false;
                    }
                    else
                    {
                        if (!_expectingWiiMotionPlusSwitch)
                            CurrentExt = ExtensionController.None;
                        // Debug.Log("An extension has been disconnected.");
                    }
                }

                break;
            case InputDataType.ReadMemoryRegisters: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                if (_currentReadData == null)
                {
                    // Debug.LogWarning("Recived Register Read Report when none was expected.  Ignoring.");
                    return result.BytesTransferred;
                }

                byte size = (byte) ((data[2] >> 4) + 0x01);
                byte error = (byte) (data[2] & 0x0f);
                // Error 0x07 means reading from a write-only register
                // Offset 0xa600fa is for the Wii Motion Plus.  This error code can be expected behavior in this case.
                if (error == 0x07)
                {
                    // if(_currentReadData.Offset != 0xa600fa)
                    // Debug.LogError("Wiimote reports Read Register error 7: Attempting to read from a write-only register ("+_currentReadData.Offset.ToString("x")+").  Aborting read.");

                    _currentReadData = null;
                    return result.BytesTransferred;
                }

                // lowOffset is reversed because the Wii Remote reports are in Big Endian order
                ushort lowOffset = BitConverter.ToUInt16(new byte[] {data[4], data[3]}, 0);
                ushort expected = (ushort) _currentReadData.ExpectedOffset;
                // if (expected != lowOffset)
                // Debug.LogWarning("Expected Register Read Offset (" + expected + ") does not match reported offset from Wii Remote (" + lowOffset + ")");
                byte[] read = new byte[size];
                for (int x = 0; x < size; x++)
                    read[x] = data[x + 5];

                _currentReadData.AppendData(read);
                if (_currentReadData.ExpectedOffset >= _currentReadData.Offset + _currentReadData.Size)
                    _currentReadData = null;

                break;
            case InputDataType.AcknowledgeOutputReport:
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);
                // TODO: doesn't do any actual error handling, or do any special code about acknowledging the output report.
                break;
            case InputDataType.ReportButtons: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);
                break;
            case InputDataType.ReportButtonsAccel: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                accel = new byte[] {data[0], data[1], data[2], data[3], data[4]};
                Accel.InterpretData(accel);
                break;
            case InputDataType.ReportButtonsExt8: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                ext = new byte[8];
                for (int x = 0; x < ext.Length; x++)
                    ext[x] = data[x + 2];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportButtonsAccelIr12: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                accel = new byte[] {data[0], data[1], data[2], data[3], data[4]};
                Accel.InterpretData(accel);

                ir = new byte[12];
                for (int x = 0; x < 12; x++)
                    ir[x] = data[x + 5];
                Ir.InterpretData(ir);
                break;
            case InputDataType.ReportButtonsExt19: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                ext = new byte[19];
                for (int x = 0; x < ext.Length; x++)
                    ext[x] = data[x + 2];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportButtonsAccelExt16: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                accel = new byte[] {data[0], data[1], data[2], data[3], data[4]};
                Accel.InterpretData(accel);

                ext = new byte[16];
                for (int x = 0; x < ext.Length; x++)
                    ext[x] = data[x + 5];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportButtonsIr10Ext9: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                ir = new byte[10];
                for (int x = 0; x < 10; x++)
                    ir[x] = data[x + 2];
                Ir.InterpretData(ir);

                ext = new byte[9];
                for (int x = 0; x < 9; x++)
                    ext[x] = data[x + 12];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportButtonsAccelIr10Ext6: // done.
                buttons = new byte[] {data[0], data[1]};
                Button.InterpretData(buttons);

                accel = new byte[] {data[0], data[1], data[2], data[3], data[4]};
                Accel.InterpretData(accel);

                ir = new byte[10];
                for (int x = 0; x < 10; x++)
                    ir[x] = data[x + 5];
                Ir.InterpretData(ir);

                ext = new byte[6];
                for (int x = 0; x < 6; x++)
                    ext[x] = data[x + 15];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportExt21: // done.
                ext = new byte[21];
                for (int x = 0; x < ext.Length; x++)
                    ext[x] = data[x];

                if (_extension != null)
                    _extension.InterpretData(ext);
                break;
            case InputDataType.ReportInterleaved:
                if (!_expectingSecondInterleavedPacket)
                {
                    _expectingSecondInterleavedPacket = true;
                    _interleavedDataBuffer = data;
                } /* else if(WiimoteManager.Debug_Messages) {
                    Debug.LogWarning(
                        "Recieved two REPORT_INTERLEAVED ("+InputDataType.ReportInterleaved.ToString("x")+") reports in a row!  "
                        + "Expected REPORT_INTERLEAVED_ALT ("+InputDataType.ReportInterleavedAlt.ToString("x")+").  Ignoring!"
                    );
                }*/

                break;
            case InputDataType.ReportInterleavedAlt:
                if (_expectingSecondInterleavedPacket)
                {
                    _expectingSecondInterleavedPacket = false;

                    buttons = new byte[] {data[0], data[1]};
                    Button.InterpretData(buttons);

                    byte[] ir1 = new byte[18];
                    byte[] ir2 = new byte[18];

                    for (int x = 0; x < 18; x++)
                    {
                        ir1[x] = _interleavedDataBuffer[x + 3];
                        ir2[x] = data[x + 3];
                    }

                    Ir.InterpretDataInterleaved(ir1, ir2);
                    Accel.InterpretDataInterleaved(_interleavedDataBuffer, data);
                }

                /*else if(WiimoteManager.Debug_Messages)
                {
                    Debug.LogWarning(
                        "Recieved two REPORT_INTERLEAVED_ALT ("+InputDataType.ReportInterleavedAlt.ToString("x")+") reports in a row!  "
                        + "Expected REPORT_INTERLEAVED ("+InputDataType.ReportInterleaved.ToString("x")+").  Ignoring!"
                    );
                }*/
                break;
        }

        if (ext == null)
            RawExtension = null;
        else
            RawExtension = Array.AsReadOnly(ext);

        return result.BytesTransferred;
    }

    /// The size, in bytes, of a given Wii Remote InputDataType when reported by the Wiimote.
    ///
    /// This is at most 21 bytes.
    public static int GetInputDataTypeSize(InputDataType type)
    {
        switch (type)
        {
            case InputDataType.StatusInfo:
                return 6;
            case InputDataType.ReadMemoryRegisters:
                return 21;
            case InputDataType.AcknowledgeOutputReport:
                return 4;
            case InputDataType.ReportButtons:
                return 2;
            case InputDataType.ReportButtonsAccel:
                return 5;
            case InputDataType.ReportButtonsExt8:
                return 10;
            case InputDataType.ReportButtonsAccelIr12:
                return 17;
            case InputDataType.ReportButtonsExt19:
                return 21;
            case InputDataType.ReportButtonsAccelExt16:
                return 21;
            case InputDataType.ReportButtonsIr10Ext9:
                return 21;
            case InputDataType.ReportButtonsAccelIr10Ext6:
                return 21;
            case InputDataType.ReportExt21:
                return 21;
            case InputDataType.ReportInterleaved:
                return 21;
            case InputDataType.ReportInterleavedAlt:
                return 21;
        }

        return 0;
    }

    #endregion

    // ------------- UTILITY ------------- //
    public static byte[] IntToBigEndian(int input, int len)
    {
        byte[] intBytes = BitConverter.GetBytes(input);
        Array.Resize(ref intBytes, len);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);

        return intBytes;
    }
}