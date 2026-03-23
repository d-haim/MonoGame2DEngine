using System;

namespace MonoGameEngine.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnInitializeAttribute : Attribute
{
    public int Priority { get; }

    public OnInitializeAttribute(int priority = 0)
    {
        Priority = priority;
    }
}
