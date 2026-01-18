using System.Collections.Generic;
using PackBuilder.Common.JsonBuilding.Drops;
using PackBuilder.Common.JsonBuilding.Drops.Changes;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems;

public sealed class DropModifier : ModSystem
{
    public static Dictionary<int, List<DropChanges>> PerNpcDropChanges { get; } = [];

    public static Dictionary<int, List<DropChanges>> PerItemDropChanges { get; } = [];

    public static List<DropChanges> GlobalNpcDropChanges { get; } = [];


    [Autoload(false)]
    [LateLoad]
    internal sealed class DropModifierNpc : GlobalNPC
    {
        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {
            foreach (var change in GlobalNpcDropChanges)
                change.ApplyTo(new IterableGlobalLoot(globalLoot));
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!PerNpcDropChanges.TryGetValue(npc.netID, out var changes))
                return;

            foreach (var change in changes)
                change.ApplyTo(new IterableNpcLoot(npcLoot));
        }
    }

    [Autoload(false)]
    [LateLoad]
    internal sealed class DropModifierItem : GlobalItem
    {
        public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
        {
            base.ModifyItemLoot(item, itemLoot);

            if (!PerItemDropChanges.TryGetValue(item.netID, out var changes))
                return;

            foreach (var change in changes)
                change.ApplyTo(new IterableItemLoot(itemLoot));
        }
    }
}
