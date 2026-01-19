using PackBuilder.Core.Systems;
using System.Collections.Generic;

namespace PackBuilder.Common.ModBuilding.Drops;

public sealed class DropMod : PackBuilderType
{
    public List<string> NPCs { get; } = [];

    public List<string> Items { get; } = [];

    public bool AllNPCs { get; set; } = false;

    // public bool AllItems { get; set; } = false;

    public List<IDropChange> Changes { get; } = [];

    public override void Load()
    {
        if (NPCs.Count == 0)
            throw new NoDropScopeException();

        foreach (var scope in NPCs)
        {
            var npcType = GetNPC(scope);
            DropModifier.RegisterNPCDropChanges(npcType, Changes);
        }

        foreach (var scope in Items)
        {
            var itemType = GetItem(scope);
            DropModifier.RegisterItemDropChanges(itemType, Changes);
        }

        if (AllNPCs)
            DropModifier.RegisterGlobalDropChanges(Changes);
    }
}
