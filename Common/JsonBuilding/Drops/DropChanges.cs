using System.Collections.Generic;
using PackBuilder.Common.JsonBuilding.Drops.Changes;
using Terraria;

namespace PackBuilder.Common.JsonBuilding.Drops;

internal sealed class DropChanges
{
    public List<IDropChange> Changes { get; } = [];

    public void ApplyTo(NPC npc)
    {
        foreach (var change in Changes)
        {
            change.ApplyTo(npc);
        }
    }
}
