using System;
using System.Reflection;

namespace MonoGameEngine.Extensions;

public static class ObjectExtensions
{
    public static bool TryGetCallbackFromMethod<T>(
        this object target,
        string methodName,
        out T callback,
        Predicate<MethodInfo> verifyMethod = null) where T : Delegate
    {
        callback = default;
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        bool hasMethod = method != null;
        if (hasMethod && (verifyMethod == null || verifyMethod(method)))
        {
            callback = (T)Delegate.CreateDelegate(typeof(T), target, method);
            return true;
        }
        return false;
    }
}
