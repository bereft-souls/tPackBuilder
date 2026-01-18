using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.NPCs
{
    internal class NPCMod : PackBuilderType
    {
        public List<string> NPCs = [];

        public required string NPC { set => NPCs.Add(value); }

        public required NPCChanges Changes { get; set; }

        public override void Load(Mod mod)
        {
            if (NPCs.Count == 0)
                throw new NoNPCsException();

            // Get the NPC mod ready for factory initialization.
            foreach (string npc in NPCs)
            {
                int npcType = GetNPC(npc);

                NPCModifier.NPCModSets.TryAdd(npcType, []);
                NPCModifier.NPCModSets[npcType].Add(Changes);
            }
        }
    }
}
