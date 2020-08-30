using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace AyeBlinkin.Centroid 
{
    public class CentroidColorForm : Form 
    {
        private class tips { public Color color; public Rectangle rectangle; public int count; }

        private class TestPanel : Panel { public TestPanel() => this.DoubleBuffered = true; }

        private class TestPictureBox : PictureBox { public TestPictureBox() => this.DoubleBuffered = true;
            protected override void OnPaint(PaintEventArgs pe) {
                pe.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                base.OnPaint(pe);
            }
        }

        private static CentroidColorForm slice = new CentroidColorForm() { TopMost = true };
        private static TestPanel colors;
        private static TestPictureBox image;
        private static Button next;
        private static Button previous;
        private ToolTip tooltip = new ToolTip();
        private static List<tips> tooltips = new List<tips>();

        private CentroidColorForm() 
        {
            this.Controls.Add(previous = new Button() {
                Text = "<",
                Dock = DockStyle.Left,
                Width = 20
            });

            previous.Click += (sender, pre) => Settings.Model.PreviewLED--;

            this.Controls.Add(image = new TestPictureBox() {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BorderStyle = BorderStyle.None
            });

            this.Controls.Add(next = new Button() {
                Text = ">",
                Dock = DockStyle.Right,
                Width = 20
            });

            next.Click += (sender, nex) => Settings.Model.PreviewLED++;

            this.Controls.Add(colors = new TestPanel() {
                Dock = DockStyle.Bottom,
                Height = 50
            });

            tooltip.SetToolTip(colors, "Color");
            colors.MouseMove += (sender, e) =>
            {
                var tip = tooltips.FirstOrDefault(x => x.rectangle.Contains(e.Location));
                if(tip == null)
                    tooltip.Hide(colors);
                else {
                    var location = new Point(e.Location.X + 9, e.Location.Y + 6);
                    tooltip.Show($"RGB({tip.color.R},{tip.color.G},{tip.color.B})\n{tip.count}", colors, location);
                }
            };

            colors.MouseLeave += (sender, e) => tooltip.Hide(colors);

            this.Text = "Dominant Color";
            this.DoubleBuffered = true;
            this.Padding = Padding.Empty;
        }

        public static void HideSlice() {
            if(!slice.IsDisposed)
                slice.Close();
        }
        
        public static void ShowSlice() {
            if(slice.IsDisposed)
                slice = new CentroidColorForm();

            slice.Show();
        }

        internal static void setColors(CentroidBase[] ks) 
        {
            if(!colors.IsHandleCreated) 
                return;

            if(colors.InvokeRequired) 
            {
                try { colors.Invoke((MethodInvoker)(() => setColors(ks))); } catch { }
                return;
            }

            tooltips.Clear();

            using(var g = colors.CreateGraphics()) 
            {
                int w, x = 0;
                var width = colors.Width;
                var total = ks.Sum(z => z.memberCount);
                var pad = width - ks.Sum(z => width * z.memberCount / total);
                foreach(var k in ks.Where(z => z.memberCount > 0).OrderByDescending(z => z.memberCount)) 
                {
                    w = width * k.memberCount / total;
                    if(pad > 0) {
                        w += pad;
                        pad = 0;
                    }

                    var color = Color.FromArgb(k.mean[0], k.mean[1], k.mean[2]);
                    var rectangle = new Rectangle(x, 0, w, 50);
                    tooltips.Add(new tips() { color = color, rectangle = rectangle, count = k.memberCount });

                    using(var brush = new SolidBrush(color))
                        g.FillRectangle(brush, rectangle);

                    x += w;
                }
            }

            colors.Update();
        }

        internal static void setBackground(ref byte[] buffer, ref Rectangle area, int padding) 
        {
            if(!slice.IsHandleCreated) return;

            int w = area.Width, h = area.Height, x = 0, end = w * h, dx = 0,
                ix = (((w * 4) + padding) * area.Y) + (area.X * 4);

            var background = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            var data = background.LockBits(new Rectangle(0,0,w,h), ImageLockMode.ReadWrite, background.PixelFormat);

            unsafe 
            {
                byte* bmp = (byte*)data.Scan0.ToPointer();
                while(x++ < end) 
                {
                    bmp[dx] = buffer[ix];
                    bmp[dx+1] = buffer[ix+1];
                    bmp[dx+2] = buffer[ix+2];

                    ix += 4;
                    dx += 3;
                    if(x % area.Width == 0) 
                    {
                        ix += padding;
                        dx += data.Stride-(w*3);
                    }
                }
            }

            background.UnlockBits(data);
            image.Image = background;
        }
    } 
}