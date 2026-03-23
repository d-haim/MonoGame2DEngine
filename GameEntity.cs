using System.Collections.Generic;
using System.Linq;
using MonoGameEngine.Components;

namespace MonoGameEngine;

public abstract class GameEntity
{
    public Transformation Transform { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public Collider Collider { get; private set; }
    public bool IsActive { get; private set; } = false;
    public bool IsVisible => Renderer != null && Renderer.Texture != null;
    public Scene AttachedScene { get; set; }

    private HashSet<Component> _components = new();

    protected GameEntity(params Component[] components)
    {
        IsActive = false;
        Transform = new Transformation(this);

        foreach (var component in components)
        {
            if (component is Transformation)
                continue;
            component.Entity = this;
            AddComponent(component);
            if (component is SpriteRenderer spriteRenderer)
                Renderer = spriteRenderer;
            if (component is Collider collider)
                Collider = collider;
        }
    }

    public void SetActive(bool active)
    {
        if (IsActive != active)
        {
            IsActive = active;
            if (active)
                AttachedScene.EnableEntity(this);
            else
                AttachedScene.DisableEntity(this);
        }
    }

    //TODO componentID for optimized query
    public void AddComponent<T>(T component) where T : Component
    {
        if (component.IsSingleInstance && _components.Any(c => c is T))
            return;

        _components.Add(component);
    }

    public void RemoveComponent<T>(T component) where T : Component
    {
        if (_components.Contains(component))
            _components.Remove(component);
    }

    public T GetComponent<T>() where T : Component
    {
        return _components.FirstOrDefault(c => c is T) as T;
    }
}