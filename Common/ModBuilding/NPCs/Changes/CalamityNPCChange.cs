using CalamityMod;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.NPCs.Changes;

public sealed record CalamityNPCChange : INPCChange
{
    public ValueModifier DamageReduction { get; set; }

    [JsonIgnore]
    public bool? CalamityActive
    {
        get { field ??= ModLoader.HasMod("CalamityMod"); return field; }
        set { field = value; }
    } = null;

    public void ApplyTo(NPC npc)
    {
        if (CalamityActive!.Value)
            ApplyCalamityChanges(npc);
    }

    [JITWhenModsEnabled("CalamityMod")]
    public void ApplyCalamityChanges(NPC npc)
    {
        var calNpc = npc.Calamity();
        calNpc.DR = this.DamageReduction.Apply(calNpc.DR);
    }
}
