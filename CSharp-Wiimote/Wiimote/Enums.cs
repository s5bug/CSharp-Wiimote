namespace Wiimote;

/// \brief A type of data storage register that can be read from / written to.
/// \sa Wiimote::SendRegisterWriteRequest(RegisterType, int, byte[]), Wiimote::SendRegisterReadRequest(RegisterType, int, int, ReadResponder)
public enum RegisterType
{
    /// The Wii Remote's 16kB generic EEPROM memory module.  This is used to store calubration data
    /// as well as Mii block data from the Mii channel.
    Eeprom = 0x00,
    /// The Wii Remote's control registers, used for managing the Wii Remote's peripherals (such as extension
    /// controllers, the speakers, and the IR camera).
    Control = 0x04
}

/// A so-called output data type represents all data that can be sent from the host to the Wii Remote.
/// This information is used by the remote to change its internal read/write remote.
public enum OutputDataType
{
    Led = 0x11,
    DataReportMode = 0x12,
    IrCameraEnable = 0x13,
    SpeakerEnable = 0x14,
    StatusInfoRequest = 0x15,
    WriteMemoryRegisters = 0x16,
    ReadMemoryRegisters = 0x17,
    SpeakerData = 0x18,
    SpeakerMute = 0x19,
    IrCameraEnable2 = 0x1A
}

/// \brief A so-called input data type represents all data that can be sent from the Wii Remote to the host.
///        This information is used by the host as basic controller data from the Wii Remote.
/// \note All REPORT_ types represent the actual data types that can be sent from the contoller.
public enum InputDataType
{
    StatusInfo = 0x20,
    ReadMemoryRegisters = 0x21,
    AcknowledgeOutputReport = 0x22,
    /// Data Report Mode: Buttons
    ReportButtons = 0x30,
    /// Data Report Mode: Buttons, Accelerometer
    ReportButtonsAccel = 0x31,
    /// Data Report Mode: Buttons, 8 Extension bytes
    ReportButtonsExt8 = 0x32,
    /// Data Report Mode: Buttons, Accelerometer, 12 IR bytes (IRDataType::EXTENDED)
    ReportButtonsAccelIr12 = 0x33,
    /// Data Report Mode: Buttons, 19 Extension Bytes
    ReportButtonsExt19 = 0x34,
    /// Data Report Mode: Buttons, Acceleromter, 16 Extension Bytes
    ReportButtonsAccelExt16 = 0x35,
    /// Data Report Mode: Buttons, 10 IR Bytes (IRDataType::BASIC), 9 Extension Bytes
    ReportButtonsIr10Ext9 = 0x36,
    /// Data Report Mode: Buttons, Accelerometer, 10 IR Bytes (IRDataType::BASIC), 6 Extension Bytes
    ReportButtonsAccelIr10Ext6 = 0x37,
    /// Data Report Mode: 21 Extension Bytes
    ReportExt21 = 0x3d,
    /// Data Report Mode: (Interleaved) Buttons, Accelerometer, 36 IR Bytes (IRDataType::FULL)
    ReportInterleaved = 0x3e,
    /// Data Report Mode: (Interleaved) Buttons, Accelerometer, 36 IR Bytes (IRDataType::FULL) Alternate
    /// 
    /// \note This is functionally identical to REPORT_INTERLEAVED.
    ReportInterleavedAlt = 0x3f
}

/// These are the 3 types of IR data accepted by the Wii Remote.  They offer more
/// or less IR data in exchange for space for other data (such as extension
/// controllers or accelerometer data).
///
/// For each IR data type you can only use certain InputDataType reports in
/// order to recieve the data.
public enum IrDataType
{
    /// \brief 10 bytes of data.  Contains position data for each dot only.
    /// 
    /// Works with reports InputDataType::REPORT_BUTTONS_IR10_EXT9 and InputDataType::REPORT_BUTTONS_ACCEL_IR10_EXT6.
    Basic = 1,
    /// \brief 12 bytes of data.  Contains position and size data for each dot.
    /// 
    /// Works with report InputDataType::REPORT_BUTTONS_ACCEL_IR12 only.
    Extended = 3,
    /// \brief 36 bytes of data.  Contains position, size, bounding box, and intensity data for each dot.
    ///
    /// Works with interleaved report InputDataType::REPORT_INTERLEAVED / InputDataType::REPORT_INTERLEAVED_ALT only.
    Full = 5
}

public enum ExtensionController
{
    /// No Extension Controller is connected.
    None, 
    /// A Nunchuck Controller
    Nunchuck, 
    /// A Classic Controller
    Classic, 
    /// A Classic Controller Pro.
    ClassicPro,
    /// A Wii U Pro Controller.  Although a Wii U Pro Controller is not technically an extension controller it is treated
    /// like one when communicating to a bluetooth host.
    WiiUPro, 
    /// An activated Wii Motion Plus with no extension controllers in passthrough mode.
    MotionPlus,
    /// An activated Wii Motion Plus with a Nunchuck in passthrough mode.
    /// \warning Nunchuck passthrough is currently not supported.
    MotionPlusNunchuck, 
    /// An activated Wii Motion Plus with a Classic Controller in passthrough mode. 
    /// \warning Classic Controller passthrough is currently not supported
    MotionPlusClassic,
    /// Guitar Hero controller
    Guitar
}

public enum AccelCalibrationStep {
    AButtonUp = 0,
    ExpansionUp = 1,
    LeftSideUp = 2
}

/// These different Wii Remote Types are used to differentiate between different devices that behave like the Wii Remote.
public enum WiimoteType {
    /// The original Wii Remote (Name: RVL-CNT-01).  This includes all Wii Remotes manufactured for the original Wii.
    Wiimote, 
    /// The new Wii Remote Plus (Name: RVL-CNT-01-TR).  Wii Remote Pluses are now standard with Wii U consoles and come
    /// with a built-in Wii Motion Plus extension.
    WiimotePlus, 
    /// The Wii U Pro Controller (Name: RVL-CNT-01-UC) behaves identically to a Wii Remote with a Classic Controller
    /// attached.  Obviously the Pro Controller does not support IR so those features will not work.
    ProController
}
