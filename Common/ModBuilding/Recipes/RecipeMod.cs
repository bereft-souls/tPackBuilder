using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Recipes;

public sealed class RecipeMod : PackBuilderType
{
    // Either All or Any.
    // If "All" is specified, ALL of the conditions will need to be met in order to activate the changes of this mod.
    // If "Any" is specified, ANY of the conditions being met will activate the changes of this mod.
    public RecipeCriteria Criteria { get; set; } = RecipeCriteria.All;

    // The condition(s) needing to be met in order for this mod to activate.
    public List<IRecipeCondition> Conditions = [];

    // The change(s) that will be applied to each of the recipes where conditions are met.
    public List<IRecipeChange> Changes = [];

    // Run in PostAddRecipes instead of PostSetupContent
    public override string? LoadingMethod => nameof(ModSystem.PostAddRecipes);

    public override void Load()
    {
        if (Conditions.Count == 0)
            throw new NoConditionsException();

        var recipeLoader_CurrentMod = typeof(RecipeLoader).GetProperty("CurrentMod", BindingFlags.Static | BindingFlags.NonPublic)!;

        try
        {
            recipeLoader_CurrentMod.SetValue(null, Mod);

            foreach (var recipe in Main.recipe)
            {
                // Do not apply recipe mods to recipes added by the same mod pack.
                if (recipe.Mod == Mod)
                    continue;

                // 'applies' will be true in any of the following cases:
                //      - There are no specified conditions.
                //      - The specified criteria is "all" and ALL specified conditions are met.
                //      - The specified criteria is "any" and ANY single specified condition is met.
                bool applies = Criteria == RecipeCriteria.Any ?
                    Conditions.Any(c => c.AppliesTo(recipe)) :
                    Conditions.All(c => c.AppliesTo(recipe));

                // If this mod does not apply to a given recipe, move to the next.
                if (!applies)
                    continue;

                // Apply this recipe mod's changes.
                if (Changes.Count == 0)
                    throw new MissingFieldException("Must specify 1 or more changes for a recipe modification!", nameof(Changes));

                foreach (var change in Changes)
                    change.ApplyTo(recipe);
            }
        }
        catch (Exception ex)
        {
            ex.Data["mod"] = Mod.Name;
            throw;
        }
        finally
        {
            recipeLoader_CurrentMod.SetValue(null, null);
        }
    }
}

// Recipe Criteria: Either All or Any.
// If "All" is specified, ALL of the conditions will need to be met in order to activate the changes of this mod.
// If "Any" is specified, ANY of the conditions being met will activate the changes of this mod.
[JsonConverter(typeof(RecipeCriteriaConverter))]
public enum RecipeCriteria
{
    All,
    Any
}

internal sealed class RecipeCriteriaConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is RecipeCriteria criteria)
            writer.WriteValue(Enum.GetName(typeof(RecipeCriteria), criteria));
    }

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer
    )
    {
        var recipeCriteria = typeof(RecipeCriteria);
        var options = Enum.GetValues(recipeCriteria);
        var value = reader.Value?.ToString() ?? Enum.GetName(recipeCriteria, 0);

        foreach (var option in options)
        {
            if (Enum.GetName(recipeCriteria, option)!.Equals(value, StringComparison.OrdinalIgnoreCase))
                return option;
        }

        return null;
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(RecipeCriteria);
    }
}
