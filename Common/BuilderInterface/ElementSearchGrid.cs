using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.BuilderInterface.Windows;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

public abstract class GridEntry : UIImageButton
{
    private sealed class ContainerElementPurelyForDraw : UIElement
    {
        public Action<UIElement> RealOnDraw;

        public ContainerElementPurelyForDraw()
        {
            OverrideSamplerState = SamplerState.PointClamp;
            UseImmediateMode = true;
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            RealOnDraw?.Invoke(this);
        }
    }

    public GridEntry() : base(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EntityEntryPanel", AssetRequestMode.ImmediateLoad))
    {
        SetVisibility(1f, 1f);
        SetHoverImage(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EntityEntryPanelHighlight", AssetRequestMode.ImmediateLoad));

        OverflowHidden = true;
        Width.Set(30, 0f);
        Height.Set(30, 0f);
        var container = new ContainerElementPurelyForDraw();
        {
            container.Left.Set(2f, 0f);
            container.Top.Set(2f, 0f);
            container.Width.Set(-4f, 1f);
            container.Height.Set(-4f, 1f);
            container.RealOnDraw += DrawEntry;
            container.OverflowHidden = true;
        }
        Append(container);
    }

    public virtual void DrawEntry(UIElement affectedElement) { }
}

internal sealed class ElementSearchGrid<T> : AbstractInterfaceWindow where T : UIElement
{
    private List<T> cachedItems;

    private bool mouseLeftLastFrame;

    public ElementSearchGrid(int entriesX, int entriesY, BuilderInterfaceState state) : base(state)
    {
        State = state;
        const int element_size = 30;
        Width.Set(element_size * (entriesX + 1) + 28f, 0f);
        Height.Set(element_size * entriesY + 32f, 0f);

        BorderColor = new Color(89, 116, 213, 255) * 0.9f;
        BackgroundColor = new Color(73, 94, 171) * 0.9f;
        SetPadding(6f);

        SearchBar = new InputField(Language.GetTextValue("Mods.PackBuilder.UI.ElementNamePrompt"));
        SearchBar.Width.Set(0f, 1f);
        SearchBar.Height.Set(20f, 0f);
        SearchBar.TextScale = 0.8f;
        SearchBar.OnTextChanged += FilterItems;
        Append(SearchBar);

        Grid = new NestedUIGrid();
        Grid.SetPadding(0f);
        Grid.Top.Set(SearchBar.Top.Pixels + SearchBar.Height.Pixels + 4f, 0f);
        Grid.Width.Set(-24f, 1f);
        Grid.Height.Set(-Grid.Top.Pixels, 1f);
        Grid.ManualSortMethod = _ => { };
        Append(Grid);

        var scrollbar = new UIScrollbar();
        {
            scrollbar.Height.Set(-Grid.Top.Pixels - 8f, 1f);
            scrollbar.Top.Set(Grid.Top.Pixels + 4f, 0f);
            scrollbar.Left.Set(-18f, 1f);
        }
        Append(scrollbar);

        cachedItems = [];
        Grid.SetScrollbar(scrollbar);
    }

    public UIGrid Grid { get; }

    public InputField SearchBar { get; }

    public event Func<T, string, bool>? FitsFilter;
    public event Action OnSubmit;

    private void FilterItems(InputField obj, string oldText, string newText)
    {
        Grid.Clear();
        Grid.AddRange(cachedItems.Where(n => obj.Text == string.Empty || (FitsFilter?.Invoke(n, obj.Text) ?? true)));
        RecalculateChildren();
    }

    public void Populate(List<T> items)
    {
        cachedItems = items;
        Grid.AddRange(items);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (!IsMouseHovering && Main.mouseLeft && !mouseLeftLastFrame)
        {
            QueueCloseWindow();
        }

        mouseLeftLastFrame = Main.mouseLeft;
    }

    public void Submit()
    {
        OnSubmit?.Invoke();
    }

    public void Close()
    {
        State.CloseWindow(this);
    }
}
