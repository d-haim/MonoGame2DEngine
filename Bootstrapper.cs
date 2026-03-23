using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace MonoGameEngine;

public static partial class Bootstrapper
{
    public static void RegisterInit(Action method)
    {
        _initMethods.Add(method);
    }

    public static void RegisterContentLoad(Action<ContentManager> method)
    {
        _contentMethods.Add(method);
    }

    private static List<Action> _initMethods = new();
    private static List<Action<ContentManager>> _contentMethods = new();

    public static void InvokeInitializationMethods()
    {
        RunGeneratedInit();
        foreach (var method in _initMethods)
        {
            method();
        }
    }

    public static void InvokeContentLoadingMethods(ContentManager content)
    {
        RunGeneratedContentLoad(content);
        foreach (var method in _contentMethods)
        {
            method(content);
        }
    }

    static partial void RunGeneratedInit();
    static partial void RunGeneratedContentLoad(ContentManager content);
}