using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using oyun1.Entities;
using oyun1.Levels;
using oyun1.UI;

namespace oyun1.Engine
{
    public class Game
    {
        private Player _player;
        private WorldManager _worldManager;
        private Camera _camera;
        private ParticleSystem _particleSystem;
        private List<Enemy> _enemies;
        private HUD _hud;
        private FallenKnight _boss;
        private RoomManager _roomManager;

        // --- HIZLI REPLAY VE GEÇİŞ AYARLARI (FAST PACING) ---
        private float _deathSequenceTimer;
        private const float DeathSequenceDuration = 0.3f; // 1.8 saniyeden 0.3 saniyeye indirildi (Anlık Yeniden Doğuş!)
        private float _fadeAlpha = 0f;
        private bool _isFadingOut;
        private bool _isFadingIn;
        private const float FadeSpeed = 850f; // 350'den 850'ye çıkarıldı (Ekran göz açıp kapayıncaya kadar kararıp açılır)

        private PointF _transitionSpawnTarget;
        private int _targetAreaToLoad = -1;

        public Game()
        {
            Initialize();
        }

        public void Initialize()
        {
            Time.Initialize();
            _particleSystem = new ParticleSystem();
            _worldManager = new WorldManager();
            _hud = new HUD();
            _roomManager = new RoomManager(_particleSystem);

            PointF initialSpawn = _worldManager.CurrentMap.PlayerSpawnPoint;
            _player = new Player(initialSpawn.X, initialSpawn.Y, _worldManager.CurrentMap, _particleSystem);

            _camera = new Camera();
            ResetCameraInstant();

            SpawnEnemiesForCurrentArea();
        }

        private void SpawnEnemiesForCurrentArea()
        {
            _enemies = new List<Enemy>();
            TileMap activeMap = _worldManager.CurrentMap;

            foreach (var pos in activeMap.SlimeSpawnPoints)
            {
                _enemies.Add(new Slime(pos.X, pos.Y, _player, activeMap));
            }

            foreach (var pos in activeMap.BatSpawnPoints)
            {
                _enemies.Add(new Bat(pos.X, pos.Y, _player, activeMap));
            }

            if (activeMap.AreaID == 3)
            {
                _boss = new FallenKnight(42 * TileMap.TileSize, 12 * TileMap.TileSize, _player, activeMap, _particleSystem);
            }
            else
            {
                _boss = null;
            }

            _roomManager.SetupRoomsForArea(activeMap, _enemies);
        }

        public void Update()
        {
            Time.Update();
            float dt = Time.DeltaTime;

            if (!_player.IsDead && !_isFadingOut)
            {
                _player.Update();
                _roomManager.Update(_player.GetBounds());

                int nextArea = _worldManager.CheckMapTransitions(_player.Position, out _transitionSpawnTarget);
                if (nextArea != -1)
                {
                    _targetAreaToLoad = nextArea;
                    _isFadingOut = true; // Hızlı kararma döngüsünü başlat
                }
            }

            _particleSystem.Update();
            foreach (var enemy in _enemies) enemy.Update();

            if (_boss != null)
            {
                if (!_boss.IsActive && _player.Position.X > 45 * TileMap.TileSize)
                {
                    _boss.IsActive = true;
                }
                _boss.Update();
            }

            // --- ULTRA HIZLI FADE MATRİS MOTORU ---
            if (_isFadingOut)
            {
                _fadeAlpha += FadeSpeed * dt;
                if (_fadeAlpha >= 255f)
                {
                    _fadeAlpha = 255f;
                    _isFadingOut = false;

                    if (_targetAreaToLoad != -1) ExecuteAreaTransition();
                    else ExecutePlayerRespawn(); // Bekletmeden anında canlandır

                    _isFadingIn = true;
                }
            }
            else if (_isFadingIn)
            {
                _fadeAlpha -= FadeSpeed * dt;
                if (_fadeAlpha <= 0f) { _fadeAlpha = 0f; _isFadingIn = false; }
            }

            // Ölüm Anında Bekleme Süresi Kırpıldı (Minimal Downtime)
            if (_player.IsDead && !_isFadingOut && !_isFadingIn && _fadeAlpha == 0f)
            {
                _deathSequenceTimer += dt;
                if (_deathSequenceTimer >= DeathSequenceDuration)
                {
                    _deathSequenceTimer = 0f;
                    _isFadingOut = true;
                }
            }

            // Uçurum Sonu Koruması
            if (!_player.IsDead && _player.Position.Y > _worldManager.CurrentMap.HeightInPixels + 64f)
            {
                _player.TakeDamage(_player.MaxHealth, 0);
                _camera.MakeShake(0.2f, 10f);
            }

            // Temas Hasarları
            if (!_player.IsDead && !_isFadingOut)
            {
                foreach (var enemy in _enemies)
                {
                    if (enemy.CurrentState != EnemyState.Dead && _player.GetBounds().IntersectsWith(enemy.GetBounds()))
                    {
                        int pushDir = (_player.Position.X + _player.Size.Width / 2 > enemy.Position.X + enemy.Size.Width / 2) ? 1 : -1;
                        _player.TakeDamage(10, pushDir);
                        _camera.MakeShake(0.15f, 8f);
                    }
                }
            }

            // J Tuşu Kombottan Geri Tepme ve Pogo Matrisi
            if (Input.GetKeyDown(Keys.J) && !_player.IsDead && !_isFadingOut)
            {
                bool isDownPress = Input.GetKey(Keys.S) || Input.GetKey(Keys.Down);
                float attackWidth = isDownPress ? _player.Size.Width + 16 : 48f;
                float attackHeight = isDownPress ? 28f : _player.Size.Height;
                float attackX = isDownPress ? _player.Position.X - 8 : (_player.FacingDirection == 1 ? _player.Position.X + _player.Size.Width : _player.Position.X - attackWidth);
                float attackY = isDownPress ? _player.Position.Y + _player.Size.Height : _player.Position.Y;

                RectangleF slashBox = new RectangleF(attackX, attackY, attackWidth, attackHeight);

                foreach (var enemy in _enemies)
                {
                    if (enemy.CurrentState != EnemyState.Dead && slashBox.IntersectsWith(enemy.GetBounds()))
                    {
                        enemy.TakeDamage(10, _player.FacingDirection);
                        _camera.MakeShake(0.1f, 6f);

                        float hitX = enemy.Position.X + enemy.Size.Width / 2;
                        float hitY = enemy.Position.Y + enemy.Size.Height / 2;

                        if (isDownPress)
                        {
                            _player.BounceOffTarget();
                            _particleSystem.SpawnCombatBurst(hitX, hitY, _player.FacingDirection, 1, Color.Cyan, ParticleType.Shockwave);
                            _particleSystem.SpawnCombatBurst(hitX, hitY, _player.FacingDirection, 4, Color.White, ParticleType.Shard);
                        }
                        else
                        {
                            _particleSystem.SpawnCombatBurst(_player.Position.X + _player.Size.Width / 2, _player.Position.Y + _player.Size.Height / 2, _player.FacingDirection, 1, Color.Cyan, ParticleType.Shockwave);
                            _particleSystem.SpawnCombatBurst(hitX, hitY, _player.FacingDirection, 5, Color.White, ParticleType.Shard);
                        }
                    }
                }

                if (_boss != null && _boss.IsActive && _boss.CurrentBossState != BossState.Dead && slashBox.IntersectsWith(_boss.GetBounds()))
                {
                    _boss.TakeDamage(10, _player.FacingDirection);
                    _camera.MakeShake(0.15f, 8f);
                    float bHitX = _boss.Position.X + _boss.Size.Width / 2;
                    float bHitY = _boss.Position.Y + _boss.Size.Height / 2;

                    if (isDownPress)
                    {
                        _player.BounceOffTarget();
                        _particleSystem.SpawnCombatBurst(bHitX, bHitY, _player.FacingDirection, 1, Color.Cyan, ParticleType.Shockwave);
                    }
                    else
                    {
                        _particleSystem.SpawnCombatBurst(_player.Position.X + _player.Size.Width / 2, _player.Position.Y + _player.Size.Height / 2, _player.FacingDirection, 1, Color.Cyan, ParticleType.Shockwave);
                    }
                }
            }

            // Akıcı Kamera Takibi
            float targetX = _player.Position.X + _player.Size.Width / 2;
            float targetY = _player.Position.Y + _player.Size.Height / 2;
            _camera.Follow(targetX, targetY, dt);

            Input.Update();
        }

        private void ExecuteAreaTransition()
        {
            _worldManager.LoadArea(_targetAreaToLoad);
            _player.ResetState(_transitionSpawnTarget);

            typeof(Player).GetField("_tileMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(_player, _worldManager.CurrentMap);

            SpawnEnemiesForCurrentArea();
            ResetCameraInstant(); // Kamerayı anında yeni odaya eşitle (Hantal kaymayı önle!)
            _targetAreaToLoad = -1;
        }

        private void ExecutePlayerRespawn()
        {
            if (_worldManager.CurrentMap.AreaID != _worldManager.ActiveCheckpointAreaID)
            {
                _worldManager.LoadArea(_worldManager.ActiveCheckpointAreaID);
                typeof(Player).GetField("_tileMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(_player, _worldManager.CurrentMap);
            }

            _player.ResetState(_worldManager.GlobalCheckpointPosition);
            SpawnEnemiesForCurrentArea();
            ResetCameraInstant(); // Öldüğünde kamerayı anında başlangıca oturt
        }

        private void ResetCameraInstant()
        {
            _camera.Position.X = _player.Position.X - 400f;
            _camera.Position.Y = _player.Position.Y - 300f;
        }

        public void Draw(System.Drawing.Graphics g)
        {
            g.Clear(Color.FromArgb(12, 10, 16));

            _particleSystem.RenderBackgroundAtmosphere(g, _camera.Position.X, _camera.Position.Y);
            g.TranslateTransform(-_camera.Position.X, -_camera.Position.Y);

            _worldManager.CurrentMap.Render(g);
            _player.Render(g);

            foreach (var enemy in _enemies) enemy.Render(g);
            if (_boss != null) _boss.Render(g);
            _particleSystem.Render(g);

            _particleSystem.RenderForegroundAtmosphere(g);
            g.ResetTransform();

            _hud.Render(g, _player);

            if (_boss != null && _boss.IsActive && _boss.CurrentBossState != BossState.Dead)
            {
                int barWidth = 400; int barHeight = 12;
                int barX = (800 - barWidth) / 2; int barY = 530;

                using (var bgBrush = new SolidBrush(Color.FromArgb(170, 12, 10, 18)))
                using (var borderPen = new Pen(Color.FromArgb(85, 95, 105), 2))
                {
                    g.FillRectangle(bgBrush, barX, barY, barWidth, barHeight);
                    g.DrawRectangle(borderPen, barX, barY, barWidth, barHeight);
                }

                float healthRatio = (float)_boss.Health / _boss.MaxHealth;
                int fillWidth = (int)(barWidth * healthRatio);

                if (fillWidth > 0)
                {
                    Color barColor = (_boss.Health <= _boss.MaxHealth / 2) ? Color.FromArgb(145, 35, 165) : Color.FromArgb(0, 165, 190);
                    using (var fillBrush = new SolidBrush(barColor))
                    {
                        g.FillRectangle(fillBrush, barX + 2, barY + 2, fillWidth - 4, barHeight - 4);
                    }
                }

                using (var font = new Font("Trebuchet MS", 10, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.FromArgb(225, 245, 255)))
                {
                    g.DrawString("FALLEN KNIGHT", font, textBrush, barX, barY - 20);
                }
            }

            // Sahne Geçiş Siyah Örtüsü (Fade-out/in)
            if (_fadeAlpha > 0)
            {
                using (var fadeBrush = new SolidBrush(Color.FromArgb((int)_fadeAlpha, Color.Black)))
                {
                    g.FillRectangle(fadeBrush, 0, 0, 800, 600);
                }
            }
        }
    }
}