using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes;

public interface IRecipeCondition
{
    public bool AppliesTo(Recipe recipe);
}
