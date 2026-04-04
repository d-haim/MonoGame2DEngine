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
        public float DeltaTime { get; internal set; }
        public KeyboardState KeyboardState { get; internal set; }
        public MouseState MouseState { get; internal set; }
        public JoystickState JoystickState { get; internal set; }
        public int HorizontalRaw { get; internal set; }
        public int VerticalRaw { get; internal set; }
        public float Horizontal { get; internal set; }
        public float Vertical { get; internal set; }
    }

    private Dictionary<Component, List<(string eventName, Predicate<InputContext> predicate, Action<InputContext> action)>> _inputCallbacks;
    private float _horizontal;
    private float _vertical;
    private float _inputAcceleration = 1.0f;

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
        var keyboardState = Keyboard.GetState();
        var mouseState = Mouse.GetState();
        var joystickState = Joystick.GetState(1);

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

        //Keyboard input
        //TODO Keys should be configurable
        if (keyboardState.IsKeyDown(Keys.W) || keyboardState.IsKeyDown(Keys.Up))
        {
            if (_vertical > 0)
                this._vertical = 0;

            this._vertical -= deltaTime * _inputAcceleration;
        }
        else if (keyboardState.IsKeyDown(Keys.S) || keyboardState.IsKeyDown(Keys.Down))
        {
            if (_vertical < 0)
                this._vertical = 0;

            this._vertical += deltaTime * _inputAcceleration;
        }
        else
        {
            this._vertical = 0;
        }


        if (keyboardState.IsKeyDown(Keys.A) || keyboardState.IsKeyDown(Keys.Left))
        {
            if (_horizontal > 0)
                this._horizontal = 0;

            this._horizontal -= deltaTime * _inputAcceleration;
        }
        else if (keyboardState.IsKeyDown(Keys.D) || keyboardState.IsKeyDown(Keys.Right))
        {
            if (_horizontal < 0)
                this._horizontal = 0;

            this._horizontal += deltaTime * _inputAcceleration;
        }
        else
        {
            this._horizontal = 0;
        }

        this._horizontal = Math.Clamp(this._horizontal, -1, 1);
        this._vertical = Math.Clamp(this._vertical, -1, 1);

        InputContext context = new InputContext
        {
            DeltaTime = deltaTime,
            KeyboardState = Keyboard.GetState(),
            MouseState = Mouse.GetState(),
            JoystickState = Joystick.GetState(1),
            HorizontalRaw = this._horizontal > 0 ? 1 : this._horizontal < 0 ? -1 : 0,
            VerticalRaw = this._vertical > 0 ? 1 : this._vertical < 0 ? -1 : 0,
            Horizontal = this._horizontal,
            Vertical = this._vertical
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