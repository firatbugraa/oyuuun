using System;
using System.Drawing;
using oyun1.Engine;
using oyun1.Levels;

namespace oyun1.Entities
{
    public enum EnemyState { Idle, Chase, Anticipation, Attack, Recovery, Hurt, Dead }

    public abstract class Enemy : Entity
    {
        public EnemyState CurrentState { get; protected set; } = EnemyState.Idle;

        // --- TIMING COROUTINE SIMULATORS ---
        protected float StateTimer;
        protected float AttackCooldownTimer;
        protected float HurtStunTimer;

        // --- HOLLOW KNIGHT HIT FLASH ENGINE ---
        protected float HitFlashTimer;
        protected const float HitFlashDuration = 0.06f; // Solid white override for 60ms

        protected Player TargetPlayer;
        protected TileMap LevelTileMap;
        protected int FacingDirection = 1;

        public float Health { get; protected set; }
        public float MaxHealth { get; protected set; }

        public Enemy(float x, float y, int width, int height, float maxHealth, Player player, TileMap tileMap)
            : base(x, y, width, height)
        {
            MaxHealth = maxHealth;
            Health = maxHealth;
            TargetPlayer = player;
            LevelTileMap = tileMap;
        }

        public virtual void TakeDamage(float amount, int knockbackDir)
        {
            if (CurrentState == EnemyState.Dead) return;

            Health -= amount;
            HitFlashTimer = HitFlashDuration; // Trigger instant structure white flash
            HurtStunTimer = 0.2f;            // Lock AI brain for 200ms
            CurrentState = EnemyState.Hurt;

            // Apply crisp directional knockback forces
            Velocity.X = knockbackDir * 300f;
            Velocity.Y = -150f;

            if (Health <= 0)
            {
                Health = 0;
                CurrentState = EnemyState.Dead;
                Velocity = new PointF(0, -250f); // Gravity tumble down on death
            }
        }

        public RectangleF GetBounds() => new RectangleF(Position.X, Position.Y, Size.Width, Size.Height);
    }
}