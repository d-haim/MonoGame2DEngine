using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MonoGameEngine.Generators;

[Generator]
public class SceneGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Get the assembly name
        var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName);

        // 2. Get all .yaml files from AdditionalFiles
        var yamlFiles = context.AdditionalTextsProvider
            .Where(file => file.Path.EndsWith(".yaml"));

        // 3. Generate the SceneManager class
        var combined = yamlFiles.Collect().Combine(assemblyNameProvider);

        context.RegisterSourceOutput(combined, (spc, source) =>
        {
            var files = source.Left;
            var assemblyName = source.Right;
            if (assemblyName == null) return;

            var allScenes = new List<SceneData>();
            var scenesFile = files.FirstOrDefault(f => Path.GetFileName(f.Path) == "Scenes.yaml");
            
            if (scenesFile != null)
            {
                var scenesContent = scenesFile.GetText()?.ToString();
                if (!string.IsNullOrEmpty(scenesContent))
                {
                    var sceneConfigs = ParseScenesYaml(scenesContent);

                    foreach (var config in sceneConfigs)
                    {
                        // Normalize path for comparison
                        string targetPath = config.Path.Replace("/", Path.DirectorySeparatorChar.ToString());
                        var sceneFile = files.FirstOrDefault(f => f.Path.EndsWith(targetPath, StringComparison.OrdinalIgnoreCase));

                        if (sceneFile != null)
                        {
                            var sceneContent = sceneFile.GetText()?.ToString();
                            if (!string.IsNullOrEmpty(sceneContent))
                            {
                                var sceneData = ParseSceneData(sceneContent, config.Name, config.Index);
                                allScenes.Add(sceneData);
                            }
                        }
                    }
                }
            }

            GenerateSceneManager(spc, assemblyName, allScenes);
        });
    }

    private List<(string Name, string Path, int Index)> ParseScenesYaml(string content)
    {
        var result = new List<(string Name, string Path, int Index)>();
        var matches = Regex.Matches(content, @"- Name:\s*""([^""]+)""\s+Path:\s*""([^""]+)""\s+Index:\s*(\d+)");
        foreach (Match match in matches)
        {
            result.Add((match.Groups[1].Value, match.Groups[2].Value, int.Parse(match.Groups[3].Value)));
        }
        return result;
    }

    private SceneData ParseSceneData(string content, string name, int index)
    {
        var data = new SceneData { Name = name, Index = index };

        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        EntityData currentEntity = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("- Type:"))
            {
                currentEntity = new EntityData { Type = trimmed.Substring("- Type:".Length).Trim().Trim('"') };
                data.Entities.Add(currentEntity);
            }
            else if (currentEntity != null)
            {
                if (trimmed.StartsWith("Texture:"))
                {
                    currentEntity.Texture = trimmed.Substring("Texture:".Length).Trim().Trim('"');
                }
                else if (trimmed.StartsWith("Position:"))
                {
                    var match = Regex.Match(trimmed, @"X:\s*([\d\.-]+),\s*Y:\s*([\d\.-]+)");
                    if (match.Success)
                    {
                        currentEntity.X = float.Parse(match.Groups[1].Value);
                        currentEntity.Y = float.Parse(match.Groups[2].Value);
                        currentEntity.HasPosition = true;
                    }
                }
                else if (trimmed.StartsWith("Scale:"))
                {
                    if (float.TryParse(trimmed.Substring("Scale:".Length).Trim(), out float val))
                    {
                        currentEntity.Scale = val;
                        currentEntity.HasScale = true;
                    }
                }
                else if (trimmed.StartsWith("Rotation:"))
                {
                    if (float.TryParse(trimmed.Substring("Rotation:".Length).Trim(), out float val))
                    {
                        currentEntity.Rotation = val;
                        currentEntity.HasRotation = true;
                    }
                }
            }
        }
        return data;
    }

    private void GenerateSceneManager(SourceProductionContext context, string assemblyName, List<SceneData> scenes)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// This file is automatically generated");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using Microsoft.Xna.Framework;");
        sb.AppendLine("using MonoGameEngine;");
        sb.AppendLine("using MonoGameEngine.Components;");
        sb.AppendLine();

        if (assemblyName == "MonoGameEngine")
        {
            sb.AppendLine("namespace MonoGameEngine");
            sb.AppendLine("{");
            sb.AppendLine("    public static partial class SceneManager");
            sb.AppendLine("    {");
            sb.AppendLine("        private static partial Scene LoadSceneInternal(int index)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch(index)");
            sb.AppendLine("            {");
            foreach (var scene in scenes)
            {
                sb.AppendLine($"                case {scene.Index}: return Load{scene.Name}();");
            }
            sb.AppendLine("                default: return null;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static partial void LoadSceneInternal(string name)");
            sb.AppendLine("        {");
            sb.AppendLine("            switch(name)");
            sb.AppendLine("            {");
            foreach (var scene in scenes)
            {
                sb.AppendLine($"                case \"{scene.Name}\": LoadScene({scene.Index}); break;");
            }
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            GenerateSceneLoadMethods(sb, scenes);
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }
        else
        {
            sb.AppendLine($"namespace {assemblyName}.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    internal static class " + assemblyName.Replace(".", "_") + "_SceneInitializer");
            sb.AppendLine("    {");
            sb.AppendLine("        [ModuleInitializer]");
            sb.AppendLine("        public static void Initialize()");
            sb.AppendLine("        {");
            foreach (var scene in scenes)
            {
                sb.AppendLine($"            SceneManager.RegisterScene({scene.Index}, \"{scene.Name}\", Load{scene.Name});");
            }
            sb.AppendLine("        }");
            sb.AppendLine();
            GenerateSceneLoadMethods(sb, scenes);
            sb.AppendLine("    }");
            sb.AppendLine("}");
        }

        context.AddSource(assemblyName + ".SceneManager.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private void GenerateSceneLoadMethods(StringBuilder sb, List<SceneData> scenes)
    {
        foreach (var scene in scenes)
        {
            sb.AppendLine($"    private static Scene Load{scene.Name}()");
            sb.AppendLine("    {");
            sb.AppendLine("        var scene = new Scene();");
            for (int i = 0; i < scene.Entities.Count; i++)
            {
                var entity = scene.Entities[i];
                sb.AppendLine($"        var entity_{i} = new {entity.Type}();");
                if (!string.IsNullOrEmpty(entity.Texture))
                {
                    sb.AppendLine($"        entity_{i}.Renderer = new SpriteRenderer(entity_{i}) {{ Texture = {entity.Texture} }};");
                }
                if (entity.HasPosition)
                {
                    sb.AppendLine($"        entity_{i}.Transform.Position = new Vector2({entity.X}f, {entity.Y}f);");
                }
                if (entity.HasScale)
                {
                    sb.AppendLine($"        entity_{i}.Transform.Scale = {entity.Scale}f;");
                }
                if (entity.HasRotation)
                {
                    sb.AppendLine($"        entity_{i}.Transform.Rotation = {entity.Rotation}f;");
                }
                sb.AppendLine($"        scene.AddEntity(entity_{i});");
            }
            sb.AppendLine("        return scene;");
            sb.AppendLine("    }");
        }
    }

    private class SceneData
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public List<EntityData> Entities { get; set; } = new();
    }

    private class EntityData
    {
        public string Type { get; set; }
        public string Texture { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public bool HasPosition { get; set; }
        public float Scale { get; set; }
        public bool HasScale { get; set; }
        public float Rotation { get; set; }
        public bool HasRotation { get; set; }
    }
}
