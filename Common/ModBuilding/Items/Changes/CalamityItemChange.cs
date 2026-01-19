using CalamityMod;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items.Changes;

public class CalamityItemChange : IItemChange
{
    public ValueModifier MaxCharge { get; set; }
    public ValueModifier ChargePerUse { get; set; }
    public ValueModifier ChargePerAltUse { get; set; }

    [JsonIgnore]
    public bool? CalamityActive
    {
        get { field ??= ModLoader.HasMod("CalamityMod"); return field; }
        set { field = value; }
    } = null;

    public void ApplyTo(Item item)
    {
        if (CalamityActive!.Value)
            ApplyCalamityChanges(item);
    }

    [JITWhenModsEnabled("CalamityMod")]
    public void ApplyCalamityChanges(Item item)
    {
        var calItem = item.Calamity();

        this.MaxCharge.ApplyTo(ref calItem.MaxCharge);
        this.ChargePerUse.ApplyTo(ref calItem.ChargePerUse);
        this.ChargePerAltUse.ApplyTo(ref calItem.ChargePerAltUse);
    }
}
