using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameEngine.Components;
using MonoGameEngine.Extensions;
using MonoGameEngine.Loggers;

namespace MonoGameEngine;

public sealed class Scene : IDisposable
{
    private const string ON_ENABLE_METHOD_NAME = "OnEnable";
    private const string ON_DISABLE_METHOD_NAME = "OnDisable";
    private const string ON_UPDATE_METHOD_NAME = "OnUpdate";
    private const string DELTA_TIME_PARAMETER_NAME = "deltaTime";
    private const string ON_DRAW_METHOD_NAME = "OnDraw";
    private const string ON_COLLISION_METHOD_NAME = "OnCollision";
    private const string ON_TRIGGER_METHOD_NAME = "OnTrigger";

    private List<GameEntity> _entities = [];
    private HashSet<Component> _cachedComponents = [];
    private List<GameEntity> _pendingAdd = [];
    private List<GameEntity> _pendingRemove = [];
    private List<GameEntity> _pendingEnable = [];
    private List<GameEntity> _pendingDisable = [];
    private Dictionary<Component, Action> _onEnableMethods = [];
    private Dictionary<Component, Action> _onDisableMethods = [];
    private Dictionary<Component, Action<float>> _onUpdateMethods = [];
    private Dictionary<Component, Action> _onDrawMethods = [];
    private Dictionary<Component, Action<Collider>> _onCollisionMethods = [];
    private Dictionary<Component, Action<Collider>> _onTriggerMethods = [];
    private HashSet<Action<float>> _activeUpdates = [];
    private readonly RenderTarget2D _target;
    private bool _isDisposing = false;

    public Scene()
    {
        _target = Viewport.GetRenderTarget2D();
    }

    /// <summary>
    /// Add an entity from the scene
    /// Invokes OnEnable callbacks on entity components
    /// </summary>
    /// <param name="entity"></param>
    public void AddEntity(GameEntity entity)
    {
        if (_isDisposing)
            return;
        try
        {
            ArgumentNullException.ThrowIfNull(entity);
        }
        catch (ArgumentNullException e)
        {
            System.Diagnostics.Debug.Write(e.Message);
            System.Diagnostics.Debug.WriteLine(e.StackTrace);
            return;
        }

        if (_pendingAdd.Contains(entity) || _entities.Contains(entity))
            return;

        _pendingAdd.Add(entity);
        EnableEntity(entity);
    }

    /// <summary>
    /// Remove an entity from the scene
    /// Invokes OnDisable callback on entity components
    /// </summary>
    /// <param name="entity"></param>
    public void RemoveEntity(GameEntity entity)
    {
        if (_isDisposing)
            return;
        try
        {
            ArgumentNullException.ThrowIfNull(entity);
        }
        catch (ArgumentNullException e)
        {
            System.Diagnostics.Debug.Write(e.Message);
            System.Diagnostics.Debug.WriteLine(e.StackTrace);
            return;
        }

        if (_pendingRemove.Contains(entity) || !_entities.Contains(entity))
            return;

        DisableEntity(entity);
        _pendingRemove.Add(entity);
    }

    /// <summary>
    /// Enable an entity that is attached to the scene and is currently disabled
    /// Invokes OnEnable callback on entity components
    /// </summary>
    /// <param name="entity"></param>
    public void EnableEntity(GameEntity entity)
    {
        if (_isDisposing)
            return;
        if (_pendingEnable.Contains(entity))
            return;

        _pendingEnable.Add(entity);
    }

    /// <summary>
    /// Disable an entity that is attached to the scene and is currently enabled
    /// Invokes OnEnable callback on entity components
    /// Entity will not render, update and perform collision check
    /// </summary>
    /// <param name="entity"></param>
    public void DisableEntity(GameEntity entity)
    {
        if (_isDisposing)
            return;
        if (_pendingDisable.Contains(entity))
            return;

        _pendingDisable.Add(entity);
    }

    public void Unload()
    {
        Dispose();
    }

    internal void Update(GameTime gameTime)
    {
        if (_isDisposing)
            return;
        ApplyChanges();
        DoEntityUpdates(gameTime);
        DoCollisions();
    }

    internal void RegisterComponentsCallbacks(IEnumerable<Component> components)
    {
        foreach (var component in components)
        {
            RegisterComponentCallbacks(component);
        }
    }

    internal void RegisterComponentCallbacks(Component component)
    {
        bool wasCached = false;

        if (component.TryGetCallbackFromMethod<Action>(ON_ENABLE_METHOD_NAME, out var activeMethod))
        {
            _onEnableMethods.Add(component, activeMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action>(ON_DISABLE_METHOD_NAME, out var deactivateMethod))
        {
            _onDisableMethods.Add(component, deactivateMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action<float>>(ON_UPDATE_METHOD_NAME, out Action<float> updateMethod,
            m => m.VerifyMethodParameters((typeof(float), DELTA_TIME_PARAMETER_NAME))))
        {
            _onUpdateMethods.Add(component, updateMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action>(ON_DRAW_METHOD_NAME, out var drawMethod))
        {
            _onDrawMethods.Add(component, drawMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action<Collider>>(ON_COLLISION_METHOD_NAME, out var onCollisionMethod,
            m => m.VerifyMethodParameters((typeof(Collider), "other"))))
        {
            _onCollisionMethods.Add(component, onCollisionMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action<Collider>>(ON_TRIGGER_METHOD_NAME, out var onTriggerMethod,
            m => m.VerifyMethodParameters((typeof(Collider), "other"))))
        {
            _onTriggerMethods.Add(component, onTriggerMethod);
            wasCached = true;
        }

        if (wasCached)
        {
            _cachedComponents.Add(component);
        }
    }

    internal void UnregisterComponentsCallbacks(IEnumerable<Component> components)
    {
        foreach (var component in components)
        {
            UnregisterComponentCallbacks(component);
        }
    }

    internal void UnregisterComponentCallbacks(Component component)
    {
        if (!_cachedComponents.Remove(component))
            return;

        _onEnableMethods.Remove(component);
        _onDisableMethods.Remove(component);
        _onUpdateMethods.Remove(component);
        _onDrawMethods.Remove(component);
        _onCollisionMethods.Remove(component);
        _onTriggerMethods.Remove(component);
    }

    private void InternalEnableEntity(GameEntity entity)
    {
        entity.SetActive(true, this);

        var components = entity.Components.DeepCopy();
        foreach (var component in components)
        {
            if (!_cachedComponents.Contains(component))
                continue;

            if (_onUpdateMethods.TryGetValue(component, out var onUpdate))
            {
                _activeUpdates.Add(onUpdate);
            }

            if (_onEnableMethods.TryGetValue(component, out var callback))
            {
                try
                {
                    callback();
                }
                catch (System.Exception e)
                {
                    OnComponentCallbackError(component, e.Message, ON_ENABLE_METHOD_NAME, e.StackTrace);
                }
            }
        }
    }

    private void InternalDisableEntity(GameEntity entity)
    {
        entity.SetActive(false, this);

        var components = entity.Components.DeepCopy();
        foreach (var component in components)
        {
            if (!_cachedComponents.Contains(component))
                continue;

            if (_onUpdateMethods.TryGetValue(component, out var onUpdate))
            {
                _activeUpdates.Remove(onUpdate);
            }

            if (_onDisableMethods.TryGetValue(component, out var callback))
            {
                try
                {
                    callback();
                }
                catch (System.Exception e)
                {
                    OnComponentCallbackError(component, e.Message, ON_DISABLE_METHOD_NAME, e.StackTrace);
                }
            }
        }
    }

    /// <summary>
    /// Apply changes to active entities and components before performing updated and collisions
    /// </summary>
    private void ApplyChanges()
    {
        if (_pendingDisable.Count > 0)
        {
            List<GameEntity> toDisable = _pendingDisable.DeepCopy();
            _pendingDisable.Clear();
            foreach (var entity in toDisable)
            {
                InternalDisableEntity(entity);
            }
            toDisable.Clear();
        }

        if (_pendingRemove.Count > 0)
        {
            List<GameEntity> toRemove = _pendingRemove.DeepCopy();
            _pendingRemove.Clear();
            foreach (var entity in toRemove)
            {
                var components = entity.Components.DeepCopy();
                UnregisterComponentsCallbacks(components);
                entity.AttachedScene = null;
                _entities.Remove(entity);
            }
            toRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            List<GameEntity> toAdd = _pendingAdd.DeepCopy();
            _pendingAdd.Clear();
            foreach (var entity in toAdd)
            {
                entity.AttachedScene = this;
                var components = entity.Components.DeepCopy();
                RegisterComponentsCallbacks(components);
                _entities.Add(entity);
            }
            toAdd.Clear();
        }

        if (_pendingEnable.Count > 0)
        {
            List<GameEntity> toEnable = _pendingEnable.DeepCopy();
            _pendingEnable.Clear();
            foreach (var entity in toEnable)
            {
                InternalEnableEntity(entity);
            }
            toEnable.Clear();
        }
    }

    private void DoEntityUpdates(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var callback in _activeUpdates)
        {
            try
            {
                callback(deltaTime);
            }
            catch (System.Exception e)
            {
                OnComponentCallbackError(null, e.Message, ON_DRAW_METHOD_NAME, e.StackTrace);
            }
        }
    }

    /// <summary>
    /// Collision check done with comparing all the active non-trigger colliders against all colliders
    /// </summary>
    private void DoCollisions()
    {
        var activeColliders = _entities.Where(e => e.Collider != null).Select(e => e.Collider);
        foreach (var collider in activeColliders)
        {
            if (collider.IsTrigger == false)
            {
                foreach (var other in activeColliders)
                {
                    if (collider != other && collider.Intersects(other))
                    {
                        var components = collider.Entity.Components.DeepCopy();
                        foreach (var component in components)
                        {
                            if (component == collider || _cachedComponents.Contains(component) == false)
                            {
                                continue;
                            }

                            if (_onCollisionMethods.TryGetValue(component, out var collision))
                            {
                                try
                                {
                                    collision(other);
                                }
                                catch (System.Exception e)
                                {
                                    OnComponentCallbackError(component, e.Message, ON_COLLISION_METHOD_NAME, e.StackTrace);
                                }
                            }
                        }

                        if (other.IsTrigger)
                        {
                            components = other.Entity.Components.DeepCopy();
                            foreach (var component in components)
                            {
                                if (component == other || _cachedComponents.Contains(component) == false)
                                {
                                    continue;
                                }

                                if (_onTriggerMethods.TryGetValue(component, out var trigger))
                                {
                                    try
                                    {
                                        trigger(collider);
                                    }
                                    catch (System.Exception e)
                                    {
                                        OnComponentCallbackError(component, e.Message, ON_TRIGGER_METHOD_NAME, e.StackTrace);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private void DrawScene(SpriteBatch spriteBatch)
    {
        ApplyChanges();
        var visibleEntities = _entities.Where(e => e.IsVisible);
        foreach (var entity in visibleEntities)
        {
            SpriteRenderer sr = null;
            foreach (var component in entity.Components)
            {
                if (component is SpriteRenderer spriteRenderer)
                {
                    sr = spriteRenderer;
                    continue;
                }

                if (_onDrawMethods.TryGetValue(component, out var callback))
                {
                    try
                    {
                        callback();
                    }
                    catch (System.Exception e)
                    {
                        OnComponentCallbackError(component, e.Message, ON_DRAW_METHOD_NAME, e.StackTrace);
                    }
                }
            }

            if (sr != null)
            {
                try
                {
                    sr.Draw(spriteBatch);
                }
                catch (System.Exception e)
                {
                    OnComponentCallbackError(sr, e.Message, "SpriteRender.Draw", e.StackTrace);
                }
            }
        }
    }

    private void OnComponentCallbackError(Component component, string message, string callbackName, string stackTrace = "")
    {
        GameEngine.Logger.Log($"{component?.Entity.Name}.{component?.GetType().Name}. {callbackName} error {message}.\n{stackTrace}", ILogger.LogLevel.Error);
    }

    public RenderTarget2D GetFrame()
    {
        GameEngine.GraphicsDevice.SetRenderTarget(_target);
        GameEngine.GraphicsDevice.Clear(Color.Black);

        GameEngine.SpriteBatch.Begin();
        DrawScene(GameEngine.SpriteBatch);
        GameEngine.SpriteBatch.End();

        GameEngine.GraphicsDevice.SetRenderTarget(null);
        return _target;
    }

    public void Dispose()
    {
        if (_isDisposing)
            return;
        _isDisposing = true;
        _pendingAdd.Clear();
        _pendingEnable.Clear();
        _pendingDisable.Clear();
        _pendingRemove.Clear();
        _pendingRemove.AddRange(_entities);
        ApplyChanges();
        _cachedComponents.Clear();
        _onCollisionMethods.Clear();
        _onTriggerMethods.Clear();
        _onDisableMethods.Clear();
        _onEnableMethods.Clear();
        _onDrawMethods.Clear();
        _onUpdateMethods.Clear();
        _activeUpdates.Clear();
        _entities.Clear();
    }
}