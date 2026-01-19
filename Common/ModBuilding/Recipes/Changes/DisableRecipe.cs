using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

public sealed record DisableRecipe : IRecipeChange
{
    public required bool Disabled;

    public void ApplyTo(Recipe recipe) => recipe.SetDisabled(Disabled);
}
