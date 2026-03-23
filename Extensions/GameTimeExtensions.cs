using Microsoft.Xna.Framework;

namespace MonoGameEngine.Extensions;

public static class GameTimeExtensions
{
    public static float DeltaTime(this GameTime gameTime) => (float)gameTime.ElapsedGameTime.TotalSeconds;
}
