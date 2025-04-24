using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Conditions
{
    internal interface IRecipeCondition
    {
        public bool AppliesTo(Recipe recipe);
    }
}
