using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;

public sealed record RecipeIngredient
{
    public required string Item;

    public int Count = 1;

    public void AddTo(Recipe recipe) => recipe.AddIngredient(GetItem(Item), Count);
}
