using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Conditions;

public sealed record RequiresTile : IRecipeCondition
{
    public required string Tile;

    public bool AppliesTo(Recipe recipe)
    {
        int tile = GetTile(Tile);
        return recipe.HasTile(tile);
    }
}
