using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace PackBuilder.Common.JsonBuilding.Recipes.Groups
{
    internal class RecipeGroupBuilder
    {
        public List<string> Items = [];

        public required string Name { get; set; }

        public required string LocalizationKey { get; set; }

        public string Item { set => Items.Add(value); }

        public void Build()
        {
            int[] items = Items.Select(GetItem).ToArray();
            RecipeGroup.RegisterGroup(Name, new RecipeGroup(() => Language.GetTextValue(LocalizationKey), items));
        }
    }
}
