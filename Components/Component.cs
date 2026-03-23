using System.Runtime.CompilerServices;

namespace MonoGameEngine.Components;

public abstract class Component
{
    public GameEntity Entity { get; internal set; }
    public bool IsSingleInstance { get; } = true;

    public Component(GameEntity entity)
    {
        Entity = entity;
    }
}