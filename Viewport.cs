using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameEngine;

public static class Viewport
{
    //TODO Should be external configuration
    public const int VIRTUAL_WIDTH = 800;
    public const int VIRTUAL_HEIGHT = 600;

    public static int Width => _graphics.PreferredBackBufferWidth;
    public static int Height => _graphics.PreferredBackBufferHeight;

    private static GraphicsDeviceManager _graphics;

    internal static void Initialize(GraphicsDeviceManager graphics, int width, int height, bool fullScreen)
    {
        _graphics = graphics;
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.IsFullScreen = fullScreen;
        _graphics.ApplyChanges();
    }

    public static Matrix GetScaleMatrix(GraphicsDevice device)
    {
        float scaleX = (float)device.PresentationParameters.BackBufferWidth / (float)VIRTUAL_WIDTH;
        float scaleY = (float)device.PresentationParameters.BackBufferHeight / (float)VIRTUAL_HEIGHT;
        float finalScale = Math.Min(scaleX, scaleY);

        // Calculate offsets to center the virtual screen
        float offsetX = (device.Viewport.Width - (VIRTUAL_WIDTH * finalScale)) / 2f;
        float offsetY = (device.Viewport.Height - (VIRTUAL_HEIGHT * finalScale)) / 2f;

        return Matrix.CreateScale(finalScale, finalScale, 1.0f) *
               Matrix.CreateTranslation(offsetX, offsetY, 0f);
    }
}