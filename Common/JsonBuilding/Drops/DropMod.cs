using System.Collections.Generic;

namespace PackBuilder.Common.JsonBuilding.Drops;

internal sealed class DropMod
{
    public List<string> NPCs { get; } = [];
    
    public List<string> Items { get; } = [];
    
    public required string NPC
    {
        set => NPCs.Add(value);
    }
    
    public required string Item
    {
        set => Items.Add(value);
    }

    public bool AllNPCs { get; set; } = false;

    // public bool AllItems { get; set; } = false;

    public required DropChanges Changes { get; set; }
    
}
