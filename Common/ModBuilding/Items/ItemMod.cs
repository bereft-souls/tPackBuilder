using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items;

public sealed class ItemMod : PackBuilderType
{
    public List<string> Items = [];

    public List<IItemChange> Changes = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Items", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new ItemModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (Items.Count == 0)
            throw new NoItemsException();

        foreach (int itemType in Items.Select(item => GetItem(item, Mod)))
        {
            if (itemType != ItemID.None)
                ItemModifier.RegisterItemChanges(itemType, Changes);
        }
    }
}
