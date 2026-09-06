using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.ModBuilding.Items.Changes;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Items;

public sealed class ItemMod : PackBuilderType
{
    public List<string> Items = [];

    public List<IItemChange> Changes = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Items", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new ItemModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (Items.Count == 0)
            throw new NoItemsException();

        foreach (int itemType in Items.Select(item => GetItem(item, Mod)))
        {
            if (itemType != ItemID.None)
                ItemModifier.RegisterItemChanges(itemType, Changes);
        }
    }
}

internal sealed class ItemModEditorWindow : AbstractEditorWindow<ItemMod, ItemModEditorWindow.ItemModElement>
{
    public sealed class ItemModElement : ModifierEditorElement<ItemMod>
    {
        private readonly SelectorItemWrapper itemsList;

        public ItemModElement() : base("Item Mod", @static: true)
        {
            itemsList = new SelectorItemWrapper([]);
            {
                itemsList.Left.Set(0f, 0f);
                //npcsList.VAlign = 1f;
                itemsList.Top.Set(38f, 0f);
                itemsList.Width.Set(-2f, 0.5f);
                itemsList.Height.Set(0f, 1f);
                itemsList.List.Width.Set(0f, 1f);
                itemsList.List.HAlign = 0f;
            }
            Append(itemsList);
            MakeAndAppendLabel(itemsList, "Items");
        }

        public override void Recalculate()
        {
            base.Recalculate();

            var height = itemsList.Top.Pixels + itemsList.List.GetTotalHeight();
            Height.Set(height + PaddingTop + PaddingBottom, 0f);
        }

        public override ItemMod CreateObject()
        {
            var mod = new ItemMod();
            {
                mod.Items.AddRange(itemsList.Items);
            }
            return mod;
        }

        public override void Populate(ItemMod obj)
        {
            itemsList.PopulateWithValues(obj.Items);
        }
    }

    private sealed class VanillaItemChangeElement : ModifierEditorElement<VanillaItemChange>, IVisitor<ItemMod>
    {
        private readonly InputField damageElement;
        private readonly InputField critRateElement;
        private readonly InputField defenseElement;
        private readonly InputField hammerPowerElement;
        private readonly InputField pickaxePowerElement;
        private readonly InputField axePowerElement;
        private readonly InputField healingElement;
        private readonly InputField manaRestorationElement;
        private readonly InputField knockbackElement;
        private readonly InputField lifeRegenElement;
        private readonly InputField manaCostElement;
        private readonly InputField shootSpeedElement;
        private readonly InputField useTimeElement;
        private readonly InputField useAnimationElement;

        public VanillaItemChangeElement() : base("Vanilla")
        {
            damageElement = new InputField(DefaultText);
            {
                damageElement.Width.Set(90f, 0f);
                damageElement.Height.Set(20f, 0f);
            }

            critRateElement = new InputField(DefaultText);
            {
                critRateElement.Width.Set(90f, 0f);
                critRateElement.Height.Set(20f, 0f);
            }

            defenseElement = new InputField(DefaultText);
            {
                defenseElement.Width.Set(90f, 0f);
                defenseElement.Height.Set(20f, 0f);
            }

            hammerPowerElement = new InputField(DefaultText);
            {
                hammerPowerElement.Width.Set(90f, 0f);
                hammerPowerElement.Height.Set(20f, 0f);
            }

            pickaxePowerElement = new InputField(DefaultText);
            {
                pickaxePowerElement.Width.Set(90f, 0f);
                pickaxePowerElement.Height.Set(20f, 0f);
            }

            axePowerElement = new InputField(DefaultText);
            {
                axePowerElement.Width.Set(90f, 0f);
                axePowerElement.Height.Set(20f, 0f);
            }

            healingElement = new InputField(DefaultText);
            {
                healingElement.Width.Set(90f, 0f);
                healingElement.Height.Set(20f, 0f);
            }

            manaRestorationElement = new InputField(DefaultText);
            {
                manaRestorationElement.Width.Set(90f, 0f);
                manaRestorationElement.Height.Set(20f, 0f);
            }

            knockbackElement = new InputField(DefaultText);
            {
                knockbackElement.Width.Set(90f, 0f);
                knockbackElement.Height.Set(20f, 0f);
            }

            lifeRegenElement = new InputField(DefaultText);
            {
                lifeRegenElement.Width.Set(90f, 0f);
                lifeRegenElement.Height.Set(20f, 0f);
            }

            manaCostElement = new InputField(DefaultText);
            {
                manaCostElement.Width.Set(90f, 0f);
                manaCostElement.Height.Set(20f, 0f);
            }

            shootSpeedElement = new InputField(DefaultText);
            {
                shootSpeedElement.Width.Set(90f, 0f);
                shootSpeedElement.Height.Set(20f, 0f);
            }

            useTimeElement = new InputField(DefaultText);
            {
                useTimeElement.Width.Set(90f, 0f);
                useTimeElement.Height.Set(20f, 0f);
            }

            useAnimationElement = new InputField(DefaultText);
            {
                useAnimationElement.Width.Set(90f, 0f);
                useAnimationElement.Height.Set(20f, 0f);
            }

            var offset = 42f;

            BuildFieldLine(ref offset, damageElement, critRateElement, knockbackElement);
            {
                MakeAndAppendLabel(damageElement, "Damage");
                MakeAndAppendLabel(critRateElement, "Crit Rate");
                MakeAndAppendLabel(knockbackElement, "Knockback");
            }

            BuildFieldLine(ref offset, defenseElement, lifeRegenElement, manaRestorationElement);
            {
                MakeAndAppendLabel(defenseElement, "Defense");
                MakeAndAppendLabel(lifeRegenElement, "Life Regen");
                MakeAndAppendLabel(manaRestorationElement, "Mana Restoration");
            }

            BuildFieldLine(ref offset, healingElement, manaCostElement);
            {
                MakeAndAppendLabel(healingElement, "Healing");
                MakeAndAppendLabel(manaCostElement, "Mana Cost");
            }

            BuildFieldLine(ref offset, useTimeElement, useAnimationElement, shootSpeedElement);
            {
                MakeAndAppendLabel(useTimeElement, "Use Time");
                MakeAndAppendLabel(useAnimationElement, "Use Animation");
                MakeAndAppendLabel(shootSpeedElement, "Shoot Speed");
            }

            BuildFieldLine(ref offset, pickaxePowerElement, axePowerElement, hammerPowerElement);
            {
                MakeAndAppendLabel(pickaxePowerElement, "Pickaxe Power");
                MakeAndAppendLabel(axePowerElement, "Axe Power");
                MakeAndAppendLabel(hammerPowerElement, "Hammer Power");
            }

            Height.Set(offset - damageElement.Height.Pixels / 2f, 0f);
        }

        public override VanillaItemChange CreateObject()
        {
            return new VanillaItemChange
            {
                Damage = damageElement.Text,
                CritRate = critRateElement.Text,
                Defense = defenseElement.Text,
                HammerPower = hammerPowerElement.Text,
                PickaxePower = pickaxePowerElement.Text,
                AxePower = axePowerElement.Text,
                Healing = healingElement.Text,
                ManaRestoration = manaRestorationElement.Text,
                Knockback = knockbackElement.Text,
                LifeRegen = lifeRegenElement.Text,
                ManaCost = manaCostElement.Text,
                ShootSpeed = shootSpeedElement.Text,
                UseTime = useTimeElement.Text,
                UseAnimation = useAnimationElement.Text,
            };
        }

        public override void Populate(VanillaItemChange obj)
        {
            damageElement.Text = obj.Damage.ToStringOrEmpty();
            critRateElement.Text = obj.CritRate.ToStringOrEmpty();
            defenseElement.Text = obj.Defense.ToStringOrEmpty();
            hammerPowerElement.Text = obj.HammerPower.ToStringOrEmpty();
            pickaxePowerElement.Text = obj.PickaxePower.ToStringOrEmpty();
            axePowerElement.Text = obj.AxePower.ToStringOrEmpty();
            healingElement.Text = obj.Healing.ToStringOrEmpty();
            manaRestorationElement.Text = obj.ManaRestoration.ToStringOrEmpty();
            knockbackElement.Text = obj.Knockback.ToStringOrEmpty();
            lifeRegenElement.Text = obj.LifeRegen.ToStringOrEmpty();
            manaCostElement.Text = obj.ManaCost.ToStringOrEmpty();
            shootSpeedElement.Text = obj.ShootSpeed.ToStringOrEmpty();
            useTimeElement.Text = obj.UseTime.ToStringOrEmpty();
            useAnimationElement.Text = obj.UseAnimation.ToStringOrEmpty();
        }

        public void Visit(ItemMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class CalamityItemChangeElement : ModifierEditorElement<CalamityItemChange>, IVisitor<ItemMod>
    {
        private readonly InputField maxChargeElement;
        private readonly InputField chargePerUseElement;
        private readonly InputField chargePerAltUseElement;

        public CalamityItemChangeElement() : base("Calamity")
        {
            var offset = 42f;

            maxChargeElement = new InputField(DefaultText);
            {
                maxChargeElement.Width.Set(90f, 0f);
                maxChargeElement.Height.Set(20f, 0f);
            }

            chargePerUseElement = new InputField(DefaultText);
            {
                chargePerUseElement.Width.Set(90f, 0f);
                chargePerUseElement.Height.Set(20f, 0f);
            }

            chargePerAltUseElement = new InputField(DefaultText);
            {
                chargePerAltUseElement.Width.Set(90f, 0f);
                chargePerAltUseElement.Height.Set(20f, 0f);
            }

            BuildFieldLine(ref offset, maxChargeElement, chargePerUseElement, chargePerAltUseElement);
            {
                MakeAndAppendLabel(maxChargeElement, "Max Charge");
                MakeAndAppendLabel(chargePerUseElement, "Charge Use");
                MakeAndAppendLabel(chargePerAltUseElement, "Charge Alt Use");
            }

            Height.Set(offset - maxChargeElement.Height.Pixels / 2f, 0f);
        }

        public override CalamityItemChange CreateObject()
        {
            return new CalamityItemChange
            {
                MaxCharge = maxChargeElement.Text,
                ChargePerUse = chargePerUseElement.Text,
                ChargePerAltUse = chargePerAltUseElement.Text,
            };
        }

        public override void Populate(CalamityItemChange obj)
        {
            maxChargeElement.Text = obj.MaxCharge.ToStringOrEmpty();
            chargePerUseElement.Text = obj.ChargePerUse.ToStringOrEmpty();
            chargePerAltUseElement.Text = obj.ChargePerAltUse.ToStringOrEmpty();
        }

        public void Visit(ItemMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    public ItemModEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(ItemMod obj)
    {
        yield return CreateAndPopulate<ItemModElement, ItemMod>(obj);

        foreach (var change in obj.Changes)
        {
            if (change is VanillaItemChange vanilla)
            {
                yield return CreateAndPopulate<VanillaItemChangeElement, VanillaItemChange>(vanilla);
            }

            if (change is CalamityItemChange calamity)
            {
                yield return CreateAndPopulate<CalamityItemChangeElement, CalamityItemChange>(calamity);
            }
        }
    }

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new VanillaItemChangeElement();
        yield return new CalamityItemChangeElement();
    }
}
