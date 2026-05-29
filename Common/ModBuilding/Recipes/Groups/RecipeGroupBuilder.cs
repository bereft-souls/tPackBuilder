using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

public sealed class RecipeGroupBuilder : PackBuilderType
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public string LocalizationKey { get; set; }
    public List<string> Items = [];

    public override string? LoadingMethod => nameof(ModSystem.AddRecipeGroups);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeGroupBuilderModal(element, state);
    }

    public override void Load()
    {
        int[] items = Items.Select(GetItem).ToArray();
        RecipeGroup.RegisterGroup(Name, new RecipeGroup(() => Language.GetOrRegister(LocalizationKey).Value, items));
    }
}
