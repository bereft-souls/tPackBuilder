using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes
{
    internal class RemoveIngredient : IRecipeChange
    {
        public required string Item;

        public void ApplyTo(Recipe recipe)
        {
            int item = GetItem(Item);
            recipe.RemoveIngredient(item);
        }
    }
}
