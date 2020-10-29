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

        }

        private static void PowerModeChanged(object sender, PowerModeChangedEventArgs e) 
        {


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
