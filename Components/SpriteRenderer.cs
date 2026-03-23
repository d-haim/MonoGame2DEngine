using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameEngine.Extensions;

namespace MonoGameEngine.Components;

public sealed class SpriteRenderer : Component
{
    public Texture2D Texture { get; set; } = null;
    public Vector2 Pivot => Texture != null ? Texture.GetCenter() : Vector2.Zero;
    public SpriteEffects Effects { get; set; } = SpriteEffects.None;
    public Color Color { get; set; } = Color.White;

    public SpriteRenderer(GameEntity entity) : base(entity) { }
}