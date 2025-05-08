using MonoMod.Cil;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Recipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    [Autoload(false)]
    [LateLoad]
    internal class RecipeModifier : ModSystem
    {
        // Ensure our recipe changes are applied after all other mods'.
        public static void SetupRecipesILEdit(ILContext il)
        {
            ILCursor cursor = new(il);

            var recipeLoader_PostAddRecipes = typeof(RecipeLoader).GetMethod("PostAddRecipes", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, []);

            if (!cursor.TryGotoNext(MoveType.After, c => c.MatchCall(recipeLoader_PostAddRecipes)))
                throw new Exception("Unable to find RecipeLoader_PostAddRecipes call!");

            var modifyRecipes = typeof(RecipeModifier).GetMethod(nameof(ModifyRecipes), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, []);
            cursor.EmitCall(modifyRecipes);
        }

        // Changes all of the loaded recipes based on provided criteria from Json files.
        public static void ModifyRecipes()
        {
            // Collects ALL .recipemod.json files from all mods into a list.
            List<(string, Mod, byte[])> jsonEntries = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .recipemod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".recipemod.json", StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod, mod.GetFileBytes(file)));
            }

            foreach (var (file, mod, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

                // Decode the json into a recipe mod.
                RecipeMod recipeMod = JsonConvert.DeserializeObject<RecipeMod>(rawJson, PackBuilder.JsonSettings)!;

                if (recipeMod.Conditions.Conditions.Count == 0)
                    throw new NoConditionsException();

                // Apply the recipe mod.
                recipeMod.Apply(mod);

                PackBuilder.LoadingFile = null;
            }
        }

        public override void Load()
        {
            //var recipe_SetupRecipes = typeof(Recipe).GetMethod(nameof(Recipe.SetupRecipes), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, []);
            //MonoModHooks.Modify(recipe_SetupRecipes, SetupRecipesILEdit);
        }
    }
}
