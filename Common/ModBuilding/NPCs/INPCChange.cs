using Terraria;

namespace PackBuilder.Common.ModBuilding.NPCs;

public interface INPCChange
{
    public void ApplyTo(NPC npc);
}
