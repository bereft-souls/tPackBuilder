using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Conditions
{
    internal class RequiresTile : IRecipeCondition
    {
        public required string Tile;

        public bool AppliesTo(Recipe recipe)
        {
            int tile = GetTile(Tile);
            return recipe.HasTile(tile);
        }
    }
}
