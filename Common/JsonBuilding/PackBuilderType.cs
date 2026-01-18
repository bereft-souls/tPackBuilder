using Newtonsoft.Json;
using PackBuilder.Core.Systems;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PackBuilder.Common.JsonBuilding;

internal sealed class PackBuilderTypeSetup : ModSystem
{
    private static readonly Dictionary<string, List<Action>> loadingMethods = [];

    public override void Load()
    {
        Type packBuilderTypeType = typeof(PackBuilderType);

        foreach (var type in ModLoader.Mods.SelectMany(m => AssemblyManager.GetLoadableTypes(m.Code)))
        {
            if (!type.IsSubclassOf(packBuilderTypeType) || type.IsAbstract)
                continue;

            var template = (Activator.CreateInstance(type) as PackBuilderType)!;

            PackBuilderType.Extensions.Add(type, template.Extension);
            string? loadingMethod = template.LoadingMethod;

            if (loadingMethod is null)
                continue;

            MethodInfo loadCall = typeof(PackBuilderType).GetMethod(nameof(PackBuilderType.LoadAll), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(type);
            loadingMethods.TryAdd(loadingMethod, []);
            loadingMethods[loadingMethod].Add(() => loadCall.Invoke(null, null));
        }

        PackBuilderTypeLoader.LoadMethods = loadingMethods.Select(kvp => new KeyValuePair<string, Action[]>(kvp.Key, kvp.Value.ToArray())).ToFrozenDictionary();
        loadingMethods.Clear();
    }
}

[Autoload(false)]
[LateLoad]
internal sealed class PackBuilderTypeLoader : ModSystem
{
    public static FrozenDictionary<string, Action[]> LoadMethods = null!;

    private static void Iterate(string methodName)
    {
        if (LoadMethods.TryGetValue(methodName, out Action[]? loadCalls))
        {
            foreach (Action loadCall in loadCalls)
                loadCall.Invoke();
        }
    }

    public override void Load() => Iterate(nameof(Load));
    public override void AddRecipeGroups() => Iterate(nameof(AddRecipeGroups));
    public override void AddRecipes() => Iterate(nameof(AddRecipes));
    public override void OnLocalizationsLoaded() => Iterate(nameof(OnLocalizationsLoaded));
    public override void OnModLoad() => Iterate(nameof(OnModLoad));
    public override void PostAddRecipes() => Iterate(nameof(PostAddRecipes));
    public override void PostSetupContent() => Iterate(nameof(PostSetupContent));
    public override void PostSetupRecipes() => Iterate(nameof(PostSetupRecipes));
    protected override void Register() { Iterate(nameof(Register)); base.Register(); }
    public override void ResizeArrays() => Iterate(nameof(ResizeArrays));
    public override void SetStaticDefaults() => Iterate(nameof(SetStaticDefaults));
    public override void SetupContent() { Iterate(nameof(SetupContent)); base.SetupContent(); }
}

public abstract class PackBuilderType
{
    public static Dictionary<Type, string> Extensions { get; } = [];

    /// <summary>
    /// Usually called during <see cref="ModSystem.PostSetupContent"/>. Allows you to handle setup tasks for this <see cref="PackBuilderType"/> like registering changes.<br/>
    /// You can override when this is called by overriding <see cref="LoadingMethod"/>.
    /// </summary>
    /// <param name="mod">The <see cref="Mod"/> that contains this <see cref="PackBuilderType"/> object.</param>
    public abstract void Load(Mod mod);

    /// <summary>
    /// Changes the <see cref="ModSystem"/> method in which loading is called.<br/>
    /// Default is <see cref="ModSystem.PostSetupContent"/>.<br/>
    /// <br/>
    /// Use <see langword="nameof"/> to specify the method you want to target. You can override to <see langword="null"/> to disable default loading.<br/>
    /// You can ONLY specify methods that are called during mod setup.
    /// </summary>
    public virtual string? LoadingMethod => nameof(ModSystem.PostSetupContent);

    /// <summary>
    /// Changes the extension used to fetch files for this type.<br/>
    /// Files are searched with <c>.{Extension}.json</c>.<br/>
    /// <br/>
    /// Default is <c>this.GetType().Name.ToLower()</c>
    /// </summary>
    public virtual string Extension => this.GetType().Name.ToLower();

    /// <summary>
    /// Represents a tPackBuilder-targetted file entry from a mod. Contains the mod that owns the file, the file path, and the deserialized object.
    /// </summary>
    public sealed record class FileEntry<T>(Mod Mod, string File, T Value) where T : PackBuilderType;

    /// <summary>
    /// Finds and deserializes all tPackBuilder-mods of the specified type across all loaded mods.<br/>
    /// <br/>
    /// Does <b>NOT</b> call <see cref="PackBuilderType.Load(Mod)"/> automatically.<br/>
    /// If you want loading logic to be called, use <see cref="LoadAll{T}"/> instead.
    /// </summary>
    public static List<FileEntry<T>> FindAll<T>() where T : PackBuilderType
    {
        List<FileEntry<T>> result = [];
        var extension = Extensions[typeof(T)];

        foreach (Mod mod in ModLoader.Mods)
        {
            // An array of all .{PackBuilderType}.json files from this specific mod.
            var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith($".{extension}.json", StringComparison.OrdinalIgnoreCase));

            // Adds the contents of each file to the list.
            foreach (var file in files)
            {
                PackBuilder.LoadingFile = file;

                string rawJson = Encoding.UTF8.GetString(mod.GetFileBytes(file));
                T packBuilderMod = JsonConvert.DeserializeObject<T>(rawJson, PackBuilder.JsonSettings)!;

                result.Add(new(mod, file, packBuilderMod));

                PackBuilder.LoadingFile = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Finds, deserializes, and loads all tPackBuilder-mods of the specified type across all loaded mods.
    /// </summary>
    public static void LoadAll<T>() where T : PackBuilderType
    {
        var packBuilderMods = FindAll<T>();

        foreach (var packBuilderMod in packBuilderMods)
        {
            PackBuilder.LoadingFile = packBuilderMod.File;
            packBuilderMod.Value.Load(packBuilderMod.Mod);
            PackBuilder.LoadingFile = null;
        }
    }
}
