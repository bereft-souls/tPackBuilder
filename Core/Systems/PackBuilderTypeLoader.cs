using PackBuilder.Common.ModBuilding;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PackBuilder.Core.Systems
{
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
                ContentInstance.Register(template);

                PackBuilderType.Extensions.Add(type, template.Extension);
                PackBuilderType.ExtensionsToTypes.Add(template.Extension, template);
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
}
