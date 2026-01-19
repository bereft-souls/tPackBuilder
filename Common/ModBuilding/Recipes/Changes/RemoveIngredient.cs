using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

public sealed record RemoveIngredient : IRecipeChange
{
    public required string Item;

    public void ApplyTo(Recipe recipe)
    {
        int item = GetItem(Item);
        recipe.RemoveIngredient(item);
    }
}
