using System;
using System.Linq;
using System.IO.Ports;
using System.Management;
using System.ComponentModel;
using System.Collections.Generic;

using Microsoft.Win32;

namespace AyeBlinkin 
{
    static class WindowsEvents 
    {
        private static ManagementEventWatcher usbEventWatcher;
        private const string deviceMask = "Name like '%(COM%)'";

        static WindowsEvents()
        {
            initUsbEventWatcher();
            SystemEvents.PowerModeChanged += PowerModeChanged;
            SystemEvents.DisplaySettingsChanged += DisplaySettingsChanged;
        }

        public static void Detach() 
        {
            SystemEvents.PowerModeChanged -= PowerModeChanged;
            SystemEvents.DisplaySettingsChanged -= DisplaySettingsChanged;
            usbEventWatcher?.Stop();
            usbEventWatcher?.Dispose();
        }

        private static void DisplaySettingsChanged(object sender, EventArgs e)
        {
            Settings.Model.Adapters = DirectX.DeviceEnumerator.GetAdapters(true);
        }

        private static void PowerModeChanged(object sender, PowerModeChangedEventArgs e) 
        {
            if(e.Mode == PowerModes.Suspend)
                Suspend();
            else if(e.Mode == PowerModes.Resume)
                Resume();
        }

        private class Model 
        {
            public bool Mirror;
            public bool Audio;
            public int Red;
            public int Green;
            public int Blue;
            public int PatternId;

        }

        private static readonly Model model = new Model();

        private static void Suspend()
        {
            model.Red = Settings.Model.Red;
            model.Green = Settings.Model.Green;
            model.Blue = Settings.Model.Blue;
            model.Audio = Settings.Model.Audio;
            model.Mirror = Settings.Model.Mirror;
            model.PatternId = Settings.Model.PatternId;

            Settings.Model.Mirror = false;
            Settings.Model.Audio = false;
            Settings.Model.PatternId = -2;
            Settings.Model.Red = 0;
            Settings.Model.Green = 0;
            Settings.Model.Blue = 0;
        }

        private static void Resume()
        {
            Settings.Model.Blue = model.Blue;
            Settings.Model.Green = model.Green;
            Settings.Model.Red = model.Red;
            Settings.Model.PatternId = model.PatternId;
            Settings.Model.Mirror = model.Mirror;
            Settings.Model.Audio = model.Audio;
        }

        private static void initUsbEventWatcher() 
        {
            usbEventWatcher = new ManagementEventWatcher(new WqlEventQuery() {
                EventClassName = "__InstanceOperationEvent",
                WithinInterval = new TimeSpan(0, 0, 1),
                Condition = $"TargetInstance ISA 'Win32_PnPEntity' and TargetInstance.{deviceMask}"
            });

            usbEventWatcher.EventArrived += (s, e) => Settings.Model.SerialComs = GetUsbDevicePorts();
            usbEventWatcher.Start();
        }
        
        internal static BindingList<KeyValuePair<string, string>> GetUsbDevicePorts() 
        {
            var usbs = new List<string>();
            using(var searcher = new ManagementObjectSearcher($"select Name From Win32_PnPEntity where {deviceMask}")) 
            using(var collection = searcher.Get())
                foreach(var obj in collection)
                    using(obj)
                        usbs.Add((string)obj.GetPropertyValue("Name"));

            var coms = new HashSet<string>(SerialPort.GetPortNames().Select(x => x.ToUpper()));
            var list = new BindingList<KeyValuePair<string, string>>();

            foreach(var usb in usbs.Select(x => {
                var ix = x.LastIndexOf("(") + 1;
                return new {
                    com = x.Substring(ix, x.Length - ix - 1).ToUpper(),
                    name = x
                };
            }).Where(x => coms.Contains(x.com)))
                list.Add(new KeyValuePair<string, string>(usb.com, usb.name));
            
            return list;
        }
    }
}
