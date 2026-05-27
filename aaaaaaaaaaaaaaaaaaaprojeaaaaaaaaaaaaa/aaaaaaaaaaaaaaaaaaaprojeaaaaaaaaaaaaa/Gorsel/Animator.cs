using System.Drawing;
using oyun1.Engine;


namespace oyun1.Gorsel
{
    public class Animation
    {
        public Image SpriteSheet { get; private set; }
        public int TotalFrames { get; private set; }
        public float FrameDuration { get; private set; }
        public bool IsLooping { get; private set; }

        public Animation(Image sheet, int totalFrames, float duration, bool loop = true)
        {
            SpriteSheet = sheet;
            TotalFrames = totalFrames;
            FrameDuration = duration;
            IsLooping = loop;
        }
    }

    public class Animator
    {
        public Animation CurrentAnimation { get; private set; }
        private int _currentFrame;
        private float _frameTimer;
        public bool IsFinished { get; private set; }

        public void Play(Animation animation)
        {
            if (CurrentAnimation == animation) return;

            CurrentAnimation = animation;
            _currentFrame = 0;
            _frameTimer = 0f;
            IsFinished = false;
        }

        public void Update()
        {
            if (CurrentAnimation == null) return;

            _frameTimer += Time.DeltaTime;

            if (_frameTimer >= CurrentAnimation.FrameDuration)
            {
                _frameTimer = 0f;
                _currentFrame++;

                if (_currentFrame >= CurrentAnimation.TotalFrames)
                {
                    if (CurrentAnimation.IsLooping)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = CurrentAnimation.TotalFrames - 1;
                        IsFinished = true;
                    }
                }
            }
        }

        public void Draw(System.Drawing.Graphics g, RectangleF destRect, bool flipHorizontal, int spriteWidth, int spriteHeight)
        {
            if (CurrentAnimation == null) return;

            int srcWidth = CurrentAnimation.SpriteSheet.Width / CurrentAnimation.TotalFrames;
            int srcHeight = CurrentAnimation.SpriteSheet.Height;

            Rectangle srcRect = new Rectangle(_currentFrame * srcWidth, 0, srcWidth, srcHeight);

            // Center the visual sprite directly over the physical bounding box
            float offsetX = (destRect.Width - spriteWidth) / 2f;
            float offsetY = (destRect.Height - spriteHeight) / 2f;
            RectangleF renderRect = new RectangleF(destRect.X + offsetX, destRect.Y + offsetY, spriteWidth, spriteHeight);

            if (flipHorizontal)
            {
                g.DrawImage(CurrentAnimation.SpriteSheet,
                    new RectangleF(renderRect.X + renderRect.Width, renderRect.Y, -renderRect.Width, renderRect.Height),
                    srcRect,
                    GraphicsUnit.Pixel);
            }
            else
            {
                g.DrawImage(CurrentAnimation.SpriteSheet, renderRect, srcRect, GraphicsUnit.Pixel);
            }
        }
    }
}