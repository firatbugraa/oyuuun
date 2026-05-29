using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using KeserKnight.Combat;

namespace KeserKnight.Entity
{
    public class GuardianGriffon
    {
        private int baseOriginX;
        private int baseOriginY;
        private int baseWidth;
        private int baseHeight;

        public Rectangle Hitbox { get; private set; }
        public int MaxHealth { get; private set; } = 100;
        public int CurrentHealth { get; private set; } = 100;
        public bool IsDead { get; private set; } = false;
        public int HurtTimer { get; private set; } = 0;

        public Rectangle SwipeHitbox { get; private set; }
        public bool IsSwiping { get; private set; } = false;

        public List<BossProjectile> Projectiles { get; private set; } = new List<BossProjectile>();
        public List<DeathExplosion> Explosions { get; private set; } = new List<DeathExplosion>();

        public enum BossState { Idle, FireAttack, SwipeAttack, Recovery, Dead }
        public BossState CurrentState { get; private set; } = BossState.Idle;

        private int stateTimer = 0;
        private int cooldownTimer = 0;
        private Random rand = new Random();

        public bool IsMouthOpen { get; private set; } = false;
        public bool IsTelegraphingSwipe { get; private set; } = false;
        public bool IsExhausted { get; private set; } = false;

        public bool SequenceFinished { get; private set; } = false;
        private int flashCounter = 0;

        private Image[] frames;
        private Image firePoseSprite;
        private int currentFrame = 0;
        private int frameTimer = 0;
        public int AnimationSpeed { get; set; } = 6;

        public GuardianGriffon(int x, int y, int width, int height, Image spriteSheet, Image firePoseSheet)
        {
            Hitbox = new Rectangle(x, y, width, height);
            this.baseOriginX = x;
            this.baseOriginY = y;
            this.baseWidth = width;
            this.baseHeight = height;

            if (firePoseSheet != null)
            {
                Bitmap bmpFire = new Bitmap(firePoseSheet);
                firePoseSprite = CleanGreenEdges(bmpFire);
            }

            frames = new Image[10];
            if (spriteSheet != null)
            {
                Bitmap bmpSheet = new Bitmap(spriteSheet);
                bmpSheet = CleanGreenEdges(bmpSheet);

                int frameWidth = bmpSheet.Width / 10;
                int frameHeight = bmpSheet.Height;

                for (int i = 0; i < 10; i++)
                {
                    Bitmap frame = new Bitmap(frameWidth, frameHeight);
                    using (Graphics g = Graphics.FromImage(frame))
                    {
                        g.DrawImage(bmpSheet,
                            new Rectangle(0, 0, frameWidth, frameHeight),
                            new Rectangle(i * frameWidth, 0, frameWidth, frameHeight),
                            GraphicsUnit.Pixel);
                    }
                    frames[i] = frame;
                }
            }
        }

        private Bitmap CleanGreenEdges(Bitmap original)
        {
            Bitmap cleanBmp = new Bitmap(original.Width, original.Height);
            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    Color pixel = original.GetPixel(x, y);
                    if (pixel.G > 140 && pixel.R < 100 && pixel.B < 100)
                    {
                        cleanBmp.SetPixel(x, y, Color.Transparent);
                    }
                    else
                    {
                        cleanBmp.SetPixel(x, y, pixel);
                    }
                }
            }
            return cleanBmp;
        }

        public void Update(Player player)
        {
            if (SequenceFinished) return;
            if (HurtTimer > 0) HurtTimer--;

            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                Projectiles[i].Update();
                if (Projectiles[i].Hitbox.X < -50) Projectiles.RemoveAt(i);
            }

            for (int i = Explosions.Count - 1; i >= 0; i--)
            {
                Explosions[i].Update();
                if (Explosions[i].IsFinished) Explosions.RemoveAt(i);
            }

            stateTimer++;

            switch (CurrentState)
            {
                case BossState.Dead:
                    IsSwiping = false; IsMouthOpen = false; IsTelegraphingSwipe = false; IsExhausted = false;
                    SwipeHitbox = Rectangle.Empty;
                    Projectiles.Clear();
                    flashCounter++;

                    if (stateTimer < 90 && flashCounter % 5 == 0)
                    {
                        int rx = rand.Next(Hitbox.X, Hitbox.Right);
                        int ry = rand.Next(Hitbox.Y, Hitbox.Bottom);
                        Explosions.Add(new DeathExplosion(rx, ry));
                    }

                    if (stateTimer >= 120) SequenceFinished = true;
                    return;

                case BossState.Idle:
                    if (cooldownTimer > 0) { cooldownTimer--; break; }
                    if (stateTimer >= 15)
                    {
                        stateTimer = 0;
                        int distanceToPlayer = Math.Abs((player.Hitbox.X + player.Hitbox.Width / 2) - Hitbox.X);
                        if (distanceToPlayer < 260)
                        {
                            if (rand.Next(100) < 85) CurrentState = BossState.SwipeAttack;
                            else CurrentState = BossState.FireAttack;
                        }
                        else CurrentState = BossState.FireAttack;
                    }
                    break;

                case BossState.FireAttack:
                    IsMouthOpen = true;
                    if (stateTimer == 15) Projectiles.Add(new BossProjectile(Hitbox.X - 30, Hitbox.Y + 60, 45, 45, true));
                    else if (stateTimer == 40) Projectiles.Add(new BossProjectile(Hitbox.X - 30, Hitbox.Y + 60, 45, 45, false));
                    else if (stateTimer == 65) Projectiles.Add(new BossProjectile(Hitbox.X - 30, Hitbox.Y + 60, 45, 45, true));

                    if (stateTimer >= 95) { stateTimer = 0; CurrentState = BossState.Recovery; }
                    break;

                case BossState.SwipeAttack:
                    if (stateTimer < 15)
                    {
                        IsTelegraphingSwipe = true;
                        Hitbox = new Rectangle(baseOriginX + 25, baseOriginY, baseWidth, baseHeight);
                        currentFrame = 4;
                    }
                    else if (stateTimer >= 15 && stateTimer < 35)
                    {
                        IsTelegraphingSwipe = false;
                        IsSwiping = true;
                        Hitbox = new Rectangle(baseOriginX - 35, baseOriginY, baseWidth + 35, baseHeight);

                        int swipeProgress = stateTimer - 15;
                        currentFrame = 5 + (swipeProgress / 4);
                        if (currentFrame > 9) currentFrame = 9;

                        int waveOffset = 80 + (swipeProgress * 15);
                        SwipeHitbox = new Rectangle(baseOriginX - waveOffset, Hitbox.Y + 80, 240, 180);
                    }
                    else if (stateTimer >= 35)
                    {
                        IsSwiping = false; SwipeHitbox = Rectangle.Empty;
                        Hitbox = new Rectangle(baseOriginX, baseOriginY, baseWidth, baseHeight);
                        currentFrame = 9;
                    }

                    if (stateTimer >= 50) { stateTimer = 0; CurrentState = BossState.Recovery; }
                    break;

                case BossState.Recovery:
                    IsExhausted = true; IsMouthOpen = false;
                    if (stateTimer >= 45) { stateTimer = 0; IsExhausted = false; cooldownTimer = 60; CurrentState = BossState.Idle; }
                    break;
            }

            if (CurrentState == BossState.Idle || CurrentState == BossState.Recovery)
            {
                frameTimer++;
                if (frameTimer >= AnimationSpeed)
                {
                    frameTimer = 0;
                    currentFrame = (currentFrame == 0) ? 1 : 0;
                }
            }
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || HurtTimer > 0) return;
            int finalAmount = IsExhausted ? (int)(amount * 1.5f) : amount;
            CurrentHealth -= finalAmount;
            HurtTimer = 15;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                IsDead = true;
                CurrentState = BossState.Dead;
                stateTimer = 0;
                flashCounter = 0;
            }
        }

        public void Draw(Graphics g)
        {
            if (SequenceFinished) return;

            // Çizim alanı DAİMA sabit. Fiziksel hitbox değişse de resim asla esnemeyecek.
            Rectangle drawRect = new Rectangle(baseOriginX, baseOriginY, baseWidth, baseHeight);

            GraphicsState state = g.Save();
            g.TranslateTransform(drawRect.X + drawRect.Width, drawRect.Y);
            g.ScaleTransform(-1, 1);

            Rectangle mirroredRect = new Rectangle(0, 0, drawRect.Width, drawRect.Height);

            if (CurrentState == BossState.Dead)
            {
                Brush deathBrush = (stateTimer > 90) ? Brushes.DimGray : Brushes.White;
                g.FillRectangle(deathBrush, mirroredRect);
                g.Restore(state);
                foreach (var exp in Explosions) exp.Draw(g);
                return;
            }

            if (CurrentState == BossState.FireAttack && firePoseSprite != null)
            {
                g.DrawImage(firePoseSprite, mirroredRect);
            }
            else if (frames != null && frames.Length > 0)
            {
                g.DrawImage(frames[currentFrame], mirroredRect);
            }
            else
            {
                g.FillRectangle(Brushes.Gold, mirroredRect);
            }

            if (HurtTimer > 0)
            {
                using (SolidBrush damageFilter = new SolidBrush(Color.FromArgb(120, 255, 0, 0)))
                    g.FillRectangle(damageFilter, mirroredRect);
            }

            g.Restore(state);

            foreach (var proj in Projectiles) proj.Draw(g);

            if (IsSwiping && !SwipeHitbox.IsEmpty)
            {
                using (Pen swipePen = new Pen(Color.White, 6f))
                {
                    swipePen.StartCap = LineCap.Round; swipePen.EndCap = LineCap.Round;
                    g.DrawArc(swipePen, SwipeHitbox.X, SwipeHitbox.Y, SwipeHitbox.Width, SwipeHitbox.Height, 120, 120);
                }
            }

            int barWidth = 600; int barHeight = 25; int barX = (1920 - barWidth) / 2; int barY = 150;
            g.FillRectangle(Brushes.Black, barX, barY, barWidth, barHeight);
            float healthRatio = (float)CurrentHealth / MaxHealth;
            int currentBarWidth = (int)(barWidth * healthRatio);
            using (SolidBrush healthBrush = new SolidBrush(Color.FromArgb(220, 40, 40))) g.FillRectangle(healthBrush, barX, barY, currentBarWidth, barHeight);
            using (Pen borderPen = new Pen(Color.White, 3f)) g.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);
            using (Font font = new Font("Impact", 18, FontStyle.Regular)) g.DrawString("GUARDIAN GRIFFON", font, Brushes.White, barX, barY - 35);
        }
    }
}