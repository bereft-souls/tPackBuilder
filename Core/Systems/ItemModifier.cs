﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Items;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.GameContent.Items;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    [Autoload(false)]
    [LateLoad]
    internal class PackBuilderItem : GlobalItem
    {
        public static FrozenDictionary<int, List<ItemChanges>> ItemModSets = null;

        public override void SetDefaults(Item entity) => ApplyChanges(entity);

        public static void ApplyChanges(Item item)
        {
            if (ItemModSets?.TryGetValue(item.type, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(item));
        }
    }

    internal class ItemModifier : ModSystem
    {
        public override void PostSetupContent()
        {
            // Collects ALL .itemmod.json files from all mods into a list.
            List<(string, byte[])> jsonEntries = [];

            // Collects the loaded item mods to pass to the set factory initialization.
            Dictionary<int, List<ItemChanges>> factorySets = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .itemmod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".itemmod.json", StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add((file, mod.GetFileBytes(file)));
            }

            foreach (var (file, data) in jsonEntries)
            {
                PackBuilder.LoadingFile = file;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(data);

                // Decode the json into an item mod.
                ItemMod itemMod = JsonConvert.DeserializeObject<ItemMod>(rawJson, PackBuilder.JsonSettings)!;

                if (itemMod.Items.Count == 0)
                    throw new NoItemsException();

                // Get the item mod ready for factory initialization.
                foreach (string item in itemMod.Items)
                {
                    int itemType = GetItem(item);

                    factorySets.TryAdd(itemType, []);
                    factorySets[itemType].Add(itemMod.Changes);
                }

                PackBuilder.LoadingFile = null;
            }

            // Setup the factory for fast access to item lookup.
            PackBuilderItem.ItemModSets = factorySets.ToFrozenDictionary();
        }
    }
}
