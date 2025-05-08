using MonoMod.Cil;
using System;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace PackBuilder.Core.Systems
{
    /// <summary>
    /// Indicates that this <see cref="ModType"/> should be loaded AFTER all other ModTypes.<br/>
    /// This means that hooks for a type tagged with this attribute will run after all other hooks of the same type from other mods.<br/>
    /// <br/>
    /// IMPORTANT:<br/>
    /// Types with this attribute should also be tagged with <see cref="AutoloadAttribute"/> as <see langword="false"/>. Otherwise they will be loaded early and again late.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class LateLoadAttribute : Attribute
    { }

    internal class ModSorter : ModSystem
    {
        public static bool LateTypesLoaded = false;

        public override void Load()
        {
            var modContent_ResizeArrays = typeof(ModContent).GetMethod("ResizeArrays", BindingFlags.Static | BindingFlags.NonPublic, [typeof(bool)]);
            MonoModHooks.Add(modContent_ResizeArrays, SortModsThenResize);
            LateTypesLoaded = false;
        }

        public void SortModsThenResize(Action<bool> orig, bool unloading = false)
        {
            if (!LateTypesLoaded)
            {
                var modLoader_Mods = typeof(ModLoader).GetProperty("Mods", BindingFlags.Public | BindingFlags.Static);

                // Remove our mod from ModLoader.Mods and re-add it.
                // This effectively shifts it to the end of the list.
                var sortedMods = ModLoader.Mods.ToList();
                sortedMods.Remove(Mod);
                sortedMods.Add(Mod);

                // Re-assign ModLoader.Mods to match our new sort value.
                modLoader_Mods.SetValue(null, sortedMods.ToArray());

                // Re-set our mod as "loading."
                // This allows us to load our types tagged with LateLoadAttribute even after mod
                // loading has techinically "finished", which ensure all hooks from these types are run last.
                var mod_Loading = typeof(Mod).GetField("loading", BindingFlags.Instance | BindingFlags.NonPublic);
                mod_Loading.SetValue(Mod, true);

                // Load all the fetched types.
                var loadableTypes = AssemblyManager.GetLoadableTypes(Mod.Code)
                    .Where(t => !t.IsAbstract && !t.ContainsGenericParameters)
                    .Where(t => t.IsAssignableTo(typeof(ILoadable)))
                    .Where(t => t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null) // has default constructor
                    .Where(t => Attribute.GetCustomAttribute(t, typeof(LateLoadAttribute)) is not null) // LateLoad attribute
                    .OrderBy(type => type.FullName, StringComparer.InvariantCulture);

                LoaderUtils.ForEachAndAggregateExceptions(loadableTypes, t => Mod.AddContent((ILoadable)Activator.CreateInstance(t, true)));

                // Make sure our "loading" is re-assigned properly.
                mod_Loading.SetValue(Mod, false);
                LateTypesLoaded = true;
            }

            orig(unloading);
        }
    }
}
