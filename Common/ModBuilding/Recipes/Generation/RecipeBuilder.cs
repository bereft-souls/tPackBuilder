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

    /// <summary>
    /// Call this to manually register a <see cref="RecipeBuilder"/>.
    /// </summary>
    /// <param name="mod">Your mod instance</param>
    public void Register(Mod mod) => Load(mod);
}
