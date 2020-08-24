using System.Windows.Forms;

namespace AyeBlinkin.Forms.Controls
{
    internal class TransparentPanel : Panel
    {
        protected override CreateParams CreateParams 
        {            
            get 
            {
                CreateParams cp =  base.CreateParams;
                //cp.ExStyle |= 0x00000020; // WS_EX_TRANSPARENT
                return cp;
           }
        }
    }
}