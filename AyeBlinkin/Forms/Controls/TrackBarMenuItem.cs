using System.Drawing;
using System.Windows.Forms;

namespace AyeBlinkin.Forms.Controls
{
    internal class TrackBarMenuItem : PanelMenuItem 
    {
        private IntegerBox box;
        private TrackBar bar;

        public TrackBarMenuItem(string model) : base() 
        {
            var setting = model == nameof(Settings.Model.Brightness)? 
                nameof(Settings.Model.BrightBarEnabled) : 
                nameof(Settings.Model.RGBBarsEnabled);

            this.panel.Controls.Add(new Label() { 
                Text = model,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Width = 45,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            });

            this.panel.Controls.Add(bar = new TrackBar() {
                TickStyle = TickStyle.None,
                Maximum = 255,
                AutoSize = false,
                Width = 255,
                Height = 25,
                Left = 45,
                TabStop = true,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.White,
            });
            bar.AddBinding(nameof(bar.Value), model);
            bar.AddBinding(nameof(bar.Enabled), setting);

            this.panel.Controls.Add(box = new IntegerBox() {
                AutoSize = false,
                Margin = new Padding(0, 4, 0, 0),
                Padding = Padding.Empty,
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = true,
                TextAlign = HorizontalAlignment.Right,
                Dock = DockStyle.Right,
                MaximumSize = new Size(25,18),
                Maximum = 255
            });
            box.AddBinding(nameof(box.Text), model);
            box.AddBinding(nameof(box.Enabled), setting);
        }
    }
}