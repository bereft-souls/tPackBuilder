using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

public sealed record AddTile : IRecipeChange
{
    public required string Tile;

    public void ApplyTo(Recipe recipe)
    {
        int tile = GetTile(Tile);
        recipe.AddTile(tile);
    }
}
