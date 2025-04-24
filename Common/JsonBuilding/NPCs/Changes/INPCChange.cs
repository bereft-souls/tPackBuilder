using Terraria;

namespace PackBuilder.Common.JsonBuilding.NPCs.Changes
{
    internal interface INPCChange
    {
        public void ApplyTo(NPC npc);
    }
}
