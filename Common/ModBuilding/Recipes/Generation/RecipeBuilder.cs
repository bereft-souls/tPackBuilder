using PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation;

public sealed class RecipeBuilder : PackBuilderType
{
    public required RecipeResult Result { get; set; }
    public List<RecipeIngredient> Ingredients = [];
    public List<RecipeGroupIngredient> Groups = [];
    public List<string> Tiles = [];

    public override string? LoadingMethod => nameof(ModSystem.AddRecipes);

    public override void Load(Mod mod)
    {
        var recipe = NewRecipe(mod);

        Result.AddTo(recipe);

        foreach (var ingredient in Ingredients)
            ingredient.AddTo(recipe);

        foreach (var group in Groups)
            group.AddTo(recipe);

        foreach (var tile in Tiles)
            recipe.AddTile(GetTile(tile));

        recipe.Register();
    }
}
