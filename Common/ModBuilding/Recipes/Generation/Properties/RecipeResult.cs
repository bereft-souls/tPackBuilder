using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Generation.Properties;

public sealed record RecipeResult
{
    public required string Item;

    public int Count = 1;

    public void AddTo(Recipe recipe)
    {
        recipe.createItem.SetDefaults(GetItem(Item), false);
        recipe.createItem.stack = Count;
    }
}
