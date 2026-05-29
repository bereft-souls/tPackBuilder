using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Drops;

public sealed class DropMod : PackBuilderType
{
    public List<string> NPCs { get; } = [];

    public List<string> Items { get; } = [];

    public bool AllNPCs { get; set; } = false;

    // public bool AllItems { get; set; } = false;

    public List<IDropChange> Changes { get; } = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Drops", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new DropModModal(element, state);
    }

    public override void Load()
    {
        if (NPCs.Count == 0 && Items.Count == 0 && !AllNPCs)
            throw new NoDropScopeException();

        foreach (var scope in NPCs)
        {
            var npcType = GetNPC(scope);
            DropModifier.RegisterNPCDropChanges(npcType, Changes);
        }

        foreach (var scope in Items)
        {
            var itemType = GetItem(scope);
            DropModifier.RegisterItemDropChanges(itemType, Changes);
        }

        if (AllNPCs)
            DropModifier.RegisterGlobalDropChanges(Changes);
    }
}
