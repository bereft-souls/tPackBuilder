using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes
{
    internal interface IRecipeChange
    {
        public abstract void ApplyTo(Recipe recipe);
    }
}
