using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Conditions
{
    internal class RequiresIngredient : IRecipeCondition
    {
        public required string Item;

        public int Count = -1;

        public bool AppliesTo(Recipe recipe)
        {
            int item = GetItem(Item);

            if (Count == -1)
                return recipe.HasIngredient(item);

            return recipe.TryGetIngredient(item, out var ingredient) && ingredient.stack == Count;
        }
    }
}
