using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;

using AyeBlinkin.Resources;

namespace AyeBlinkin.Forms
{
    internal partial class SettingsForm
    {
        private IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new Container();
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.AutoScaleMode = AutoScaleMode.Font;
            this.DoubleBuffered = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            this.Controls.AddRange(new Control[] {
                                    makeLabel(18, 14, "Adapter"),
                     this.adapter = makeComboBox(103, 12),

                                    makeLabel(18, 41, "Display"),
                     this.display = makeComboBox(103, 39),

                                    makeLabel(18, 68,"Serial Com"),
                      this.serial = makeComboBox(103, 66),

                                    makeLabel(18, 97, "LEDs (W x H)"),
                  this.horizontal = makeNumericUpDown(103, 95),
                    this.vertical = makeNumericUpDown(143, 95),

                                    makeLabel(18, 123, "Mirror Display"),
              this.mirrorCheckbox = makeCheckBox(103, 126),

                                    makeLabel(123, 123, "Audio Strobe"),
               this.audioCheckbox = makeCheckBox(208, 126)
            });

            this.AddBinding("Location", "Location");
            this.AddBinding("ClientSize", "ClientSize");
            this.adapter.AddBinding("SelectedValue", "Adapters");
            this.display.AddBinding("SelectedValue", "Displays");
            this.serial.AddBinding("SelectedValue", "SerialComs");
            this.horizontal.AddBinding("Value", "HorizontalLEDs");
            this.vertical.AddBinding("Value", "VerticalLEDs");
            this.mirrorCheckbox.AddBinding("Checked", "Mirror");
            this.audioCheckbox.AddBinding("Checked", "Audio");

            this.Text = AyeBlinkin.Name;
            this.Icon = Icons.Program;
            this.ClientSize = new Size(minWidth, minHeight);
        }

        private CheckBox makeCheckBox(int left, int top) =>
            new CheckBox() {
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                BackColor = Color.Transparent,
                Location = new Point(left, top),
                Height = 13,
                Width = 13,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

        private NumericUpDown makeNumericUpDown(int left, int top) =>
            new NumericUpDown() {
                Location = new Point(left, top),
                DecimalPlaces = 0,
                Minimum = 2,
                Maximum = 20,
                Width = 35,
                Font = new Font(Font.FontFamily, 8),
                AutoSize = false,
                TextAlign = HorizontalAlignment.Right
            };

        private ComboBox makeComboBox(int left, int top) =>
            new ComboBox() {
                DisplayMember = "Value",
                ValueMember = "Key",
                Width = 24,
                Font = new Font(Font.FontFamily, 8),
                Location = new Point(left, top),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

        private Label makeLabel(int left, int top, string text) =>
            new Label() {
                Location = new Point(left, top),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Width = 82,
                Height = 18,
                Text = text
            };
    }
}