using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace KeserKnight.Entity
{
    public class Enemy
    {
        //  GEOMETRİK SINIRLAR (ÇELİŞKİSİZ MOTOR) 
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

        
        //  Düşman öldüyse oyuncuya zarar vermemesi için hitbox'ı boşaltıyoruz 
        public Rectangle Hitbox => IsDead ? Rectangle.Empty : _hitbox;
        private Rectangle _hitbox;

        private int speed = 4;
        private int direction = 1; // 1 = Sağ, -1 = Sol
        private int leftBound;    // Devriye atacağı sol sınır
        private int rightBound;   // Devriye atacağı sağ sınır

        public Image Texture { get; set; }

       
        public int Health { get; set; } = 20;          // 2 vuruşta ölmesi için (10 + 10 hasar)
        public bool IsHurt { get; set; } = false;      // Hasar kilit bayrağı
        public int HurtTimer { get; set; } = 0;        // Flaş ve yerde silinme sayacı
        public bool IsDead { get; set; } = false;      // Ölüm bayrağı
        public bool IsGrounded { get; set; } = false;  // Yerde cansız yatıyor mu?
        public float DeathRotation { get; set; } = 0f; // Yan yatma açısı

        private float velocityX;
        private float velocityY;

        public Enemy(int x, int y, int width, int height, int patrolRange, Image texture = null)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            leftBound = x - patrolRange;
            rightBound = x + patrolRange;
            this.Texture = texture ?? KeserKnight.Properties.Resources.anadusman;

            // Haritadan gelen değer ne olursa olsun bizim 2 vuruş barajını dayatıyoruz 
            this.Health = 20;

            UpdateHitbox();
        }

        private void UpdateHitbox()
        {
            int paddingX = 8; // Düşman için sağ-sol kırpma payı usta

            _hitbox = new Rectangle(
                _x + paddingX,
                _y,
                _width - (paddingX * 2),
                _height
            );
        }


        //  RETRO HASAR ALMA VE SIÇRAMA MOTORU 

        public void TakeDamage(int damage, int hitDirection)
        {
            if (IsDead) return;

            Health -= damage;
            IsHurt = true;
            HurtTimer = 15; // 15 kare hasar flaşı

            
            velocityX = hitDirection * 12;
            velocityY = 0;

            if (Health <= 0)
            {
                IsDead = true;
                IsHurt = false;
                velocityY = -22;              // Havaya o istediğin sert retro sıçrayışı
                velocityX = hitDirection * 5; // Yana süzülme
                DeathRotation = 90f;          // 90 derece yan yatma
            }
        }

        // PARAMETRELİ UPDATE: Form1'deki ana döngünün çağıracağı tam senkronize metot 
        public void Update(List<Rectangle> platforms)
        {
            // 1. DURUM: Düşman öldüyse havada takla atar veya yerde hareketsiz silinir
            if (IsDead)
            {
                if (!IsGrounded)
                {
                    velocityY += 1.2f; // Yerçekimi ivmesi
                    _x += (int)velocityX;
                    _y += (int)velocityY;
                    UpdateHitbox();

                    // Platform tespiti (Yerde kalma motoru)
                    if (Form1.GlobalPlatforms != null)
                    {
                        foreach (var platform in Form1.GlobalPlatforms)
                        {
                            if (this._hitbox.IntersectsWith(platform) && velocityY > 0)
                            {
                                _y = platform.Top - _height + 14; // Zemine tam oturt 
                                velocityY = 0;
                                velocityX = 0;
                                IsGrounded = true;
                                UpdateHitbox();
                                break;
                            }
                        }
                    }
                }
                else
                {
                    HurtTimer++; // Yerde yattığı sürece silinme sayacını artır 
                }
                return; 
            }

            // 2. DURUM: Düşman darbe aldıysa savrulur (Yürüyemez)
            // Enemy.cs -> Update metodu içindeki 2. DURUM:
            if (IsHurt)
            {
                HurtTimer--;

                
                _x += (int)velocityX;
                UpdateHitbox();

                velocityX *= 0.80f; 

                if (HurtTimer <= 0)
                {
                    IsHurt = false; // Hasar durumundan çık, normal yürümeye geri dön
                }
                return; 
            }

            // 3. DURUM: ARKADAŞININ ORİJİNAL YAPAY ZEKASI (DOKUNULMADI)
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

        // Form1'deki eski çağrıların patlamaması için yedek köprü metodu usta
        public void Update()
        {
            Update(Form1.GlobalPlatforms);
        }

        // Düşmanı ekrana çizme fonksiyonu
        public void Draw(Graphics g)
        {
            //  ÖLÜYKEN ÇİZİM KATMANI: 90 Derece Yan Yatmış ve Fade-out (Silinme) Efektli 
            if (IsDead)
            {
                var state = g.Save();
                g.TranslateTransform(_x + _width / 2, _y + _height / 2);
                g.RotateTransform(DeathRotation);

                // Yerde kaldıkça opaklık erir usta (255'ten 0'a)
                int alpha = 255 - (HurtTimer * 3);
                if (alpha < 0) alpha = 0;

                if (Texture != null)
                {
                    using (System.Drawing.Imaging.ImageAttributes ia = new System.Drawing.Imaging.ImageAttributes())
                    {
                        float[][] ptsArray = {
                            new float[] {1, 0, 0, 0, 0},
                            new float[] {0, 1, 0, 0, 0},
                            new float[] {0, 0, 1, 0, 0},
                            new float[] {0, 0, 0, (float)alpha/255f, 0}, 
                            new float[] {0, 0, 0, 0, 1}
                        };
                        ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(ptsArray));
                        g.DrawImage(Texture, new Rectangle(-_width / 2, -_height / 2, _width, _height), 0, 0, Texture.Width, Texture.Height, GraphicsUnit.Pixel, ia);
                    }
                }
                else
                {
                    using (SolidBrush fadeBrush = new SolidBrush(Color.FromArgb(alpha, Color.Crimson)))
                    {
                        g.FillRectangle(fadeBrush, -_width / 2, -_height / 2, _width, _height);
                    }
                }

                g.Restore(state);
                return;
            }

           
            if (Texture != null)
            {
                Rectangle drawRect = new Rectangle(Hitbox.X, Hitbox.Y, Hitbox.Width, Hitbox.Height);

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

                // Hasar aldığında kırmızı flaş katmanını üzerine bindiriyoruz 
                if (IsHurt && HurtTimer % 4 < 2)
                {
                    using (SolidBrush flashBrush = new SolidBrush(Color.FromArgb(150, Color.Red)))
                    {
                        g.FillRectangle(flashBrush, drawRect);
                    }
                }
            }
            else
            {
                g.FillRectangle(IsHurt ? Brushes.Red : Brushes.Crimson, Hitbox);
            }
        }
    }
}