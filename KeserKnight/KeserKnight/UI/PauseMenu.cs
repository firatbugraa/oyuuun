using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace KeserKnight.UI
{
    public static class PauseMenuUI
    {
        public static void Draw(Graphics g, int pauseSelection, Rectangle resumeBtn, Rectangle settingsBtn, Rectangle mainMenuBtn)
        {
            // Arka planı hafif karartmak için transparan perde
            using (SolidBrush pauseOverlay = new SolidBrush(Color.FromArgb(180, Color.Black)))
            {
                g.FillRectangle(pauseOverlay, 0, 0, 1920, 1080);
            }

            // "OYUN DURAKLATILDI" Başlığı
            using (Font pauseTitleFont = new Font("Impact", 55, FontStyle.Bold))
            {
                string pTitleText = "OYUN DURAKLATILDI";
                int pTitleX = (1920 - TextRenderer.MeasureText(pTitleText, pauseTitleFont).Width) / 2;
                g.DrawString(pTitleText, pauseTitleFont, Brushes.Gold, pTitleX, 280);
            }

            // Devam Et Butonu
            g.FillRectangle(Brushes.DarkBlue, resumeBtn);
            if (pauseSelection == 0) g.DrawRectangle(new Pen(Color.White, 5), resumeBtn);
            else g.DrawRectangle(Pens.Cyan, resumeBtn);
            g.DrawString("DEVAM ET", new Font("Arial", 18, FontStyle.Bold), Brushes.White, resumeBtn.X + 85, resumeBtn.Y + 15);

            // Ayarlar Butonu
            g.FillRectangle(Brushes.DarkSlateGray, settingsBtn);
            if (pauseSelection == 1) g.DrawRectangle(new Pen(Color.White, 5), settingsBtn);
            else g.DrawRectangle(Pens.LightGray, settingsBtn);
            g.DrawString("AYARLAR", new Font("Arial", 18, FontStyle.Bold), Brushes.White, settingsBtn.X + 90, settingsBtn.Y + 15);

            // Ana Menü Butonu
            g.FillRectangle(Brushes.DarkRed, mainMenuBtn);
            if (pauseSelection == 2) g.DrawRectangle(new Pen(Color.White, 5), mainMenuBtn);
            else g.DrawRectangle(Pens.Red, mainMenuBtn);
            g.DrawString("ANA MENÜ", new Font("Arial", 18, FontStyle.Bold), Brushes.White, mainMenuBtn.X + 85, mainMenuBtn.Y + 15);
        }
    }
}
