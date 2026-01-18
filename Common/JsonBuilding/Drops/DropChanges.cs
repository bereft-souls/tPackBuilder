using System.Collections.Generic;
using PackBuilder.Common.JsonBuilding.Drops.Changes;

namespace PackBuilder.Common.JsonBuilding.Drops;

public sealed class DropChanges
{
    public List<IDropChange> Changes { get; } = [];

    public AddDrop AddDrop
    {
        set => Changes.Add(value);
    }

    public RemoveDrop RemoveDrop
    {
        set => Changes.Add(value);
    }

    public ModifyDrop ModifyDrop
    {
        set => Changes.Add(value);
    }

    public void ApplyTo(IIterableLoot loot)
    {
        foreach (var change in Changes)
        {
            change.ApplyTo(loot);
        }
    }
}
