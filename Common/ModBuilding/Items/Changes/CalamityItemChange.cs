using CalamityMod;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items.Changes
{
    public class CalamityItemChange : IItemChange
    {
        public ValueModifier MaxCharge { get; set; }
        public ValueModifier ChargePerUse { get; set; }
        public ValueModifier ChargePerAltUse { get; set; }

        [JITWhenModsEnabled("CalamityMod")]
        public void ApplyTo(Item item)
        {
            var calItem = item.Calamity();

            this.MaxCharge.ApplyTo(ref calItem.MaxCharge);
            this.ChargePerUse.ApplyTo(ref calItem.ChargePerUse);
            this.ChargePerAltUse.ApplyTo(ref calItem.ChargePerAltUse);
        }
    }
}
