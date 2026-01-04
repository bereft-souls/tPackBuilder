using System.Collections.Generic;

namespace PackBuilder.Common.JsonBuilding.Drops;

internal sealed class DropMod
{
    public List<string> NPCs { get; } = [];

    public string NPC
    {
        set => NPCs.Add(value);
    }
    
    
}
