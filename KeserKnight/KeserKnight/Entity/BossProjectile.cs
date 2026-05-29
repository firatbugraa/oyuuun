using System;
using System.Drawing;

namespace KeserKnight.Combat
{
    public class BossProjectile
    {
        public Rectangle Hitbox { get; private set; }

        private int basePriceY; // Merminin ilk fırlatıldığı ağız yüksekliği
        private int speedX = -10; // Sola doğru ilerleme hızı
        private float waveTimer = 0f;

        // Shovel Knight tarzı pürüzsüz dalga ayarları
        private float waveSpeed = 0.08f; // Dalgalanma frekansı (hızı)
        private float amplitude = 120f;  // Dalgalanma yüksekliği (genliği)
        private int waveDirection = 1;   // 1: Önce Yukarı, -1: Önce Aşağı kavis

        public BossProjectile(int x, int y, int width, int height, bool moveUpFirst)
        {
            Hitbox = new Rectangle(x, y, width, height);
            this.basePriceY = y;
            this.waveDirection = moveUpFirst ? 1 : -1;
        }

        public void Update()
        {
            // Zamanlayıcıyı ilerlet
            waveTimer += waveSpeed;

            // Yatayda sola doğru sabit ilerleme
            int nextX = Hitbox.X + speedX;

            // Dikeyde Sinüs dalgası hesaplama (waveDirection ile yukarı/aşağı kavis yönü belirlenir)
            int nextY = basePriceY - (int)(Math.Sin(waveTimer) * amplitude * waveDirection);

            // Kutuyu güncelle
            Hitbox = new Rectangle(nextX, nextY, Hitbox.Width, Hitbox.Height);
        }

        public void Draw(Graphics g)
        {
            // Ritmik parlayan alev topu tasarımı
            using (SolidBrush fireBrush = new SolidBrush(Color.FromArgb(255, 90, 0)))
            {
                g.FillEllipse(fireBrush, Hitbox);
            }
            using (SolidBrush coreBrush = new SolidBrush(Color.Gold))
            {
                g.FillEllipse(coreBrush, Hitbox.X + 8, Hitbox.Y + 8, Hitbox.Width - 16, Hitbox.Height - 16);
            }
            using (Pen glowPen = new Pen(Color.White, 2f))
            {
                g.DrawEllipse(glowPen, Hitbox);
            }
        }
    }
}