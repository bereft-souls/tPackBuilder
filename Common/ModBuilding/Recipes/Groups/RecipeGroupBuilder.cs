using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups
{
    internal class RecipeGroupBuilder : PackBuilderType
    {
        public List<string> Items = [];

        public required string Name { get; set; }

        public required string LocalizationKey { get; set; }

        public string Item { set => Items.Add(value); }

        public override string? LoadingMethod => nameof(ModSystem.AddRecipeGroups);

        public override void Load(Mod mod)
        {
            int[] items = Items.Select(GetItem).ToArray();
            RecipeGroup.RegisterGroup(Name, new RecipeGroup(() => Language.GetOrRegister(LocalizationKey).Value, items));
        }
    }
}
