using System.Collections.Generic;
using PackBuilder.Common.ModBuilding.NPCs;
using PackBuilder.Common.ModBuilding.NPCs.Changes;

namespace PackBuilder.Common.BuilderInterface.Windows.EditorModals;

internal sealed class NpcModModal : AbstractModal<NPCMod, NpcModModal.NpcModElement>
{
    public sealed class NpcModElement : ModifierModalElement<NPCMod>
    {
        private readonly SelectorNpcWrapper npcsList;

        public NpcModElement() : base("NPC Mod", @static: true)
        {
            npcsList = new SelectorNpcWrapper([]);
            {
                npcsList.Left.Set(0f, 0f);
                //npcsList.VAlign = 1f;
                npcsList.Top.Set(38f, 0f);
                npcsList.Width.Set(-2f, 0.5f);
                npcsList.Height.Set(0f, 1f);
                npcsList.List.Width.Set(0f, 1f);
                npcsList.List.HAlign = 0f;
            }
            Append(npcsList);
            MakeAndAppendLabel(npcsList, "NPCs");
        }

        public override void Recalculate()
        {
            base.Recalculate();

            var height = npcsList.Top.Pixels + npcsList.List.GetTotalHeight();
            Height.Set(height + PaddingTop + PaddingBottom, 0f);
        }

        public override NPCMod CreateObject()
        {
            var mod = new NPCMod();
            {
                mod.NPCs.AddRange(npcsList.Items);
            }
            return mod;
        }

        public override void Populate(NPCMod obj)
        {
            npcsList.PopulateWithValues(obj.NPCs);
        }
    }

    private sealed class VanillaNpcChangeElement : ModifierModalElement<VanillaNpcChange>, IVisitor<NPCMod>
    {
        private readonly InputField damageElement;
        private readonly InputField defenseElement;
        private readonly InputField healthElement;
        private readonly InputField knockbackScalingElement;
        private readonly InputField npcSlotsElement;

        public VanillaNpcChangeElement() : base("Vanilla")
        {
            var offset = 42f;
            
            damageElement = new InputField(DefaultText);
            {
                damageElement.Width.Set(90f, 0f);
                damageElement.Height.Set(20f, 0f);
            }
            
            defenseElement = new InputField(DefaultText);
            {
                defenseElement.Width.Set(90f, 0f);
                defenseElement.Height.Set(20f, 0f);
            }
            
            healthElement = new InputField(DefaultText);
            {
                healthElement.Width.Set(90f, 0f);
                healthElement.Height.Set(20f, 0f);
            }
            
            knockbackScalingElement = new InputField(DefaultText);
            {
                knockbackScalingElement.Width.Set(90f, 0f);
                knockbackScalingElement.Height.Set(20f, 0f);
            }
            
            npcSlotsElement = new InputField(DefaultText);
            {
                npcSlotsElement.Width.Set(90f, 0f);
                npcSlotsElement.Height.Set(20f, 0f);
            }
            
            BuildFieldLine(ref offset, healthElement, defenseElement, damageElement, knockbackScalingElement);
            {
                MakeAndAppendLabel(damageElement, "Damage");
                MakeAndAppendLabel(defenseElement, "Defense");
                MakeAndAppendLabel(healthElement, "Health");
                MakeAndAppendLabel(knockbackScalingElement, "KB Resist");
            }
            
            BuildFieldLine(ref offset, npcSlotsElement);
            {
                MakeAndAppendLabel(npcSlotsElement, "NPC Slots");
            }
            
            Height.Set(offset - damageElement.Height.Pixels / 2f, 0f);
        }

        public override VanillaNpcChange CreateObject()
        {
            return new VanillaNpcChange
            {
                Damage = damageElement.Text,
                Defense = defenseElement.Text,
                Health = healthElement.Text,
                KnockbackScaling = knockbackScalingElement.Text,
                NPCSlots = npcSlotsElement.Text,
            };
        }

        public override void Populate(VanillaNpcChange obj)
        {
            damageElement.Text = obj.Damage.ToStringOrEmpty();
            defenseElement.Text = obj.Defense.ToStringOrEmpty();
            healthElement.Text = obj.Health.ToStringOrEmpty();
            knockbackScalingElement.Text = obj.KnockbackScaling.ToStringOrEmpty();
            npcSlotsElement.Text = obj.NPCSlots.ToStringOrEmpty();
        }

        public void Visit(NPCMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class CalamityNpcChangeElement : ModifierModalElement<CalamityNPCChange>, IVisitor<NPCMod>
    {
        private readonly InputField damageReductionElement;

        public CalamityNpcChangeElement() : base("Calamity")
        {
            var offset = 42f;
            
            damageReductionElement = new InputField(DefaultText);
            {
                damageReductionElement.Width.Set(90f, 0f);
                damageReductionElement.Height.Set(20f, 0f);
            }
            
            BuildFieldLine(ref offset, damageReductionElement);
            {
                MakeAndAppendLabel(damageReductionElement, "Damage Reduction");
            }
            
            Height.Set(offset - damageReductionElement.Height.Pixels / 2f, 0f);
        }

        public override CalamityNPCChange CreateObject()
        {
            return new CalamityNPCChange
            {
                DamageReduction = damageReductionElement.Text,
            };
        }

        public override void Populate(CalamityNPCChange obj)
        {
            damageReductionElement.Text = obj.DamageReduction.ToStringOrEmpty();
        }

        public void Visit(NPCMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    public NpcModModal(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierModalElement> DeriveModifiers(NPCMod obj)
    {
        yield return CreateAndPopulate<NpcModElement, NPCMod>(obj);

        foreach (var change in obj.Changes)
        {
            if (change is VanillaNpcChange vanilla)
            {
                yield return CreateAndPopulate<VanillaNpcChangeElement, VanillaNpcChange>(vanilla);
            }

            if (change is CalamityNPCChange calamity)
            {
                yield return CreateAndPopulate<CalamityNpcChangeElement, CalamityNPCChange>(calamity);
            }
        }
    }

    protected override IEnumerable<ModifierModalElement> GetAvailableModifiers()
    {
        yield return new VanillaNpcChangeElement();
        yield return new CalamityNpcChangeElement();
    }
}
