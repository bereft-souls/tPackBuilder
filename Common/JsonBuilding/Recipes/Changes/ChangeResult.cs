using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Changes
{
    internal class ChangeResult : IRecipeChange
    {
        public string Item = null;

        public int Count = -1;

        public void ApplyTo(Recipe recipe)
        {
            int stack = Count == -1 ? recipe.createItem.stack : Count;
            int newItem = Item is null ? recipe.createItem.type : GetItem(Item);

            recipe.ReplaceResult(newItem, stack);
        }
    }
}
