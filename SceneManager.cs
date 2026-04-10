using System;
using System.Collections.Generic;

namespace MonoGameEngine;

public static partial class SceneManager
{
    public static Scene CurrentScene { get; private set; }
    
    private static readonly Dictionary<int, Func<Scene>> _sceneLoaders = new();
    private static readonly Dictionary<string, int> _sceneNames = new();

    public static bool HasScenes => _sceneLoaders.Count > 0 || GetInternalSceneCount() > 0;

    public static void RegisterScene(int index, string name, Func<Scene> loader)
    {
        _sceneLoaders[index] = loader;
        _sceneNames[name] = index;
    }

    public static void LoadScene(int index)
    {
        // 1. Try internal engine scenes first
        Scene scene = LoadSceneInternal(index);
        
        // 2. Try registered game scenes
        if (scene == null && _sceneLoaders.TryGetValue(index, out var loader))
        {
            scene = loader();
        }

        if (scene != null)
        {
            CurrentScene = scene;
        }
        else
        {
            throw new ArgumentException($"Scene index {index} not found.");
        }
    }

    public static void LoadScene(string name)
    {
        // 1. Try internal engine scenes first
        LoadSceneInternal(name);
        if (CurrentScene != null) return;

        // 2. Try registered game scenes
        if (_sceneNames.TryGetValue(name, out var index))
        {
            LoadScene(index);
        }
        else
        {
            throw new ArgumentException($"Scene name '{name}' not found.");
        }
    }

    // These remain for MonoGameEngine's own internal scenes (if any)
    private static partial Scene LoadSceneInternal(int index);
    private static partial void LoadSceneInternal(string name);
    private static partial int GetInternalSceneCount();
}
