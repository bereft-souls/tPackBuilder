using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

public sealed class RecipeGroupBuilder : PackBuilderType
{
    public required string Name { get; set; }
    public required string LocalizationKey { get; set; }
    public List<string> Items = [];

    public override string? LoadingMethod => nameof(ModSystem.AddRecipeGroups);

    public override void Load()
    {
        int[] items = Items.Select(GetItem).ToArray();
        RecipeGroup.RegisterGroup(Name, new RecipeGroup(() => Language.GetOrRegister(LocalizationKey).Value, items));
    }
}
