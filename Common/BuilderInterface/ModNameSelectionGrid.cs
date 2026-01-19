using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.Project;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class ModNameDropDown : UIPanel
{
    private static Color AbsentColor => Color.Gray;

    private static Color PresentColor => Color.White;

    public ModProjectView? SelectedProject
    {
        get;

        set
        {
            field = value;

            if (value.HasValue)
            {
                Text = Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(value.Value.InternalName);
                color = PresentColor;
            }
            else
            {
                Text = Language.GetText("Mods.PackBuilder.UI.SelectMod");
                color = AbsentColor;
            }
        }
    }

    public float TextHAlign { get; set; } = 0f;

    public LocalizedText Text
    {
        get;

        set
        {
            field = value;

            var font = FontAssets.MouseText.Value;
            textSize = ChatManager.GetStringSize(font, value.ToString(), Vector2.One);
            textSize.Y = 16f;

            /*
            MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
            MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);
            */
        }
    }

    private Vector2 textSize;
    private Color color;

    public ModNameDropDown(ModProjectView? project)
    {
        Text = Language.GetText("Mods.PackBuilder.UI.SelectMod");
        color = AbsentColor;

        SelectedProject = project;
        OverflowHidden = true;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        DrawText(spriteBatch);
    }

    private void DrawText(SpriteBatch sb)
    {
        var innerDims = GetInnerDimensions();
        var pos = innerDims.Position();
        pos.Y -= 2f;
        pos.X += (innerDims.Width - textSize.X) * TextHAlign;

        var text = Text.ToString();
        Utils.DrawBorderString(sb, text, pos, color);
    }
}

internal sealed class ModNameSelectionGrid : UIPanel
{
    private const int default_step_index = -1;

    private readonly List<GroupOptionButton<int>> buttonsBySorting = [];
    private int currentSelected = default_step_index;
    private readonly List<ModProjectView> projects;

    public event Action<ModProjectView?>? OnClickingOption;

    public UIPanel? Panel { get; private set; }

    public ModNameSelectionGrid(List<ModProjectView> projects, ModProjectView? selectedProject)
    {
        this.projects = projects;

        if (selectedProject.HasValue)
        {
            currentSelected = projects.IndexOf(selectedProject.Value);
        }

        BackgroundColor = new Color(35, 40, 83) * 0.5f;
        BorderColor = new Color(35, 40, 83) * 0.5f;
        IgnoresMouseInteraction = false;
        SetPadding(0f);
        BuildGrid();
    }

    private void BuildGrid()
    {
        Panel = new UIPanel();
        {
            // backPanel.Width.Set(0f, 2f / 3f);
            // backPanel.Height.Set(num3 * num2 + 5 + 3, 0f);
            Panel.Width.Set(0f, 1f);
            Panel.Height.Set(0f, 1f);
            // Panel.HAlign = 1f;
            // Panel.VAlign = 0f;
            // Panel.Left.Set(-118f, 0f);
            // Panel.Top.Set(0f, 0f);
            Panel.BorderColor = new Color(89, 116, 213, 255) * 0.9f;
            Panel.BackgroundColor = new Color(73, 94, 171) * 0.9f;
            Panel.SetPadding(8f);
        }
        Append(Panel);

        var scrollbar = new UIScrollbar();
        {
            const float stupid_padding_to_fix_scrollbar_overflow = 4f;
            scrollbar.Height.Set(-stupid_padding_to_fix_scrollbar_overflow * 2f, 1f);
            scrollbar.Top.Set(stupid_padding_to_fix_scrollbar_overflow, 0f);
            scrollbar.HAlign = 1f;
        }
        Panel.Append(scrollbar);

        var listContainer = new UIElement();
        {
            var offset = scrollbar.Width.Pixels + 4f;
            // listContainer.Left.Set(offset, 0f);
            listContainer.Width.Set(-offset, 1f);
            listContainer.Height.Set(0f, 1f);
        }
        Panel.Append(listContainer);

        var list = new UIList();
        {
            list.Width.Set(0f, 1f);
            list.Height.Set(0f, 1f);
            list.SetPadding(0f);
            list.SetScrollbar(scrollbar);
            list.ManualSortMethod = _ => { };
        }
        listContainer.Append(list);

        for (var i = 0; i < projects.Count; i++)
        {
            var project = projects[i];

            var groupOptionButton = new GroupOptionButton<int>(i, Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(project.InternalName), null, Color.White, null, 0.8f)
            {
                Width = new StyleDimension(0f, 1f),
                Height = new StyleDimension(30f, 0f),
                HAlign = 0.5f,
                ShowHighlightWhenSelected = false,
            };
            groupOptionButton.OnLeftClick += ClickOption;
            list.Add(groupOptionButton);
            buttonsBySorting.Add(groupOptionButton);
        }

        foreach (var item in buttonsBySorting)
        {
            var theSame = currentSelected == item.OptionValue;
            item.SetCurrentOption(theSame ? currentSelected : default_step_index);

            if (theSame)
            {
                item.SetColor(new Color(152, 175, 235), 1f);
            }
            else
            {
                item.SetColor(Colors.InventoryDefaultColor, 0.7f);
            }
        }
    }

    private void ClickOption(UIMouseEvent evt, UIElement listeningElement)
    {
        if (listeningElement is not GroupOptionButton<int> option)
        {
            return;
        }

        var idx = option.OptionValue;
        if (idx == currentSelected)
        {
            idx = default_step_index;
        }

        foreach (var button in buttonsBySorting)
        {
            var theSame = idx == button.OptionValue;
            button.SetCurrentOption(theSame ? idx : default_step_index);

            if (theSame)
            {
                button.SetColor(new Color(152, 175, 235), 1f);
            }
            else
            {
                button.SetColor(Colors.InventoryDefaultColor, 0.7f);
            }
        }

        currentSelected = idx;

        OnClickingOption?.Invoke(idx == default_step_index ? null : projects[idx]);
    }
}
