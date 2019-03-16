using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;

namespace OpenSimplexNoiseSample {
    public partial class FormMain : Form {
        private enum Modes {
            BW = 0,
            Color = 1
        }

        private double xInc = 0.045;
        private double yInc = 0.045;
        private double zInc = 0.005;
        private OpenSimplexNoise noise = new OpenSimplexNoise();
        private DirectBitmap bmp;
        private object syncObj = new object();
        private HLSRGB c = new HLSRGB() { Luminance = 0.5F, Saturation = 0.8F };
        private Modes mode = Modes.Color;
        private int resolution = 6; // 1 = Maximum resolution
        private bool pixelation = true;
        private bool helpVisible = true;
        private SizeF ss;
        private SolidBrush hlpBackColor = new SolidBrush(Color.FromArgb(196, 33, 33, 33));
        private Rectangle formBounds;
        private int frameCounter = 0;
        private string fps = "";
        private Stopwatch sw = new Stopwatch();

        public FormMain() {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e) {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.UserPaint, true);

            ss = TextRenderer.MeasureText("X", this.Font);
            ss.Width -= 9.2F;
            ss.Height -= 4.8F;

            RebuildBitmap();
            sw.Start();

            this.Resize += (object o1, EventArgs e1) => RebuildBitmap();

            this.Paint += (object o1, PaintEventArgs e1) => {
                if(pixelation) e1.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                lock(syncObj) {
                    e1.Graphics.DrawImage(bmp.Bitmap, 0, 0, this.DisplayRectangle.Width + resolution, this.DisplayRectangle.Height + resolution);
                    if(helpVisible) RenderHelp(e1.Graphics);
                }
            };

            this.KeyDown += (object o1, KeyEventArgs e1) => {
                switch(e1.KeyCode) {
                    case Keys.F1:
                        helpVisible = !helpVisible;
                        break;
                    case Keys.C:
                        mode = (mode == Modes.BW ? Modes.Color : Modes.BW);
                        break;
                    case Keys.F:
                        SwitchFullScreen();
                        break;
                    case Keys.P:
                        pixelation = !pixelation;
                        break;
                    case Keys.Add:
                    case Keys.Up:
                        if(resolution > 1) {
                            resolution -= 1;
                            RebuildBitmap();
                        }
                        break;
                    case Keys.Subtract:
                    case Keys.Down:
                        resolution += 1;
                        RebuildBitmap();
                        break;
                    case Keys.X:
                        if(e1.Shift) {
                            if(xInc > 0.005) xInc -= 0.005;
                        } else
                            xInc += 0.005;
                        break;
                    case Keys.Y:
                        if(e1.Shift) {
                            if(yInc > 0.005) yInc -= 0.005;
                        } else
                            yInc += 0.005;
                        break;
                    case Keys.Z:
                        if(e1.Shift) {
                            if(zInc > 0.005) zInc -= 0.005;
                        } else
                            zInc += 0.005;
                        break;
                }
            };

            Task.Run(() => {
                while(true) {
                    Thread.Sleep(33);
                    this.Invalidate();
                }
            });

            Task.Run(() => RenderNoise());
        }

        private void RenderNoise() {
            double xOff, yOff, zOff = 0.0;
            int x, y;
            int bValue;

            while(true) {
                for(y = 0, yOff = 0.0; y < bmp.Height; y++, yOff += xInc) {
                    for(x = 0, xOff = 0.0; x < bmp.Width; x++, xOff += yInc) {
                        switch(mode) {
                            case Modes.BW:
                                bValue = (int)((noise.Evaluate(xOff, yOff, zOff) + 1.0) * 128.0);
                                bmp.SetPixel(x, y, Color.FromArgb(bValue, bValue, bValue));
                                break;
                            case Modes.Color:
                                c.Hue = noise.Evaluate(xOff, yOff, zOff) * 360.0;
                                bmp.SetPixel(x, y, c.Color);
                                break;
                        }
                    }
                }
                zOff += zInc;

                frameCounter += 1;
                if(frameCounter >= 30) {
                    fps = $"FPS: {(double)(frameCounter * 1000) / sw.ElapsedMilliseconds:N2}";
                    frameCounter = 0;
                    sw.Restart();
                }
            }
        }

        private void RebuildBitmap() {
            lock(syncObj) {
                bmp?.Dispose();
                bmp = new DirectBitmap(this.DisplayRectangle.Width / resolution, this.DisplayRectangle.Height / resolution);
            }
        }

        private void SwitchFullScreen() {
            if(this.FormBorderStyle == FormBorderStyle.None) {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Bounds = formBounds;
                this.TopMost = false;
            } else {
                formBounds = this.Bounds;
                this.FormBorderStyle = FormBorderStyle.None;
                this.Bounds = Screen.FromPoint(this.Location).Bounds;
                this.TopMost = true;
            }
        }

        private void RenderHelp(Graphics g) {
            PointF p = new Point(10, 10);

            g.FillRectangle(hlpBackColor, p.X - 5,
                                          p.Y - 5,
                                          p.X + 5 + (42 * ss.Width),
                                          p.Y + 5 + (14 * ss.Height));

            g.DrawString(this.Text, this.Font, Brushes.OrangeRed, p);
            g.DrawString(fps, this.Font, Brushes.CadetBlue, p.X + (this.Text.Length + 19 - fps.Length) * ss.Width, p.Y);
            p.Y += ss.Height * 2;
            g.DrawString("F1:   Toggle this dialog (help)", this.Font, Brushes.Gainsboro, p);
            p.Y += ss.Height * 2;
            g.DrawString("C:    Toggle B&W and Color modes", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"P:    Toggle Pixelation   [{(pixelation ? "ON" : "OFF").PadLeft(3)}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"F:    Toggle Fullscreen   [{(this.TopMost ? "ON" : "OFF").PadLeft(3)}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"Up/+: Increase Resolution [{1.0 / resolution * 100.0:N2}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"Dn/-: Decrease Resolution [{1.0 / resolution * 100.0:N2}]", this.Font, Brushes.Gainsboro, p);
            p.Y += ss.Height * 2;
            g.DrawString("For the following, press SHIFT to decrease", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"X: X noise sweep step     [{xInc:F3}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"Y: Y noise sweep step     [{yInc:F3}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"Z: Z noise sweep step     [{zInc:F3}]", this.Font, Brushes.Gainsboro, p);
        }
    }
}
