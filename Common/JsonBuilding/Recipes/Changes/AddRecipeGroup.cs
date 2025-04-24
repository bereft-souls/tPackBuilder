using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Changes
{
    internal class AddRecipeGroup : IRecipeChange
    {
        public required string Group;

        public int Count = 1;

        public void ApplyTo(Recipe recipe)
        {
            int id = GetRecipeGroup(Group);
            recipe.AddRecipeGroup(id, Count);
        }
    }
}
