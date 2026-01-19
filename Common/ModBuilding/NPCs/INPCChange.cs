using Terraria;

namespace PackBuilder.Common.ModBuilding.NPCs;

internal interface INPCChange
{
    public void ApplyTo(NPC npc);
}
