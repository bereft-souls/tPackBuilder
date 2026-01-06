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
    internal class RecipeGroupGenerator : ModSystem
    {
        public override void AddRecipeGroups()
        {
            // Collects ALL .recipegroupbuilder.json files from all mods into a list.
            List<(string, byte[])> jsonEntries = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .recipegroupbuilder.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".recipegroupbuilder.json", StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod.GetFileBytes(file)));
            }

            foreach (var (file, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

                // Decode the json into a recipe group builder.
                RecipeGroupBuilder recipeGroupBuilder = JsonConvert.DeserializeObject<RecipeGroupBuilder>(rawJson, PackBuilder.JsonSettings)!;

                // Apply the recipe builder.
                recipeGroupBuilder.Build();

                PackBuilder.LoadingFile = null;
            }
        }
    }
}
