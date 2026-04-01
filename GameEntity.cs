using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using MonoGameEngine.Components;

namespace MonoGameEngine;

public sealed class GameEntity
{
    public Transformation Transform { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public Collider Collider { get; private set; }
    public AudioPlayer Audio { get; private set; }

    public bool IsActive { get; private set; } = false;
    public bool IsVisible => Renderer != null && Renderer.Texture != null;
    public Scene AttachedScene { get; set; }

    internal List<Component> Components = [];

    public GameEntity(params Component[] components)
    {
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

    //TODO componentID for optimized query
    public void AddComponent(Component component)
    {
        if (component is Transformation)
            return;

        component.Entity = this;
        if (component.IsSingleInstance && Components.Any(c => c.GetType() == component.GetType()))
            return;

        Components.Add(component);
        if (component is SpriteRenderer spriteRenderer)
            Renderer = spriteRenderer;
        if (component is Collider collider)
            Collider = collider;
        if (component is AudioPlayer audioPlayer)
            Audio = audioPlayer;
        AttachedScene?.RegisterComponentCallbacks(component);
    }

    public void RemoveComponent(Component component)
    {
        if (component is Transformation)
            return;

        if (Components.Contains(component))
            Components.Remove(component);

        component.Entity = null;
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
        return Components.FirstOrDefault(c => c is T) as T;
    }

    public bool TryGetComponent<T>(out T component) where T : Component
    {
        component = GetComponent<T>();
        return component != null;
    }
}