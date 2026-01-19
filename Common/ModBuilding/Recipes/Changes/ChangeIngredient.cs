using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

internal class ChangeIngredient : IRecipeChange
{
    public required string Item;

    public string NewItem = null;

    public int NewCount = -1;

    public void ApplyTo(Recipe recipe)
    {
        int item = GetItem(Item);

        if (!recipe.TryGetIngredient(item, out var ingredient))
            return;

        int stack = NewCount == -1 ? ingredient.stack : NewCount;
        int newItem = NewItem is null ? item : GetItem(NewItem);

        recipe.RemoveIngredient(ingredient);
        recipe.AddIngredient(newItem, stack);
    }
}
