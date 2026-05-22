using System;
using System.Drawing;

namespace KeserKnight.Entity
{
    public class Player
    {
        // --- GEOMETRİK SINIRLAR ---
        public Rectangle Hitbox;
        public int X { get => Hitbox.X; set => Hitbox.X = value; }
        public int Y { get => Hitbox.Y; set => Hitbox.Y = value; }
        public int Width => Hitbox.Width;
        public int Height => Hitbox.Height;
        public int Right => Hitbox.Right;
        public int Bottom => Hitbox.Bottom;
        public int Top => Hitbox.Top;

        // --- YÖN VE HAREKET SİSTEMİ ---
        public enum Direction { Left, Right }
        public Direction CurrentDirection { get; set; } = Direction.Right;
        public bool MoveLeft { get; set; } = false;
        public bool MoveRight { get; set; } = false;
        public int Speed { get; set; } = 14;

        // --- ZIPLAMA VE YERÇEKİMİ SİSTEMİ ---
        public bool IsJumping { get; set; } = false;
        public int VerticalVelocity { get; set; } = 0;
        public int Gravity { get; set; } = 3;
        public int JumpPower { get; set; } = -38;

        // --- CAN SİSTEMİ ---
        public int MaxHealth { get; set; } = 3;
        public int CurrentHealth { get; set; } = 3;

        // --- ÖLÜMSÜZLÜK (INVINCIBLE) SİSTEMİ ---
        public bool IsInvincible { get; set; } = false;
        public int InvincibilityTimer { get; set; } = 0;
        public int InvincibilityDuration { get; set; } = 40;

        // --- SALDIRI (ATTACK) SİSTEMİ ---
        public bool IsAttacking { get; set; } = false;
        public int AttackTimer { get; set; } = 0;
        public int AttackDuration { get; set; } = 10;
        public Rectangle AttackHitbox { get; set; }

        // Görsel Doku
        public Image Texture { get; set; }

        public Player(int x, int y, int width, int height, Image texture = null)
        {
            Hitbox = new Rectangle(x, y, width, height);
            this.Texture = texture;
        }

        // --- GÜNCELLEME MOTORU (Zamanlayıcı Tetiklemeleri) ---
        public void Update()
        {
            // Ölümsüzlük sayacını ilerlet
            if (IsInvincible)
            {
                InvincibilityTimer++;
                if (InvincibilityTimer >= InvincibilityDuration)
                {
                    IsInvincible = false;
                }
            }

            // Saldırı süresini denetle
            if (IsAttacking)
            {
                AttackTimer++;
                if (AttackTimer >= AttackDuration)
                {
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                }
            }
        }

        // --- HASAR ALMA MEKANİZMASI ---
        public bool TakeDamage()
        {
            if (IsInvincible) return false;

            CurrentHealth--;
            if (CurrentHealth <= 0) return true; // Öldü mü? -> True

            // Ölmediyse ölümsüzlük sürecini başlat usta
            IsInvincible = true;
            InvincibilityTimer = 0;
            return false;
        }

        // --- RESET SİSTEMİ ---
        public void Reset(int startX, int startY)
        {
            CurrentHealth = MaxHealth;
            IsInvincible = false;
            InvincibilityTimer = 0;
            IsJumping = false;
            IsAttacking = false;
            AttackTimer = 0;
            MoveLeft = false;
            MoveRight = false;
            VerticalVelocity = 0;
            X = startX;
            Y = startY;
            CurrentDirection = Direction.Right;
            AttackHitbox = Rectangle.Empty;
        }
    }
}