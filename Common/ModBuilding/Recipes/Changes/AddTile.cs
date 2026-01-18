using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Changes
{
    internal class AddTile : IRecipeChange
    {
        public required string Tile;

        public void ApplyTo(Recipe recipe)
        {
            int tile = GetTile(Tile);
            recipe.AddTile(tile);
        }
    }
}
