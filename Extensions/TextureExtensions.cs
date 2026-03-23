using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameEngine.Extensions;

public static class TextureExtensions
{
    public static Vector2 GetCenter(this Texture2D texture)
    {
        return new Vector2(texture.Width / 2, texture.Height / 2);
    }
}