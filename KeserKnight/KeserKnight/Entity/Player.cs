using System;
using System.Drawing;

namespace KeserKnight.Entity
{
    public class Player
    {
        private int _x;
        private int _y;
        private int _width;
        private int _height;

        public int X { get => _x; set { _x = value; UpdateHitbox(); } }
        public int Y { get => _y; set { _y = value; UpdateHitbox(); } }
        public int Width => _width;
        public int Height => _height;
        public Rectangle Hitbox { get; private set; }
        public int Right => Hitbox.Right;
        public int Bottom => Hitbox.Bottom;
        public int Top => Hitbox.Top;

        public enum Direction { Left, Right }
        public Direction CurrentDirection { get; set; } = Direction.Right;
        public bool MoveLeft { get; set; } = false;
        public bool MoveRight { get; set; } = false;
        public int Speed { get; set; } = 14;

        public bool IsJumping { get; set; } = false;
        public float VerticalVelocity { get; set; } = 0;
        public int Gravity { get; set; } = 3;
        public int JumpPower { get; set; } = -38;

        public int MaxHealth { get; set; } = 3;
        public int CurrentHealth { get; set; } = 3;

        public bool IsInvincible { get; set; } = false;
        public int InvincibilityTimer { get; set; } = 0;
        public int InvincibilityDuration { get; set; } = 40;

        public bool IsAttacking { get; set; } = false;
        public int AttackTimer { get; set; } = 0;
        public int AttackDuration { get; set; } = 15;
        public Rectangle AttackHitbox { get; set; }

        // --- İŞTE ÇÖZÜM BURADA: Gerçek boyutu ve Y eksenini değiştiriyoruz ---
        private bool _isCrouching = false;
        public bool IsCrouching
        {
            get => _isCrouching;
            set
            {
                if (_isCrouching != value)
                {
                    _isCrouching = value;
                    int crouchOffset = 30; // 30 Piksel tepeden bastırıyoruz

                    if (_isCrouching)
                    {
                        _y += crouchOffset;      // Aşağı kaydır ki ayaklar yere bassın
                        _height -= crouchOffset; // Boyu kısalt ki tepeden basık görünsün
                    }
                    else
                    {
                        _y -= crouchOffset;      // Ayağa kalktığında Y'yi eski yerine al
                        _height += crouchOffset; // Boyu eski haline getir
                    }
                    UpdateHitbox();
                }
            }
        }

        public enum PlayerState { Idle, Run, Jump, Crouch }
        public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
        private PlayerState _previousState = PlayerState.Idle;

        private Image[] idleFrames;
        private Image[] runFrames;
        private Image[] jumpFrames;
        private Image[] crouchFrames;

        private int currentFrame = 0;
        private int frameTimer = 0;
        public int AnimationSpeed { get; set; } = 4;

        public Player(int x, int y, int width, int height, Image idleSheet, Image runSheet, Image jumpSheet, Image crouchSheet)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;

            idleFrames = ExtractFrames(idleSheet, 2);
            runFrames = ExtractFrames(runSheet, 4);
            jumpFrames = ExtractFrames(jumpSheet, 2);
            crouchFrames = ExtractFrames(crouchSheet, 2);

            UpdateHitbox();
        }

        private Image[] ExtractFrames(Image sheet, int frameCount)
        {
            if (sheet == null) return new Image[0];
            Image[] frames = new Image[frameCount];
            int frameWidth = sheet.Width / frameCount;
            int frameHeight = sheet.Height;

            for (int i = 0; i < frameCount; i++)
            {
                Bitmap bmp = new Bitmap(frameWidth, frameHeight);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.DrawImage(sheet,
                        new Rectangle(0, 0, frameWidth, frameHeight),
                        new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                        GraphicsUnit.Pixel);
                }
                frames[i] = bmp;
            }
            return frames;
        }

        private void UpdateHitbox()
        {
            int paddingX = 30;
            // Hitbox artık dinamik _y ve _height değerlerini takip ediyor
            Hitbox = new Rectangle(_x + paddingX, _y, _width - (paddingX * 2), _height);
        }

        public void Update()
        {
            if (IsInvincible)
            {
                InvincibilityTimer++;
                if (InvincibilityTimer >= InvincibilityDuration) IsInvincible = false;
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

            if (VerticalVelocity != 0) CurrentState = PlayerState.Jump;
            else if (IsCrouching) CurrentState = PlayerState.Crouch;
            else if (MoveLeft || MoveRight) CurrentState = PlayerState.Run;
            else CurrentState = PlayerState.Idle;

            if (CurrentState != _previousState)
            {
                currentFrame = 0;
                frameTimer = 0;
                _previousState = CurrentState;
                UpdateHitbox();
            }

            if (CurrentState == PlayerState.Jump || CurrentState == PlayerState.Crouch)
            {
                currentFrame = 1;
            }
            else if (CurrentState == PlayerState.Idle)
            {
                currentFrame = 0;
            }
            else
            {
                frameTimer++;
                if (frameTimer >= AnimationSpeed)
                {
                    frameTimer = 0;
                    currentFrame++;
                    int maxFrames = GetCurrentFrameCount();
                    if (maxFrames > 0 && currentFrame >= maxFrames)
                    {
                        currentFrame = 0;
                    }
                }
            }
        }

        private int GetCurrentFrameCount()
        {
            switch (CurrentState)
            {
                case PlayerState.Run: return runFrames.Length;
                case PlayerState.Jump: return jumpFrames.Length;
                case PlayerState.Crouch: return crouchFrames.Length;
                case PlayerState.Idle:
                default: return idleFrames.Length;
            }
        }

        public Image GetCurrentFrameImage()
        {
            switch (CurrentState)
            {
                case PlayerState.Run: return runFrames.Length > 0 ? runFrames[currentFrame] : null;
                case PlayerState.Jump: return jumpFrames.Length > 0 ? jumpFrames[currentFrame] : null;
                case PlayerState.Crouch: return crouchFrames.Length > 0 ? crouchFrames[currentFrame] : null;
                case PlayerState.Idle:
                default: return idleFrames.Length > 0 ? idleFrames[currentFrame] : null;
            }
        }

        public bool TakeDamage()
        {
            if (IsInvincible) return false;
            CurrentHealth--;
            if (CurrentHealth <= 0) return true;
            IsInvincible = true;
            InvincibilityTimer = 0;
            return false;
        }

        public void Reset(int startX, int startY)
        {
            CurrentHealth = MaxHealth;
            IsInvincible = false;
            InvincibilityTimer = 0;
            IsJumping = false;
            IsAttacking = false;
            IsCrouching = false;
            AttackTimer = 0;
            MoveLeft = false;
            MoveRight = false;
            VerticalVelocity = 0;
            _x = startX;
            _y = startY;
            CurrentDirection = Direction.Right;
            AttackHitbox = Rectangle.Empty;
            CurrentState = PlayerState.Idle;
            UpdateHitbox();
        }
    }
}