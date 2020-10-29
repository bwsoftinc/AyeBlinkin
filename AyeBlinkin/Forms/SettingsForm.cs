using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;

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
        private Panel controlPanel;

        internal SettingsForm()
        { 
            ShowInTaskbar = false;
            TopMost = true;
            InitializeComponent();
            Settings.Model.PropertyChanged += ResizeControls;
        }

        protected override void OnShown(EventArgs e) 
        {
            Settings.SettingsHwnd = this.Handle;
#if DEBUG
            CentroidColorForm.ShowSlice();
#endif
        }
        
        protected override void OnClosing(CancelEventArgs e) 
        { 
            Settings.SettingsHwnd = IntPtr.Zero;
#if DEBUG
            CentroidColorForm.HideSlice();
#endif
        }

        private void ResizeControls(object sender, PropertyChangedEventArgs e) 
        {
            switch(e.PropertyName) {
                case nameof(Settings.Model.SerialComs):
                    (this.serial.DataSource as BindingSource)?.ResetBindings(false);
                    break;
                case nameof(Settings.Model.Displays):
                    (this.display.DataSource as BindingSource)?.ResetBindings(false);
                    break;
                case nameof(Settings.Model.Adapters):
                    (this.adapter.DataSource as BindingSource)?.ResetBindings(false);
                    break;
                default:
                    return;
            }

            var width = 0;
            using(var g = Graphics.FromHwnd(IntPtr.Zero))
                width = new[] { 
                    Settings.Model.Adapters.Select(x => x.Value),
                    Settings.Model.Displays.Select(x => x.Value), 
                    Settings.Model.SerialComs.Select(x => x.Value)
                }.SelectMany(x => x).Max(x => (int)g.MeasureString(x, adapter.Font).Width);

            adapter.Width = serial.Width = display.Width = width + 24;
        }
    }
}
