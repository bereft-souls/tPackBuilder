using PackBuilder.Common.BuilderInterface.Windows.EditorWindows;
using PackBuilder.Common.ModBuilding.Drops;
using PackBuilder.Common.ModBuilding.Drops.Changes;
using System;
using System.Collections.Generic;

namespace PackBuilder.Common.BuilderInterface.Windows.EditorModals;

internal sealed class DropModEditorWindow : AbstractEditorWindow<DropMod, DropModEditorWindow.DropModElement>
{
    public sealed class DropModElement : ModifierEditorElement<DropMod>
    {
        private readonly SelectorNpcWrapper npcsList;
        private readonly SelectorItemWrapper itemsList;
        private readonly BoolWrapper globalLootButton;

        public DropModElement() : base("Drop Mod", @static: true)
        {
            var belowBoolElement = 38f;
            globalLootButton = new BoolWrapper("Apply to Global Loot", false);
            {
                globalLootButton.Width.Set(0f, 0.5f);
                globalLootButton.Left.Set(0f, 0.5f);
                globalLootButton.Top.Set(16f, 0f);
            }
            Append(globalLootButton);
            belowBoolElement += globalLootButton.Height.Pixels;

            npcsList = new SelectorNpcWrapper([]);
            {
                npcsList.Left.Set(0f, 0f);
                //npcsList.VAlign = 1f;
                npcsList.Top.Set(belowBoolElement, 0f);
                npcsList.Width.Set(-2f, 0.5f);
                npcsList.Height.Set(0f, 1f);
                npcsList.List.Width.Set(0f, 1f);
                npcsList.List.HAlign = 0f;
            }
            Append(npcsList);
            MakeAndAppendLabel(npcsList, "NPCs");

            itemsList = new SelectorItemWrapper([]);
            {
                itemsList.Left.Set(2f, 0.5f);
                //itemsList.VAlign = 1f;
                itemsList.Top.Set(belowBoolElement, 0f);
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

            var height = Math.Max(
                npcsList.Top.Pixels + npcsList.List.GetTotalHeight(),
                itemsList.Top.Pixels + itemsList.List.GetTotalHeight()
            );

            Height.Set(height + PaddingTop + PaddingBottom, 0f);
        }

        public override DropMod CreateObject()
        {
            var mod = new DropMod();
            {
                mod.NPCs.AddRange(npcsList.Items);
                mod.Items.AddRange(itemsList.Items);
                mod.AllNPCs = globalLootButton.Value;
            }
            return mod;
        }

        public override void Populate(DropMod obj)
        {
            globalLootButton.Value = obj.AllNPCs;
            npcsList.PopulateWithValues(obj.NPCs);
            itemsList.PopulateWithValues(obj.Items);
        }
    }

    private sealed class AddDropElement : ModifierEditorElement<AddDrop>, IVisitor<DropMod>
    {
        private readonly ItemTypeSelector itemInput;

        // TODO: conditions
        private readonly InputField amountInput;
        private readonly InputField chanceInput;

        public AddDropElement() : base("Add Drop")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");

            amountInput = new InputField("1-100");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");

            chanceInput = new InputField("1.00");
            {
                chanceInput.TextScale = 0.66f;
                chanceInput.Left.Set(amountInput.Width.Pixels + 2f + amountInput.Left.Pixels, 0f);
                // chanceInput.VAlign = 1f;
                chanceInput.Width.Set(90f, 0f);
                chanceInput.Height.Set(24f, 0f);
                chanceInput.Top.Set(-chanceInput.Height.Pixels, 1f);
            }
            Append(chanceInput);
            MakeAndAppendLabel(chanceInput, "Chance");
        }

        public override AddDrop CreateObject()
        {
            return new AddDrop(itemInput.Entity)
            {
                Amount = amountInput.Text,
                Chance = chanceInput.Text,
            };
        }

        public override void Populate(AddDrop obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = obj.Amount;
            chanceInput.Text = obj.Chance;
        }

        public void Visit(DropMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class RemoveDropElement : ModifierEditorElement<RemoveDrop>, IVisitor<DropMod>
    {
        private readonly ItemTypeSelector itemInput;

        public RemoveDropElement() : base("Remove Drop")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");
        }

        public override RemoveDrop CreateObject()
        {
            return new RemoveDrop(itemInput.Entity);
        }

        public override void Populate(RemoveDrop obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
        }

        public void Visit(DropMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    private sealed class ModifyDropElement : ModifierEditorElement<ModifyDrop>, IVisitor<DropMod>
    {
        private readonly ItemTypeSelector itemInput;
        private readonly InputField amountInput;
        private readonly InputField chanceInput;

        public ModifyDropElement() : base("Modify Drop")
        {
            itemInput = new ItemTypeSelector();
            {
                itemInput.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                itemInput.Width.Set(140f, 0f);
                itemInput.Height.Set(24f, 0f);
                itemInput.Top.Set(-itemInput.Height.Pixels, 1f);
            }
            Append(itemInput);
            MakeAndAppendLabel(itemInput, "Item");

            amountInput = new InputField("+0");
            {
                amountInput.TextScale = 0.66f;
                amountInput.Left.Set(itemInput.Width.Pixels + 2f, 0f);
                // amountInput.VAlign = 1f;
                amountInput.Width.Set(90f, 0f);
                amountInput.Height.Set(24f, 0f);
                amountInput.Top.Set(-amountInput.Height.Pixels, 1f);
            }
            Append(amountInput);
            MakeAndAppendLabel(amountInput, "Amount");

            chanceInput = new InputField("x1.0");
            {
                chanceInput.TextScale = 0.66f;
                chanceInput.Left.Set(amountInput.Width.Pixels + 2f + amountInput.Left.Pixels, 0f);
                // chanceInput.VAlign = 1f;
                chanceInput.Width.Set(90f, 0f);
                chanceInput.Height.Set(24f, 0f);
                chanceInput.Top.Set(-chanceInput.Height.Pixels, 1f);
            }
            Append(chanceInput);
            MakeAndAppendLabel(chanceInput, "Chance");
        }

        public override ModifyDrop CreateObject()
        {
            return new ModifyDrop(itemInput.Entity)
            {
                Amount = amountInput.Text,
                Chance = chanceInput.Text,
            };
        }

        public override void Populate(ModifyDrop obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj.Item;
            amountInput.Text = obj.Amount.ToStringOrEmpty();
            chanceInput.Text = obj.Chance.ToStringOrEmpty();
        }

        public void Visit(DropMod obj)
        {
            obj.Changes.Add(CreateObject());
        }
    }

    public DropModEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(DropMod obj)
    {
        yield return CreateAndPopulate<DropModElement, DropMod>(obj);

        foreach (var change in obj.Changes)
        {
            if (change is AddDrop addDrop)
            {
                yield return CreateAndPopulate<AddDropElement, AddDrop>(addDrop);
            }
            
            if (change is RemoveDrop removeDrop)
            {
                yield return CreateAndPopulate<RemoveDropElement, RemoveDrop>(removeDrop);
            }
            
            if (change is ModifyDrop modifyDrop)
            {
                yield return CreateAndPopulate<ModifyDropElement, ModifyDrop>(modifyDrop);
            }
        }
    }

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new AddDropElement();
        yield return new RemoveDropElement();
        yield return new ModifyDropElement();
    }
}
