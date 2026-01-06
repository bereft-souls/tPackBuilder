using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Recipes.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    [Autoload(false)]
    [LateLoad]
    internal class RecipeGroupModifier : ModSystem
    {
        public override void PostSetupRecipes()
        {
            // Collects ALL .recipegroupmod.json files from all mods into a list.
            List<(string, byte[])> jsonEntries = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .recipemod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".recipegroupmod.json", StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod.GetFileBytes(file)));
            }

            foreach (var (file, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

                // Decode the json into a recipe group mod.
                RecipeGroupMod recipeGroupMod = JsonConvert.DeserializeObject<RecipeGroupMod>(rawJson, PackBuilder.JsonSettings)!;

                // Apply the recipe mod.
                recipeGroupMod.Apply();

                PackBuilder.LoadingFile = null;
            }
        }
    }
}
