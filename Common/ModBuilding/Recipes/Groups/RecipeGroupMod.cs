using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

internal class RecipeGroupMod : PackBuilderType
{
    public List<string> Groups = [];
    public List<string> AddItems = [];
    public List<string> RemoveItems = [];

    public required string Group { set => Groups.Add(value); }

    public string Add { set => AddItems.Add(value); }

    public string Remove { set => RemoveItems.Add(value); }

    public override string? LoadingMethod => nameof(ModSystem.PostSetupRecipes);

    public override void Load(Mod mod)
    {
        if (AddItems.Count == 0 && RemoveItems.Count == 0)
            throw new NoGroupChangesException();

        foreach (var groupId in Groups.Select(GetRecipeGroup))
        {
            var recipeGroup = RecipeGroup.recipeGroups[groupId];

            foreach (var item in AddItems.Select(GetItem))
            {
                recipeGroup.ValidItems.Add(item);
                recipeGroup.ValidItemsLookup?[item] = true;
            }

            foreach (var item in RemoveItems.Select(GetItem))
            {
                recipeGroup.ValidItems.Remove(item);
                recipeGroup.ValidItemsLookup?[item] = false;
            }
        }
    }
}
