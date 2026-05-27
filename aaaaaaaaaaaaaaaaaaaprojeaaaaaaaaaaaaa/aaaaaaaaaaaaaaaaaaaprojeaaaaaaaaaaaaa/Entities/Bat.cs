using oyun1.Levels;
using System;
using System.Drawing;
using Time = oyun1.Engine.Time;

namespace oyun1.Entities
{
    public class Bat : Enemy
    {
        private PointF _attackDirectionVector;
        private PointF _homePosition;

        public Bat(float x, float y, Player player, TileMap tileMap)
            : base(x, y, 22, 22, 15f, player, tileMap)
        {
            _homePosition = new PointF(x, y);
        }

        public override void Update()
        {
            float dt = Time.DeltaTime;

            if (HitFlashTimer > 0) HitFlashTimer -= dt;
            if (AttackCooldownTimer > 0) AttackCooldownTimer -= dt;

            if (CurrentState == EnemyState.Dead)
            {
                Velocity.Y += 1000f * dt; // Gravity drops corpse from sky
                Position.X += Velocity.X * dt;
                Position.Y += Velocity.Y * dt;
                if (LevelTileMap.HasCollision(GetBounds())) Velocity = new PointF(0, 0);
                return;
            }

            if (CurrentState == EnemyState.Hurt)
            {
                HurtStunTimer -= dt;
                Position.X += Velocity.X * dt;
                Position.Y += Velocity.Y * dt;
                if (HurtStunTimer <= 0) CurrentState = EnemyState.Idle;
                return;
            }

            StateTimer -= dt;
            FacingDirection = (TargetPlayer.Position.X > Position.X) ? 1 : -1;

            float deltaX = TargetPlayer.Position.X - Position.X;
            float deltaY = TargetPlayer.Position.Y - Position.Y;
            float distance = (float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            switch (CurrentState)
            {
                case EnemyState.Idle:
                    // Float smoothly back to home hovering heights
                    float homeDX = _homePosition.X - Position.X;
                    float homeDY = _homePosition.Y - Position.Y;
                    Velocity.X = MoveTowards(Velocity.X, homeDX * 2f, 150f * dt);
                    Velocity.Y = MoveTowards(Velocity.Y, homeDY * 2f, 150f * dt);

                    if (distance < 240f && AttackCooldownTimer <= 0)
                    {
                        CurrentState = EnemyState.Chase;
                    }
                    TranslateFlyPosition(dt);
                    break;

                case EnemyState.Chase:
                    // Hover smoothly into overhead strike positions
                    float targetX = TargetPlayer.Position.X;
                    float targetY = TargetPlayer.Position.Y - 60f; // Hover 60 pixels above head

                    Velocity.X = MoveTowards(Velocity.X, (targetX - Position.X) * 4f, 300f * dt);
                    Velocity.Y = MoveTowards(Velocity.Y, (targetY - Position.Y) * 4f, 300f * dt);
                    TranslateFlyPosition(dt);

                    if (distance < 140f && AttackCooldownTimer <= 0)
                    {
                        CurrentState = EnemyState.Anticipation;
                        StateTimer = 0.35f; // Freeze air anchor for 350ms
                        Velocity = new PointF(0, 0);
                    }
                    break;

                case EnemyState.Anticipation:
                    // Anchor in mid-air and compute final sharp dive vector
                    Velocity = new PointF(0, 0);
                    if (StateTimer <= 0)
                    {
                        CurrentState = EnemyState.Attack;
                        StateTimer = 0.4f; // Dive movement window limit

                        // Standardize heading angle vectors accurately
                        _attackDirectionVector = new PointF(deltaX / distance, deltaY / distance);
                        Velocity.X = _attackDirectionVector.X * 450f; // High speed dive impact speed
                        Velocity.Y = _attackDirectionVector.Y * 450f;
                    }
                    break;

                case EnemyState.Attack:
                    TranslateFlyPosition(dt);

                    // Complete move window limits or wall clicks move to retreat
                    if (StateTimer <= 0 || LevelTileMap.HasCollision(GetBounds()))
                    {
                        CurrentState = EnemyState.Recovery;
                        StateTimer = 0.5f; // Retract retreat state limits for 500ms
                        AttackCooldownTimer = 1.5f; // Prevent spamming attacks immediately

                        // Fling backwards away from player coordinates
                        Velocity.X = -FacingDirection * 200f;
                        Velocity.Y = -120f;
                    }
                    break;

                case EnemyState.Recovery:
                    // Retreat/Reposition back up to safe boundaries safely
                    Velocity.X = MoveTowards(Velocity.X, -FacingDirection * 150f, 200f * dt);
                    Velocity.Y = MoveTowards(Velocity.Y, -100f, 200f * dt);
                    TranslateFlyPosition(dt);

                    if (StateTimer <= 0) CurrentState = EnemyState.Idle;
                    break;
            }
        }

        private void TranslateFlyPosition(float dt)
        {
            Position.X += Velocity.X * dt;
            Position.Y += Velocity.Y * dt;

            // Basic wrap check to keep floating nodes bounding correctly within arena layers
            if (LevelTileMap.HasCollision(GetBounds()))
            {
                Position.X -= Velocity.X * dt;
                Position.Y -= Velocity.Y * dt;
                Velocity = new PointF(-Velocity.X * 0.5f, -Velocity.Y * 0.5f);
            }
        }

        public override void Render(System.Drawing.Graphics g)
        {
            if (HitFlashTimer > 0)
            {
                g.FillRectangle(Brushes.White, GetBounds());
                return;
            }

            Color batColor = Color.MediumPurple;
            if (CurrentState == EnemyState.Anticipation)
                batColor = Color.DarkViolet; // Violent violet charge indicator
            else if (CurrentState == EnemyState.Attack)
                batColor = Color.Fuchsia;

            using (var brush = new SolidBrush(batColor))
            {
                g.FillRectangle(brush, GetBounds());
            }

            // Angry bat eyes
            float eyeX = FacingDirection == 1 ? Position.X + Size.Width - 8 : Position.X + 4;
            g.FillRectangle(Brushes.Red, eyeX, Position.Y + 4, 3, 3);
        }

        private float MoveTowards(float current, float target, float maxDelta)
        {
            if (Math.Abs(target - current) <= maxDelta) return target;
            return current + Math.Sign(target - current) * maxDelta;
        }
    }
}