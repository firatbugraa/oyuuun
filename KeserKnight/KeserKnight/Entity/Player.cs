using System;
using System.Drawing;

namespace KeserKnight.Entity
{
    public class Player
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

        // Form1 ve Fizik Motorunun doğrudan okuduğu kilit kapsül
        public Rectangle Hitbox { get; private set; }

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
            _x = x;
            _y = y;
            _width = width;
            _height = height;
            this.Texture = texture;
            UpdateHitbox();
        }

        private void UpdateHitbox()
        {
            int paddingX = 25; // Sağ ve soldan kırpılacak miktar

            Hitbox = new Rectangle(
                _x + paddingX,
                _y,
                _width - (paddingX * 2),
                _height
            );
        }

        // --- GÜNCELLEME MOTORU ---
        public void Update()
        {
            if (IsInvincible)
            {
                InvincibilityTimer++;
                if (InvincibilityTimer >= InvincibilityDuration)
                {
                    IsInvincible = false;
                }
            }

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
            if (CurrentHealth <= 0) return true;

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
            _x = startX;
            _y = startY;
            CurrentDirection = Direction.Right;
            AttackHitbox = Rectangle.Empty;
            UpdateHitbox();
        }
    }
}