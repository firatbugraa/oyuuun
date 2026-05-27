using System.Drawing;

namespace oyun1.Entities
{
    public abstract class Entity
    {
        public PointF Position;
        public Size Size;
        public PointF Velocity;

        public Entity(float x, float y, int width, int height)
        {
            Position = new PointF(x, y);
            Size = new Size(width, height);
            Velocity = new PointF(0, 0);
        }

        public abstract void Update();
        public abstract void Render(System.Drawing.Graphics g);
    }
}