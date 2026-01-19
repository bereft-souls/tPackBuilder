using PackBuilder.Core.Systems;
using System.Collections.Generic;

namespace PackBuilder.Common.ModBuilding.NPCs;

public sealed class NPCMod : PackBuilderType
{
    public List<string> NPCs = [];

    public List<INPCChange> Changes = [];

    public override void Load()
    {
        if (NPCs.Count == 0)
            throw new NoNPCsException();

        // Get the NPC mod ready for factory initialization.
        foreach (string npc in NPCs)
        {
            int npcType = GetNPC(npc);
            NPCModifier.RegisterChanges(npcType, Changes);
        }
    }
}
