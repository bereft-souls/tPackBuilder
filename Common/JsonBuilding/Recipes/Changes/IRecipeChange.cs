using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Changes
{
    internal interface IRecipeChange
    {
        public abstract void ApplyTo(Recipe recipe);
    }
}
