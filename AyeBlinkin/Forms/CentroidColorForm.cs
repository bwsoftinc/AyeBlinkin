using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

using AyeBlinkin.Centroid;

namespace AyeBlinkin.Forms 
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

        private static CentroidColorForm slice = new CentroidColorForm() { TopMost = true, ShowInTaskbar = false };
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

            previous.Click += (sender, pre) => Settings.PreviewLED--;

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

            next.Click += (sender, nex) => Settings.PreviewLED++;

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

        internal static void Update(CentroidBase c) {
            if(slice.IsDisposed)
                return;
                
            setBackground(c);
            setColors(c.Clusters);
        }

        private static void setColors(CentroidColor[] clusters) 
        {
            if(!colors.IsHandleCreated) 
                return;

            if(colors.InvokeRequired) 
            {
                try { colors.Invoke((MethodInvoker)(() => setColors(clusters))); } catch { }
                return;
            }

            tooltips.Clear();

            using(var g = colors.CreateGraphics()) 
            {
                int w, x = 0;
                var width = colors.Width;
                var total = clusters.Sum(z => z.memberCount);
                var pad = width - clusters.Sum(z => width * z.memberCount / total);
                foreach(var k in clusters.Where(z => z.memberCount > 0).OrderByDescending(z => z.memberCount)) 
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

        private static void setBackground(CentroidBase c) 
        {
            if(!slice.IsHandleCreated) return;

            int i = 0, dx = 0, ix = c.bufferIndex;
            var background = new Bitmap(c.width, c.height, PixelFormat.Format24bppRgb);
            var data = background.LockBits(new Rectangle(0,0,c.width,c.height), ImageLockMode.ReadWrite, background.PixelFormat);

            unsafe 
            {
                byte* bmp = (byte*)data.Scan0.ToPointer();
                while(i++ < c.end) 
                {
                    bmp[dx] = c.buffer[ix];
                    bmp[dx+1] = c.buffer[ix+1];
                    bmp[dx+2] = c.buffer[ix+2];

                    ix += 4;
                    dx += 3;
                    if(i % c.width == 0) 
                    {
                        ix += c.padding;
                        dx += data.Stride - (c.width * 3);
                    }
                }
            }

            background.UnlockBits(data);
            image.Image = background;
        }
    } 
}