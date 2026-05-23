using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace KeserKnight.Entity
{
    public class Enemy
    {
        // --- GEOMETRİK SINIRLAR (ÇELİŞKİSİZ MOTOR) ---
        private int _x;
        private int _y;
        private int _width;
        private int _height;

        public int X
        {
            get => _x;
            set { _x = value; UpdateHitbox(); }
        }

        public int Y
        {
            get => _y;
            set { _y = value; UpdateHitbox(); }
        }

        public int Width => _width;
        public int Height => _height;

        // Dışarıdan çarpışma tespiti için okunacak mülk (Property)
        public Rectangle Hitbox { get; private set; }

        private int speed = 4;
        private int direction = 1; // 1 = Sağ, -1 = Sol
        private int leftBound;    // Devriye atacağı sol sınır
        private int rightBound;   // Devriye atacağı sağ sınır

        public Image Texture { get; set; }

        public Enemy(int x, int y, int width, int height, int patrolRange, Image texture = null)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            leftBound = x - patrolRange;
            rightBound = x + patrolRange;
            this.Texture = texture ?? KeserKnight.Properties.Resources.anadusman;
            UpdateHitbox();
        }

        private void UpdateHitbox()
        {
            int paddingX = 8; // Düşman için sağ-sol kırpma payı usta

            Hitbox = new Rectangle(
                _x + paddingX,
                _y,
                _width - (paddingX * 2),
                _height
            );
        }

        // Düşmanın hareket mantığı (Yapay Zeka)
        public void Update()
        {
            _x += speed * direction;
            UpdateHitbox();

            // Sınırlara geldiğinde yön değiştir usta
            if (_x >= rightBound)
            {
                direction = -1; // Sola dön
            }
            else if (_x <= leftBound)
            {
                direction = 1;  // Sağa dön
            }
        }

        // Düşmanı ekrana çizme fonksiyonu (RAM Dostu ve Aynalama Motorlu)
        public void Draw(Graphics g)
        {
            if (Texture != null)
            {
                // Düşmanın drawRect alanını da doğrudan kendi Hitbox sınırlarına kilitledik usta
                Rectangle drawRect = new Rectangle(
                    Hitbox.X,
                    Hitbox.Y,
                    Hitbox.Width, // Sünme ve genişleme payı sıfırlandı
                    Hitbox.Height
                );

                if (direction == -1)
                {
                    GraphicsState state = g.Save();
                    g.TranslateTransform(drawRect.X + drawRect.Width, drawRect.Y);
                    g.ScaleTransform(-1, 1);
                    g.DrawImage(Texture, 0, 0, drawRect.Width, drawRect.Height);
                    g.Restore(state);
                }
                else
                {
                    g.DrawImage(Texture, drawRect);
                }
            }
            else
            {
                g.FillRectangle(Brushes.Crimson, Hitbox);
            }
        }
    }
}