using System;
using System.Collections.Generic;
using System.Reflection;
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
    private List<GameEntity> _pendingAdd = [];
    private List<GameEntity> _pendingRemove = [];
    private Dictionary<GameEntity, Action> _onActiveMethods = [];
    private Dictionary<GameEntity, Action> _onDeactivateMethods = [];
    private Dictionary<GameEntity, Action<float>> _onUpdateMethods = [];
    private Dictionary<GameEntity, Action> _onDrawMethods = [];
    private Dictionary<GameEntity, Action<GameEntity>> _onCollisionMethods = [];
    private Dictionary<GameEntity, Action<GameEntity>> _onTriggerMethods = [];
    private List<Action<float>> _activeUpdates = new();
    private List<Collider> _activeColliders = new();

    public void AddEntity(GameEntity entity)
    {
        _pendingAdd.Add(entity);
    }

    public void RemoveEntity(GameEntity entity)
    {
        _pendingRemove.Add(entity);
    }

    public void EnableEntity(GameEntity entity)
    {
        if (entity.IsActive)
            return;

        entity.SetActive(true);
        if (_onUpdateMethods.TryGetValue(entity, out var onUpdate))
        {
            _activeUpdates.Add(onUpdate);
        }

        if (entity.Collider != null)
        {
            _activeColliders.Add(entity.Collider);
        }

        if (_onActiveMethods.TryGetValue(entity, out var callback))
        {
            callback();
        }
    }

    public void DisableEntity(GameEntity entity)
    {
        if (entity.IsActive == false)
            return;

        entity.SetActive(false);
        if (_onUpdateMethods.TryGetValue(entity, out var onUpdate))
        {
            _activeUpdates.Remove(onUpdate);
        }

        if (entity.Collider != null)
        {
            _activeColliders.Remove(entity.Collider);
        }

        if (_onDeactivateMethods.TryGetValue(entity, out var callback))
        {
            callback();
        }
    }

    private void ApplyChanges()
    {
        if (_pendingRemove.Count > 0)
        {
            foreach (var entity in _pendingRemove)
            {
                DisableEntity(entity);
                entity.AttachedScene = null;
                _entities.Remove(entity);
                _onActiveMethods.Remove(entity);
                _onDeactivateMethods.Remove(entity);
                _onUpdateMethods.Remove(entity);
                _onDrawMethods.Remove(entity);
                _onCollisionMethods.Remove(entity);
                _onTriggerMethods.Remove(entity);
            }
            _pendingRemove.Clear();
        }

        if (_pendingAdd.Count > 0)
        {
            foreach (var entity in _pendingAdd)
            {
                if (_entities.Add(entity))
                {
                    entity.AttachedScene = this;

                    if (TryGetMethodFromEntity<Action>(entity, ON_ACTIVE_METHOD_NAME, out var activeMethod))
                    {
                        _onActiveMethods.Add(entity, activeMethod);
                    }

                    if (TryGetMethodFromEntity<Action>(entity, ON_DEACTIVATE_METHOD_NAME, out var deactivateMethod))
                    {
                        _onDeactivateMethods.Add(entity, deactivateMethod);
                    }

                    if (TryGetMethodFromEntity<Action<float>>(entity, ON_UPDATE_METHOD_NAME, out var updateMethod,
                        m => VerifyMethodParameters(m, (typeof(float), DELTA_TIME_PARAMETER_NAME))))
                    {
                        _onUpdateMethods.Add(entity, updateMethod);
                    }

                    if (entity.Renderer != null)
                    {
                        if (TryGetMethodFromEntity<Action>(entity, ON_DRAW_METHOD_NAME, out var drawMethod))
                        {
                            _onDrawMethods.Add(entity, drawMethod);
                        }
                    }

                    if (entity.Collider != null)
                    {
                        if (TryGetMethodFromEntity<Action<GameEntity>>(entity, ON_COLLISION_METHOD_NAME, out var onCollisionMethod,
                            m => VerifyMethodParameters(m, (typeof(GameEntity), "other"))))
                        {
                            _onCollisionMethods.Add(entity, onCollisionMethod);
                        }

                        if (TryGetMethodFromEntity<Action<GameEntity>>(entity, ON_TRIGGER_METHOD_NAME, out var onTriggerMethod,
                            m => VerifyMethodParameters(m, (typeof(GameEntity), "other"))))
                        {
                            _onTriggerMethods.Add(entity, onTriggerMethod);
                        }
                    }
                    EnableEntity(entity);
                }
            }
            _pendingAdd.Clear();
        }
    }

    public void Update(GameTime gameTime)
    {
        ApplyChanges();
        DoEntityUpdates(gameTime);
        DoCollisions();
    }

    private void DoCollisions()
    {
        foreach (var collider in _activeColliders)
        {
            if (collider.IsTrigger == false)
            {
                foreach (var other in _activeColliders)
                {
                    if (collider.Intersects(other))
                    {
                        if (_onCollisionMethods.TryGetValue(collider.Entity, out var collision))
                        {
                            collision(other.Entity);
                        }

                        if (other.IsTrigger)
                        {
                            if (_onTriggerMethods.TryGetValue(other.Entity, out var trigger))
                            {
                                trigger(collider.Entity);
                            }
                        }
                    }
                }
            }
        }
    }

    private void DoEntityUpdates(GameTime gameTime)
    {
        float deltaTime = gameTime.DeltaTime();
        foreach (var callback in _activeUpdates)
        {
            callback(deltaTime);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        ApplyChanges();
        var scalingMatrix = Viewport.GetScaleMatrix(spriteBatch.GraphicsDevice);
        spriteBatch.Begin(transformMatrix: scalingMatrix, samplerState: SamplerState.PointClamp);
        foreach (var entity in _entities)
        {
            if (entity.IsVisible == false)
                continue;

            if (_onDrawMethods.TryGetValue(entity, out var callback))
            {
                callback();
            }

            if (entity.Renderer != null && entity.Renderer.Texture != null)
            {
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
        }
        spriteBatch.End();
    }

    private static bool TryGetMethodFromEntity<T>(
        GameEntity entity,
        string methodName,
        out T callback,
        Predicate<MethodInfo> verifyMethod = null) where T : Delegate
    {
        callback = default;
        var method = entity.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        bool hasMethod = method != null;
        if (hasMethod && (verifyMethod == null || verifyMethod(method)))
        {
            callback = (T)Delegate.CreateDelegate(typeof(T), entity, method);
            return true;
        }
        return false;
    }

    private static bool VerifyMethodParameters(MethodInfo method, params (Type parameterType, string parameterName)[] expectedParameters)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != expectedParameters.Length)
            return false;

        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var expectedProperty = expectedParameters[i];
            if (parameter.ParameterType != expectedProperty.parameterType)
                return false;
            if (parameter.Name != expectedProperty.parameterName)
                return false;
        }
        return true;
    }
}