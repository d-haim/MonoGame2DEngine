using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameEngine;

public static class Viewport
{
    //TODO Should be external configuration
    public const int VIRTUAL_WIDTH = 600;
    public const int VIRTUAL_HEIGHT = 800;

    public static int Width => _graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
    public static int Height => _graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;
    public static Rectangle Bounds => new Rectangle(0, 0, Width, Height);

    private static GraphicsDeviceManager _graphics;
    private static Rectangle _bounds => new Rectangle(0, 0, Width, Height);

    internal static void Initialize(GraphicsDeviceManager graphics, int width, int height, bool fullScreen)
    {
        _graphics = graphics;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.IsFullScreen = fullScreen;
        _graphics.ApplyChanges();
    }

    public static Matrix GetScaleMatrix()
    {
        float width = Width;
        float height = Height;

        float scaleX = width / (float)VIRTUAL_WIDTH;
        float scaleY = height / (float)VIRTUAL_HEIGHT;
        float finalScale = Math.Min(scaleX, scaleY);

        // Calculate offsets to center the virtual screen
        float offsetX = (width - (VIRTUAL_WIDTH * finalScale)) / 2f;
        float offsetY = (height - (VIRTUAL_HEIGHT * finalScale)) / 2f;

        return Matrix.CreateScale(finalScale, finalScale, 1.0f) *
               Matrix.CreateTranslation(offsetX, offsetY, 0f);
    }

    public static RenderTarget2D GetRenderTarget2D()
    {
        return new RenderTarget2D(_graphics.GraphicsDevice, VIRTUAL_WIDTH, VIRTUAL_HEIGHT);
    }

    public static bool InBounds(int x, int y) => Bounds.Contains(x, y);
    public static bool InBounds(Vector2 position) => Bounds.Contains(position);

}