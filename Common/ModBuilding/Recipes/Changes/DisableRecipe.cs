using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes
{
    internal class DisableRecipe : IRecipeChange
    {
        public required bool Disabled;

        public void ApplyTo(Recipe recipe) => recipe.SetDisabled(Disabled);
    }
}
