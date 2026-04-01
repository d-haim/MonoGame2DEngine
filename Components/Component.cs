namespace MonoGameEngine.Components;

public abstract class Component
{
    public GameEntity Entity { get; internal set; }
    public Transformation Transform => Entity.Transform;
    public SpriteRenderer Renderer => Entity.Renderer;
    public Collider Collider => Entity.Collider;
    public AudioPlayer Audio => Entity.Audio;
    public bool IsSingleInstance { get; internal set; } = true;
}