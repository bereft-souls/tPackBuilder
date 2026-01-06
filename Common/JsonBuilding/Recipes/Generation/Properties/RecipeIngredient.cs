using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Generation.Properties
{
    internal class RecipeIngredient
    {
        public required string Item;

        public int Count = 1;

        public void AddTo(Recipe recipe) => recipe.AddIngredient(GetItem(Item), Count);
    }
}
