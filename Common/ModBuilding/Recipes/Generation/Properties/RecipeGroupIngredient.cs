using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;

internal class RecipeGroupIngredient
{
    public required string Group;

    public int Count = 1;

    public void AddTo(Recipe recipe) => recipe.AddRecipeGroup(GetRecipeGroup(Group), Count);
}
