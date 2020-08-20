using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;

using AyeBlinkin.DirectX;
using AyeBlinkin.Serial;

namespace AyeBlinkin.Forms
{
    internal partial class SettingsForm : Form
    {
        private ComboBox serial;
        private ComboBox adapter;
        private ComboBox display;
        private CheckBox audioCheckbox;
        private CheckBox mirrorCheckbox;
        private NumericUpDown vertical;
        private NumericUpDown horizontal;
        internal const int minWidth = 300;
        internal const int minHeight = 150;

        internal SettingsForm() 
        { 
            InitializeComponent();
            Settings.Model.PropertyChanged += ResizeControls;
            Settings.Model.Adapters = DeviceEnumerator.GetAdapters();
            Settings.Model.SerialComs = SerialCom.GetUsbDevicePorts();
        }
        protected override void OnShown(EventArgs e) => Settings.SettingsHwnd = this.Handle;
        protected override void OnClosing(CancelEventArgs e) => Settings.SettingsHwnd = IntPtr.Zero;

        private void ResizeControls(object sender, PropertyChangedEventArgs e) 
        {
            if(e.PropertyName == nameof(Settings.Model.Adapters) 
                || e.PropertyName == nameof(Settings.Model.Displays) 
                || e.PropertyName == nameof(Settings.Model.SerialComs))
            {
                var width = 0;
                using(var g = Graphics.FromHwnd(IntPtr.Zero))
                    width = new[] { Settings.Model.Adapters, Settings.Model.Displays, Settings.Model.SerialComs }
                        .SelectMany(x => x.Cast<KeyValuePair<string,string>>())
                        .Max(x => (int)g.MeasureString(x.Value,  adapter.Font).Width);

                adapter.Width = serial.Width = display.Width = width + 24;
            }
            else if(e.PropertyName == nameof(Settings.Model.DisplayId))
            {
                
            }
        }
    }
}
