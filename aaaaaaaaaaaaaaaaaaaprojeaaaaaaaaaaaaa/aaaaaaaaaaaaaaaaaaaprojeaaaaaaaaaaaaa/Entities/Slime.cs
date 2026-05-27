using oyun1.Levels;
using System;
using System.Drawing;
using Time = oyun1.Engine.Time;

namespace oyun1.Entities
{
    public class Slime : Enemy
    {
        private const float HopCooldown = 1.2f;
        private bool _isGrounded;

        public Slime(float x, float y, Player player, TileMap tileMap)
            : base(x, y, 28, 20, 20f, player, tileMap) { }

        public override void Update()
        {
            float dt = Time.DeltaTime;

            // 1. GLOBAL VISUAL TIMERS
            if (HitFlashTimer > 0) HitFlashTimer -= dt;
            if (AttackCooldownTimer > 0) AttackCooldownTimer -= dt;

            // 2. DEAD STATE MECHANICS
            if (CurrentState == EnemyState.Dead)
            {
                Velocity.Y += 1200f * dt; // Gravity pull
                Position.Y += Velocity.Y * dt;
                Position.X += Velocity.X * dt;
                Velocity.X = MoveTowards(Velocity.X, 0, 150f * dt);
                return;
            }

            // 3. HURT STATE MECHANICS
            if (CurrentState == EnemyState.Hurt)
            {
                HurtStunTimer -= dt;
                ApplyGravityAndCollisions(dt);
                if (HurtStunTimer <= 0) CurrentState = EnemyState.Idle;
                return;
            }

            // 4. CORE BEHAVIOR STATE ENGINE
            StateTimer -= dt;
            FacingDirection = (TargetPlayer.Position.X > Position.X) ? 1 : -1;

            switch (CurrentState)
            {
                case EnemyState.Idle:
                    Velocity.X = 0;
                    ApplyGravityAndCollisions(dt);

                    // Reposition/Track when player enters active aggression zones
                    float distToPlayer = Math.Abs(TargetPlayer.Position.X - Position.X);
                    if (distToPlayer < 300f && AttackCooldownTimer <= 0 && _isGrounded)
                    {
                        CurrentState = EnemyState.Anticipation;
                        StateTimer = 0.45f; // Windup shake window for 450ms
                    }
                    break;

                case EnemyState.Anticipation:
                    Velocity.X = 0; // Freeze completely to show intent
                    ApplyGravityAndCollisions(dt);

                    if (StateTimer <= 0)
                    {
                        // Launch attack jump!
                        CurrentState = EnemyState.Attack;
                        Velocity.X = FacingDirection * 320f;
                        Velocity.Y = -420f; // Arc up into air space
                        _isGrounded = false;
                    }
                    break;

                case EnemyState.Attack:
                    ApplyGravityAndCollisions(dt);

                    // Landed or lost speed targets return to recovery
                    if (_isGrounded || LevelTileMap.HasCollision(GetBounds()))
                    {
                        CurrentState = EnemyState.Recovery;
                        StateTimer = 0.3f; // Brief breather landing window
                        Velocity.X = 0;
                        AttackCooldownTimer = HopCooldown;
                    }
                    break;

                case EnemyState.Recovery:
                    Velocity.X = 0;
                    ApplyGravityAndCollisions(dt);
                    if (StateTimer <= 0) CurrentState = EnemyState.Idle;
                    break;
            }
        }

        private void ApplyGravityAndCollisions(float dt)
        {
            Velocity.Y += 1400f * dt; // Gravity constant

            // X Axis
            Position.X += Velocity.X * dt;
            if (LevelTileMap.HasCollision(GetBounds()))
            {
                if (Velocity.X > 0) Position.X = (float)(Math.Floor((Position.X + Size.Width) / TileMap.TileSize) * TileMap.TileSize) - Size.Width - 0.1f;
                else if (Velocity.X < 0) Position.X = (float)(Math.Ceiling(Position.X / TileMap.TileSize) * TileMap.TileSize) + 0.1f;
                Velocity.X = 0;
            }

            // Y Axis
            Position.Y += Velocity.Y * dt;
            _isGrounded = false;
            if (LevelTileMap.HasCollision(GetBounds()))
            {
                if (Velocity.Y > 0)
                {
                    Position.Y = (float)(Math.Floor((Position.Y + Size.Height) / TileMap.TileSize) * TileMap.TileSize) - Size.Height - 0.1f;
                    _isGrounded = true;
                }
                else if (Velocity.Y < 0) Position.Y = (float)(Math.Ceiling(Position.Y / TileMap.TileSize) * TileMap.TileSize) + 0.1f;
                Velocity.Y = 0;
            }
        }

        public override void Render(System.Drawing.Graphics g)
        {
            // --- WHITE FLASH OVERRIDE LOOP ---
            if (HitFlashTimer > 0)
            {
                g.FillRectangle(Brushes.White, GetBounds());
                return;
            }

            // Adjust colors dynamically based on telegraph phase
            Color slimeColor = Color.LimeGreen;
            if (CurrentState == EnemyState.Anticipation)
                slimeColor = Color.Orange; // Turning orange indicates compression build-up
            else if (CurrentState == EnemyState.Attack)
                slimeColor = Color.SpringGreen;

            using (var brush = new SolidBrush(slimeColor))
            {
                g.FillRectangle(brush, GetBounds());
            }

            // Draw simple eye looking at direction
            float eyeX = FacingDirection == 1 ? Position.X + Size.Width - 6 : Position.X + 2;
            g.FillRectangle(Brushes.Black, eyeX, Position.Y + 4, 4, 4);
        }

        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }
}