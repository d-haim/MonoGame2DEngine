using System;
using System.Collections.Generic;
using MonoGameEngine.Components;

namespace MonoGameEngine;

public sealed class GameEntity
{
    public string Name { get; set; }
    public Transformation Transform { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public Collider Collider { get; private set; }
    public AudioPlayer Audio { get; private set; }

    public bool IsActive { get; private set; } = false;
    public bool IsVisible => Renderer != null && Renderer.Texture != null;
    public Scene AttachedScene { get; set; }

    private Dictionary<Type, Component> _components = [];
    private HashSet<Component> _cachedComponents = [];
    internal IReadOnlyCollection<Component> Components => _cachedComponents;

    public GameEntity(string name = "new GameEntity", params Type[] components)
    {
        Name = name;
        IsActive = false;
        Transform = new Transformation() { Entity = this };

        foreach (var component in components)
        {
            AddComponent(component);
        }
    }

    public void SetActive(bool active, object sender = null)
    {
        if (IsActive != active)
        {
            IsActive = active;
            if (AttachedScene != null)
            {
                if (sender != null && sender is Scene scene && scene == AttachedScene)
                {
                    return;
                }

                if (active)
                    AttachedScene.EnableEntity(this);
                else
                    AttachedScene.DisableEntity(this);
            }
        }
    }

    public void AddComponent<TComponent>() where TComponent : Component
    {
        AddComponent(typeof(TComponent));
    }

    public void AddComponent(Type componentType)
    {
        if (!typeof(Component).IsAssignableFrom(componentType))
            return;


        if (_components.TryGetValue(componentType, out _))
        {
            GameEngine.Logger.Log("Component ${component.GetType().Name} already exists on entity ${Name}.", Loggers.ILogger.LogLevel.Error);
            return;
        }

        var ctr = componentType.GetConstructor(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic, null, [typeof(GameEntity)], null);
        if (ctr == null)
        {
            GameEngine.Logger.Log("Failed to get component constructor for ${componentType.Name}.", Loggers.ILogger.LogLevel.Error);
            return;
        }

        Component component = (Component)ctr.Invoke([this]);
        if (component == null)
        {
            GameEngine.Logger.Log("Failed to instantiate component ${componentType.Name}.", Loggers.ILogger.LogLevel.Error);
            return;
        }

        component.Entity = this;
        if (component is Transformation)
            return;

        _components.Add(componentType, component);
        _cachedComponents.Add(component);

        if (component is SpriteRenderer spriteRenderer)
            Renderer = spriteRenderer;
        if (component is Collider collider)
            Collider = collider;
        if (component is AudioPlayer audioPlayer)
            Audio = audioPlayer;
        AttachedScene?.RegisterComponentCallbacks(component);
    }

    public void RemoveComponent<TComponent>() where TComponent : Component
    {
        RemoveComponent(typeof(TComponent));
    }

    public void RemoveComponent(Type componentType)
    {
        if (!typeof(Component).IsAssignableFrom(componentType))
        {
            GameEngine.Logger.Log("${componentType.Name} is not a component.", Loggers.ILogger.LogLevel.Warning);
            return;
        }

        if (!_components.TryGetValue(componentType, out var component))
        {
            GameEngine.Logger.Log("Component ${componentType.Name} does not exist on entity ${Name}.", Loggers.ILogger.LogLevel.Warning);
            return;
        }

        if (component is Transformation)
            return;

        component.Entity = null;
        _components.Remove(componentType);
        _cachedComponents.Remove(component);

        if (component is SpriteRenderer spriteRenderer)
            Renderer = null;
        if (component is Collider collider)
            Collider = null;
        if (component is AudioPlayer audioPlayer)
            Audio = null;
        AttachedScene?.UnregisterComponentCallbacks(component);
    }

    public T GetComponent<T>() where T : Component
    {
        if (_components.TryGetValue(typeof(T), out var component))
            return component as T;
        return null;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        component = GetComponent<T>();
        return component != null;
    }
}