using Mono.Cecil.Cil;
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
    internal class PackBuilderItem : GlobalItem
    {
        public static FrozenDictionary<int, List<ItemChanges>> ItemModSets = null;

        public static void ApplyChanges(Item item)
        {
            if (ItemModSets?.TryGetValue(item.type, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(item));
        }
    }

    internal class ItemModifier : ModSystem
    {
        // Ensure our "SetDefaults" is applied AFTER all other mods'.
        public static void SetDefaultsILEdit(ILContext il)
        {
            ILCursor cursor = new(il);

            // Move directly after the call to ItemLoader.SetDefaults().
            var itemLoader_SetDefaults = typeof(ItemLoader).GetMethod("SetDefaults", BindingFlags.Static | BindingFlags.NonPublic, [typeof(Item), typeof(bool)]);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCall(itemLoader_SetDefaults)))
                throw new Exception("Unable to locate ItemLoader_SetDefaults in IL edit!");

            // Add a call to PackBuilderItem.ApplyChanges() using the item
            // that SetDefaults() is being called on.
            var packBuilderItem_ApplyChanges = typeof(PackBuilderItem).GetMethod("ApplyChanges", BindingFlags.Static | BindingFlags.Public, [typeof(Item)]);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Call, packBuilderItem_ApplyChanges);
        }

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

        public override void Load()
        {
            var method = typeof(Item).GetMethod("SetDefaults", BindingFlags.Instance | BindingFlags.Public, [typeof(int), typeof(bool), typeof(ItemVariant)]);
            MonoModHooks.Modify(method, SetDefaultsILEdit);
        }
    }
}
