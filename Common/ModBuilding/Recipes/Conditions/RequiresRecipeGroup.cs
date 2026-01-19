using Terraria;

namespace PackBuilder.Common.ModBuilding.Recipes.Conditions;

public sealed record RequiresRecipeGroup : IRecipeCondition
{
    public required string Group;

    public int Count = -1;

    public bool AppliesTo(Recipe recipe)
    {
        int id = GetRecipeGroup(Group);

        if (Count == -1)
            return recipe.HasRecipeGroup(id);

        RecipeGroup group = RecipeGroup.recipeGroups[id];
        return recipe.HasRecipeGroup(id) && recipe.TryGetIngredient(group.IconicItemId, out var ingredient) && ingredient.stack == Count;
    }
}
