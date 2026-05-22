using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KeserKnight;

namespace KeserKnight
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            // --- BU SATIRI EKLE: Windows'un DPI ölçeklemesini kapatır, kaymaları engeller ---
            if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        // Bu DLL import kodunu da Main'in hemen altına (class'ın içine) yapıştır usta
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }
}
