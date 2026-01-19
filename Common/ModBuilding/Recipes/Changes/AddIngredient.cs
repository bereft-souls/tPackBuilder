using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

internal class AddIngredient : IRecipeChange
{
    public required string Item;

    public int Count = 1;

    public void ApplyTo(Recipe recipe)
    {
        int item = GetItem(Item);
        recipe.AddIngredient(item, Count);
    }
}
