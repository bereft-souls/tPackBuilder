using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;

public sealed record RecipeGroupIngredient
{
    public required string Group;

    public int Count = 1;

    public void AddTo(Recipe recipe) => recipe.AddRecipeGroup(GetRecipeGroup(Group), Count);
}
