using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace oyun1.Engine
{
    // Shard/Shockwave/Spark kombat içindir, Dust ortam tozları içindir
    public enum ParticleType { Shard, Shockwave, Spark, Dust }

    public class Particle
    {
        public PointF Position;
        public PointF Velocity;
        public Color Color;
        public float Size;
        public float MaxLife;
        public float Life;
        public ParticleType Type;
        public float Angle;
        public float GrowthSpeed;
        public float PulseOffset; // Ortam tozlarının birbirinden bağımsız parıldaması için zaman sapması
    }

    public class ParticleSystem
    {
        private readonly List<Particle> _combatParticles = new List<Particle>();
        private readonly List<Particle> _ambientDustPool = new List<Particle>(); // Optimize sabit toz havuzu
        private readonly Random _rand = new Random();

        // Ritmik dalgalanmalar için dahili zaman sayacı
        private float _animationTimer = 0f;
        private const int MaxAmbientDust = 45; // Ekrandaki maksimum atmosferik toz sayısı

        public ParticleSystem()
        {
            InitializeAmbientDustPool();
        }

        private void InitializeAmbientDustPool()
        {
            // Performans Hilesi: Seviye açılırken havuzu bir kez dolduruyoruz, oyun boyu yok etmiyoruz
            for (int i = 0; i < MaxAmbientDust; i++)
            {
                _ambientDustPool.Add(GenerateRandomDustParticle(true));
            }
        }

        private Particle GenerateRandomDustParticle(bool randomizeLifeStage = false)
        {
            float maxLife = (float)(_rand.NextDouble() * 3.0f + 2.0f); // 2-5 saniye ömür
            float initialLife = randomizeLifeStage ? (float)(_rand.NextDouble() * maxLife) : 0f;

            return new Particle
            {
                // Tozları ekrana ve biraz da ekran dışı sınırlara dağıtıyoruz (Kamera kaydıkça pürüzsüz görünsün)
                Position = new PointF(_rand.Next(-200, 2000), _rand.Next(-100, 900)),
                Velocity = new PointF((float)(_rand.NextDouble() * 15f - 7.5f), (float)(_rand.NextDouble() * -10f - 5f)), // Hafifçe sola/sağa ve yukarı süzülme
                Color = Color.FromArgb(_rand.Next(150, 230), 130, 240, 255), // Hollow Knight Turkuaz/Mavi parıltısı
                Size = _rand.Next(2, 5),
                MaxLife = maxLife,
                Life = initialLife,
                Type = ParticleType.Dust,
                PulseOffset = (float)(_rand.NextDouble() * Math.PI * 2) // Farklı fazlarda parıldama
            };
        }

        public void SpawnBurst(float x, float y, Color color, int count, float baseSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = _rand.NextDouble() * Math.PI * 2;
                float speed = (float)(_rand.NextDouble() * 0.6f + 0.4f) * baseSpeed;

                _combatParticles.Add(new Particle
                {
                    Position = new PointF(x, y),
                    Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Color = color,
                    Size = _rand.Next(3, 7),
                    MaxLife = (float)(_rand.NextDouble() * 0.3f + 0.2f),
                    Life = 0f,
                    Type = ParticleType.Spark
                });
            }
        }

        public void SpawnCombatBurst(float x, float y, int direction, int count, Color color, ParticleType type)
        {
            for (int i = 0; i < count; i++)
            {
                float vx = 0f, vy = 0f;
                float size = 4f, maxLife = 0.4f, growth = 0f;

                double baseAngle = (direction == 1) ? 0 : Math.PI;
                double spread = (_rand.NextDouble() * 2.0 - 1.0) * (Math.PI / 3.0);
                double finalAngle = baseAngle + spread;
                float speed = (float)(_rand.NextDouble() * 180f + 120f);

                if (type == ParticleType.Shard)
                {
                    vx = (float)Math.Cos(finalAngle) * speed;
                    vy = (float)Math.Sin(finalAngle) * speed - _rand.Next(30, 80);
                    size = _rand.Next(4, 8);
                    maxLife = (float)(_rand.NextDouble() * 0.25f + 0.2f);
                }
                else if (type == ParticleType.Shockwave)
                {
                    size = 25f; maxLife = 0.12f; growth = 450f;
                }
                else if (type == ParticleType.Spark)
                {
                    double randAngle = _rand.NextDouble() * Math.PI * 2;
                    float randSpeed = (float)(_rand.NextDouble() * 220f + 60f);
                    vx = (float)Math.Cos(randAngle) * randSpeed;
                    vy = (float)Math.Sin(randAngle) * randSpeed;
                    size = _rand.Next(2, 5);
                    maxLife = (float)(_rand.NextDouble() * 0.4f + 0.2f);
                }

                _combatParticles.Add(new Particle
                {
                    Position = new PointF(x, y),
                    Velocity = new PointF(vx, vy),
                    Color = color,
                    Size = size,
                    MaxLife = maxLife,
                    Life = 0f,
                    Type = type,
                    Angle = direction,
                    GrowthSpeed = growth
                });
            }
        }

        public void Update()
        {
            float dt = Time.DeltaTime;
            _animationTimer += dt;

            // 1. DİNAMİK: Kombat Parçacıklarının Güncellenmesi
            for (int i = _combatParticles.Count - 1; i >= 0; i--)
            {
                var p = _combatParticles[i];
                p.Life += dt;

                if (p.Life >= p.MaxLife) { _combatParticles.RemoveAt(i); continue; }

                float drag = (p.Type == ParticleType.Shockwave) ? 0f : 4.5f;
                p.Velocity.X -= p.Velocity.X * drag * dt;
                p.Velocity.Y -= p.Velocity.Y * drag * dt;

                if (p.Type == ParticleType.Spark) p.Velocity.Y += 150f * dt;

                p.Position.X += p.Velocity.X * dt;
                p.Position.Y += p.Velocity.Y * dt;

                if (p.Type == ParticleType.Shockwave) p.Size += p.GrowthSpeed * dt;
            }

            // 2. DİNAMİK: Ortam Tozlarının Güncellenmesi (Sıfır Çöp Bellek - Nesne Koruma)
            for (int i = 0; i < _ambientDustPool.Count; i++)
            {
                var p = _ambientDustPool[i];
                p.Life += dt;

                // Ömrü bittiyse silmiyoruz, haritanın rastgele bir yerinde yeniden canlandırıyoruz
                if (p.Life >= p.MaxLife)
                {
                    _ambientDustPool[i] = GenerateRandomDustParticle(false);
                    continue;
                }

                // Havada süzülme fiziği güncellemesi
                p.Position.X += p.Velocity.X * dt;
                p.Position.Y += p.Velocity.Y * dt;
            }
        }

        // --- KATMANLI ORTAM VE SİS EFEKTLERİ (RENDER METOTLARI) ---

        public void RenderBackgroundAtmosphere(Graphics g, float camX, float camY)
        {
            // KATMAN 1: Derin Mağara Arka Plan Sisi (Fog)
            // Kamera koordinatlarına göre hafifçe paralaks (yavaş) kayan büyük sis kütleleri
            float fogX1 = -(camX * 0.2f) % 800f;
            float fogX2 = -(camX * 0.35f) % 800f;

            // Yumuşak ışık ve sis hissi için anti-alias açıyoruz
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Yumuşak, neredeyse şeffaf mor sis katmanı 1
            using (var fogBrush = new LinearGradientBrush(new RectangleF(0, 100, 900, 400),
                Color.FromArgb(0, 15, 5, 25), Color.FromArgb(28, 25, 15, 40), LinearGradientMode.Vertical))
            {
                g.FillRectangle(fogBrush, fogX1, 50, 800, 500);
                g.FillRectangle(fogBrush, fogX1 + 800, 50, 800, 500);
            }

            // Silksong esintili meşale parıltı efekti (Glow Effect)
            // Haritadaki belirli antik sütun koordinatlarına (Örn: 22. ve 33. sütun piksellerine) yumuşak ışık halkası basar
            float torchPixelX = (22 * 32) - camX;
            float torchPixelY = (8 * 32) - camY;

            // Sinüs dalgası ile meşale alevinin titreme (Pulse) animasyonu hesaplanıyor
            float pulse = (float)Math.Sin(_animationTimer * 4.5f) * 4f;
            float glowRadius = 70f + pulse;

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(torchPixelX - glowRadius, torchPixelY - glowRadius, glowRadius * 2, glowRadius * 2);
                using (var pgb = new PathGradientBrush(path))
                {
                    pgb.CenterColor = Color.FromArgb(40, 0, 160, 185); // Parlak Kristal Merkez Işığı
                    pgb.SurroundColors = new Color[] { Color.FromArgb(0, 0, 0, 0) };
                    g.FillPath(pgb, path);
                }
            }

            g.SmoothingMode = SmoothingMode.None; // Retro piksellere dön
        }

        public void RenderForegroundAtmosphere(Graphics g)
        {
            // KATMAN 4: Karakterlerin Önünde Süzülen Kristal Toz Taneleri
            foreach (var p in _ambientDustPool)
            {
                float lifeRatio = p.Life / p.MaxLife;
                // Sinüs parıldaması ile toz taneleri havada ritmik olarak parlayıp söner (Breathe etkisi)
                float pulseAlpha = (float)Math.Sin(_animationTimer * 2f + p.PulseOffset) * 0.3f + 0.7f;
                int alpha = (int)((1f - lifeRatio) * 160 * pulseAlpha);
                alpha = Math.Max(0, Math.Min(255, alpha));

                using (var brush = new SolidBrush(Color.FromArgb(alpha, p.Color)))
                {
                    // Kristal tozlarını minik pikseller halinde basıyoruz
                    g.FillRectangle(brush, p.Position.X, p.Position.Y, p.Size, p.Size);
                }
            }

            // KATMAN 5: En Ön Plan Sisi (Hafif ve şeffaf hava akımı)
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float foreFogX = -(_animationTimer * 40f) % 800f; // Zamanla kendiliğinden akan rüzgar sisi
            using (var foreFogBrush = new LinearGradientBrush(new RectangleF(0, 0, 800, 600),
                Color.FromArgb(12, 0, 60, 75), Color.FromArgb(0, 0, 0, 0), LinearGradientMode.Horizontal))
            {
                g.FillRectangle(foreFogBrush, foreFogX, 0, 800, 600);
                g.FillRectangle(foreFogBrush, foreFogX + 800, 0, 800, 600);
            }
            g.SmoothingMode = SmoothingMode.None;
        }

        public void Render(Graphics g)
        {
            // Kombottan fırlayan kılıç/kıvılcım parçacıklarının geleneksel çizimi
            foreach (var p in _combatParticles)
            {
                float lifeRatio = p.Life / p.MaxLife;
                int alpha = (int)((1f - lifeRatio) * 255);
                alpha = Math.Max(0, Math.Min(255, alpha));

                if (p.Type == ParticleType.Shockwave)
                {
                    using (var path = new GraphicsPath())
                    {
                        float radiusOut = p.Size * 1.2f;
                        float radiusIn = p.Size * 0.75f;
                        float startAngle = (p.Angle == 1) ? -75f : 105f;
                        float sweepAngle = 150f;

                        path.AddArc(p.Position.X - radiusOut, p.Position.Y - radiusOut, radiusOut * 2, radiusOut * 2, startAngle, sweepAngle);
                        path.AddArc(p.Position.X - radiusIn, p.Position.Y - radiusIn, radiusIn * 2, radiusIn * 2, startAngle + sweepAngle, -sweepAngle);
                        path.CloseAllFigures();

                        using (var pgb = new PathGradientBrush(path))
                        {
                            pgb.CenterColor = Color.FromArgb(alpha, Color.White);
                            pgb.SurroundColors = new Color[] { Color.FromArgb(alpha, Color.Cyan) };
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.FillPath(pgb, path);
                            g.SmoothingMode = SmoothingMode.None;
                        }
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(Color.FromArgb(alpha, p.Color)))
                    {
                        if (p.Type == ParticleType.Shard)
                        {
                            PointF[] points = {
                                new PointF(p.Position.X, p.Position.Y - p.Size),
                                new PointF(p.Position.X + p.Size / 1.5f, p.Position.Y),
                                new PointF(p.Position.X, p.Position.Y + p.Size),
                                new PointF(p.Position.X - p.Size / 1.5f, p.Position.Y)
                            };
                            g.FillPolygon(brush, points);
                        }
                        else
                        {
                            g.FillRectangle(brush, p.Position.X - p.Size / 2, p.Position.Y - p.Size / 2, p.Size, p.Size);
                        }
                    }
                }
            }
        }
    }
}