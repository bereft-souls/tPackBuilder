using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.Project;
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

            MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
            MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);
        }
    }

    private Vector2 textSize;
    private Color color;

    public ModNameDropDown(ModProjectView? project)
    {
        Text = Language.GetText("Mods.PackBuilder.UI.SelectMod");
        color = AbsentColor;

        SelectedProject = project;
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
    private int currentSelected = -1;
    private readonly List<ModProjectView> projects;

    public event Action<ModProjectView?>? OnClickingOption;

    public ModNameSelectionGrid(List<ModProjectView> projects)
    {
        this.projects = projects;

        Width.Set(0f, 1f);
        Height.Set(0f, 1f);
        BackgroundColor = new Color(35, 40, 83) * 0.5f;
        BorderColor = new Color(35, 40, 83) * 0.5f;
        IgnoresMouseInteraction = false;
        SetPadding(0f);
        BuildGrid();
    }

    private void BuildGrid()
    {
        var backPanel = new UIPanel();
        {
            // backPanel.Width.Set(0f, 2f / 3f);
            // backPanel.Height.Set(num3 * num2 + 5 + 3, 0f);
            backPanel.Width.Set(0f, 1f);
            backPanel.Height.Set(0f, 1f);
            backPanel.HAlign = 1f;
            backPanel.VAlign = 0f;
            backPanel.Left.Set(-118f, 0f);
            backPanel.Top.Set(0f, 0f);
            backPanel.BorderColor = new Color(89, 116, 213, 255) * 0.9f;
            backPanel.BackgroundColor = new Color(73, 94, 171) * 0.9f;
            backPanel.SetPadding(8f);
        }
        Append(backPanel);

        var scrollbar = new UIScrollbar();
        {
            const float stupid_padding_to_fix_scrollbar_overflow = 4f;
            scrollbar.Height.Set(-stupid_padding_to_fix_scrollbar_overflow * 2f, 1f);
            scrollbar.Top.Set(stupid_padding_to_fix_scrollbar_overflow, 0f);
            scrollbar.HAlign = 1f;
        }
        backPanel.Append(scrollbar);

        var listContainer = new UIElement();
        {
            var offset = scrollbar.Width.Pixels + 4f;
            // listContainer.Left.Set(offset, 0f);
            listContainer.Width.Set(-offset, 1f);
            listContainer.Height.Set(0f, 1f);
        }
        backPanel.Append(listContainer);

        var list = new UIList();
        {
            list.Width.Set(0f, 1f);
            list.Height.Set(0f, 1f);
            list.SetPadding(0f);
            list.SetScrollbar(scrollbar);
            list.ManualSortMethod = _ => { };
        }
        listContainer.Append(list);

        var num4 = 0;
        for (var j = 0; j < projects.Count; j++)
        {
            /*
            if (!bestiarySortStep.HiddenFromSortOptions)
            {
                GroupOptionButton<int> groupOptionButton = new GroupOptionButton<int>(j, Language.GetText(bestiarySortStep.GetDisplayNameKey()), null, Color.White, null, 0.8f)
                {
                    Width = new StyleDimension(114f, 0f),
                    Height = new StyleDimension(num2 - num, 0f),
                    HAlign = 0.5f,
                    Top = new StyleDimension(5 + num2 * num4, 0f)
                };

                groupOptionButton.ShowHighlightWhenSelected = false;
                groupOptionButton.OnLeftClick += ClickOption;
                groupOptionButton.SetSnapPoint("SortSteps", num4);
                uIPanel.Append(groupOptionButton);
                _buttonsBySorting.Add(groupOptionButton);
                num4++;
            }
            */

            var project = projects[j];

            var groupOptionButton = new GroupOptionButton<int>(j, Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(project.InternalName), null, Color.White, null, 0.8f)
            {
                Width = new StyleDimension(0f, 1f),
                Height = new StyleDimension(30f, 0f),
                HAlign = 0.5f,
                // Top = new StyleDimension(5 + num2 * num4, 0f),
                ShowHighlightWhenSelected = false,
            };

            groupOptionButton.OnLeftClick += ClickOption;
            // groupOptionButton.SetSnapPoint("SortSteps", num4);
            list.Add(groupOptionButton);
            buttonsBySorting.Add(groupOptionButton);
            num4++;
        }

        foreach (var item in buttonsBySorting)
        {
            item.SetCurrentOption(-1);
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
            button.SetCurrentOption(theSame ? idx : -1);

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
        // sorter.SetPrioritizedStepIndex(idx);

        OnClickingOption?.Invoke(idx == -1 ? null : projects[idx]);
    }

    /*
    public void GetEntriesToShow(
        out int maxEntriesWidth,
        out int maxEntriesHeight,
        out int maxEntriesToHave
    )
    {
        maxEntriesWidth = 1;
        maxEntriesHeight = buttonsBySorting.Count;
        maxEntriesToHave = buttonsBySorting.Count;
    }
    */
}
