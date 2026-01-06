using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace PackBuilder.Common.JsonBuilding.Recipes.Groups
{
    internal class RecipeGroupMod
    {
        public List<string> Groups = [];
        public List<string> AddItems = [];
        public List<string> RemoveItems = [];

        public required string Group { set => Groups.Add(value); }

        public string Add { set => AddItems.Add(value); }

        public string Remove { set => RemoveItems.Add(value); }

        public void Apply()
        {
            if (AddItems.Count == 0 && RemoveItems.Count == 0)
                throw new NoGroupChangesException();

            foreach (var groupId in Groups.Select(GetRecipeGroup))
            {
                var recipeGroup = RecipeGroup.recipeGroups[groupId];

                foreach (var item in AddItems.Select(GetItem))
                {
                    recipeGroup.ValidItems.Add(item);

                    if (recipeGroup.ValidItemsLookup is not null)
                        recipeGroup.ValidItemsLookup[item] = true;
                }

                foreach (var item in RemoveItems.Select(GetItem))
                {
                    recipeGroup.ValidItems.Remove(item);

                    if (recipeGroup.ValidItemsLookup is not null)
                        recipeGroup.ValidItemsLookup[item] = false;
                }
            }
        }
    }
}
