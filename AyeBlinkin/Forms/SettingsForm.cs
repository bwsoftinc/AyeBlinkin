using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

using AyeBlinkin.Serial;
using AyeBlinkin.DirectX;

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
                    width = new[] { Settings.Model.Adapters.Values, Settings.Model.Displays.Values, Settings.Model.SerialComs.Values }
                        .SelectMany(x => x).Max(x => (int)g.MeasureString(x, adapter.Font).Width);

                adapter.Width = serial.Width = display.Width = width + 24;
            }
        }
    }
}
