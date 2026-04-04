using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameEngine.Components;
using MonoGameEngine.Events;

namespace MonoGameEngine;

public class InputController : GameComponent
{
    public struct InputContext
    {
        public float deltaTime;
        public KeyboardState keyboardState;
        public MouseState mouseState;
        public JoystickState joystickState;
    }

    private Dictionary<Component, List<(string eventName, Predicate<InputContext> predicate, Action<InputContext> action)>> _inputCallbacks;

    public InputController(Game game) : base(game)
    {
        _inputCallbacks = [];
        game.Components.Add(this);
    }

    public void RegisterInputCallback(Component owner, string eventName, Predicate<InputContext> predicate, Action<InputContext> action)
    {
        if (!_inputCallbacks.ContainsKey(owner))
        {
            _inputCallbacks[owner] = [];
        }

        _inputCallbacks[owner].Add((eventName, predicate, action));
    }

    public override void Update(GameTime gameTime)
    {
        InputContext context = new InputContext
        {
            deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds,
            keyboardState = Keyboard.GetState(),
            mouseState = Mouse.GetState(),
            joystickState = Joystick.GetState(1)
        };

        foreach (var owner in _inputCallbacks.Keys)
        {
            if (owner.Entity?.IsActive == false)
            {
                continue;
            }

            foreach (var callback in _inputCallbacks[owner])
            {
                if (callback.predicate(context))
                {
                    callback.action(context);
                    GameEngine.EventBus.Publish(new InputEvent(callback.eventName));
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        _inputCallbacks.Clear();
        _inputCallbacks = null;
        Game.Components.Remove(this);
        base.Dispose(disposing);
    }

    public class InputEvent : Event
    {
        public InputEvent(string name) : base(name) { }
    }
}