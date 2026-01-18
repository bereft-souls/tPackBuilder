using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops;

public sealed class DropMod : PackBuilderType
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

    public override void Load(Mod mod)
    {
        if (NPCs.Count == 0)
            throw new NoDropScopeException();

        foreach (var scope in NPCs)
        {
            var npcType = GetNPC(scope);

            if (!DropModifier.PerNpcDropChanges.TryGetValue(npcType, out var changes))
                changes = DropModifier.PerNpcDropChanges[npcType] = [];

            changes.Add(Changes);
        }

        foreach (var scope in Items)
        {
            var itemType = GetItem(scope);

            if (!DropModifier.PerItemDropChanges.TryGetValue(itemType, out var changes))
                changes = DropModifier.PerItemDropChanges[itemType] = [];

            changes.Add(Changes);
        }

        if (AllNPCs)
            DropModifier.GlobalNpcDropChanges.Add(Changes);
    }
}
