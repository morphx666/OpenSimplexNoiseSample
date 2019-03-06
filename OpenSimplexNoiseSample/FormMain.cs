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
#if DEBUG
        private int frameCounter = 0;
        private Stopwatch sw = new Stopwatch();
#endif 

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
#if DEBUG
            sw.Start();
#endif

            this.Resize += (object o1, EventArgs e1) => {
                RebuildBitmap();
            };

            this.Paint += (object o1, PaintEventArgs e1) => {
                // hmmm... I really prefer the pixelation effect with 'NearestNeighbor'
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
                    Thread.Sleep(1000 / 30);
                    this.Invalidate();
                }
            });

            Task.Run(() => {
                RenderNoise();
            });
        }

        private void RenderNoise() {
            double xOff;
            double yOff;
            double zOff = 0.0;
            int x;
            int y;
            int bValue;

            while(true) {
                lock(syncObj) {
                    xOff = 0.0;
                    for(x = 0; x < bmp.Width; x++) {
                        xOff += xInc;
                        yOff = 0.0;
                        for(y = 0; y < bmp.Height; y++) {
                            yOff += yInc;

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

#if DEBUG
                    frameCounter += 1;
                    if(frameCounter >= 15) {
                        Debug.WriteLine($"FPS: {(double)(frameCounter * 1000) / sw.ElapsedMilliseconds:N2}");
                        frameCounter = 0;
                        sw.Restart();
                    }
#endif
                }
            }
        }

        private void RebuildBitmap() {
            lock(syncObj) {
                bmp?.Dispose();
                bmp = new DirectBitmap(this.DisplayRectangle.Width / resolution, this.DisplayRectangle.Height / resolution);
            }
        }

        private void RenderHelp(Graphics g) {
            PointF p = new Point(10, 10);

            g.FillRectangle(hlpBackColor, p.X - 5, p.Y - 5, p.X + 5 + (42 * ss.Width), p.Y + 5 + (11 * ss.Height));

            g.DrawString("F1:   Toggle this dialog (help)", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            ; p.Y += ss.Height;
            g.DrawString("C:    Toggle B&W and Color modes", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"P:    Toggle Pixelation   [{(pixelation ? "ON" : "OFF")}]", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height;
            g.DrawString($"Up/+: Increase Resolution [{resolution}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height;
            g.DrawString($"Dn/-: Decrease Resolution [{resolution}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height;
            ; p.Y += ss.Height;
            g.DrawString("For the following, press SHIFT to decrease", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height;
            g.DrawString($"X: X noise swept step     [{xInc:F3}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height;
            g.DrawString($"Y: Y noise swept step     [{yInc:F3}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height;
            g.DrawString($"Z: Z noise swept step     [{zInc:F3}]", this.Font, Brushes.Gainsboro, p.X, p.Y);
        }
    }
}
