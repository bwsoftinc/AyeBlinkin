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

            this.Controls.Add(this.controlPanel = new Panel() {
                BackColor = Color.Transparent,
                AutoSize = false,
                Width = 300,
                Height = 150,
                Left = 30,
                Top = 30
            });

            this.controlPanel.Controls.AddRange(new Control[] {
                                    makeLabel(18, 14, "Adapter"),
                     this.adapter = makeComboBox(103, 12),

                                    makeLabel(18, 41, "Display"),
                     this.display = makeComboBox(103, 39),

                                    makeLabel(18, 68,"Serial Com"),
                      this.serial = makeComboBox(103, 66),

                                    makeLabel(18, 97, "LEDs (W , H)"),
                  this.horizontal = makeNumericUpDown(103, 95, 25),
                    this.vertical = makeNumericUpDown(143, 95, 10),

                                    makeLabel(18, 123, "Mirror Display"),
              this.mirrorCheckbox = makeCheckBox(103, 126),

                                    makeLabel(123, 123, "Audio Strobe"),
               this.audioCheckbox = makeCheckBox(208, 126)
            });

            this.AddBinding(nameof(this.Location), nameof(Settings.Model.SettingsWindowLocation));
            this.AddBinding(nameof(this.ClientSize), nameof(Settings.Model.SettingsWindowSize));
            this.adapter.AddBinding(nameof(this.adapter.SelectedValue), nameof(Settings.Model.Adapters));
            this.display.AddBinding(nameof(this.display.SelectedValue), nameof(Settings.Model.Displays));
            this.serial.AddBinding(nameof(this.serial.SelectedValue), nameof(Settings.Model.SerialComs));
            this.horizontal.AddBinding(nameof(this.horizontal.Value), nameof(Settings.Model.HorizontalLEDs));
            this.vertical.AddBinding(nameof(this.vertical.Value), nameof(Settings.Model.VerticalLEDs));
            this.mirrorCheckbox.AddBinding(nameof(this.mirrorCheckbox.Checked), nameof(Settings.Model.Mirror));
            this.audioCheckbox.AddBinding(nameof(this.audioCheckbox.Checked), nameof(Settings.Model.Audio));

            this.Text = $"{AyeBlinkin.Name} - Settings (preview)";
            this.Icon = Icons.Program;
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

        private NumericUpDown makeNumericUpDown(int left, int top, int max) =>
            new NumericUpDown() {
                Location = new Point(left, top),
                DecimalPlaces = 0,
                Minimum = 2,
                Maximum = max,
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