using System;
using System.Drawing;
using System.Windows.Forms;

using AyeBlinkin.Resources;

namespace AyeBlinkin.Forms.Controls 
{
    internal class MirrorMenuItem : PanelMenuItem 
    {
        private CheckBox checkbox;        
        private PictureBox button;

        internal MirrorMenuItem(string model, EventHandler settingsClickHandler) : base ()
        {
            this.panel.Controls.Add(checkbox = new CheckBox() {
                TextAlign = ContentAlignment.BottomRight,
                AutoSize = false,
                BackColor = Color.Transparent,
                Height = 18,
                Width = 18,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            });
            checkbox.AddBinding("Checked", model);

            this.panel.Controls.Add(new Label() { 
                Text = model.ToString(),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = 18,
                Left = 18,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            });

            this.panel.Controls.Add(button = new PictureBox() {
                Image = Icons.Settings.ToBitmap(),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Dock = DockStyle.Right,
                Width = 23,
                Height = 23,
                BorderStyle = BorderStyle.None,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            });
            button.Click += settingsClickHandler;
            button.MouseMove += (sender, e) => (sender as PictureBox).Cursor = Cursors.Hand;
        }
    }
}