using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Recipes.Generation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    internal class RecipeGenerator : ModSystem
    {
        public override void AddRecipes()
        {
            // Collects ALL .recipebuilder.json files from all mods into a list.
            List<(string, Mod, byte[])> jsonEntries = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .recipebuilder.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".recipebuilder.json", StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod, mod.GetFileBytes(file)));
            }

            foreach (var (file, mod, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

                // Decode the json into a recipe builder.
                RecipeBuilder recipeBuilder = JsonConvert.DeserializeObject<RecipeBuilder>(rawJson, PackBuilder.JsonSettings)!;

                if (recipeBuilder.Result is null)
                    throw new NoResultException();

                // Apply the recipe builder.
                recipeBuilder.Build(mod);

                PackBuilder.LoadingFile = null;
            }
        }
    }
}
