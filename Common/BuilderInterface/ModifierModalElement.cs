using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent.UI.Elements;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

public abstract class ModifierModalElement : UIElement
{
    public string? Name { get; protected set; }

    public bool Static { get; protected set; }

    public static string DefaultText => "+0.0";

    protected UIText MakeAndAppendLabel(UIElement anchor, string text)
    {
        var textElement = new UIText(text, textScale: 0.75f);
        {
            textElement.VAlign = anchor.VAlign;
            textElement.Top = anchor.Top;
            textElement.Top.Pixels -= 14f;
            textElement.Left = anchor.Left;
        }
        Append(textElement);
        return textElement;
    }

    protected void BuildFieldLine(ref float offset, params InputField[] fields)
    {
        var horizontalOffset = 0f;
        foreach (var field in fields)
        {
            field.TextScale = 0.66f;
            field.Top.Set(offset, 0f);
            field.Left.Set(horizontalOffset, 0f);
            
            horizontalOffset += field.Width.Pixels + 8f;

            Append(field);
        }

        offset += fields.Max(x => x.Height.Pixels) + 24f;
    }
}

public abstract class ModifierModalElement<T> : ModifierModalElement
{
    public ModifierModalElement(string name, bool @static = false)
    {
        Name = name;
        Static = @static;

        Width.Set(0f, 1f);
        Height.Set(76, 0f);
        MinHeight.Set(76, 0f);
        SetPadding(8f);

        var back = new UIPanel();
        {
            back.Left.Set(-PaddingLeft, 0f);
            back.Top.Set(-PaddingTop, 0f);
            back.Width.Set(PaddingLeft + PaddingRight, 1f);
            back.Height.Set(PaddingTop + PaddingBottom, 1f);
            back.MaxWidth.Set(PaddingLeft + PaddingRight, 1f);
            back.MaxHeight.Set(PaddingTop + PaddingBottom, 1f);
            back.IgnoresMouseInteraction = true;
        }
        Append(back);

        var titleBar = new UIText(name, textScale: 0.85f);
        {
            titleBar.Width.Set(0f, 1f);
            titleBar.Height.Set(12f, 0f);
            titleBar.TextOriginX = 0f;
            titleBar.TextOriginY = 0.33f;
            titleBar.HAlign = 0f;
            titleBar.VAlign = 0f;
            titleBar.IgnoresMouseInteraction = true;
        }
        Append(titleBar);

        if (Static)
        {
            return;
        }

        var upButton = new UIImageButton(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/MoveUpSmall", AssetRequestMode.ImmediateLoad));
        {
            upButton.Left.Set(-upButton.Width.Pixels, 1f);
            upButton.VAlign = 0.5f;
            upButton.Top.Set(-(upButton.Height.Pixels / 2f + 2f), 0f);
            upButton.OnLeftClick += MoveUpInList;
        }
        Append(upButton);

        var downButton = new UIImageButton(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/MoveDownSmall", AssetRequestMode.ImmediateLoad));
        {
            downButton.Left.Set(-downButton.Width.Pixels, 1f);
            // downButton.Top.Set(2f + upButton.Height.Pixels, 0f);
            downButton.VAlign = 0.5f;
            downButton.Top.Set(downButton.Height.Pixels / 2f + 2f, 0f);
            downButton.OnLeftClick += MoveDownInList;
        }
        Append(downButton);

        var trashButton = new UIImageButton(ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/TrashSmall", AssetRequestMode.ImmediateLoad));
        {
            trashButton.Left.Set(upButton.Left.Pixels - trashButton.Width.Pixels - 2f, 1f);
            // trashButton.Top.Set((upButton.Top.Pixels + downButton.Top.Pixels) / 2, 0f);
            trashButton.VAlign = 0.5f;
            trashButton.OnLeftClick += RemoveSelfFromList;
        }
        Append(trashButton);
    }

    private void MoveUpInList(UIMouseEvent evt, UIElement listeningElement)
    {
        if (SwapModifiers(-1))
        {
            listeningElement.IsMouseHovering = false;
        }
    }

    private void MoveDownInList(UIMouseEvent evt, UIElement listeningElement)
    {
        if (SwapModifiers(1))
        {
            listeningElement.IsMouseHovering = false;
        }
    }

    private bool SwapModifiers(int offset)
    {
        if (Parent?.Parent is not UIList list)
        {
            return false;
        }

        var elements = list._innerList.Elements;
        var index = elements.IndexOf(this);
        if (index < 0)
        {
            return false;
        }

        var swapIndex = index + offset;
        if (swapIndex < 0 || swapIndex >= list.Count)
        {
            return false;
        }

        if (elements[swapIndex] is ModifierModalElement { Static: true })
        {
            return false;
        }

        (elements[index], elements[swapIndex]) = (elements[swapIndex], elements[index]);
        list._items.Clear();
        list._items.AddRange(elements);
        list.UpdateOrder();
        list._innerList.Recalculate();
        return true;
    }

    private void RemoveSelfFromList(UIMouseEvent evt, UIElement listeningElement)
    {
        if (Parent?.Parent is UIList list)
        {
            list.Remove(this);
            listeningElement.IsMouseHovering = false;
        }
    }

    public abstract T CreateObject();

    public abstract void Populate(T obj);
}
