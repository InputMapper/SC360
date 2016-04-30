using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ODIF;
using ODIF.Extensions;
using ODIF.Localization;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace SC360
{
    public static class SC360Globals
    {
        public static X360Device scBus;
        public static SC360_Plugin PluginRef;
    }

    [PluginInfo(
        PluginName = "Scarlet Crush 360 Virtual Bus",
        PluginDescription = "Creates a virtual bus for 360 controllers.",
        PluginID = 39,
        PluginAuthorName = "Scarlet Crush",
        PluginAuthorEmail = "a",
        PluginAuthorURL = "a",
        PluginIconPath = @"pack://application:,,,/SC360;component/Resources/ScarletCrush.ico"
    )]
    public class SC360_Plugin : OutputDevicePlugin , pluginSettings, DynamicDeviceList
    {
        //public AsyncObservableCollection<OutputDevice> Devices { get; set; }
        
        public SettingGroup settings { get; set; }

        public SC360_Plugin()
        {
            settings = new SettingGroup("General Settings","");
            Setting lowLatencyMode = new Setting("Low latency mode", "Extremely low latency and low system overhead, but does not support rumble.", SettingControl.Checkbox, SettingType.Bool, false);
            lowLatencyMode.descriptionVisibility = DescriptionVisibility.SubText;
            settings.settings.Add(lowLatencyMode);

            settings.loadSettings();

            ApplicationMenuItem mymenu = new ApplicationMenuItem("XInput Test");
            mymenu.Clicked += (s, e) => { Process.Start(Global.PluginsDirectory +"\\sc360\\XInputTest.exe"); };
            Global.MainMenu.FirstOrDefault(m => m.Text == "Tools").Items.Add(mymenu);

            SC360Globals.scBus = new X360Device();
            SC360Globals.PluginRef = this;
            SC360Globals.scBus.Start();
        }

        public void RemoveDevice(Device device)
        {
            lock (Devices)
            {
                if (Devices.Count > 0)
                {
                    device.Dispose();
                    Devices.Remove(device as ODIF.OutputDevice);
                }
            }
        }

        public void AddDevice()
        {
            lock (Devices)
            {
                Devices.Add(new SC360_Device_Plugin(Devices.Count + 1, settings));
            }
        }

        protected override void Dispose(bool disposing)
        {
            settings.saveSettings();
            foreach (SC360_Device_Plugin device in Devices)
            {
                device.Dispose();
            }

            SC360Globals.scBus.UnplugAll();
            SC360Globals.scBus.Stop();

            base.Dispose(disposing);
        }
    }

    internal class x360device
    {
        public InputChannelTypes.JoyAxis LSx { get; set; }
        public InputChannelTypes.JoyAxis LSy { get; set; }
        public InputChannelTypes.JoyAxis RSx { get; set; }
        public InputChannelTypes.JoyAxis RSy { get; set; }

        public InputChannelTypes.Button LS { get; set; }
        public InputChannelTypes.Button RS { get; set; }

        public InputChannelTypes.JoyAxis LT { get; set; }
        public InputChannelTypes.JoyAxis RT { get; set; }
        public InputChannelTypes.Button LB { get; set; }
        public InputChannelTypes.Button RB { get; set; }

        public InputChannelTypes.Button DUp { get; set; }
        public InputChannelTypes.Button DDown { get; set; }
        public InputChannelTypes.Button DLeft { get; set; }
        public InputChannelTypes.Button DRight { get; set; }

        public InputChannelTypes.Button A { get; set; }
        public InputChannelTypes.Button B { get; set; }
        public InputChannelTypes.Button X { get; set; }
        public InputChannelTypes.Button Y { get; set; }

        public InputChannelTypes.Button Start { get; set; }
        public InputChannelTypes.Button Back { get; set; }
        public InputChannelTypes.Button Guide { get; set; }

        public OutputChannelTypes.RumbleMotor BigRumble { get; set; }
        public OutputChannelTypes.RumbleMotor SmallRumble { get; set; }

        public x360device()
        {
            LSx = new InputChannelTypes.JoyAxis("Left Stick X","",Properties.Resources._24_360_Left_Stick.ToImageSource());
            LSy = new InputChannelTypes.JoyAxis("Left Stick Y","", Properties.Resources._24_360_Left_Stick.ToImageSource());
            RSx = new InputChannelTypes.JoyAxis("Right Stick X","", Properties.Resources._24_360_Right_Stick.ToImageSource());
            RSy = new InputChannelTypes.JoyAxis("Right Stick Y","", Properties.Resources._24_360_Right_Stick.ToImageSource());

            LS = new InputChannelTypes.Button("Left Stick","", Properties.Resources._24_360_Left_Stick.ToImageSource());
            RS = new InputChannelTypes.Button("Right Stick","", Properties.Resources._24_360_Right_Stick.ToImageSource());

            LT = new InputChannelTypes.JoyAxis("Left Trigger","", Properties.Resources._24_360_LT.ToImageSource()) { min_Value = 0 };
            RT = new InputChannelTypes.JoyAxis("Right Trigger","", Properties.Resources._24_360_RT.ToImageSource()) { min_Value = 0 };
            LB = new InputChannelTypes.Button("Left Bumper","", Properties.Resources._24_360_LB.ToImageSource());
            RB = new InputChannelTypes.Button("Right Bumper","", Properties.Resources._24_360_RB.ToImageSource());

            DUp = new InputChannelTypes.Button("DPad Up","", Properties.Resources._24_360_Dpad_Up.ToImageSource());
            DDown = new InputChannelTypes.Button("DPad Down","", Properties.Resources._24_360_Dpad_Down.ToImageSource());
            DLeft = new InputChannelTypes.Button("DPad Left","", Properties.Resources._24_360_Dpad_Left.ToImageSource());
            DRight = new InputChannelTypes.Button("DPad Right","", Properties.Resources._24_360_Dpad_Right.ToImageSource());

            A = new InputChannelTypes.Button("A","", Properties.Resources._24_360_A.ToImageSource());
            B = new InputChannelTypes.Button("B","", Properties.Resources._24_360_B.ToImageSource());
            X = new InputChannelTypes.Button("X","", Properties.Resources._24_360_X.ToImageSource());
            Y = new InputChannelTypes.Button("Y","", Properties.Resources._24_360_Y.ToImageSource());

            Start = new InputChannelTypes.Button("Start","", Properties.Resources._24_360_Start.ToImageSource());
            Back = new InputChannelTypes.Button("Back","", Properties.Resources._24_360_Back.ToImageSource());
            Guide = new InputChannelTypes.Button("Guide","", Properties.Resources._24_360_Guide.ToImageSource());

            BigRumble = new OutputChannelTypes.RumbleMotor("Big Rumble", "");
            SmallRumble = new OutputChannelTypes.RumbleMotor("Small Rumble", "");
        }
    }

    public class SC360_Device_Plugin : OutputDevice
    {
        public System.Windows.Controls.Page MappingPage()
        {
            return new System.Windows.Controls.Page();
        }

        private SettingGroup settings { get; set; }
        public ConnectionTypes DeviceConnectionType { get { return ConnectionTypes.Virtual; } }
        private Thread ReportingThread;
        private int ControllerID;
        
        public SC360_Device_Plugin(int device, SettingGroup settings)
        {
            ControllerID = device;
            this.settings = settings;
            this.StatusIcon = new BitmapImage(new Uri(@"pack://application:,,,/SC360;component/Resources/360.ico"));
            this.DeviceName = "Controller " + ControllerID;

            x360device myDevice = new x360device();
            
            InputChannels.Add(myDevice.LSx);
            InputChannels.Add(myDevice.LSy);
            InputChannels.Add(myDevice.RSx);
            InputChannels.Add(myDevice.RSy);

            InputChannels.Add(myDevice.LS);
            InputChannels.Add(myDevice.RS);

            InputChannels.Add(myDevice.LT);
            InputChannels.Add(myDevice.RT);
            InputChannels.Add(myDevice.LB);
            InputChannels.Add(myDevice.RB);

            InputChannels.Add(myDevice.DUp);
            InputChannels.Add(myDevice.DDown);
            InputChannels.Add(myDevice.DLeft);
            InputChannels.Add(myDevice.DRight);

            InputChannels.Add(myDevice.A);
            InputChannels.Add(myDevice.B);
            InputChannels.Add(myDevice.X);
            InputChannels.Add(myDevice.Y);

            InputChannels.Add(myDevice.Start);
            InputChannels.Add(myDevice.Back);
            InputChannels.Add(myDevice.Guide);

            OutputChannels.Add(myDevice.SmallRumble);
            OutputChannels.Add(myDevice.BigRumble);

            if (SC360Globals.scBus.Open() && SC360Globals.scBus.Start())
            {
                SC360Globals.scBus.Plugin(device);

                if (settings.getSetting("Low latency mode"))
                {

                    byte[] Report = new byte[28];
                    byte[] Rumble = new byte[8];

                    foreach (DeviceChannel channel in InputChannels)
                    {
                        channel.PropertyChanged += (s, e) => {

                            SC360Globals.scBus.Parse(myDevice, Report, ControllerID);
                            if (SC360Globals.scBus.Report(Report, Rumble))
                            {
                            //    if (Rumble[1] == 0x08)
                            //    {
                            //        myDevice.SmallRumble.Value = Rumble[3];
                            //        myDevice.BigRumble.Value = Rumble[4];
                            //    }
                            }
                        };
                    }
                } else
                {
                    ReportingThread = new Thread(() => ReportingThreadLoop(myDevice));
                    ReportingThread.Start();
                }
            }
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, IntPtr count);

        private void ReportingThreadLoop(x360device device)
        {
            byte[] Report = new byte[28];
            byte[] Rumble = new byte[8];
            while (!Global.IsShuttingDown)
            {
                Thread.Sleep(1);
                SC360Globals.scBus.Parse(device, Report, ControllerID);

                if (SC360Globals.scBus.Report(Report, Rumble))
                {
                    
                    if (Rumble[1] == 0x08)
                    {
                        device.SmallRumble.Value = Rumble[3]/255d;
                        device.BigRumble.Value = Rumble[4]/255d;
                    }
                }

            }
        }
        public SC360_Device_Plugin()
        {
        }
        public bool StartDevice()
        {

            return true;
        }
        protected override void Dispose(bool disposing)
        {
            if (ReportingThread != null && ReportingThread.IsAlive)
                ReportingThread.Abort();
            SC360Globals.scBus.Unplug(ControllerID);

            base.Dispose(disposing);
        }

    }
}
