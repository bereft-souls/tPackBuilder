using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Conditions
{
    internal interface IRecipeCondition
    {
        public bool AppliesTo(Recipe recipe);
    }
}
