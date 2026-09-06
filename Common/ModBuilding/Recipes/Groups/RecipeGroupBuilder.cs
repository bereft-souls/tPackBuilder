using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ID;

namespace PackBuilder.Common.ModBuilding.Recipes.Groups;

public sealed class RecipeGroupBuilder : PackBuilderType
{
    [JsonRequired]
    public string Name { get; set; }
    [JsonRequired]
    public string LocalizationKey { get; set; }
    public List<string> Items = [];

    public override string? LoadingMethod => nameof(ModSystem.AddRecipeGroups);

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Recipes", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new RecipeGroupBuilderEditorWindow(element, state);
    }

    public override void Load()
    {
        int[] items = Items.Select(item => GetItem(item, Mod)).Where(itemType => itemType != ItemID.None).ToArray();
        RecipeGroup.RegisterGroup(Name, new RecipeGroup(() => Language.GetOrRegister(LocalizationKey).Value, items));
    }
}

internal sealed class RecipeGroupBuilderEditorWindow : AbstractEditorWindow<RecipeGroupBuilder, RecipeGroupBuilderEditorWindow.RecipeGroupBuilderElement>
{
    public sealed class RecipeGroupBuilderElement : ModifierEditorElement<RecipeGroupBuilder>
    {
        private readonly InputField nameInput;
        private readonly InputField localizationKeyInput;

        public RecipeGroupBuilderElement() : base("Recipe Group Builder", @static: true)
        {
            nameInput = new InputField("InternalName");
            {
                nameInput.TextScale = 0.66f;
                nameInput.Left.Set(0f, 0f);
                // amountInput.VAlign = 1f;
                nameInput.Width.Set(90f, 0f);
                nameInput.Height.Set(24f, 0f);
                nameInput.Top.Set(-nameInput.Height.Pixels, 1f);
            }
            Append(nameInput);
            MakeAndAppendLabel(nameInput, "Name");

            localizationKeyInput = new InputField("Display Name");
            {
                localizationKeyInput.TextScale = 0.66f;
                localizationKeyInput.Left.Set(nameInput.Width.Pixels + 2f, 0f);
                // chanceInput.VAlign = 1f;
                localizationKeyInput.Width.Set(90f, 0f);
                localizationKeyInput.Height.Set(24f, 0f);
                localizationKeyInput.Top.Set(-localizationKeyInput.Height.Pixels, 1f);
            }
            Append(localizationKeyInput);
            MakeAndAppendLabel(localizationKeyInput, "Localization Key");
        }

        public override RecipeGroupBuilder CreateObject()
        {
            return new RecipeGroupBuilder
            {
                Name = nameInput.Text,
                LocalizationKey = localizationKeyInput.Text,
            };
        }

        public override void Populate(RecipeGroupBuilder obj)
        {
            nameInput.Text = obj.Name ?? "";
            localizationKeyInput.Text = obj.LocalizationKey ?? "";
        }
    }

    private sealed class AddItemElement : ModifierEditorElement<string>, IVisitor<RecipeGroupBuilder>
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

        public void Visit(RecipeGroupBuilder obj)
        {
            obj.Items.Add(CreateObject());
        }
    }

    public RecipeGroupBuilderEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(element, state) { }

    protected override IEnumerable<ModifierEditorElement> DeriveModifiers(RecipeGroupBuilder obj)
    {
        yield return CreateAndPopulate<RecipeGroupBuilderElement, RecipeGroupBuilder>(obj);

        foreach (var item in obj.Items)
        {
            yield return CreateAndPopulate<AddItemElement, string>(item);
        }
    }

    protected override IEnumerable<ModifierEditorElement> GetAvailableModifiers()
    {
        yield return new AddItemElement();
    }
}