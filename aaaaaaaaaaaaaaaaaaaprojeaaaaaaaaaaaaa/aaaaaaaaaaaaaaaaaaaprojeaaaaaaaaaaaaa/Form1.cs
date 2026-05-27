using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Threading;
using oyun1.Engine;

namespace oyun1
{
    public partial class Form1 : Form
    {
        private readonly Game _game;
        private const double TargetFPS = 60.0;
        private const double TargetFrameTime = 1.0 / TargetFPS;

        public Form1()
        {
            this.Text = "Shotgun Knight";
            this.ClientSize = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // WinForms'un kendi çizim döngüsünü kapatıp kontrolü tamamen elomuza alıyoruz
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.UserPaint |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.Opaque, true);
            this.UpdateStyles();

            // CRITICAL FOCUS FIX: Formun klavye girdilerini kaçırmasını ve donmasını önleyen kilit satırlar
            this.KeyPreview = true;
            this.Focus();

            // Girdi ve Klavye Dinleyicileri
            this.KeyDown += (s, e) => Input.SetKeyState(e.KeyCode, true);
            this.KeyUp += (s, e) => Input.SetKeyState(e.KeyCode, false);

            // Oyun Motorunu Başlatıyoruz (Constructor kendi içinde Initialize metodunu çalıştırır)
            _game = new Game();

            // Oyun döngüsünü Windows mesaj pompasına bağlama
            Application.Idle += Application_Idle;
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            while (IsApplicationIdle())
            {
                double frameStartTime = Time.TotalTime;

                _game.Update();
                this.Invalidate();

                double frameEndTime = Time.TotalTime;
                double timeTaken = frameEndTime - frameStartTime;

                if (timeTaken < TargetFrameTime)
                {
                    int sleepTimeMs = (int)((TargetFrameTime - timeTaken) * 1000);
                    if (sleepTimeMs > 0)
                    {
                        Thread.Sleep(sleepTimeMs);
                    }
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

            // Game.cs içindeki yeni metodumuz çağrılıyor
            _game.Draw(e.Graphics);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr hWnd;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        [DllImport("user32.dll")]
        public static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

        private bool IsApplicationIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        }
    }
}