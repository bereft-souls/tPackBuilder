using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

public sealed class RecipeGroupMod : PackBuilderType
{
    public List<string> Groups = [];
    public List<string> AddItems = [];
    public List<string> RemoveItems = [];

    public override string? LoadingMethod => nameof(ModSystem.PostSetupRecipes);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeGroupModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (AddItems.Count == 0 && RemoveItems.Count == 0)
            throw new NoGroupChangesException();

        foreach (var groupId in Groups.Select(GetRecipeGroup))
        {
            var recipeGroup = RecipeGroup.recipeGroups[groupId];

            foreach (int itemType in AddItems.Select(item => GetItem(item, Mod)))
            {
                if (itemType == ItemID.None)
                    continue;

                recipeGroup.ValidItems.Add(itemType);
                recipeGroup.ValidItemsLookup?[itemType] = true;
            }

            foreach (int itemType in RemoveItems.Select(item => GetItem(item, Mod)))
            {
                if (itemType == ItemID.None)
                    continue;

                recipeGroup.ValidItems.Remove(itemType);
                recipeGroup.ValidItemsLookup?[itemType] = false;
            }
        }
    }
}

internal sealed class RecipeGroupModEditorWindow : AbstractEditorWindow<RecipeGroupMod, RecipeGroupModEditorWindow.RecipeGroupModElement>
{
    public sealed class RecipeGroupModElement : ModifierEditorElement<RecipeGroupMod>
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

    private sealed class AddItemElement : ModifierEditorElement<string>, IVisitor<RecipeGroupMod>
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

    private sealed class RemoveItemElement : ModifierEditorElement<string>, IVisitor<RecipeGroupMod>
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

    public RecipeGroupModEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(RecipeGroupMod obj)
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

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new AddItemElement();
        yield return new RemoveItemElement();
    }
}