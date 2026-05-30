using System;
using System.Drawing;

namespace KeserKnight.Entity
{
    public class CrimsonKnight
    {
        private Random rand = new Random();

        // Temel Boss Nitelikleri
        public Rectangle Hitbox { get; private set; }
        public int MaxHealth { get; private set; } = 200;
        public int CurrentHealth { get; private set; } = 200;
        public bool IsDead { get; private set; } = false;
        public int HurtTimer { get; private set; } = 0;

        // Pozisyon ve Fizik Sabitleri
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool FacingLeft { get; private set; } = true;

        private int normalSpeed = 5;
        private int chargeSpeed = 16;
        private float verticalVelocity = 0;
        private const float gravity = 1.8f;
        private int groundY = 870;

        // Durum Makinesi Safhalari
        public enum BossState { WalkTowardPlayer, ChargePrep, DashAttack, JumpSlash, FallingSlash, PhaseTransition, Recovery, Dead }
        public BossState CurrentState { get; private set; } = BossState.WalkTowardPlayer;

        private int stateTimer = 0;
        private int cooldownTimer = 30;
        private bool isPhase2 = false;
        private bool transitionTriggered = false;

        // Kombo Zincirleme Yonetim Degiskenleri
        private int comboStep = 0;
        private int maxCombo = 2;

        // Yenilgi Efekt Sayaclari
        public bool SequenceFinished { get; private set; } = false;
        private int explosionCounter = 0;

        // Saldırı Alanları
        public Rectangle AttackHitbox { get; private set; }
        public bool IsAttacking { get; private set; } = false;

        public CrimsonKnight(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.Hitbox = new Rectangle(x, y, width, height);
        }

        public void Update(Player player)
        {
            // Eger yenilgi sekansi tamamen bittiyse guncellemeyi durdur
            if (SequenceFinished) return;

            if (CurrentState == BossState.Dead)
            {
                stateTimer++;
                IsAttacking = false;
                AttackHitbox = Rectangle.Empty;
                verticalVelocity = 0;

                // 120 kare boyunca (yaklasik 2 saniye) dramatik olum sekansi oynatilir
                if (stateTimer >= 120)
                {
                    SequenceFinished = true; // Oyun sonu bitis tetikleyicisi aktif
                }
                return;
            }

            if (HurtTimer > 0) HurtTimer--;

            if (!transitionTriggered && CurrentHealth <= MaxHealth / 2)
            {
                transitionTriggered = true;
                stateTimer = 0;
                CurrentState = BossState.PhaseTransition;
            }

            stateTimer++;

            if (CurrentState != BossState.PhaseTransition && (Y + Height < groundY || verticalVelocity < 0))
            {
                verticalVelocity += gravity;
                Y += (int)verticalVelocity;
                if (Y + Height >= groundY)
                {
                    Y = groundY - Height;
                    verticalVelocity = 0;
                }
            }

            switch (CurrentState)
            {
                case BossState.PhaseTransition:
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                    verticalVelocity = 0;
                    if (stateTimer >= 60)
                    {
                        isPhase2 = true;
                        normalSpeed = 9;
                        chargeSpeed = 22;
                        maxCombo = 3;
                        stateTimer = 0;
                        cooldownTimer = 0;
                        CurrentState = BossState.WalkTowardPlayer;
                    }
                    break;

                case BossState.WalkTowardPlayer:
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                    FacingLeft = (player.Hitbox.X + player.Hitbox.Width / 2) < (X + Width / 2);

                    if (cooldownTimer > 0) cooldownTimer--;
                    else
                    {
                        X += FacingLeft ? -normalSpeed : normalSpeed;
                        if (stateTimer >= 40)
                        {
                            stateTimer = 0;
                            comboStep = 1;
                            if (rand.Next(100) < 50) CurrentState = BossState.ChargePrep;
                            else CurrentState = BossState.JumpSlash;
                        }
                    }
                    break;

                case BossState.ChargePrep:
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                    int prepDuration = isPhase2 ? 12 : 20;
                    if (stateTimer >= prepDuration)
                    {
                        stateTimer = 0;
                        CurrentState = BossState.DashAttack;
                    }
                    break;

                case BossState.DashAttack:
                    IsAttacking = true;
                    X += FacingLeft ? -chargeSpeed : chargeSpeed;
                    AttackHitbox = new Rectangle(FacingLeft ? X - 80 : X + Width, Y + 30, 80, Height - 40);

                    if (X <= 12 || X + Width >= 1908)
                    {
                        stateTimer = 0;
                        comboStep = maxCombo;
                        CurrentState = BossState.Recovery;
                        break;
                    }

                    if (stateTimer >= 22)
                    {
                        stateTimer = 0;
                        if (comboStep < maxCombo)
                        {
                            comboStep++;
                            CurrentState = BossState.JumpSlash;
                        }
                        else
                        {
                            CurrentState = BossState.Recovery;
                        }
                    }
                    break;

                case BossState.JumpSlash:
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                    if (stateTimer == 1) verticalVelocity = -32;
                    X += FacingLeft ? -4 : 4;

                    if (verticalVelocity > 0)
                    {
                        stateTimer = 0;
                        CurrentState = BossState.FallingSlash;
                    }
                    break;

                case BossState.FallingSlash:
                    IsAttacking = true;
                    verticalVelocity = isPhase2 ? 28 : 22;
                    AttackHitbox = new Rectangle(X - 20, Y + Height - 10, Width + 40, 50);

                    if (Y + Height >= groundY - 5)
                    {
                        stateTimer = 0;
                        AttackHitbox = Rectangle.Empty;

                        if (comboStep < maxCombo)
                        {
                            comboStep++;
                            FacingLeft = (player.Hitbox.X + player.Hitbox.Width / 2) < (X + Width / 2);
                            CurrentState = BossState.DashAttack;
                        }
                        else
                        {
                            CurrentState = BossState.Recovery;
                        }
                    }
                    break;

                case BossState.Recovery:
                    IsAttacking = false;
                    AttackHitbox = Rectangle.Empty;
                    int requiredRecovery = isPhase2 ? 18 : 45;
                    if (stateTimer >= requiredRecovery)
                    {
                        stateTimer = 0;
                        cooldownTimer = isPhase2 ? 8 : 35;
                        CurrentState = BossState.WalkTowardPlayer;
                    }
                    break;
            }

            if (X < 10) X = 10;
            if (X + Width > 1910) X = 1910 - Width;

            Hitbox = new Rectangle(X, Y, Width, Height);
        }

        public void TakeDamage(int amount)
        {
            if (IsDead || HurtTimer > 0 || CurrentState == BossState.PhaseTransition) return;

            CurrentHealth -= amount;
            HurtTimer = 15;

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                IsDead = true;
                stateTimer = 0;
                CurrentState = BossState.Dead; // Olum modu kilitlendi
            }
        }

        public void Draw(Graphics g)
        {
            if (SequenceFinished) return;

            Rectangle destRect = new Rectangle(X, Y, Width, Height);

            //  RETRO DEFEAT RENDER MOTORU
            if (CurrentState == BossState.Dead)
            {
                // Ekran donma ve dramatik flas etkisi: Beyaz ve kirmizi arasinda yuksek hizda flas patlamasi
                Brush deadBrush = (stateTimer % 4 < 2) ? Brushes.White : Brushes.Crimson;
                g.FillRectangle(deadBrush, destRect);

                // Rastgele konumlarda olusan retro patlama kareleri efekti
                explosionCounter++;
                if (explosionCounter % 6 == 0)
                {
                    int exX = X + rand.Next(-30, Width);
                    int exY = Y + rand.Next(-30, Height);
                    int exSize = rand.Next(20, 50);
                    g.FillRectangle(Brushes.Gold, exX, exY, exSize, exSize);
                    g.FillRectangle(Brushes.Orange, exX + 5, exY + 5, exSize - 10, exSize - 10);
                }
                return; // Oluyken can bari cizilmez
            }

            if (HurtTimer > 0 && (HurtTimer % 4 < 2)) return;

            Brush bodyBrush = Brushes.Crimson;
            if (CurrentState == BossState.PhaseTransition) bodyBrush = (stateTimer % 6 < 3) ? Brushes.White : Brushes.DarkRed;
            else
            {
                if (CurrentState == BossState.ChargePrep) bodyBrush = Brushes.Orange;
                if (CurrentState == BossState.DashAttack) bodyBrush = Brushes.Firebrick;
                if (CurrentState == BossState.FallingSlash) bodyBrush = Brushes.Gold;
                if (CurrentState == BossState.Recovery) bodyBrush = Brushes.Maroon;
            }

            g.FillRectangle(bodyBrush, Hitbox);

            int vizorW = 20; int vizorH = 8;
            int vizorX = FacingLeft ? X + 15 : X + Width - 35;
            int vizorY = Y + 25;
            Brush vizorBrush = isPhase2 ? Brushes.Magenta : Brushes.Cyan;
            g.FillRectangle(vizorBrush, vizorX, vizorY, vizorW, vizorH);

            int barWidth = 800; int barHeight = 28; int barX = (1920 - barWidth) / 2; int barY = 120;
            g.FillRectangle(Brushes.Black, barX, barY, barWidth, barHeight);

            float healthRatio = (float)CurrentHealth / MaxHealth;
            int currentBarWidth = (int)(barWidth * healthRatio);

            Color barColor = isPhase2 ? Color.FromArgb(255, 0, 50) : Color.FromArgb(230, 20, 20);
            using (SolidBrush healthBrush = new SolidBrush(barColor))
                g.FillRectangle(healthBrush, barX, barY, currentBarWidth, barHeight);

            using (Pen borderPen = new Pen(Color.Goldenrod, 4f))
                g.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);

            using (Font font = new Font("Impact", 20, FontStyle.Regular))
            {
                string label = isPhase2 ? "CRIMSON KNIGHT - UNLEASHED RIVAL (PHASE 2)" : "CRIMSON KNIGHT - THE RIVAL";
                g.DrawString(label, font, isPhase2 ? Brushes.Tomato : Brushes.White, barX, barY - 38);
            }
        }
    }
}