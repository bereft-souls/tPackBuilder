using System;
using System.Collections.Generic;
using System.IO;
using PackBuilder.Common.Project;
using System.Linq;
using Newtonsoft.Json;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal sealed class ModControlPanelWindow : AbstractInterfaceWindow
{
    private static string CachePath => Path.Combine(Main.SavePath, "PackBuilder", "mod_control_panel.json");

    private sealed class CacheData
    {
        public string? LastOpenedSource { get; set; }
    }

    private ModNameSelectionGrid? projectViewThing;
    private ModNameDropDown? modNameDropDown;
    private UITextPanel<LocalizedText>? editButton;
    private UITextPanel<LocalizedText>? openButton;

    private CacheData cacheData = new();

    public override void OnInitialize()
    {
        base.OnInitialize();

        if (File.Exists(CachePath))
        {
            try
            {
                cacheData = JsonConvert.DeserializeObject<CacheData>(File.ReadAllText(CachePath)) ?? new CacheData();
            }
            catch
            {
                cacheData = new CacheData();
            }
        }

        var resizablePanelButton = new ResizablePanelButton();
        {
            resizablePanelButton.Left.Pixels += PaddingLeft;
            resizablePanelButton.Top.Pixels += PaddingTop;
        }
        Append(resizablePanelButton);

        var containerElement = new UIElement();
        {
            containerElement.Width.Set(0f, 1f);
            containerElement.Height.Set(0f, 1f);
            containerElement.IgnoresMouseInteraction = true;
        }
        Append(containerElement);

        var topBarContainer = new UIGrid();
        {
            topBarContainer.ManualSortMethod = _ => { };
            topBarContainer.Width.Set(0f, 1f);
            topBarContainer.Height.Set(40f, 0f);
            // topBarContainer.IgnoresMouseInteraction = true;
        }
        Append(topBarContainer);

        var projects = ModProjectProvider.ModSourcesViews.ToList();

        const float regular_button_width = 76f;
        const float top_bar_padding = 4f;
        modNameDropDown = new ModNameDropDown(GetProjectFromDirectory(projects, cacheData.LastOpenedSource));
        {
            modNameDropDown.Left.Set(top_bar_padding, 0f);
            modNameDropDown.Width.Set(-top_bar_padding - ((top_bar_padding + regular_button_width) * 2f), 1f);
            modNameDropDown.Height.Set(0f, 1f);
            modNameDropDown.WithFadedMouseOver();
            modNameDropDown.OnLeftClick += OpenOrCloseModNameGrid;
        }
        topBarContainer.Add(modNameDropDown);

        editButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.PackBuilder.UI.Edit"));
        {
            editButton.Width.Set(regular_button_width, 0f);
            editButton.Height.Set(0f, 1f);
            editButton.WithFadedMouseOver();
        }
        topBarContainer.Add(editButton);

        openButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.PackBuilder.UI.Open"));
        {
            openButton.Left.Set(top_bar_padding, 0f);
            openButton.Width.Set(regular_button_width, 0f);
            openButton.Height.Set(0f, 1f);
            openButton.WithFadedMouseOver();
        }
        topBarContainer.Add(openButton);

        projectViewThing = new ModNameSelectionGrid(projects);
        {
            projectViewThing.Width.Set(0f, 1f);
            projectViewThing.Height.Set(0f, 1f);
            projectViewThing.OnLeftClick += ProjectViewThing_OnLeftClick;
            projectViewThing.OnClickingOption += ProjectViewThing_OnClickingOption;

            projectViewThing.Panel?.Width = modNameDropDown.Width;
            projectViewThing.Panel?.Height.Pixels -= topBarContainer.Height.Pixels + 4f;
            projectViewThing.Panel?.Top.Pixels = topBarContainer.Height.Pixels + 4f;
        }
        // Append(projectViewThing);
    }

    private static ModProjectView? GetProjectFromDirectory(List<ModProjectView> projects, string? source)
    {
        if (source is null)
        {
            return null;
        }

        foreach (var project in projects)
        {
            if (project.Directory.Equals(source, StringComparison.OrdinalIgnoreCase))
            {
                return project;
            }
        }

        return null;
    }

    public override void Recalculate()
    {
        base.Recalculate();

        // FIXME: Required to update children's positions properly.
        RecalculateChildren();
    }

    private void ProjectViewThing_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target == projectViewThing)
        {
            CloseModNameGrid();
        }
    }

    private void ProjectViewThing_OnClickingOption(ModProjectView? selectedProject)
    {
        modNameDropDown?.SelectedProject = selectedProject;
        cacheData.LastOpenedSource = selectedProject?.Directory;
        CloseModNameGrid();

        try
        {
            Directory.GetParent(CachePath)?.Create();
            File.WriteAllText(CachePath, JsonConvert.SerializeObject(cacheData));
        }
        catch
        {
            // ignore
        }
    }

    private void OpenOrCloseModNameGrid(UIMouseEvent evt, UIElement listeningElement)
    {
        if (projectViewThing?.Parent is not null)
        {
            CloseModNameGrid();
            return;
        }

        projectViewThing?.Remove();
        Append(projectViewThing);
    }

    private void CloseModNameGrid()
    {
        projectViewThing?.Remove();
    }
}
