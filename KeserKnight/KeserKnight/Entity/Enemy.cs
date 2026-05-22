using System;
using System.Drawing;

namespace KeserKnight.Entity
{
    public class Enemy
    {
        // --- PRO ÖZELLİKLER ---
        public Rectangle Hitbox;
        private int speed = 4;
        private int direction = 1; // 1 = Sağ, -1 = Sol
        private int leftBound;    // Devriye atacağı sol sınır
        private int rightBound;   // Devriye atacağı sağ sınır

        // İleride düşmana da tek bir asset (görsel) vermek istersen burayı kullanabiliriz usta
        public Image Texture { get; set; }

        // Düşmanı oluştururken yerini ve devriye alanını belirliyoruz
        public Enemy(int x, int y, int width, int height, int patrolRange, Image texture = null)
        {
            Hitbox = new Rectangle(x, y, width, height);
            leftBound = x - patrolRange;
            rightBound = x + patrolRange;
            this.Texture = texture;
        }

        // Düşmanın hareket mantığı (Yapay Zeka)
        public void Update()
        {
            // Belirlenen yöne doğru yürü
            Hitbox.X += speed * direction;

            // Sınırlara geldiğinde yön değiştir usta
            if (Hitbox.X >= rightBound)
            {
                direction = -1; // Sola dön
            }
            else if (Hitbox.X <= leftBound)
            {
                direction = 1;  // Sağa dön
            }
        }

        // Düşmanı ekrana çizme fonksiyonu (Sanal Tuval Motoruna Uygun)
        public void Draw(Graphics g)
        {
            if (Texture != null)
            {
                // Eğer düşmanın bir resmi varsa, gittiği yöne göre otomatik aynalayıp çiziyoruz usta
                if (direction == -1)
                {
                    using (Bitmap bmp = new Bitmap(Texture))
                    {
                        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX);
                        g.DrawImage(bmp, Hitbox);
                    }
                }
                else
                {
                    g.DrawImage(Texture, Hitbox);
                }
            }
            else
            {
                // Şimdilik grafiğimiz olmadığı için düşmanı o ikonik Crimson kırmızısı kutu olarak basalım
                g.FillRectangle(Brushes.Crimson, Hitbox);
            }
        }
    }
}