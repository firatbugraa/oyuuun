using System;
using System.Drawing;

namespace oyun1.Engine
{
    public class Camera
    {
        public PointF Position;
        public SizeF ViewportSize { get; private set; }
        public RectangleF Bounds { get; private set; }

        // Sarsıntı (Shake) Değişkenleri
        private float _shakeTimer;
        private float _shakeIntensity;
        private readonly Random _rand = new Random();

        // Varsayılan olarak makul bir ekran ve harita boyutuyla başlatıyoruz (Hata fırlatmaması için)
        public Camera()
        {
            Position = new PointF(0, 0);
            ViewportSize = new SizeF(800, 600); // Form ekran boyutu varsayılanı
            Bounds = new RectangleF(0, 0, 60 * 32, 40 * 32); // Harita sınırları (60x40 tile)
        }

        // Yumuşak Takip (Lerp) Metodu
        public void Follow(float targetX, float targetY, float dt)
        {
            // Kamera hedefin tam merkezine odaklanacak şekilde kaydırılır
            float destX = targetX - (ViewportSize.Width / 2f);
            float destY = targetY - (ViewportSize.Height / 2f);

            // Yumuşak geçiş (Interpolation) hızı
            float lerpSpeed = 6f;
            Position.X += (destX - Position.X) * lerpSpeed * dt;
            Position.Y += (destY - Position.Y) * lerpSpeed * dt;

            // Kamerayı harita sınırlarının dışına taşırmayacak şekilde kelepçele (Clamp)
            Position.X = Math.Max(Bounds.Left, Math.Min(Position.X, Bounds.Right - ViewportSize.Width));
            Position.Y = Math.Max(Bounds.Top, Math.Min(Position.Y, Bounds.Bottom - ViewportSize.Height));

            // Sarsıntı efektini işlet
            if (_shakeTimer > 0)
            {
                _shakeTimer -= dt;
                Position.X += (float)(_rand.NextDouble() * 2 - 1) * _shakeIntensity;
                Position.Y += (float)(_rand.NextDouble() * 2 - 1) * _shakeIntensity;
            }
        }

        // Sarsıntıyı dışarıdan tetikleme metodu
        public void MakeShake(float duration, float intensity)
        {
            _shakeTimer = duration;
            _shakeIntensity = intensity;
        }

        // Görünür alan sınırlarını dikdörtgen olarak dönen metot (Frustum Culling için)
        public RectangleF GetViewportBounds()
        {
            return new RectangleF(Position.X, Position.Y, ViewportSize.Width, ViewportSize.Height);
        }
    }
}