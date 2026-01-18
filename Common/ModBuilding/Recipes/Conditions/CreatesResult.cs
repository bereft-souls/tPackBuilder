using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Conditions
{
    internal class CreatesResult : IRecipeCondition
    {
        public required string Item;

        public int Count = -1;

        public bool AppliesTo(Recipe recipe)
        {
            int item = GetItem(Item);

            if (Count == -1)
                return recipe.HasResult(item);

            return recipe.TryGetResult(item, out var ingredient) && ingredient.stack == Count;
        }
    }
}
