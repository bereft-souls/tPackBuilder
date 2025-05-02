using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Projectiles;
using PackBuilder.Core.Utils;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using static PackBuilder.Core.Systems.PackBuilderProjectile;

namespace PackBuilder.Core.Systems
{
    internal class PackBuilderProjectile : GlobalProjectile
    {
        public static FrozenDictionary<int, List<ProjectileChanges>> ProjectileModSets = null;

        public static void ApplyChanges(Projectile projectile)
        {
            if (ProjectileModSets?.TryGetValue(projectile.type, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(projectile));
        }
    }

    internal class ProjectileModifier : ModSystem
    {
        public static void FinalSetDefaults(ProjectileLoader_SetDefaults orig, Projectile entity, bool createModProjectile)
        {
            orig(entity, createModProjectile);
            ApplyChanges(entity);
        }

        public override void PostSetupContent()
        {
            // Collects ALL .projectilemod.json files from all mods into a list.
            Dictionary<string, byte[]> jsonEntries = [];

            // Collects the loaded projectile mods to pass to the set factory initialization.
            Dictionary<int, List<ProjectileChanges>> factorySets = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .projectilemod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".projectilemod.json", System.StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add(file, mod.GetFileBytes(file));
            }

            foreach (var jsonEntry in jsonEntries)
            {
                PackBuilder.LoadingFile = jsonEntry.Key;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(jsonEntry.Value);

                // Decode the json into an projectile mod.
                ProjectileMod projectileMod = JsonConvert.DeserializeObject<ProjectileMod>(rawJson, PackBuilder.JsonSettings)!;

                if (projectileMod.Projectiles.Count == 0)
                    throw new NoProjectilesException();

                // Get the projectile mod ready for factory initialization.
                foreach (string projectile in projectileMod.Projectiles)
                {
                    int projectileType = GetProjectile(projectile);

                    factorySets.TryAdd(projectileType, []);
                    factorySets[projectileType].Add(projectileMod.Changes);
                }

                PackBuilder.LoadingFile = null;
            }

            // Setup the factory for fast access to projectile lookup.
            ProjectileModSets = factorySets.ToFrozenDictionary();
        }

        public delegate void ProjectileLoader_SetDefaults(Projectile projectile, bool createModProjectile = false);
        public static Hook ProjectileLoaderHook = null;

        public override void Load()
        {
            var method = typeof(ProjectileLoader).GetMethod("SetDefaults", BindingFlags.Static | BindingFlags.NonPublic);
            ProjectileLoaderHook = new(method, FinalSetDefaults);
        }
    }
}
