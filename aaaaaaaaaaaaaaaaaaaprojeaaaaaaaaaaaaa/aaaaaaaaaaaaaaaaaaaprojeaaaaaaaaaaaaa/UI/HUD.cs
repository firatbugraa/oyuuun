using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using oyun1.Entities;

namespace oyun1.UI
{
    public class HUD
    {
        // UI Renk Paleti (Retro Shovel Knight Esintili)
        private readonly SolidBrush _bgBrush = new SolidBrush(Color.FromArgb(10, 12, 18));       // Koyu lacivert panel fonu
        private readonly SolidBrush _borderBrush = new SolidBrush(Color.FromArgb(90, 105, 135)); // Metalik mavi/gri çerçeve
        private readonly SolidBrush _hpDarkBrush = new SolidBrush(Color.FromArgb(50, 15, 20));    // Boş can yuvası
        private readonly SolidBrush _hpRedBrush = new SolidBrush(Color.FromArgb(215, 35, 55));    // Aktif Kırmızı Can
        private readonly SolidBrush _textBrush = new SolidBrush(Color.Yellow);                   // Altın sarısı yazı rengi

        private readonly Font _retroFont;

        public HUD()
        {
            // Bilgisayarda özel piksel fontu olmasa bile retro durması için kalın ve dekoratif bir font ailesi seçiyoruz
            _retroFont = new Font("Courier New", 12, FontStyle.Bold);
        }

        public void Render(System.Drawing.Graphics g, Player player)
        {
            // --- 1. ANA ARKA PLAN PANELİ (Ekrana Sabit Üst Bar) ---
            // Genişlik: 800 piksel, Yükseklik: 50 piksel
            g.FillRectangle(_bgBrush, 0, 0, 800, 50);

            // Pikselsel Alt Çerçeve Çizgisi (3 piksel kalınlığında)
            g.FillRectangle(_borderBrush, 0, 47, 800, 3);

            // --- 2. RETRO METİN GÖSTERGELERİ ---
            g.DrawString("WARDEN", _retroFont, Brushes.White, 15, 15);

            // --- 3. DİNAMİK CAN KRİSTALLERİ SİSTEMİ (HP DISPLAY) ---
            // Shovel Knight'taki gibi canı yan yana dizilmiş pikselsel kare yuvalar olarak çiziyoruz
            int startX = 100;
            int startY = 15;
            int crystalSize = 16;
            int gap = 8;

            int maxCrystals = (int)(player.MaxHealth / 10);
            int currentCrystals = (int)Math.Ceiling(player.Health / 10);

            for (int i = 0; i < maxCrystals; i++)
            {
                int xPos = startX + (i * (crystalSize + gap));

                // Kristal çerçevesi/yuvası
                g.FillRectangle(_borderBrush, xPos, startY, crystalSize, crystalSize);

                if (i < currentCrystals)
                {
                    // Dolu Can Kristali (İç dolgu pikselleri)
                    g.FillRectangle(_hpRedBrush, xPos + 2, startY + 2, crystalSize - 4, crystalSize - 4);
                    // Parlama efekti (Minik beyaz piksel)
                    g.FillRectangle(Brushes.White, xPos + 4, startY + 4, 3, 3);
                }
                else
                {
                    // Boş Can Yuvası
                    g.FillRectangle(_hpDarkBrush, xPos + 2, startY + 2, crystalSize - 4, crystalSize - 4);
                }
            }

            // Sayısal HP Yazısı (Örn: "HP 30/30")
            string hpText = $"HP {Math.Max(0, (int)player.Health)}/{(int)player.MaxHealth}";
            g.DrawString(hpText, _retroFont, _textBrush, startX + (maxCrystals * (crystalSize + gap)) + 10, 15);

            // --- 4. KOMBO / SKOR GÖSTERGESİ ---
            // Prototip altın/skor takibi için sağ köşeye yerleştirme
            g.DrawString("GOLD: 00250", _retroFont, _textBrush, 650, 15);
        }
    }
}