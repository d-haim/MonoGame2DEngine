using System;
using System.Text;
using Microsoft.Xna.Framework;

namespace MonoGameEngine.Components;

public sealed class Transformation : Component
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public float Scale { get; set; } = 1.0f;
    public float Rotation { get; set; } = 0.0f;

    public Transformation(GameEntity entity) : base(entity) { }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position, Scale, Rotation);
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Position: {Position}");
        builder.AppendLine($"Scale: {Scale}");
        builder.AppendLine($"Rotation: {Rotation}");
        return builder.ToString();
    }

    public override bool Equals(object obj)
    {
        if (obj is Transformation other)
        {
            return Position == other.Position && Scale == other.Scale && Rotation == other.Rotation;
        }
        return false;
    }

    public static bool operator ==(Transformation left, Transformation right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(Transformation left, Transformation right)
    {
        return !(left == right);
    }
}