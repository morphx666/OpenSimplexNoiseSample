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

        private double xInc = 0.01;
        private double yInc = 0.01;
        private double zInc = 0.02;
        private double zOff = 0.0;
        private OpenSimplexNoise noise = new OpenSimplexNoise();
        private DirectBitmap bmp;
        private object syncObj = new object();
        private HLSRGB c = new HLSRGB() { Luminance = 0.5F, Saturation = 0.8F };
        private Modes mode = Modes.Color;
        private int resolution = 3; // 1 = Maximum resolution
        private bool helpVisible = true;
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

            RebuildBitmap();
#if DEBUG
            sw.Start();
#endif

            this.Resize += (object o1, EventArgs e1) => {
                RebuildBitmap();
            };

            this.Paint += (object o1, PaintEventArgs e1) => {
                lock(syncObj) {
                    e1.Graphics.DrawImage(bmp.Bitmap, 0, 0, this.DisplayRectangle.Width, this.DisplayRectangle.Height);
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
                            if(xInc > 0.01) xInc -= 0.01;
                        } else
                            xInc += 0.01;
                        break;
                    case Keys.Y:
                        if(e1.Shift) {
                            if(yInc > 0.01) yInc -= 0.01;
                        } else
                            yInc += 0.01;
                        break;
                    case Keys.Z:
                        if(e1.Shift) {
                            if(zInc > 0.01) zInc -= 0.01;
                        } else
                            zInc += 0.01;
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
            SizeF ss = g.MeasureString("X", this.Font);
            ss.Width -= 6;
            ss.Height -= 6;
            PointF p = new Point(10, 10);

            g.FillRectangle(new SolidBrush(Color.FromArgb(196, 33,33,33)), p.X - 5, p.Y - 5, p.X + 5 + (42 * ss.Width), p.Y + 5 + (9 * ss.Height));

            g.DrawString("F1:   Toggle this help dialog", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height; ;
            g.DrawString("C:    Toggle B&W and Color modes", this.Font, Brushes.Gainsboro, p); p.Y += ss.Height; ;
            g.DrawString($"Up/+: Increase Resolution [{resolution}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height; ;
            g.DrawString($"Dn/-: Decrease Resolution [{resolution}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height; ;
            ; p.Y += ss.Height;
            g.DrawString("For the following, press SHIFT to decrease", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height; ;
            g.DrawString($"X: X noise swept step     [{xInc:F2}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height; ;
            g.DrawString($"Y: Y noise swept step     [{yInc:F2}]", this.Font, Brushes.Gainsboro, p.X, p.Y); p.Y += ss.Height; ;
            g.DrawString($"Z: Z noise swept step     [{zInc:F2}]", this.Font, Brushes.Gainsboro, p.X, p.Y);
        }
    }
}
