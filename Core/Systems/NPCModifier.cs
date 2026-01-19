using Mono.Cecil.Cil;
using MonoMod.Cil;
using PackBuilder.Common.ModBuilding.NPCs;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    internal class NPCModifier : ModSystem
    {
        public static Dictionary<int, List<INPCChange>> NPCMods { get; } = [];

        /// <summary>
        /// Registers changes for a given NPC type.
        /// </summary>
        public static void RegisterChanges(int npcType, params IEnumerable<INPCChange> changes)
        {
            NPCMods.TryAdd(npcType, []);
            NPCMods[npcType].AddRange(changes);
        }

        public override void Load()
        {
            // We need to cache the netID (and, subsequently, apply our changes
            // based on that netID in the same method) for NPCs spawned using
            // a netID (negative number).
            IL_NPC.SetDefaultsFromNetId += SetDefaultsFromNetIdILEdit;
        }

        // This ensures an NPC's netID is cached and used for
        // NPC mods when it is spawned using one.
        private static void SetDefaultsFromNetIdILEdit(ILContext il)
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
                    if (!ModSorter.LateTypesLoaded || !npc.TryGetGlobalNPC(out PackBuilderNPC result))
                        return;

                    result.CachedNetId = netID;
                });

                // Then we need to make sure that our ApplyChanges method is called before difficulty scaling.
                // This is the bigger reason we need this il edit.

                // Navigate to the next instruction that assigns the NPC's netID.
                var netIdField = typeof(NPC).GetField("netID", BindingFlags.Instance | BindingFlags.Public)!;
                cursor.GotoNext(i => i.MatchStfld(netIdField));

                // After this assigning, call our apply changes method.
                // This allows users to specify different changes for NPCs that share a 'type' but have difference netID's (ie. slimes)
                cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0); // Push the NPC to the stack
                cursor.Emit(OpCodes.Ldarg_1); // Push the designated net ID to the stack

                var applyChangesMethod = typeof(PackBuilderNPC).GetMethod("ApplyChanges", BindingFlags.Static | BindingFlags.Public)!;
                cursor.Emit(OpCodes.Call, applyChangesMethod); // Call our ApplyChanges method. Pops both of our pushed values.
            }
            catch
            {
                MonoModHooks.DumpIL(ModContent.GetInstance<PackBuilder>(), il);
            }
        }

        [Autoload(false)]
        [LateLoad]
        internal class PackBuilderNPC : GlobalNPC
        {
            public override bool InstancePerEntity => true;
            public int CachedNetId = 0;

            public override void SetDefaults(NPC entity)
            {
                // If a netID is used to summon an NPC, we cache that netID during setup
                // inside our GlobalNPC. The netID is reset during setup and IL editing
                // it to not reset can cause issues, so caching it externally is generally the better option.
                if (!entity.TryGetGlobalNPC<PackBuilderNPC>(out var packNPC))
                    return;

                // If an NPC is summoned via netID, we apply changes inside of the
                // IL edit. We don't need to apply them twice.
                if (packNPC.CachedNetId >= 0)
                    ApplyChanges(entity, entity.type);
            }

            public static void ApplyChanges(NPC npc, int npcId)
            {
                if (!NPCMods.TryGetValue(npcId, out var changes))
                    return;

                foreach (var change in changes)
                    change.ApplyTo(npc);
            }
        }
    }
}
