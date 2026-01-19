using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes;

public sealed record ChangeTile : IRecipeChange
{
    public string Tile = null!;

    public required string NewTile;

    public void ApplyTo(Recipe recipe)
    {
        int oldTile = Tile == null ? -1 : GetTile(Tile);
        int newTile = GetTile(NewTile);

        if (oldTile == -1)
            recipe.requiredTile.Clear();

        else
            recipe.RemoveTile(oldTile);

        recipe.AddTile(newTile);
    }
}
