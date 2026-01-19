using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes;

public interface IRecipeChange
{
    public abstract void ApplyTo(Recipe recipe);
}
