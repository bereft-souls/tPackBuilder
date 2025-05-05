using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Projectiles;
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
            float width = projectile.width / projectile.scale;
            float height = projectile.height / projectile.scale;

            if (ProjectileModSets?.TryGetValue(projectile.type, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(projectile));

            projectile.width = (int)(width * projectile.scale);
            projectile.height = (int)(height * projectile.scale);
        }
    }

    internal class ProjectileModifier : ModSystem
    {
        // Ensure our "SetDefaults" is applied AFTER all other mods'.
        public static void SetDefaultsILEdit(ILContext il)
        {
            ILCursor cursor = new(il);

            // Move directly after the call to Projectile.SetDefaults_End().
            // This is a sort of cheap get-around that tMod uses to evade a compilation
            // bug on Mac development environments.
            var projectile_SetDefaults_End = typeof(Projectile).GetMethod("SetDefaults_End", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(int)]);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall(projectile_SetDefaults_End)))
                throw new Exception("Unable to locate Projectile_SetDefaults_End in IL edit!");

            // Add a call to PackBuilderProjectile.ApplyChanges() using the projectile
            // that SetDefaults() is being called on.
            var packBuilderProjectile_ApplyChanges = typeof(PackBuilderProjectile).GetMethod("ApplyChanges", BindingFlags.Static | BindingFlags.Public, [typeof(Projectile)]);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, packBuilderProjectile_ApplyChanges);
        }

        public override void PostSetupContent()
        {
            // Collects ALL .projectilemod.json files from all mods into a list.
            List<(string, byte[])> jsonEntries = [];

            // Collects the loaded projectile mods to pass to the set factory initialization.
            Dictionary<int, List<ProjectileChanges>> factorySets = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .projectilemod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".projectilemod.json", System.StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod.GetFileBytes(file)));
            }

            foreach (var (file, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

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

        public override void Load()
        {
            var projectile_SetDefaults = typeof(Projectile).GetMethod("SetDefaults", BindingFlags.Instance | BindingFlags.Public, [typeof(int)]);
            MonoModHooks.Modify(projectile_SetDefaults, SetDefaultsILEdit);
        }
    }
}
