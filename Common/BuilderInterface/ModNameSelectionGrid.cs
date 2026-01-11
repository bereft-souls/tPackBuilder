using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PackBuilder.Common.Project;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class ModNameSelectionGrid : UIPanel
{
    private const int default_step_index = 0;

    private readonly List<GroupOptionButton<int>> buttonsBySorting = [];
    private int currentSelected = -1;
    private readonly List<ModProjectView> projects;

    public event Action? OnClickingOption;

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
        const int num = 2;
        const int num2 = 26 + num;
        var num3 = 0;
        for (var i = 0; i < projects.Count; i++)
        {
            /*
            if (!_sorter.Steps[i].HiddenFromSortOptions)
                num3++;
                */
            num3++;
        }

        var uIPanel = new UIPanel
        {
            Width = new StyleDimension(126f, 0f),
            Height = new StyleDimension(num3 * num2 + 5 + 3, 0f),
            HAlign = 1f,
            VAlign = 0f,
            Left = new StyleDimension(-118f, 0f),
            Top = new StyleDimension(0f, 0f),
            BorderColor = new Color(89, 116, 213, 255) * 0.9f,
            BackgroundColor = new Color(73, 94, 171) * 0.9f,
        };

        uIPanel.SetPadding(0f);
        Append(uIPanel);
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

            var groupOptionButton = new GroupOptionButton<int>(j, Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(project.Properties.DisplayName), null, Color.White, null, 0.8f)
            {
                Width = new StyleDimension(114f, 0f),
                Height = new StyleDimension(num2 - num, 0f),
                HAlign = 0.5f,
                Top = new StyleDimension(5 + num2 * num4, 0f),
                ShowHighlightWhenSelected = false,
            };

            groupOptionButton.OnLeftClick += ClickOption;
            // groupOptionButton.SetSnapPoint("SortSteps", num4);
            uIPanel.Append(groupOptionButton);
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

        OnClickingOption?.Invoke();
    }

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
}
