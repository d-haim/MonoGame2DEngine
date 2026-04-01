using System;
using System.Reflection;

namespace MonoGameEngine.Extensions;

public static class MethodInfoExtensions
{
    public static bool VerifyMethodParameters(this MethodInfo method, params (Type parameterType, string parameterName)[] expectedParameters)
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