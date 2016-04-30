using Microsoft.Win32.SafeHandles;
using ODIF;
using ODIF.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SC360
{
    public partial class X360Device : ScpDevice
    {
        private const String DS3_BUS_CLASS_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        private const int CONTROLLER_OFFSET = 1 + 10; // Device 0 is the virtual USB hub itself, and we leave devices 1-10 available for other software (like the Scarlet.Crush DualShock driver itself)

        private int firstController = 1 + 10;
        // Device 0 is the virtual USB hub itself, and we can leave more available for other software (like the Scarlet.Crush DualShock driver)
        public int FirstController
        {
            get { return firstController; }
            set { firstController = value > 0 ? value : 1; }
        }

        protected Int32 Scale(Int32 Value, Boolean Flip)
        {
            Value -= 0x80;

            if (Value == -128) Value = -127;
            if (Flip) Value *= -1;

            return (Int32)((float)Value * 258.00787401574803149606299212599f);
        }


        public X360Device()
            : base(DS3_BUS_CLASS_GUID)
        {
            InitializeComponent();
        }

        public X360Device(IContainer container)
            : base(DS3_BUS_CLASS_GUID)
        {
            container.Add(this);

            InitializeComponent();
        }


        /* public override Boolean Open(int Instance = 0)
        {
            if (base.Open(Instance))
            {
            }

            return true;
        } */

        public override Boolean Open(String DevicePath)
        {
            m_Path = DevicePath;
            m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;

            if (GetDeviceHandle(m_Path))
            {
                m_IsActive = true;
            }

            return true;
        }

        public override Boolean Start()
        {
            if (IsActive)
            {
            }

            return true;
        }

        public override Boolean Stop()
        {
            if (IsActive)
            {
                //Unplug(0);
            }

            return base.Stop();
        }

        public override Boolean Close()
        {
            if (IsActive)
            {
                Unplug(0);
            }

            return base.Close();
        }


        internal void Parse(x360device Data, Byte[] Output, int device)
        {
            Output[0] = 0x1C;
            Output[4] = (Byte)(device + firstController);
            Output[9] = 0x14;

            for (int i = 10; i < Output.Length; i++)
            {
                Output[i] = 0;
            }
            
            if (Data.Back.Value) Output[10] |= (Byte)(1 << 5); // Back
            if (Data.LS.Value) Output[10] |= (Byte)(1 << 6); // Left  Thumb
            if (Data.RS.Value) Output[10] |= (Byte)(1 << 7); // Right Thumb
            if (Data.Start.Value) Output[10] |= (Byte)(1 << 4); // Start

            if (Data.DUp.Value) Output[10] |= (Byte)(1 << 0); // Up
            if (Data.DRight.Value) Output[10] |= (Byte)(1 << 3); // Down
            if (Data.DDown.Value) Output[10] |= (Byte)(1 << 1); // Right
            if (Data.DLeft.Value) Output[10] |= (Byte)(1 << 2); // Left

            if (Data.LB.Value) Output[11] |= (Byte)(1 << 0); // Left  Shoulder
            if (Data.RB.Value) Output[11] |= (Byte)(1 << 1); // Right Shoulder

            if (Data.Y.Value) Output[11] |= (Byte)(1 << 7); // Y
            if (Data.B.Value) Output[11] |= (Byte)(1 << 5); // B
            if (Data.A.Value) Output[11] |= (Byte)(1 << 4); // A
            if (Data.X.Value) Output[11] |= (Byte)(1 << 6); // X

            if (Data.Guide.Value) Output[11] |= (Byte)(1 << 2); // Guide     

            Output[12] = (byte)Math.Floor(Convert.ToDouble(Data.LT.Value) * 255);
            Output[13] = (byte)Math.Floor(Convert.ToDouble(Data.RT.Value) * 255);

            Int32 ThumbLX = (int)Math.Floor(Convert.ToDouble(Data.LSx.Value) * 32512);
            Int32 ThumbLY = (int)Math.Floor(Convert.ToDouble(Data.LSy.Value) * -32512);
            Int32 ThumbRX = (int)Math.Floor(Convert.ToDouble(Data.RSx.Value) * 32512);
            Int32 ThumbRY = (int)Math.Floor(Convert.ToDouble(Data.RSy.Value) * -32512);


            Output[14] = (Byte)((ThumbLX >> 0) & 0xFF); // LX
            Output[15] = (Byte)((ThumbLX >> 8) & 0xFF);

            Output[16] = (Byte)((ThumbLY >> 0) & 0xFF); // LY
            Output[17] = (Byte)((ThumbLY >> 8) & 0xFF);

            Output[18] = (Byte)((ThumbRX >> 0) & 0xFF); // RX
            Output[19] = (Byte)((ThumbRX >> 8) & 0xFF);

            Output[20] = (Byte)((ThumbRY >> 0) & 0xFF); // RY
            Output[21] = (Byte)((ThumbRY >> 8) & 0xFF);
        }

        public Boolean Plugin(Int32 Serial)
        {
            if (IsActive)
            {
                Int32 Transfered = 0;
                Byte[] Buffer = new Byte[16];

                Buffer[0] = 0x10;
                Buffer[1] = 0x00;
                Buffer[2] = 0x00;
                Buffer[3] = 0x00;

                Serial += firstController;
                Buffer[4] = (Byte)((Serial >> 0) & 0xFF);
                Buffer[5] = (Byte)((Serial >> 8) & 0xFF);
                Buffer[6] = (Byte)((Serial >> 16) & 0xFF);
                Buffer[7] = (Byte)((Serial >> 24) & 0xFF);

                return DeviceIoControl(m_FileHandle, 0x2A4000, Buffer, Buffer.Length, null, 0, ref Transfered, IntPtr.Zero);
            }

            return false;
        }

        public Boolean Unplug(Int32 Serial)
        {
            if (IsActive)
            {
                Int32 Transfered = 0;
                Byte[] Buffer = new Byte[16];

                Buffer[0] = 0x10;
                Buffer[1] = 0x00;
                Buffer[2] = 0x00;
                Buffer[3] = 0x00;

                Serial += firstController;
                Buffer[4] = (Byte)((Serial >> 0) & 0xFF);
                Buffer[5] = (Byte)((Serial >> 8) & 0xFF);
                Buffer[6] = (Byte)((Serial >> 16) & 0xFF);
                Buffer[7] = (Byte)((Serial >> 24) & 0xFF);

                return DeviceIoControl(m_FileHandle, 0x2A4004, Buffer, Buffer.Length, null, 0, ref Transfered, IntPtr.Zero);
            }

            return false;
        }

        public Boolean UnplugAll() //not yet implemented, not sure if will
        {
            if (IsActive)
            {
                Int32 Transfered = 0;
                Byte[] Buffer = new Byte[16];

                Buffer[0] = 0x10;
                Buffer[1] = 0x00;
                Buffer[2] = 0x00;
                Buffer[3] = 0x00;

                return DeviceIoControl(m_FileHandle, 0x2A4004, Buffer, Buffer.Length, null, 0, ref Transfered, IntPtr.Zero);
            }

            return false;
        }


        public Boolean Report(Byte[] Input, Byte[] Output)
        {
            if (IsActive)
            {
                Int32 Transfered = 0;

                return DeviceIoControl(m_FileHandle, 0x2A400C, Input, Input.Length, Output, Output.Length, ref Transfered, IntPtr.Zero) && Transfered > 0;
            }

            return false;
        }
    }
    partial class X360Device
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        #endregion
    }
    public partial class ScpDevice : Component
    {
        public virtual Boolean IsActive
        {
            get { return m_IsActive; }
        }

        public virtual String Path
        {
            get { return m_Path; }
        }


        public ScpDevice()
        {
            InitializeComponent();
        }

        public ScpDevice(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        public ScpDevice(String Class)
        {
            InitializeComponent();

            this.m_Class = new Guid(Class);
        }


        public virtual Boolean Open(Int32 Instance = 0)
        {
            String DevicePath = String.Empty;
            m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;

            if (Find(m_Class, ref DevicePath, Instance))
            {
                Open(DevicePath);
            }

            return m_IsActive;
        }

        public virtual Boolean Open(String DevicePath)
        {
            m_Path = DevicePath.ToUpper();
            m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;

            if (GetDeviceHandle(m_Path))
            {
                if (WinUsb_Initialize(m_FileHandle, ref m_WinUsbHandle))
                {
                    if (InitializeDevice())
                    {
                        m_IsActive = true;
                    }
                    else
                    {
                        WinUsb_Free(m_WinUsbHandle);
                        m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;
                    }
                }
                else
                {
                    m_FileHandle.Close();
                }
            }

            return m_IsActive;
        }

        public virtual Boolean Start()
        {
            return m_IsActive;
        }

        public virtual Boolean Stop()
        {
            m_IsActive = false;

            if (!(m_WinUsbHandle == (IntPtr)INVALID_HANDLE_VALUE))
            {
                WinUsb_AbortPipe(m_WinUsbHandle, m_IntIn);
                WinUsb_AbortPipe(m_WinUsbHandle, m_BulkIn);
                WinUsb_AbortPipe(m_WinUsbHandle, m_BulkOut);

                WinUsb_Free(m_WinUsbHandle);
                m_WinUsbHandle = (IntPtr)INVALID_HANDLE_VALUE;
            }

            if (m_FileHandle != null && !m_FileHandle.IsInvalid && !m_FileHandle.IsClosed)
            {
                m_FileHandle.Close();
                m_FileHandle = null;
            }

            return true;
        }

        public virtual Boolean Close()
        {
            return Stop();
        }


        public virtual Boolean ReadIntPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_ReadPipe(m_WinUsbHandle, m_IntIn, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean ReadBulkPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_ReadPipe(m_WinUsbHandle, m_BulkIn, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean WriteIntPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_WritePipe(m_WinUsbHandle, m_IntOut, Buffer, Length, ref Transfered, IntPtr.Zero);
        }

        public virtual Boolean WriteBulkPipe(Byte[] Buffer, Int32 Length, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            return WinUsb_WritePipe(m_WinUsbHandle, m_BulkOut, Buffer, Length, ref Transfered, IntPtr.Zero);
        }


        public virtual Boolean SendTransfer(Byte RequestType, Byte Request, UInt16 Value, Byte[] Buffer, ref Int32 Transfered)
        {
            if (!m_IsActive) return false;

            WINUSB_SETUP_PACKET Setup = new WINUSB_SETUP_PACKET();

            Setup.RequestType = RequestType;
            Setup.Request = Request;
            Setup.Value = Value;
            Setup.Index = 0;
            Setup.Length = (UInt16)Buffer.Length;

            return WinUsb_ControlTransfer(m_WinUsbHandle, Setup, Buffer, Buffer.Length, ref Transfered, IntPtr.Zero);
        }


        #region Constant and Structure Definitions
        public const Int32 SERVICE_CONTROL_STOP = 0x00000001;
        public const Int32 SERVICE_CONTROL_SHUTDOWN = 0x00000005;
        public const Int32 SERVICE_CONTROL_DEVICEEVENT = 0x0000000B;
        public const Int32 SERVICE_CONTROL_POWEREVENT = 0x0000000D;

        public const Int32 DBT_DEVICEARRIVAL = 0x8000;
        public const Int32 DBT_DEVICEQUERYREMOVE = 0x8001;
        public const Int32 DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const Int32 DBT_DEVTYP_DEVICEINTERFACE = 0x0005;
        public const Int32 DBT_DEVTYP_HANDLE = 0x0006;

        public const Int32 PBT_APMRESUMEAUTOMATIC = 0x0012;
        public const Int32 PBT_APMSUSPEND = 0x0004;

        public const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0x0000;
        public const Int32 DEVICE_NOTIFY_SERVICE_HANDLE = 0x0001;
        public const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 0x0004;

        public const Int32 WM_DEVICECHANGE = 0x0219;

        public const Int32 DIGCF_PRESENT = 0x0002;
        public const Int32 DIGCF_DEVICEINTERFACE = 0x0010;

        public delegate Int32 ServiceControlHandlerEx(Int32 Control, Int32 Type, IntPtr Data, IntPtr Context);

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_DEVICEINTERFACE
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            internal Int16 dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class DEV_BROADCAST_DEVICEINTERFACE_M
        {
            public Int32 dbcc_size;
            public Int32 dbcc_devicetype;
            public Int32 dbcc_reserved;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            public Byte[] dbcc_classguid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            public Char[] dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class DEV_BROADCAST_HDR
        {
            public Int32 dbch_size;
            public Int32 dbch_devicetype;
            public Int32 dbch_reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_DEVICE_INTERFACE_DATA
        {
            internal Int32 cbSize;
            internal Guid InterfaceClassGuid;
            internal Int32 Flags;
            internal IntPtr Reserved;
        }

        protected const UInt32 FILE_ATTRIBUTE_NORMAL = 0x80;
        protected const UInt32 FILE_FLAG_OVERLAPPED = 0x40000000;
        protected const UInt32 FILE_SHARE_READ = 1;
        protected const UInt32 FILE_SHARE_WRITE = 2;
        protected const UInt32 GENERIC_READ = 0x80000000;
        protected const UInt32 GENERIC_WRITE = 0x40000000;
        protected const Int32 INVALID_HANDLE_VALUE = -1;
        protected const UInt32 OPEN_EXISTING = 3;
        protected const UInt32 DEVICE_SPEED = 1;
        protected const Byte USB_ENDPOINT_DIRECTION_MASK = 0x80;

        protected enum POLICY_TYPE
        {
            SHORT_PACKET_TERMINATE = 1,
            AUTO_CLEAR_STALL = 2,
            PIPE_TRANSFER_TIMEOUT = 3,
            IGNORE_SHORT_PACKETS = 4,
            ALLOW_PARTIAL_READS = 5,
            AUTO_FLUSH = 6,
            RAW_IO = 7,
        }

        protected enum USBD_PIPE_TYPE
        {
            UsbdPipeTypeControl = 0,
            UsbdPipeTypeIsochronous = 1,
            UsbdPipeTypeBulk = 2,
            UsbdPipeTypeInterrupt = 3,
        }

        protected enum USB_DEVICE_SPEED
        {
            UsbLowSpeed = 1,
            UsbFullSpeed = 2,
            UsbHighSpeed = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct USB_CONFIGURATION_DESCRIPTOR
        {
            internal Byte bLength;
            internal Byte bDescriptorType;
            internal UInt16 wTotalLength;
            internal Byte bNumInterfaces;
            internal Byte bConfigurationValue;
            internal Byte iConfiguration;
            internal Byte bmAttributes;
            internal Byte MaxPower;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct USB_INTERFACE_DESCRIPTOR
        {
            internal Byte bLength;
            internal Byte bDescriptorType;
            internal Byte bInterfaceNumber;
            internal Byte bAlternateSetting;
            internal Byte bNumEndpoints;
            internal Byte bInterfaceClass;
            internal Byte bInterfaceSubClass;
            internal Byte bInterfaceProtocol;
            internal Byte iInterface;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct WINUSB_PIPE_INFORMATION
        {
            internal USBD_PIPE_TYPE PipeType;
            internal Byte PipeId;
            internal UInt16 MaximumPacketSize;
            internal Byte Interval;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        protected struct WINUSB_SETUP_PACKET
        {
            internal Byte RequestType;
            internal Byte Request;
            internal UInt16 Value;
            internal UInt16 Index;
            internal UInt16 Length;
        }

        protected const Int32 DIF_PROPERTYCHANGE = 0x12;
        protected const Int32 DICS_ENABLE = 1;
        protected const Int32 DICS_DISABLE = 2;
        protected const Int32 DICS_PROPCHANGE = 3;
        protected const Int32 DICS_FLAG_GLOBAL = 1;

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_CLASSINSTALL_HEADER
        {
            internal Int32 cbSize;
            internal Int32 InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SP_PROPCHANGE_PARAMS
        {
            internal SP_CLASSINSTALL_HEADER ClassInstallHeader;
            internal Int32 StateChange;
            internal Int32 Scope;
            internal Int32 HwProfile;
        }
        #endregion

        #region Protected Data Members
        protected Guid m_Class = Guid.Empty;
        protected String m_Path = String.Empty;

        protected SafeFileHandle m_FileHandle = null;
        protected IntPtr m_WinUsbHandle = IntPtr.Zero;

        protected Byte m_IntIn = 0xFF;
        protected Byte m_IntOut = 0xFF;
        protected Byte m_BulkIn = 0xFF;
        protected Byte m_BulkOut = 0xFF;

        protected Boolean m_IsActive = false;
        #endregion

        #region Static Helper Methods
        public enum Notified { Ignore = 0x0000, Arrival = 0x8000, QueryRemove = 0x8001, Removal = 0x8004 };

        public static Boolean RegisterNotify(IntPtr Form, Guid Class, ref IntPtr Handle, Boolean Window = true)
        {
            IntPtr devBroadcastDeviceInterfaceBuffer = IntPtr.Zero;

            try
            {
                DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
                Int32 Size = Marshal.SizeOf(devBroadcastDeviceInterface);

                devBroadcastDeviceInterface.dbcc_size = Size;
                devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
                devBroadcastDeviceInterface.dbcc_reserved = 0;
                devBroadcastDeviceInterface.dbcc_classguid = Class;

                devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(Size);
                Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);

                Handle = RegisterDeviceNotification(Form, devBroadcastDeviceInterfaceBuffer, Window ? DEVICE_NOTIFY_WINDOW_HANDLE : DEVICE_NOTIFY_SERVICE_HANDLE);

                Marshal.PtrToStructure(devBroadcastDeviceInterfaceBuffer, devBroadcastDeviceInterface);

                return Handle != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
            finally
            {
                if (devBroadcastDeviceInterfaceBuffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
                }
            }
        }

        public static Boolean UnregisterNotify(IntPtr Handle)
        {
            try
            {
                return UnregisterDeviceNotification(Handle);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
        }
        #endregion

        #region Protected Methods
        protected virtual Boolean Find(Guid Target, ref String Path, Int32 Instance = 0)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA(), da = new SP_DEVICE_INTERFACE_DATA();
                Int32 bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref Target, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref Target, memberIndex, ref DeviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                        {
                            IntPtr pDevicePathName = new IntPtr(IntPtr.Size == 4 ? detailDataBuffer.ToInt32() + 4 : detailDataBuffer.ToInt64() + 4);

                            Path = Marshal.PtrToStringAuto(pDevicePathName).ToUpper();
                            Marshal.FreeHGlobal(detailDataBuffer);

                            if (memberIndex == Instance) return true;
                        }
                        else Marshal.FreeHGlobal(detailDataBuffer);
                    }

                    memberIndex++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }

        protected virtual Boolean GetDeviceInstance(ref String Instance)
        {
            IntPtr detailDataBuffer = IntPtr.Zero;
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA DeviceInterfaceData = new SP_DEVICE_INTERFACE_DATA(), da = new SP_DEVICE_INTERFACE_DATA();
                Int32 bufferSize = 0, memberIndex = 0;

                deviceInfoSet = SetupDiGetClassDevs(ref m_Class, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                DeviceInterfaceData.cbSize = da.cbSize = Marshal.SizeOf(DeviceInterfaceData);

                while (SetupDiEnumDeviceInterfaces(deviceInfoSet, IntPtr.Zero, ref m_Class, memberIndex, ref DeviceInterfaceData))
                {
                    SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, IntPtr.Zero, 0, ref bufferSize, ref da);
                    {
                        detailDataBuffer = Marshal.AllocHGlobal(bufferSize);

                        Marshal.WriteInt32(detailDataBuffer, (IntPtr.Size == 4) ? (4 + Marshal.SystemDefaultCharSize) : 8);

                        if (SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref DeviceInterfaceData, detailDataBuffer, bufferSize, ref bufferSize, ref da))
                        {
                            IntPtr pDevicePathName = new IntPtr(IntPtr.Size == 4 ? detailDataBuffer.ToInt32() + 4 : detailDataBuffer.ToInt64() + 4);

                            String Current = Marshal.PtrToStringAuto(pDevicePathName).ToUpper();
                            Marshal.FreeHGlobal(detailDataBuffer);

                            if (Current == Path)
                            {
                                Int32 nBytes = 256;
                                IntPtr ptrInstanceBuf = Marshal.AllocHGlobal(nBytes);

                                CM_Get_Device_ID(da.Flags, ptrInstanceBuf, nBytes, 0);
                                Instance = Marshal.PtrToStringAuto(ptrInstanceBuf).ToUpper();

                                Marshal.FreeHGlobal(ptrInstanceBuf);
                                return true;
                            }
                        }
                        else Marshal.FreeHGlobal(detailDataBuffer);
                    }

                    memberIndex++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }

        protected virtual Boolean GetDeviceHandle(String Path)
        {
            m_FileHandle = CreateFile(Path, (GENERIC_WRITE | GENERIC_READ), FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, 0);

            return !m_FileHandle.IsInvalid;
        }

        protected virtual Boolean UsbEndpointDirectionIn(Int32 addr)
        {
            return (addr & 0x80) == 0x80;
        }

        protected virtual Boolean UsbEndpointDirectionOut(Int32 addr)
        {
            return (addr & 0x80) == 0x00;
        }

        protected virtual Boolean InitializeDevice()
        {
            try
            {
                USB_INTERFACE_DESCRIPTOR ifaceDescriptor = new USB_INTERFACE_DESCRIPTOR();
                WINUSB_PIPE_INFORMATION pipeInfo = new WINUSB_PIPE_INFORMATION();

                if (WinUsb_QueryInterfaceSettings(m_WinUsbHandle, 0, ref ifaceDescriptor))
                {
                    for (Int32 i = 0; i < ifaceDescriptor.bNumEndpoints; i++)
                    {
                        WinUsb_QueryPipe(m_WinUsbHandle, 0, System.Convert.ToByte(i), ref pipeInfo);

                        if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) & UsbEndpointDirectionIn(pipeInfo.PipeId)))
                        {
                            m_BulkIn = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_BulkIn);
                        }
                        else if (((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeBulk) & UsbEndpointDirectionOut(pipeInfo.PipeId)))
                        {
                            m_BulkOut = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_BulkOut);
                        }
                        else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) & UsbEndpointDirectionIn(pipeInfo.PipeId))
                        {
                            m_IntIn = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_IntIn);
                        }
                        else if ((pipeInfo.PipeType == USBD_PIPE_TYPE.UsbdPipeTypeInterrupt) & UsbEndpointDirectionOut(pipeInfo.PipeId))
                        {
                            m_IntOut = pipeInfo.PipeId;
                            WinUsb_FlushPipe(m_WinUsbHandle, m_IntOut);
                        }
                    }

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
        }

        protected virtual Boolean RestartDevice(String InstanceId)
        {
            IntPtr deviceInfoSet = IntPtr.Zero;

            try
            {
                SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new SP_DEVICE_INTERFACE_DATA();

                deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData);
                deviceInfoSet = SetupDiGetClassDevs(ref m_Class, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (SetupDiOpenDeviceInfo(deviceInfoSet, InstanceId, IntPtr.Zero, 0, ref deviceInterfaceData))
                {
                    SP_PROPCHANGE_PARAMS props = new SP_PROPCHANGE_PARAMS();

                    props.ClassInstallHeader = new SP_CLASSINSTALL_HEADER();
                    props.ClassInstallHeader.cbSize = Marshal.SizeOf(props.ClassInstallHeader);
                    props.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;

                    props.Scope = DICS_FLAG_GLOBAL;
                    props.StateChange = DICS_PROPCHANGE;
                    props.HwProfile = 0x00;

                    if (SetupDiSetClassInstallParams(deviceInfoSet, ref deviceInterfaceData, ref props, Marshal.SizeOf(props)))
                    {
                        return SetupDiChangeState(deviceInfoSet, ref deviceInterfaceData);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(string.Format("{0} {1}", ex.HelpLink, ex.Message));
                throw;
            }
            finally
            {
                if (deviceInfoSet != IntPtr.Zero)
                {
                    SetupDiDestroyDeviceInfoList(deviceInfoSet);
                }
            }

            return false;
        }
        #endregion

        #region Interop Definitions
        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Int32 SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, Int32 hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Int32 SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        protected static extern Boolean SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, Int32 MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, Int32 Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, IntPtr DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        protected static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern Boolean UnregisterDeviceNotification(IntPtr Handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern SafeFileHandle CreateFile(String lpFileName, UInt32 dwDesiredAccess, UInt32 dwShareMode, IntPtr lpSecurityAttributes, UInt32 dwCreationDisposition, UInt32 dwFlagsAndAttributes, UInt32 hTemplateFile);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_Initialize(SafeFileHandle DeviceHandle, ref IntPtr InterfaceHandle);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_QueryInterfaceSettings(IntPtr InterfaceHandle, Byte AlternateInterfaceNumber, ref USB_INTERFACE_DESCRIPTOR UsbAltInterfaceDescriptor);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_QueryPipe(IntPtr InterfaceHandle, Byte AlternateInterfaceNumber, Byte PipeIndex, ref WINUSB_PIPE_INFORMATION PipeInformation);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_AbortPipe(IntPtr InterfaceHandle, Byte PipeID);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_FlushPipe(IntPtr InterfaceHandle, Byte PipeID);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_ControlTransfer(IntPtr InterfaceHandle, WINUSB_SETUP_PACKET SetupPacket, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_ReadPipe(IntPtr InterfaceHandle, Byte PipeID, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_WritePipe(IntPtr InterfaceHandle, Byte PipeID, Byte[] Buffer, Int32 BufferLength, ref Int32 LengthTransferred, IntPtr Overlapped);

        [DllImport("winusb.dll", SetLastError = true)]
        protected static extern Boolean WinUsb_Free(IntPtr InterfaceHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(String ServiceName, ServiceControlHandlerEx Callback, IntPtr Context);

        [DllImport("kernel32.dll", SetLastError = true)]
        protected static extern Boolean DeviceIoControl(SafeFileHandle DeviceHandle, Int32 IoControlCode, Byte[] InBuffer, Int32 InBufferSize, Byte[] OutBuffer, Int32 OutBufferSize, ref Int32 BytesReturned, IntPtr Overlapped);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Int32 CM_Get_Device_ID(Int32 dnDevInst, IntPtr Buffer, Int32 BufferLen, Int32 ulFlags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiOpenDeviceInfo(IntPtr DeviceInfoSet, String DeviceInstanceId, IntPtr hwndParent, Int32 Flags, ref SP_DEVICE_INTERFACE_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiChangeState(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        protected static extern Boolean SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, ref SP_PROPCHANGE_PARAMS ClassInstallParams, Int32 ClassInstallParamsSize);
        #endregion
    }
    partial class ScpDevice
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }

        #endregion
    }
}
