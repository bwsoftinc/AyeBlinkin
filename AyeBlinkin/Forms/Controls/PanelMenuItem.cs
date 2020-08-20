using System;
using System.Drawing;
using System.Windows.Forms;

namespace AyeBlinkin.Forms.Controls
{
    internal class PanelMenuItem : ToolStripControlHost 
    {
        protected Panel panel;

        internal PanelMenuItem() : base(new Panel() {
                Height = 24,
                Width = 334,
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BackColor = Color.Transparent,
        }) { 
            this.panel = this.Control as Panel;
            this.Height = 24;
            this.Width = 334;
            this.AutoSize = false;
            this.Margin = Padding.Empty;
            this.Padding = Padding.Empty;
            this.BackColor = Color.Transparent;
        }
    }
}