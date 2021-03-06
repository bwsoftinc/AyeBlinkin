using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

using AyeBlinkin.DirectX;
using AyeBlinkin.CoreAudio;
using AyeBlinkin.Resources;
using AyeBlinkin.Forms.Controls;

namespace AyeBlinkin.Forms 
{
    internal class AyeBlinkinTray : ApplicationContext 
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip strip;
        private BindableToolStripMenuItem patterns;
        private SettingsForm settingsForm;

        internal AyeBlinkinTray() 
        {
            trayIcon = new NotifyIcon() {
                ContextMenuStrip = MakeContextMenuStrip(),
                Icon = Icons.Program,
                Visible = true,
                Text = AyeBlinkin.Name
            };
            trayIcon.MouseUp += LeftClickOpenMenu;
            Settings.Model.uiContext = SynchronizationContext.Current;

            settingsForm = new SettingsForm();
            Settings.Model.PropertyChanged += buildPatternOptions;
            Settings.Model.SerialComs = WindowsEvents.GetUsbDevicePorts();
            Settings.Model.Adapters = DeviceEnumerator.GetAdapters();
            WasapiSoundCapture.Stop();
#if DEBUG
            OpenSettingsForm(null, null);
#endif
        }

        private ContextMenuStrip MakeContextMenuStrip() 
        {
            strip = new ContextMenuStrip() { 
                ShowImageMargin = false, 
                ShowCheckMargin = false,
                AutoSize = false,
                Width = 350,
                Height = 200,
                Renderer = new BindableToolStripMenuItem.ToolStripArrowRenderer(nameof(Settings.Model.Patterns))
            };

            strip.Items.AddRange(new ToolStripItem[] {
                new MirrorMenuItem(nameof(Settings.Model.Mirror), OpenSettingsForm),
                new ToolStripSeparator(),
                new TrackBarMenuItem(nameof(Settings.Model.Red)),
                new TrackBarMenuItem(nameof(Settings.Model.Green)),
                new TrackBarMenuItem(nameof(Settings.Model.Blue)),
                new ToolStripSeparator(),
                new TrackBarMenuItem(nameof(Settings.Model.Brightness)),
                new ToolStripSeparator(),
                patterns = makeMenuItem(nameof(Settings.Model.Patterns), null),
                makeMenuItem("Settings (preview)...", OpenSettingsForm),
                makeMenuItem("Exit", Exit)
            });

            strip.Opening += (s, e) => this.settingsForm.Focus();
            return strip;
        }

        private void buildPatternOptions(object sender, PropertyChangedEventArgs e)
        {
            //new pattern selected update which item shows checked
            if(e.PropertyName == nameof(Settings.Model.PatternId))
            {
                var value = Settings.Model.PatternId;
                foreach(var item in patterns.DropDown.Items.Cast<BindableToolStripMenuItem>())
                {
                    if(item.Checked && (int)item.Tag != value)
                        item.Checked = false;
                    else if(!item.Checked && (int)item.Tag == value)
                        item.Checked = true;
                }
            }
            //new list of patterns, create the list control with events
            else if (e.PropertyName == nameof(Settings.Model.Patterns))
            {
                var value = Settings.Model.Patterns;
                var items = patterns.DropDown.Items.Cast<BindableToolStripMenuItem>().ToList();

                var different = value.Count != items.Count ||
                    value.OrderBy(x => x.Value).ThenBy(x => x.Key).Select(x => new { x.Key, x.Value })
                        .Zip(items.Select(x => new { Key = (int)x.Tag, Value = x.Text }))
                        .Any(x => x.First.Key != x.Second.Key || x.First.Value != x.Second.Value);

                if(!different)
                    return;

                foreach(var item in items)
                {
                    item.DataBindings.Clear();
                    item.Dispose();
                }

                patterns.DropDown = new ContextMenuStrip() {
                    ShowCheckMargin = true,
                    ShowImageMargin = false
                };

                patterns.DropDown.Closing += (s, ea) =>
                    ea.Cancel = ea.CloseReason == ToolStripDropDownCloseReason.ItemClicked;

                var patternId = Settings.Model.PatternId;
                patterns.DropDown.Items.AddRange(value.OrderBy(x => x.Value).Select(p => {
                    var item = new BindableToolStripMenuItem(p.Value) {
                        CheckOnClick = true,
                        Checked = p.Key == patternId,
                        Tag = p.Key
                    };

                    item.Click += (mi, ea) =>
                        Settings.Model.PatternId = (int)(mi as BindableToolStripMenuItem).Tag;

                    item.AddBinding(nameof(item.Enabled), nameof(Settings.Model.MirrorOff));

                    return item;
                }).ToArray());
            }
        }

        private BindableToolStripMenuItem makeMenuItem(string name, EventHandler handler) =>
            new BindableToolStripMenuItem(name, handler) {
                Width = 348,
                AutoSize = false,
                Name = name
            };

        private void Exit(object s, EventArgs e)  => Application.Exit();
        
        internal void hideIcon() => trayIcon.Visible = false;

        private void LeftClickOpenMenu(object sender, MouseEventArgs e) 
        {
            if(e.Button == MouseButtons.Left)
                typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(this.trayIcon, null);
        }

        private void OpenSettingsForm(object sender, EventArgs e) 
        {
            if(settingsForm.IsDisposed)
                settingsForm = new SettingsForm();
            if(!settingsForm.Visible)
                settingsForm.Show();
        }
     }
}