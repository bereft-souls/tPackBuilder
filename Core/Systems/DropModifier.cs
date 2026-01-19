using PackBuilder.Common.ModBuilding.Drops;
using PackBuilder.Common.ModBuilding.Drops.Changes;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems;

public sealed class DropModifier : ModSystem
{
    public static Dictionary<int, List<IDropChange>> PerNpcDropMods { get; } = [];

    public static Dictionary<int, List<IDropChange>> PerItemDropMods { get; } = [];

    public static List<IDropChange> GlobalNpcDropMods { get; } = [];

    public static void RegisterNPCDropChanges(int npcType, params IEnumerable<IDropChange> changes)
    {
        PerNpcDropMods.TryAdd(npcType, []);
        PerNpcDropMods[npcType].AddRange(changes);
    }

    public static void RegisterItemDropChanges(int itemType, params IEnumerable<IDropChange> changes)
    {
        PerItemDropMods.TryAdd(itemType, []);
        PerItemDropMods[itemType].AddRange(changes);
    }

    public static void RegisterGlobalDropChanges(params IEnumerable<IDropChange> changes) => GlobalNpcDropMods.AddRange(changes);

    [Autoload(false)]
    [LateLoad]
    internal sealed class DropModifierNpc : GlobalNPC
    {
        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {
            foreach (var change in GlobalNpcDropMods)
                change.ApplyTo(new IterableGlobalLoot(globalLoot));
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (!PerNpcDropMods.TryGetValue(npc.netID, out var changes))
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

            if (!PerItemDropMods.TryGetValue(item.netID, out var changes))
                return;

            foreach (var change in changes)
                change.ApplyTo(new IterableItemLoot(itemLoot));
        }
    }
}
