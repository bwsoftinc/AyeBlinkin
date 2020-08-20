using System;
using System.Drawing;
using System.Windows.Forms;

namespace AyeBlinkin.Forms.Controls
{
    internal class BindableToolStripMenuItem : ToolStripMenuItem, IBindableComponent
    {
        private ControlBindingsCollection dataBindings;
        public BindingContext BindingContext { get; set; }
        public ControlBindingsCollection DataBindings
        {
            get {
                if (dataBindings == null)
                    dataBindings = new ControlBindingsCollection(this);

                return dataBindings;
            }
        }

        public BindableToolStripMenuItem(string name, EventHandler handler=null) : base(name, null, handler) { }

        internal class ToolStripArrowRenderer : ToolStripProfessionalRenderer 
        { 
            private string doFor;
            public ToolStripArrowRenderer(string name) : base() { doFor = name; }
            protected override void OnRenderArrow (ToolStripArrowRenderEventArgs e) { 
                var item = e.Item as BindableToolStripMenuItem;
                if (item != null && item.Name == doFor)
                    e.ArrowRectangle = new Rectangle(e.ArrowRectangle.X - 20, e.ArrowRectangle.Y, e.ArrowRectangle.Width, e.ArrowRectangle.Height);

                base.OnRenderArrow(e);
            }
        }
    }
}