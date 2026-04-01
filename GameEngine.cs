using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameEngine;

public class GameEngine : Game
{
    public static GraphicsDeviceManager Graphics { get; private set; }
    public static new GraphicsDevice GraphicsDevice { get; private set; }
    public static SpriteBatch SpriteBatch { get; private set; }
    public static new ContentManager Content { get; private set; }
    public static AudioController Audio { get; private set; }

    public GameEngine(string title, int width, int height, bool fullScreen)
    {
        Window.Title = title;
        Graphics = new GraphicsDeviceManager(this);
        Audio = new AudioController();
        Viewport.Initialize(Graphics, width, height, fullScreen);
        Content = base.Content;
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        base.Initialize();
        GraphicsDevice = base.GraphicsDevice;
        Bootstrapper.InvokeInitializationMethods();
    }

    protected override void LoadContent()
    {
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        Bootstrapper.InvokeContentLoadingMethods(Content);
        SceneManager.LoadScene(0);
        base.LoadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        SceneManager.CurrentScene.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        SceneManager.CurrentScene.Draw(SpriteBatch);
        base.Draw(gameTime);
    }
}
