using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameEngine.Components;
using MonoGameEngine.Extensions;

namespace MonoGameEngine;

public sealed class Scene
{
    private const string ON_ACTIVE_METHOD_NAME = "OnEnable";
    private const string ON_DEACTIVATE_METHOD_NAME = "OnDisable";
    private const string ON_UPDATE_METHOD_NAME = "OnUpdate";
    private const string DELTA_TIME_PARAMETER_NAME = "deltaTime";
    private const string ON_DRAW_METHOD_NAME = "OnDraw";
    private const string ON_COLLISION_METHOD_NAME = "OnCollision";
    private const string ON_TRIGGER_METHOD_NAME = "OnTrigger";

    private HashSet<GameEntity> _entities = [];
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

    /// <summary>
    /// Add an entity from the scene
    /// Invokes OnEnable callbacks on entity components
    /// </summary>
    /// <param name="entity"></param>
    public void AddEntity(GameEntity entity)
    {
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

        if (_pendingAdd.Contains(entity))
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

        if (_pendingRemove.Contains(entity))
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
        if (_pendingDisable.Contains(entity))
            return;

        _pendingDisable.Add(entity);
    }

    internal void Update(GameTime gameTime)
    {
        ApplyChanges();
        DoEntityUpdates(gameTime);
        DoCollisions();
    }

    internal void Draw(SpriteBatch spriteBatch)
    {
        ApplyChanges();
        var visibleEntities = _entities.Where(e => e.IsVisible);
        var scalingMatrix = Viewport.GetScaleMatrix();
        spriteBatch.Begin(transformMatrix: scalingMatrix, samplerState: SamplerState.PointClamp);
        foreach (var entity in visibleEntities)
        {
            foreach (var component in entity.Components)
            {
                if (_onDrawMethods.TryGetValue(component, out var callback))
                {
                    callback();
                }
            }

            spriteBatch.Draw(
                entity.Renderer.Texture,
                entity.Transform.Position,
                null,
                entity.Renderer.Color,
                entity.Transform.Rotation,
                entity.Renderer.Pivot,
                entity.Transform.Scale,
                entity.Renderer.Effects,
                0f);
        }
        spriteBatch.End();
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

        if (component.TryGetCallbackFromMethod<Action>(ON_ACTIVE_METHOD_NAME, out var activeMethod))
        {
            _onEnableMethods.Add(component, activeMethod);
            wasCached = true;
        }

        if (component.TryGetCallbackFromMethod<Action>(ON_DEACTIVATE_METHOD_NAME, out var deactivateMethod))
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
                try
                {
                    _activeUpdates.Add(onUpdate);
                }
                catch (System.Exception e)
                {
                    System.Diagnostics.Debug.Write(e.Message);
                    System.Diagnostics.Debug.WriteLine(e.StackTrace);
                }
            }

            if (_onEnableMethods.TryGetValue(component, out var callback))
            {
                callback();
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
                callback();
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
                if (_entities.Add(entity))
                {
                    entity.AttachedScene = this;
                    var components = entity.Components.DeepCopy();
                    RegisterComponentsCallbacks(components);
                }
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
            callback(deltaTime);
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
                                collision(other);
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
                                    trigger(collider);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}