using System.Collections.Generic;
using Terraria.GameContent.ItemDropRules;
using Terraria.ModLoader;

namespace PackBuilder.Common.JsonBuilding.Drops.Changes;

internal interface IDropChange
{
    void ApplyTo(ILoot loot);
}
