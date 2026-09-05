using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria.ModLoader;
using System.Linq;
using Terraria.ID;

namespace PackBuilder.Common.ModBuilding.NPCs;

public sealed class NPCMod : PackBuilderType
{
    public List<string> NPCs = [];

    public List<INPCChange> Changes = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/NPCs", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorModal(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new NpcModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (NPCs.Count == 0)
            throw new NoNPCsException();

        foreach (int npcType in NPCs.Select(npc => GetNPC(npc, Mod)))
        {
            if (npcType != NPCID.None)
                NPCModifier.RegisterChanges(npcType, Changes);
        }
    }
}
