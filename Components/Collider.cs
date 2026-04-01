using Microsoft.Xna.Framework;

namespace MonoGameEngine.Components;

public sealed class Collider : Component
{
    public bool IsTrigger { get; set; } = false;

    public Rectangle Bounds
    {
        get
        {
            if (Entity.Renderer?.Texture == null)
                return Rectangle.Empty;

            float width = Entity.Renderer.Texture.Width * Entity.Transform.Scale;
            float height = Entity.Renderer.Texture.Height * Entity.Transform.Scale;

            return new Rectangle(
                (int)(Entity.Transform.Position.X - width / 2),
                (int)(Entity.Transform.Position.Y - height / 2),
                (int)width,
                (int)height
            );
        }
    }

    public Collider(bool isTrigger = false)
    {
        IsTrigger = isTrigger;
    }

    public bool Intersects(Collider other)
    {
        if (other == null) return false;
        return Bounds.Intersects(other.Bounds);
    }
}
