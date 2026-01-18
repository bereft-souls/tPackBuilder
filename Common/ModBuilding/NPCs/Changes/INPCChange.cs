using Terraria;

namespace PackBuilder.Common.ModBuilding.NPCs.Changes
{
    internal interface INPCChange
    {
        public void ApplyTo(NPC npc);
    }
}
