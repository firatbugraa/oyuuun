using System;
using System.Drawing;
using System.Windows.Forms;
using oyun1.Engine;
using oyun1.Levels;
using oyun1.Gorsel; // Yenilenen steril görsel motorumuz

namespace oyun1.Entities
{
    public enum PlayerAnimState { Idle, Run, Jump, Fall, Attack, Pogo, Hurt, Dash }

    public class Player : Entity
    {
        // --- 16-BIT RETRO FİZİK KALİBRASYON AYARLARI ---
        private const float Gravity = 1500f;
        private const float MaxFallSpeed = 700f;
        private const float MoveSpeed = 350f;
        private const float Acceleration = 2500f;
        private const float Deceleration = 2000f;
        private const float JumpForce = -680f;
        private const float VariableJumpDampening = 0.35f;
        private const float CoyoteTimeDuration = 0.15f;
        private const float JumpBufferDuration = 0.12f;

        // --- SILKSONG DASH SİSTEMİ SABİTLERİ ---
        private const float DashSpeed = 750f;
        private const float DashDuration = 0.18f;        // 180ms yıldırım hızında dash penceresi
        private const float DashCooldownDuration = 0.45f; // Akıcı yetenek sıfırlanma süresi
        private float _dashTimer;
        private float _dashCooldownTimer;
        private bool _isDashing => _dashTimer > 0;

        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isGrounded;
        private TileMap _tileMap;

        // --- HOLLOW KNIGHT VISUAL EFFECT (VFX) YAPILARI ---
        private struct Afterimage
        {
            public RectangleF Bounds;
            public float Alpha;
            public int Direction;
        }
        private readonly Afterimage[] _ghosts = new Afterimage[4];
        private float _ghostSpawnTimer;
        private const float GhostSpawnInterval = 0.04f; // 40 milisaniyede bir gölge bırak

        // Performans ve çakışma koruması için statik GDI+ fırçaları
        private readonly Pen _slashWhitePen = new Pen(Color.White, 4);
        private readonly Pen _slashCyanPen = new Pen(Color.FromArgb(150, Color.Cyan), 8);
        private readonly Pen _slashOuterPen = new Pen(Color.FromArgb(60, Color.LightBlue), 12);

        // Eski animasyon kırıntıları (Gelecekteki görseller için yapıyı bozmadık)
        private Animator _animator;
        private Animation _animIdle, _animRun, _animJump, _animFall, _animAttack, _animPogo, _animHurt, _animDash;

        // Hasar Süre Sabitleri
        private const float InvincibilityDuration = 1.0f;
        private const float HurtStateDuration = 0.25f;

        private float _attackCooldownTimer;
        private const float AttackCooldownDuration = 0.22f;
        private float _attackActiveTimer;
        private const float AttackActiveDuration = 0.12f;
        private bool _isAttacking;
        private bool _isPogoAttack;
        public int FacingDirection { get; private set; } = 1;

        private RectangleF _attackHitbox;
        private ParticleSystem _particleSystem;

        public float Health { get; private set; } = 30f;
        public float MaxHealth { get; private set; } = 30f;
        public bool IsDead { get; private set; } = false;
        private float _invincibilityTimer;
        private float _hurtStateTimer;
        private bool _isHurt => _hurtStateTimer > 0;
        private float _flickerTimer;

        public Player(float x, float y, TileMap tileMap, ParticleSystem particleSystem) : base(x, y, 24, 32)
        {
            _tileMap = tileMap;
            _particleSystem = particleSystem;
            SetupHollowAnimations();
        }

        private void SetupHollowAnimations()
        {
            _animator = new Animator();

            Image imgFallback = new Bitmap(48, 48);
            using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(imgFallback))
            {
                g.Clear(Color.FromArgb(40, 40, 50));
            }

            _animIdle = new Animation(LoadSprite("Assets/Player/idle.png", imgFallback), 4, 0.15f);
            _animRun = new Animation(LoadSprite("Assets/Player/run.png", imgFallback), 6, 0.08f);
            _animJump = new Animation(LoadSprite("Assets/Player/jump.png", imgFallback), 2, 0.06f);
            _animFall = new Animation(LoadSprite("Assets/Player/fall.png", imgFallback), 2, 0.08f);
            _animAttack = new Animation(LoadSprite("Assets/Player/attack.png", imgFallback), 4, 0.03f, false);
            _animPogo = new Animation(LoadSprite("Assets/Player/pogo.png", imgFallback), 4, 0.03f, false);
            _animHurt = new Animation(LoadSprite("Assets/Player/hurt.png", imgFallback), 1, 0.1f);
            _animDash = new Animation(LoadSprite("Assets/Player/dash.png", imgFallback), 2, 0.09f);

            _animator.Play(_animIdle);
        }

        private Image LoadSprite(string path, Image fallback)
        {
            if (System.IO.File.Exists(path)) return Image.FromFile(path);
            return fallback;
        }

        public override void Update()
        {
            float dt = Time.DeltaTime;

            if (IsDead)
            {
                Velocity.Y += Gravity * dt;
                Position.Y += Velocity.Y * dt;
                if (_tileMap.HasCollision(GetBounds())) Velocity = new PointF(0, 0);
                _animator.Update();
                return;
            }

            // Zamanlayıcıları erit
            if (_attackCooldownTimer > 0) _attackCooldownTimer -= dt;
            if (_attackActiveTimer > 0) _attackActiveTimer -= dt;
            if (_invincibilityTimer > 0) _invincibilityTimer -= dt;
            if (_hurtStateTimer > 0) _hurtStateTimer -= dt;
            if (_dashCooldownTimer > 0) _dashCooldownTimer -= dt;

            _flickerTimer += dt * 25f;

            if (_isAttacking && _attackActiveTimer <= 0)
            {
                _isAttacking = false;
                _isPogoAttack = false;
            }

            // GÖLGE TRAIL (AFTERIMAGE) EFEKTLERİNİ GÜNCELLE VE ERİT
            for (int i = 0; i < _ghosts.Length; i++)
            {
                if (_ghosts[i].Alpha > 0)
                {
                    _ghosts[i].Alpha -= dt * 500f; // Hızlıca yok ol
                    if (_ghosts[i].Alpha < 0) _ghosts[i].Alpha = 0;
                }
            }

            if (_isDashing)
            {
                _ghostSpawnTimer -= dt;
                if (_ghostSpawnTimer <= 0)
                {
                    _ghostSpawnTimer = GhostSpawnInterval;
                    for (int i = 0; i < _ghosts.Length; i++)
                    {
                        if (_ghosts[i].Alpha <= 0)
                        {
                            _ghosts[i].Bounds = GetBounds();
                            _ghosts[i].Alpha = 180f;
                            _ghosts[i].Direction = FacingDirection;
                            break;
                        }
                    }
                }
            }

            HandleInput(dt);
            ApplyPhysics(dt);
            _animator.Update();
        }

        private void HandleInput(float dt)
        {
            if (_isHurt) return;

            // Dash atarken yön girdilerini tamamen kilitle (Hollow Knight mekaniği)
            if (_isDashing) return;

            // DASH TETİKLEME KONTROLÜ
            if ((Input.GetKeyDown(Keys.LShiftKey) || Input.GetKeyDown(Keys.K)) && _dashCooldownTimer <= 0)
            {
                _dashTimer = DashDuration;
                _dashCooldownTimer = DashCooldownDuration;

                Velocity.X = FacingDirection * DashSpeed;
                Velocity.Y = 0; // Yerçekimini anlık olarak sıfırla

                _invincibilityTimer = DashDuration; // Dash süresince hasar almaz ol (I-Frame)

                // Topuklardan geriye doğru fırlayan toz bulutu
                float dustX = FacingDirection == 1 ? Position.X : Position.X + Size.Width;
                _particleSystem.SpawnCombatBurst(dustX, Position.Y + Size.Height - 8, -FacingDirection, 6, Color.White, ParticleType.Spark);
                return;
            }

            float targetVelocityX = 0;
            if (Input.GetKey(Keys.Left) || Input.GetKey(Keys.A))
            {
                targetVelocityX = -MoveSpeed;
                FacingDirection = -1;
            }
            if (Input.GetKey(Keys.Right) || Input.GetKey(Keys.D))
            {
                targetVelocityX = MoveSpeed;
                FacingDirection = 1;
            }

            if (targetVelocityX != 0)
                Velocity.X = MoveTowards(Velocity.X, targetVelocityX, Acceleration * dt);
            else
                Velocity.X = MoveTowards(Velocity.X, 0, Deceleration * dt);

            if (Input.GetKeyDown(Keys.Space) || Input.GetKeyDown(Keys.W) || Input.GetKeyDown(Keys.Up))
                _jumpBufferTimer = JumpBufferDuration;
            else
                _jumpBufferTimer -= dt;

            if (Input.GetKeyUp(Keys.Space) || Input.GetKeyUp(Keys.W) || Input.GetKeyUp(Keys.Up))
            {
                if (Velocity.Y < 0) Velocity.Y *= VariableJumpDampening;
            }

            if (Input.GetKeyDown(Keys.J) && _attackCooldownTimer <= 0)
            {
                if ((Input.GetKey(Keys.S) || Input.GetKey(Keys.Down)) && !_isGrounded) ExecutePogoAttack();
                else ExecuteStandardAttack();
            }
        }

        private void ExecuteStandardAttack()
        {
            _isAttacking = true;
            _isPogoAttack = false;
            _attackActiveTimer = AttackActiveDuration;
            _attackCooldownTimer = AttackCooldownDuration;

            float attackWidth = 48f;
            float attackHeight = 32f;
            _attackHitbox = new RectangleF(FacingDirection == 1 ? Position.X + Size.Width : Position.X - attackWidth, Position.Y, attackWidth, attackHeight);

            Velocity.X += FacingDirection * 100f;

            float slashX = FacingDirection == 1 ? Position.X + Size.Width + 5 : Position.X - 5;
            _particleSystem.SpawnCombatBurst(slashX, Position.Y + Size.Height / 2, FacingDirection, 8, Color.White, ParticleType.Shard);
        }

        private void ExecutePogoAttack()
        {
            _isAttacking = true;
            _isPogoAttack = true;
            _attackActiveTimer = AttackActiveDuration;
            _attackCooldownTimer = AttackCooldownDuration;

            _attackHitbox = new RectangleF(Position.X - 8, Position.Y + Size.Height, Size.Width + 16, 28);
            _particleSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height + 5, 0, 10, Color.LightSteelBlue, ParticleType.Spark);
        }

        public void BounceOffTarget()
        {
            if (_isPogoAttack)
            {
                Velocity.Y = JumpForce * 0.85f;
                _coyoteTimer = CoyoteTimeDuration;
                _isGrounded = false;
                _isAttacking = false;
                _isPogoAttack = false;
            }
        }

        public void TakeDamage(float amount, int knockbackDirection)
        {
            if (IsDead || _invincibilityTimer > 0) return;

            Health -= amount;
            _invincibilityTimer = InvincibilityDuration;
            _hurtStateTimer = HurtStateDuration;

            Velocity.X = knockbackDirection * 420f;
            Velocity.Y = -220f;
            _isGrounded = false;
            _isAttacking = false;

            _particleSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height / 2, -FacingDirection, 12, Color.DarkRed, ParticleType.Spark);

            if (Health <= 0)
            {
                Health = 0;
                IsDead = true;
                Velocity = new PointF(0, -380f);
                _particleSystem.SpawnCombatBurst(Position.X + Size.Width / 2, Position.Y + Size.Height / 2, 0, 35, Color.Black, ParticleType.Spark);
            }
        }

        public void ResetState(PointF respawnPosition)
        {
            Position = respawnPosition;
            Health = MaxHealth;
            IsDead = false;
            Velocity = new PointF(0, 0);
            _isGrounded = true;
            _invincibilityTimer = 0f;
            _hurtStateTimer = 0f;
            _attackCooldownTimer = 0f;
            _attackActiveTimer = 0f;
            _dashTimer = 0f;
            _dashCooldownTimer = 0f;
            _isAttacking = false;
            _animator.Play(_animIdle);
        }

        private void ApplyPhysics(float dt)
        {
            // --- YÜKSEK HIZLI DETERMINISTIC DASH FİZİĞİ ---
            if (_isDashing)
            {
                _dashTimer -= dt;
                Position.X += Velocity.X * dt;

                if (_tileMap.HasCollision(GetBounds()))
                {
                    if (Velocity.X > 0) Position.X = (float)(Math.Floor((Position.X + Size.Width) / TileMap.TileSize) * TileMap.TileSize) - Size.Width - 0.1f;
                    else if (Velocity.X < 0) Position.X = (float)(Math.Ceiling(Position.X / TileMap.TileSize) * TileMap.TileSize) + 0.1f;
                    _dashTimer = 0;
                    Velocity.X = 0;
                }

                if (_dashTimer <= 0) Velocity.X = 0;
                return; // Normal yerçekimi haritasını tamamen bypass et
            }

            if (_isGrounded) _coyoteTimer = CoyoteTimeDuration;
            else _coyoteTimer -= dt;

            if (_isHurt) Velocity.X = MoveTowards(Velocity.X, 0, Deceleration * 0.4f * dt);

            Velocity.Y += Gravity * dt;
            if (Velocity.Y > MaxFallSpeed) Velocity.Y = MaxFallSpeed;

            if (_jumpBufferTimer > 0 && _coyoteTimer > 0 && !_isHurt)
            {
                Velocity.Y = JumpForce;
                _jumpBufferTimer = 0;
                _coyoteTimer = 0;
                _isGrounded = false;
            }

            Position.X += Velocity.X * dt;
            if (_tileMap.HasCollision(GetBounds()))
            {
                if (Velocity.X > 0) Position.X = (float)(Math.Floor((Position.X + Size.Width) / TileMap.TileSize) * TileMap.TileSize) - Size.Width - 0.1f;
                else if (Velocity.X < 0) Position.X = (float)(Math.Ceiling(Position.X / TileMap.TileSize) * TileMap.TileSize) + 0.1f;
                Velocity.X = 0;
            }

            Position.Y += Velocity.Y * dt;
            _isGrounded = false;

            if (_tileMap.HasCollision(GetBounds()))
            {
                if (Velocity.Y > 0)
                {
                    Position.Y = (float)(Math.Floor((Position.Y + Size.Height) / TileMap.TileSize) * TileMap.TileSize) - Size.Height - 0.5f;
                    _isGrounded = true;
                }
                else if (Velocity.Y < 0) Position.Y = (float)(Math.Ceiling(Position.Y / TileMap.TileSize) * TileMap.TileSize) + 0.1f;
                Velocity.Y = 0;
            }
        }

        public RectangleF GetBounds() => new RectangleF(Position.X, Position.Y, Size.Width, Size.Height);

        public override void Render(System.Drawing.Graphics g)
        {
            // 1. DASH SİLÜET GÖLGELERİNİ ÇİZ (AFTERIMAGES)
            for (int i = 0; i < _ghosts.Length; i++)
            {
                if (_ghosts[i].Alpha > 0)
                {
                    using (var ghostBrush = new SolidBrush(Color.FromArgb((int)_ghosts[i].Alpha, Color.FromArgb(0, 120, 150))))
                    {
                        g.FillRectangle(ghostBrush, _ghosts[i].Bounds);
                        float gEyeX = _ghosts[i].Direction == 1 ? _ghosts[i].Bounds.X + _ghosts[i].Bounds.Width - 6 : _ghosts[i].Bounds.X + 2;
                        g.FillRectangle(Brushes.Black, gEyeX, _ghosts[i].Bounds.Y + 6, 4, 2);
                    }
                }
            }

            if (_invincibilityTimer > 0 && (int)_flickerTimer % 2 == 0 && !IsDead) return;

            // 2. ANA KARAKTER PROTOTİP GÖVDESİ
            Color playerColor = Color.White;
            if (IsDead) playerColor = Color.DarkRed;
            else if (_isHurt) playerColor = Color.OrangeRed;
            else if (_isDashing) playerColor = Color.LightCyan;

            using (var brush = new SolidBrush(playerColor))
            {
                g.FillRectangle(brush, GetBounds());
            }

            using (var eyePen = new Pen(Color.Black, 2))
            {
                float eyeX = FacingDirection == 1 ? Position.X + Size.Width - 6 : Position.X + 2;
                g.DrawLine(eyePen, eyeX, Position.Y + 6, eyeX + 4, Position.Y + 6);
            }

            // 3. HOLLOW KNIGHT TARZI KATMANLI KILIÇ SALLAMA ARKLARI
            if (_isAttacking && !IsDead)
            {
                if (_isPogoAttack)
                {
                    g.DrawArc(_slashOuterPen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, 20, 140);
                    g.DrawArc(_slashCyanPen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, 20, 140);
                    g.DrawArc(_slashWhitePen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, 20, 140);
                }
                else
                {
                    int startAngle = (FacingDirection == 1) ? -50 : 130;
                    g.DrawArc(_slashOuterPen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, startAngle, 100);
                    g.DrawArc(_slashCyanPen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, startAngle, 100);
                    g.DrawArc(_slashWhitePen, _attackHitbox.X, _attackHitbox.Y, _attackHitbox.Width, _attackHitbox.Height, startAngle, 100);
                }
            }
        }

        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }
}