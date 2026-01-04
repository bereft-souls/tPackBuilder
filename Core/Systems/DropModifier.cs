using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Drops;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems;

[Autoload(false)]
[LateLoad]
internal sealed class DropModifierNpc : GlobalNPC
{
    public override void ModifyGlobalLoot(GlobalLoot globalLoot)
    {
        base.ModifyGlobalLoot(globalLoot);

        foreach (var change in DropModifier.GlobalNpcDropChanges)
        {
            change.ApplyTo(globalLoot);
        }
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        base.ModifyNPCLoot(npc, npcLoot);

        if (!DropModifier.PerNpcDropChanges.TryGetValue(npc.netID, out var changes))
        {
            return;
        }

        foreach (var change in changes)
        {
            change.ApplyTo(npcLoot);
        }
    }
}

[Autoload(false)]
[LateLoad]
internal sealed class DropModifierItem : GlobalItem
{
    public override void ModifyItemLoot(Item item, ItemLoot itemLoot)
    {
        base.ModifyItemLoot(item, itemLoot);

        if (!DropModifier.PerItemDropChanges.TryGetValue(item.netID, out var changes))
        {
            return;
        }

        foreach (var change in changes)
        {
            change.ApplyTo(itemLoot);
        }
    }
}

internal sealed class DropModifier : ModSystem
{
    public static Dictionary<int, List<DropChanges>> PerNpcDropChanges { get; } = [];

    public static Dictionary<int, List<DropChanges>> PerItemDropChanges { get; } = [];

    public static List<DropChanges> GlobalNpcDropChanges { get; } = [];

    // public static List<DropChanges> GlobalItemDropChanges { get; } = [];

    public override void PostSetupContent()
    {
        base.PostSetupContent();

        var jsonEntries = new List<(string, byte[])>();

        foreach (var mod in ModLoader.Mods)
        {
            var files = (mod.GetFileNames() ?? []).Where(x => x.EndsWith(".dropmod.json", StringComparison.OrdinalIgnoreCase));

            foreach (var file in files)
            {
                jsonEntries.Add((file, mod.GetFileBytes(file)));
            }
        }

        foreach (var (file, data) in jsonEntries)
        {
            PackBuilder.LoadingFile = file;

            var rawJson = Encoding.UTF8.GetString(data);
            var dropMod = JsonConvert.DeserializeObject<DropMod>(rawJson, PackBuilder.JsonSettings);

            if (dropMod is null || dropMod.NPCs.Count == 0)
            {
                throw new NoDropScopeException();
            }

            foreach (var scope in dropMod.NPCs)
            {
                var npcType = GetNPC(scope);

                if (!PerNpcDropChanges.TryGetValue(npcType, out var changes))
                {
                    changes = PerNpcDropChanges[npcType] = [];
                }

                changes.Add(dropMod.Changes);
            }

            foreach (var scope in dropMod.Items)
            {
                var itemType = GetItem(scope);

                if (!PerItemDropChanges.TryGetValue(itemType, out var changes))
                {
                    changes = PerItemDropChanges[itemType] = [];
                }

                changes.Add(dropMod.Changes);
            }

            if (dropMod.AllNPCs)
            {
                GlobalNpcDropChanges.Add(dropMod.Changes);
            }

            PackBuilder.LoadingFile = null;
        }
    }
}
