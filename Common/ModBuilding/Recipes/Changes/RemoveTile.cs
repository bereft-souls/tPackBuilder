using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

public sealed record RemoveTile : IRecipeChange
{
    public required string Tile;

    public void ApplyTo(Recipe recipe)
    {
        int tile = GetTile(Tile);
        recipe.RemoveTile(tile);
    }
}
