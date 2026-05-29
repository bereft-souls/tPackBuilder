using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.ModBuilding.Items;

public sealed class ItemMod : PackBuilderType
{
    public List<string> Items = [];

    public List<IItemChange> Changes = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Items", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new ItemModModal(element, state);
    }

    public override void Load()
    {
        if (Items.Count == 0)
            throw new NoItemsException();

        // Get the item mod ready for factory initialization.
        foreach (string item in Items)
        {
            int itemType = GetItem(item);
            ItemModifier.RegisterItemChanges(itemType, Changes);
        }
    }
}
