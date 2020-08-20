using System;
using System.Windows.Forms;

namespace AyeBlinkin.Forms.Controls
{
    internal class IntegerBox : TextBox {
        internal int Minimum { get; set; } = 0;
        internal int Maximum { get; set; } = int.MaxValue;
        internal IntegerBox() : base() { }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            if (!Char.IsDigit(e.KeyChar) || e.KeyChar == '\b')
                e.Handled = true;
            else
                validate();
        }

        protected override void OnTextChanged(EventArgs e) {
            validate();
        }

        private void validate() {
            if(Text.Length == 0  || !int.TryParse(Text, out int value))
                Text = Minimum.ToString();
            else if(value > Maximum)
                Text = Maximum.ToString();
        }
    }
}