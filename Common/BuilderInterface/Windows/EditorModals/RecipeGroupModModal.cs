using System.Collections.Generic;
using PackBuilder.Common.ModBuilding.Recipes.Groups;

namespace PackBuilder.Common.BuilderInterface.Windows.EditorModals;

internal sealed class RecipeGroupModModal : AbstractModal<RecipeGroupMod, RecipeGroupModModal.RecipeGroupModElement>
{
    public sealed class RecipeGroupModElement : ModifierModalElement<RecipeGroupMod>
    {
        private readonly SelectorRecipeGroupWrapper recipeGroupSelector;

        public RecipeGroupModElement() : base("Recipe Group Mod", @static: true)
        {
            recipeGroupSelector = new SelectorRecipeGroupWrapper([]);
            {
                recipeGroupSelector.Left.Set(0f, 0f);
                // itemInput.VAlign = 1f;
                recipeGroupSelector.Width.Set(-2f, 0.5f);
                recipeGroupSelector.Height.Set(24f, 0f);
                recipeGroupSelector.Top.Set(38f, 0f);
                recipeGroupSelector.List.Width.Set(0f, 1f);
                recipeGroupSelector.List.HAlign = 0f;
            }
            Append(recipeGroupSelector);
            MakeAndAppendLabel(recipeGroupSelector, "Groups");
        }

        public override void Recalculate()
        {
            base.Recalculate();

            var height = recipeGroupSelector.Top.Pixels + recipeGroupSelector.List.GetTotalHeight();
            Height.Set(height + PaddingTop + PaddingBottom, 0f);
        }

        public override RecipeGroupMod CreateObject()
        {
            var obj = new RecipeGroupMod();
            {
                obj.Groups.AddRange(recipeGroupSelector.Items);
            }
            return obj;
        }

        public override void Populate(RecipeGroupMod obj)
        {
            recipeGroupSelector.PopulateWithValues(obj.Groups);
        }
    }

    private sealed class AddItemElement : ModifierModalElement<string>, IVisitor<RecipeGroupMod>
    {
        private readonly ItemTypeSelector itemInput;

        public AddItemElement() : base("Add Item")
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

        public override string CreateObject()
        {
            return itemInput.Entity;
        }

        public override void Populate(string obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj;
        }

        public void Visit(RecipeGroupMod obj)
        {
            obj.AddItems.Add(CreateObject());
        }
    }

    private sealed class RemoveItemElement : ModifierModalElement<string>, IVisitor<RecipeGroupMod>
    {
        private readonly ItemTypeSelector itemInput;
        
        public RemoveItemElement() : base("Remove Item")
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

        public override string CreateObject()
        {
            return itemInput.Entity;
        }

        public override void Populate(string obj)
        {
            itemInput.Text.Text = itemInput.Entity = obj;
        }

        public void Visit(RecipeGroupMod obj)
        {
            obj.RemoveItems.Add(CreateObject());
        }
    }

    public RecipeGroupModModal(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierModalElement> DeriveModifiers(RecipeGroupMod obj)
    {
        yield return CreateAndPopulate<RecipeGroupModElement, RecipeGroupMod>(obj);
        
        foreach (var item in obj.AddItems)
        {
            yield return CreateAndPopulate<AddItemElement, string>(item);
        }
        
        foreach (var item in obj.RemoveItems)
        {
            yield return CreateAndPopulate<RemoveItemElement, string>(item);
        }
    }

    protected override IEnumerable<ModifierModalElement> GetAvailableModifiers()
    {
        yield return new AddItemElement();
        yield return new RemoveItemElement();
    }
}
