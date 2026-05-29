using System;
using System.Collections.Generic;
using System.Drawing;
using KeserKnight.Combat;

namespace KeserKnight.Entity
{
    public class GuardianGriffon
    {
        private int baseOriginX;

        public Rectangle Hitbox { get; private set; }
        public int MaxHealth { get; private set; } = 100;
        public int CurrentHealth { get; private set; } = 100;
        public bool IsDead { get; private set; } = false;
        public int HurtTimer { get; private set; } = 0;

        public Rectangle SwipeHitbox { get; private set; }
        public bool IsSwiping { get; private set; } = false;

        public List<BossProjectile> Projectiles { get; private set; } = new List<BossProjectile>();

        // YENİ: Aktif ölüm patlama efektleri listesi
        public List<DeathExplosion> Explosions { get; private set; } = new List<DeathExplosion>();

        public enum BossState { Idle, FireAttack, SwipeAttack, Recovery, Dead }
        public BossState CurrentState { get; private set; } = BossState.Idle;

        private int stateTimer = 0;
        private int cooldownTimer = 0;
        private Random rand = new Random();

        // Animasyon ve Görsel Durum İpuçları
        public bool IsMouthOpen { get; private set; } = false;
        public bool IsTelegraphingSwipe { get; private set; } = false;
        public bool IsExhausted { get; private set; } = false;

        // YENİ: Ölüm animasyon kontrolcüleri
        public bool SequenceFinished { get; private set; } = false;
        private int flashCounter = 0;

        public GuardianGriffon(int x, int y, int width, int height)
        {
            Hitbox = new Rectangle(x, y, width, height);
            this.baseOriginX = x;
        }

        public void Update(Player player)
        {
            // Eğer ölüm sekansı tamamen bittiyse motoru yorma, çık usta
            if (SequenceFinished) return;

            // Hasar parlamasını erit
            if (HurtTimer > 0) HurtTimer--;

            // Mevcut aktif mermileri güncelle
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                Projectiles[i].Update();
                if (Projectiles[i].Hitbox.X < -50) Projectiles.RemoveAt(i);
            }

            // YENİ: Ölüm patlama parçacıklarını güncelle ve temizle
            for (int i = Explosions.Count - 1; i >= 0; i--)
            {
                Explosions[i].Update();
                if (Explosions[i].IsFinished) Explosions.RemoveAt(i);
            }

            stateTimer++;

            // DURUM MAKİNESİ
            switch (CurrentState)
            {
                case BossState.Dead:
                    // 💥 RETRO ÖLÜM SEKANSI MOTORU
                    IsSwiping = false; IsMouthOpen = false; IsTelegraphingSwipe = false; IsExhausted = false;
                    SwipeHitbox = Rectangle.Empty;
                    Projectiles.Clear(); // Ekrandaki tüm mermilerini yok et oyuncu rahatlasın usta

                    flashCounter++;

                    // Her 5 karede bir boss'un gövdesinde rastgele patlama halkası oluştur
                    if (stateTimer < 90 && flashCounter % 5 == 0)
                    {
                        int rx = rand.Next(Hitbox.X, Hitbox.Right);
                        int ry = rand.Next(Hitbox.Y, Hitbox.Bottom);
                        Explosions.Add(new DeathExplosion(rx, ry));
                    }

                    // Sekans süresi (2 saniye) dolduğunda her şeyi tamamla usta
                    if (stateTimer >= 120)
                    {
                        SequenceFinished = true;
                    }
                    return; // Diğer yapay zeka durumlarına geçmesini engelle!

                case BossState.Idle:
                    if (cooldownTimer > 0) { cooldownTimer--; return; }
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
                        Hitbox = new Rectangle(baseOriginX + 25, Hitbox.Y, Hitbox.Width, Hitbox.Height);
                    }
                    else if (stateTimer >= 15 && stateTimer < 35)
                    {
                        IsTelegraphingSwipe = false; IsSwiping = true;
                        Hitbox = new Rectangle(baseOriginX - 35, Hitbox.Y, Hitbox.Width, Hitbox.Height);
                        SwipeHitbox = new Rectangle(Hitbox.X - 240, Hitbox.Y + 80, 240, 180);
                    }
                    else if (stateTimer >= 35)
                    {
                        IsSwiping = false; SwipeHitbox = Rectangle.Empty;
                        Hitbox = new Rectangle(baseOriginX, Hitbox.Y, Hitbox.Width, Hitbox.Height);
                    }

                    if (stateTimer >= 50) { stateTimer = 0; CurrentState = BossState.Recovery; }
                    break;

                case BossState.Recovery:
                    IsExhausted = true;
                    if (stateTimer >= 45) { stateTimer = 0; IsExhausted = false; cooldownTimer = 60; CurrentState = BossState.Idle; }
                    break;
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
                stateTimer = 0; // Ölüm sekans sayacını sıfırdan başlat usta!
                flashCounter = 0;
            }
        }

        public void Draw(Graphics g)
        {
            // Eğer ölüm animasyonları bittiyse boss'u ekrandan tamamen sil usta (Fade Out nihayeti)
            if (SequenceFinished) return;

            if (CurrentState == BossState.Dead)
            {
                // 1. ÖLÜM YANIP SÖNME EFEKTİ (Flashing Sprite)
                // Her 4 karede bir görünmez/beyaz/orijinal renk döngüsü
                if ((flashCounter / 4) % 2 == 0)
                {
                    // Kare 90'dan sonra yavaşça karartma filtresi uygula
                    Brush deathBrush = (stateTimer > 90) ? Brushes.DimGray : Brushes.White;
                    g.FillRectangle(deathBrush, Hitbox);
                }

                // Patlama parçacıklarını çizdir
                foreach (var exp in Explosions)
                {
                    exp.Draw(g);
                }
                return; // Can barını ve normal gövdeyi çizme usta, öldü çünkü!
            }

            // NORMAL DURUM ÇİZİMLERİ (Aynen korundu)
            Brush bodyBrush = Brushes.Gold;
            if (HurtTimer > 0) bodyBrush = Brushes.DarkRed;
            else if (IsTelegraphingSwipe) bodyBrush = Brushes.OrangeRed;
            else if (IsExhausted) bodyBrush = Brushes.LightGray;

            g.FillRectangle(bodyBrush, Hitbox);

            using (SolidBrush detailBrush = new SolidBrush(Color.FromArgb(200, 140, 0)))
            {
                g.FillRectangle(detailBrush, Hitbox.X + 20, Hitbox.Y + 20, 50, 50);
                Brush eyeBrush = IsExhausted ? Brushes.DimGray : Brushes.Red;
                g.FillEllipse(eyeBrush, Hitbox.X + 35, Hitbox.Y + 40, 15, 15);
            }

            if (IsMouthOpen)
            {
                using (SolidBrush mouthBrush = new SolidBrush(Color.FromArgb(255, 50, 0)))
                    g.FillRectangle(mouthBrush, Hitbox.X - 20, Hitbox.Y + 65, 30, 35);
            }

            foreach (var proj in Projectiles) proj.Draw(g);

            if (IsSwiping && !SwipeHitbox.IsEmpty)
            {
                using (Pen swipePen = new Pen(Color.White, 6f))
                {
                    swipePen.StartCap = System.Drawing.Drawing2D.LineCap.Round; swipePen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                    g.DrawArc(swipePen, SwipeHitbox.X, SwipeHitbox.Y, SwipeHitbox.Width, SwipeHitbox.Height, 120, 120);
                }
            }

            // BOSS HEALTH BAR UI
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