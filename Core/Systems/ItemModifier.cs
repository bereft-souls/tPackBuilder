using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.Items;
using PackBuilder.Core.Utils;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    internal class PackBuilderItem : GlobalItem
    {
        public static FrozenDictionary<int, List<ItemChanges>> ItemModSets = null;

        public static void FinalSetDefaults(Item entity)
        {
            if (ItemModSets?.TryGetValue(entity.type, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(entity));
        }

        public class ItemLoaderSetDefaultsDetour : AutoVoidDetour<Item, bool>
        {
            public override MethodInfo Method => typeof(ItemLoader).GetMethod("SetDefaults", BindingFlags.Static | BindingFlags.NonPublic);
            public override void Detour(Action<Item, bool> orig, Item arg1, bool arg2)
            {
                // Run normal SetDefaults first.
                orig(arg1, arg2);
                FinalSetDefaults(arg1);
            }
        }
    }

    internal class ItemModifier : ModSystem
    {
        public override void PostSetupContent()
        {
            // Collects ALL .itemmod.json files from all mods into a list.
            Dictionary<string, byte[]> jsonEntries = [];

            // Collects the loaded item mods to pass to the set factory initialization.
            Dictionary<int, List<ItemChanges>> factorySets = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .itemmod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".itemmod.json", System.StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add(file, mod.GetFileBytes(file));
            }

            foreach (var jsonEntry in jsonEntries)
            {
                PackBuilder.LoadingFile = jsonEntry.Key;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.Default.GetString(jsonEntry.Value);

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
