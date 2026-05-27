using System;
using System.Drawing;
using oyun1.Engine;
using oyun1.Levels;

namespace oyun1.Entities
{
    public enum BossState { Idle, Telegraphing, DashSlash, JumpSlam, Stunned, Dead }
    public enum BossAttackType { Slash, Slam }

    public class FallenKnight : Enemy
    {
        // Boss Sağlık ve Faz Bilgileri
        public int MaxHealth { get; private set; } = 300;
        public int Health { get; private set; }
        public bool IsActive { get; set; } = false;

        // Boss Durum Makinesi Değişkenleri
        public BossState CurrentBossState { get; private set; } = BossState.Idle;
        private BossAttackType _nextAttack;
        private float _stateTimer = 0f;
        private float _actionCooldown = 1.2f; // Ataklar arası bekleme süresi

        // Hareket ve Atak Alan Ölçüleri
        private readonly Player _targetPlayer;
        private readonly TileMap _currentTileMap;
        private readonly ParticleSystem _pSystem;
        private float _gravity = 900f;
        private int _facingDir = -1;

        // Atak Özel Fizik Değişkenleri
        private bool _hasHitThisAttack = false;
        private float _dashSpeed = 550f;
        private float _jumpForce = -420f;

        public FallenKnight(float x, float y, Player player, TileMap tileMap, ParticleSystem pSystem)
     : base(x, y, 48, 64, 300f, player, tileMap)
        {
            this.Position = new PointF(x, y);
            this.Size = new Size(48, 64);
            this.Health = MaxHealth;
            this._targetPlayer = player;
            this._currentTileMap = tileMap;
            this._pSystem = pSystem;
            this.CurrentState = EnemyState.Idle;
        }

        public override void Update()
        {
            if (CurrentBossState == BossState.Dead || !IsActive) return;

            float dt = Time.DeltaTime;
            _stateTimer += dt;

            // Oyuncuya doğru dönme mantığı (Sadece Idle ve Hazırlık anında)
            if (CurrentBossState == BossState.Idle || CurrentBossState == BossState.Telegraphing)
            {
                _facingDir = (_targetPlayer.Position.X + _targetPlayer.Size.Width / 2 > Position.X + Size.Width / 2) ? 1 : -1;
            }

            // Yerçekimi Uygulaması (Sadece zıplama ve düşme anlarında koruma)
            if (CurrentBossState != BossState.DashSlash)
            {
                Velocity.Y += _gravity * dt;
                if (Velocity.Y > 600f) Velocity.Y = 600f;
            }

            // Çarpışma ve Konum Güncellemesi
            Position.X += Velocity.X * dt;
            if (_currentTileMap.HasCollision(GetBounds()))
            {
                Position.X -= Velocity.X * dt;
                Velocity.X = 0;
            }

            Position.Y += Velocity.Y * dt;
            if (_currentTileMap.HasCollision(GetBounds()))
            {
                Position.Y -= Velocity.Y * dt;
                Velocity.Y = 0;
            }

            // DURUM MAKİNESİ AKIŞ KONTROLÜ (State Machine Matrix)
            switch (CurrentBossState)
            {
                case BossState.Idle:
                    Velocity.X = 0;
                    float currentCooldown = (Health <= MaxHealth / 2) ? _actionCooldown * 0.4f : _actionCooldown; // Faz 2'de çok daha agresif ve hızlı
                    if (_stateTimer >= currentCooldown)
                    {
                        TriggerNextRandomAttack();
                    }
                    break;

                case BossState.Telegraphing:
                    Velocity.X = 0;
                    float telegraphDuration = (Health <= MaxHealth / 2) ? 0.25f : 0.5f; // Faz 2'de uyarı süresi jilet gibi kısalır
                    if (_stateTimer >= telegraphDuration)
                    {
                        ExecuteAttackPayload();
                    }
                    break;

                case BossState.DashSlash:
                    // İleri doğru fırlama mekaniği
                    Velocity.X = _facingDir * _dashSpeed;
                    CheckPlayerDamage(64f, 40f, 20); // Geniş kılıç menzili hasar kontrolü
                    if (_stateTimer >= 0.35f) // Atılma süresi bitti
                    {
                        EndAttackCycle();
                    }
                    break;

                case BossState.JumpSlam:
                    // Havada süzülüp oyuncunun üstüne çakılma takibi
                    if (Velocity.Y > 0 && _stateTimer > 0.2f)
                    {
                        // Yere çakıldı mı?
                        if (Velocity.Y <= 50f)
                        {
                            TriggerSlamImpact();
                            EndAttackCycle();
                        }
                    }
                    break;

                case BossState.Stunned:
                    Velocity.X = 0;
                    if (_stateTimer >= 2.0f) // 2 saniye diz çöktükten sonra sinirle kalkar
                    {
                        EndAttackCycle();
                    }
                    break;
            }
        }

        private void TriggerNextRandomAttack()
        {
            _stateTimer = 0f;
            CurrentBossState = BossState.Telegraphing;
            _hasHitThisAttack = false;

            // Rastgele saldırı seçimi
            Random rand = new Random();
            _nextAttack = (rand.Next(0, 2) == 0) ? BossAttackType.Slash : BossAttackType.Slam;
        }

        private void ExecuteAttackPayload()
        {
            _stateTimer = 0f;

            if (_nextAttack == BossAttackType.Slash)
            {
                CurrentBossState = BossState.DashSlash;
            }
            else if (_nextAttack == BossAttackType.Slam)
            {
                CurrentBossState = BossState.JumpSlam;
                Velocity.Y = _jumpForce; // Havaya sıçra
                // Oyuncunun üstüne doğru yatay ivme ver
                Velocity.X = _facingDir * 180f;
            }
        }

        private void TriggerSlamImpact()
        {
            Velocity.X = 0;
            // EKRAN SARSINTISI VE ŞOK DALGASI PARÇACIKLARI (Cinematic Feeling)
            _pSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height, 1, 1, Color.Cyan, ParticleType.Shockwave);
            _pSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height, -1, 8, Color.White, ParticleType.Shard);

            // Alan Hasar Kontrolü (Yerdeki şok dalgası oyuncuya vurur)
            float dist = Math.Abs((Position.X + Size.Width / 2) - (_targetPlayer.Position.X + _targetPlayer.Size.Width / 2));
            if (dist < 120f && _targetPlayer.Position.Y >= Position.Y)
            {
                _targetPlayer.TakeDamage(25, _facingDir);
            }
        }

        private void CheckPlayerDamage(float reachX, float reachY, int damage)
        {
            if (_hasHitThisAttack) return;

            RectangleF attackBox = new RectangleF(
                _facingDir == 1 ? Position.X + Size.Width : Position.X - reachX,
                Position.Y + 10,
                reachX,
                reachY
            );

            if (attackBox.IntersectsWith(_targetPlayer.GetBounds()))
            {
                _targetPlayer.TakeDamage(damage, _facingDir);
                _hasHitThisAttack = true; // Tek döngüde iki kez vurmayı engelle
            }
        }

        private void EndAttackCycle()
        {
            _stateTimer = 0f;
            Velocity.X = 0;
            CurrentBossState = BossState.Idle;
        }

        public override void TakeDamage(float amount, int knockbackDir)
        {
            if (CurrentBossState == BossState.Dead || CurrentBossState == BossState.Stunned) return;

            Health -= (int)amount;

            // Havaya bembeyaz asil parıltı pikselleri fırlat (Hit Feedback)
            _pSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height / 2, knockbackDir, 8, Color.White, ParticleType.Spark);

            // STUN / BROKEN MEKANİĞİ: Can %50 veya %25 altına indiğinde boss diz çöker!
            if (Health == MaxHealth / 2 || Health == MaxHealth / 4)
            {
                CurrentBossState = BossState.Stunned;
                _stateTimer = 0f;
                Velocity.X = 0;
                return;
            }

            if (Health <= 0)
            {
                Health = 0;
                CurrentBossState = BossState.Dead;
                CurrentState = EnemyState.Dead;
                // Devasa bir patlamayla dünyadan silinme efekti
                _pSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height / 2, 1, 2, Color.Cyan, ParticleType.Shockwave);
                _pSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height / 2, -1, 20, Color.White, ParticleType.Shard);
            }
        }

        public override void Render(Graphics g)
        {
            if (CurrentBossState == BossState.Dead) return;

            float px = Position.X;
            float py = Position.Y;

            // Bossun zırh rengi (Faz 2'de mor yozlaşma zırhına bürünür)
            Color armorColor = (Health <= MaxHealth / 2) ? Color.FromArgb(75, 20, 90) : Color.FromArgb(60, 65, 80);
            Color eyeColor = (CurrentBossState == BossState.Telegraphing) ? Color.Red : ((Health <= MaxHealth / 2) ? Color.Magenta : Color.Cyan);

            using (var b = new SolidBrush(armorColor))
            using (var p = new Pen(Color.FromArgb(90, 100, 120), 2))
            {
                if (CurrentBossState == BossState.Stunned) // Diz çökme görsel pozisyon kayması
                {
                    g.FillRectangle(b, px, py + 20, Size.Width, Size.Height - 20);
                    g.DrawRectangle(p, px, py + 20, Size.Width, Size.Height - 20);
                }
                else // Ayakta heybetli duruş
                {
                    g.FillRectangle(b, px, py, Size.Width, Size.Height);
                    g.DrawRectangle(p, px, py, Size.Width, Size.Height);

                    // Boss Parlayan Miğfer Gözü (Baktığı yöne göre jilet gibi çizim)
                    using (var eyeBrush = new SolidBrush(eyeColor))
                    {
                        float eyeX = (_facingDir == 1) ? px + Size.Width - 16 : px + 8;
                        g.FillRectangle(eyeBrush, eyeX, py + 12, 10, 6);
                    }
                }
            }
        }
    }
}