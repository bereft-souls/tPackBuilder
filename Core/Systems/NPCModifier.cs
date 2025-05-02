using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using PackBuilder.Common.JsonBuilding.NPCs;
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
    internal class PackBuilderNPC : GlobalNPC
    {
        public static FrozenDictionary<int, List<NPCChanges>> NPCModSets = null;

        public override bool InstancePerEntity => true;
        public int CachedNetId = 0;

        public static void ApplyChanges(NPC npc, int npcId)
        {
            if (NPCModSets?.TryGetValue(npcId, out var value) ?? false)
                value.ForEach(c => c.ApplyTo(npc));
        }
    }

    internal class NPCModifier : ModSystem
    {
        public static void FinalSetDefaults(NPCLoader_SetDefaults orig, NPC npc, bool createModNPC)
        {
            // Run normal SetDefaults first.
            orig(npc, createModNPC);

            // If a netID is used to summon an NPC, we cache that netID during setup
            // inside our GlobalNPC. The netID is reset during setup and IL editing
            // it to not reset can cause issues, so caching it externally is generally the better option.
            if (!npc.TryGetGlobalNPC<PackBuilderNPC>(out var packNPC))
                return;

            // If an NPC is summoned via netID, we apply changes inside of the
            // IL edit. We don't need to apply them twice.
            if (packNPC.CachedNetId >= 0)
                PackBuilderNPC.ApplyChanges(npc, npc.type);
        }

        public override void PostSetupContent()
        {
            // Collects ALL .npcmod.json files from all mods into a list.
            Dictionary<string, byte[]> jsonEntries = [];

            // Collects the loaded NPC mods to pass to the set factory initialization.
            Dictionary<int, List<NPCChanges>> factorySets = [];

            foreach (Mod mod in ModLoader.Mods)
            {
                // An array of all .npcmod.json files from this specific mod.
                var files = (mod.GetFileNames() ?? []).Where(s => s.EndsWith(".npcmod.json", System.StringComparison.OrdinalIgnoreCase));

                // Adds the byte contents of each file to the list.
                foreach (var file in files)
                    jsonEntries.Add(file, mod.GetFileBytes(file));
            }

            foreach (var jsonEntry in jsonEntries)
            {
                PackBuilder.LoadingFile = jsonEntry.Key;

                // Convert the raw bytes into raw text.
                string rawJson = Encoding.UTF8.GetString(jsonEntry.Value);

                // Decode the json into an NPC mod.
                NPCMod npcMod = JsonConvert.DeserializeObject<NPCMod>(rawJson, PackBuilder.JsonSettings)!;

                if (npcMod.NPCs.Count == 0)
                    throw new NoNPCsException();

                // Get the NPC mod ready for factory initialization.
                foreach (string npc in npcMod.NPCs)
                {
                    int npcType = GetNPC(npc);

                    factorySets.TryAdd(npcType, []);
                    factorySets[npcType].Add(npcMod.Changes);
                }

                PackBuilder.LoadingFile = null;
            }

            // Setup the factory for fast access to NPC lookup.
            PackBuilderNPC.NPCModSets = factorySets.ToFrozenDictionary();
        }

        public static Hook NPCLoaderHook = null;
        public delegate void NPCLoader_SetDefaults(NPC npc, bool createModNPC = false);

        public override void Load()
        {
            IL_NPC.SetDefaultsFromNetId += IL_NPC_SetDefaultsFromNetId;

            var method = typeof(NPCLoader).GetMethod("SetDefaults", BindingFlags.Static | BindingFlags.NonPublic);
            NPCLoaderHook = new(method, FinalSetDefaults);
        }

        private static void IL_NPC_SetDefaultsFromNetId(ILContext il)
        {
            try
            {
                var cursor = new ILCursor(il);

                // Navigate to the first SetDefaults call.
                // This method is passed a value of 0 to essentially only act as a field initializer.
                var setDefaultsMethod = typeof(NPC).GetMethod("SetDefaults", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!;
                cursor.GotoNext(i => i.MatchCall(setDefaultsMethod));

                // After the NPC is initialized, we want to immediately cache netID.
                // This allows us to differentiate between netID specific NPCs for NPC modifications.
                cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0); // Push the NPC to the stack
                cursor.Emit(OpCodes.Ldarg_1); // Push the designated net ID to the stack

                // Assign the cached netID field. Pops both of our pushed values.
                cursor.EmitDelegate<Action<NPC, int>>((npc, netID) =>
                {
                    if (!npc.TryGetGlobalNPC(out PackBuilderNPC result))
                        return;

                    result.CachedNetId = netID;
                });

                // Then we need to make sure that our ApplyChanges method is called before difficulty scaling.
                // This is the bigger reason we need this il edit.

                // Navigate to the next instruction that assigns the NPC's netID.
                var netIdField = typeof(NPC).GetField("netID", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)!;
                cursor.GotoNext(i => i.MatchStfld(netIdField));

                // After this assigning, call our apply changes method.
                // This allows users to specify different changes for NPCs that share a 'type' but have difference netID's (ie. slimes)
                cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0); // Push the NPC to the stack
                cursor.Emit(OpCodes.Ldarg_1); // Push the designated net ID to the stack

                var applyChangesMethod = typeof(PackBuilderNPC).GetMethod("ApplyChanges", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!;
                cursor.Emit(OpCodes.Call, applyChangesMethod); // Call our ApplyChanges method. Pops both of our pushed values.
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<PackBuilder>(), il);
            }
        }
    }
}
