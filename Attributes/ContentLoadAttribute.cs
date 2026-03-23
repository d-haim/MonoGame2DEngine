using System;
using Microsoft.Xna.Framework.Content;

namespace MonoGameEngine.Attributes;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ContentLoadAttribute<T> : Attribute where T : ContentManager
{
}