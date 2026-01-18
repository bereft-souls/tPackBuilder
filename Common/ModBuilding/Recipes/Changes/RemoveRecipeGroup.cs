using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes
{
    internal class RemoveRecipeGroup : IRecipeChange
    {
        public required string Group;

        public void ApplyTo(Recipe recipe)
        {
            int id = GetRecipeGroup(Group);
            RecipeGroup group = RecipeGroup.recipeGroups[id];

            recipe.RemoveRecipeGroup(id);
            recipe.RemoveIngredient(group.IconicItemId);
        }
    }
}
