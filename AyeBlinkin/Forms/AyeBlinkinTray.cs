using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;

using AyeBlinkin.Resources;
using AyeBlinkin.Forms.Controls;

namespace AyeBlinkin.Forms 
{
    internal class AyeBlinkinTray : ApplicationContext 
    {
        private NotifyIcon trayIcon;
        private ContextMenuStrip strip;
        private BindableToolStripMenuItem patterns;
        private SettingsForm settingsForm = new SettingsForm();

        internal AyeBlinkinTray() 
        {
            this.trayIcon = new NotifyIcon() {
                ContextMenuStrip = MakeContextMenuStrip(),
                Icon = Icons.Program,
                Visible = true,
                Text = AyeBlinkin.Name
            };

            Settings.Model.PropertyChanged += buildPatternOptions;
            Settings.Model.uiContext = SynchronizationContext.Current;
            this.trayIcon.MouseUp += LeftClickOpenMenu;
            OpenSettingsForm(null, null);
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
                makeMenuItem("Settings...", OpenSettingsForm),
                makeMenuItem("Exit", Exit)
            });

            strip.Opening += (sender, e) => this.settingsForm.Focus();
            return strip;
        }

        private void buildPatternOptions(object sender, PropertyChangedEventArgs e)
        {
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

                var patternId = Settings.Model.PatternId;
                patterns.DropDown.Items.AddRange(value.OrderBy(x => x.Value).Select(p => {
                    var item = new BindableToolStripMenuItem(p.Value) {
                        CheckOnClick = true,
                        Checked = p.Key == patternId,
                        Tag = p.Key
                    };

                    item.Click += (object sender, EventArgs e) =>
                            Settings.Model.PatternId = (int)(sender as BindableToolStripMenuItem).Tag;

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

        private void Exit(object sender, EventArgs e)  => Application.Exit();
        internal void hideIcon() => trayIcon.Visible = false;

        private void LeftClickOpenMenu(object sender, MouseEventArgs e) {
            if(e.Button == MouseButtons.Left)
                typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(this.trayIcon, null);
        }

        private void OpenSettingsForm(object sender, EventArgs e) {
            if(settingsForm.IsDisposed)
                settingsForm = new SettingsForm();
            if(!settingsForm.Visible)
                settingsForm.Show();
        }
     }
}