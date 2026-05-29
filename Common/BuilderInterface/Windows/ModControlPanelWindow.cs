using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PackBuilder.Common.ModBuilding;
using PackBuilder.Common.Project;
using PackBuilder.Common.Project.IO.Browsing;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.Social.Steam;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal sealed class ModControlPanelWindow(BuilderInterfaceState state) : AbstractInterfaceWindow(state)
{
    private sealed class AddFileDropDown : UIPanel
    {
        public AddFileDropDown()
        {
            VAlign = 1f;

            BackgroundColor = new Color(35, 40, 83) * 0.5f;
            BorderColor = new Color(35, 40, 83) * 0.5f;
            IgnoresMouseInteraction = false;
            SetPadding(0f);

            BuildList();
        }

        public UIPanel? Panel { get; private set; }

        public event Action<PackBuilderType>? OnSelected;

        private void BuildList()
        {
            var types = PackBuilderType.ExtensionsToTypes.Values.ToArray();

            Panel = new UIPanel();
            {
                Panel.Width.Set(300f, 0f);
                Panel.Height.Set(-28f, 1f);

                Panel.Left.Set(-Panel.Width.Pixels, 1f);
                Panel.Top.Set(28f, 0f);
                Panel.BorderColor = new Color(89, 116, 213, 255) * 0.9f;
                Panel.BackgroundColor = new Color(73, 94, 171) * 0.9f;
                Panel.SetPadding(8f);
            }
            Append(Panel);

            var scrollbar = new UIScrollbar();
            {
                scrollbar.Height.Set(-8f, 1f);
                scrollbar.Top.Set(4f, 0f);
                scrollbar.Left.Set(-16f, 1f);
            }

            var list = new UIList();
            {
                list.Width.Set(-scrollbar.Width.Pixels, 1f);
                list.Height.Set(0f, 1f);
                list.SetPadding(0f);
                list.SetScrollbar(scrollbar);
                list.ManualSortMethod = _ => { };
            }
            Panel.Append(list);
            Panel.Append(scrollbar);

            foreach (var type in types)
            {
                var button = new GroupOptionButton<PackBuilderType>(type, Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(type.GetType().Name), null, Color.White, null, 0.8f);
                {
                    button.Width.Set(0f, 1f);
                    button.Height.Set(24f, 0f);
                    button.HAlign = 0.5f;
                    button.ShowHighlightWhenSelected = false;
                    button.OnLeftClick += SelectOption;
                }
                list.Add(button);
            }
        }

        private void SelectOption(UIMouseEvent evt, UIElement listeningElement)
        {
            if (listeningElement is not GroupOptionButton<PackBuilderType> option)
            {
                return;
            }

            OnSelected?.Invoke(option.OptionValue);
        }
    }

    private sealed class CacheData
    {
        public string? LastOpenedSource { get; set; }
    }

    private static readonly Dictionary<string, (Asset<Texture2D>? Icon, long LastUpdated)> icon_cache = [];

    private readonly object fgLock = new();
    private UIImageButton? addButton;
    private UIImageButton? buildButton;

    private CacheData cacheData = new();
    private AddFileDropDown? currentDropdown;
    private bool delayedListRefresh;
    private DirectoryDisplay? directoryDisplay;
    private UIImageButton? editButton;
    private FileGraph? fileGraph;
    private string? fileToFocusOn;
    private int iconUpdateTimer;

    private UIList? modifiersList;
    private ModNameDropDown? modNameDropDown;
    private bool needsModifierListRefresh;
    private UIImageButton? openButton;

    private ModNameSelectionGrid? projectViewThing;

    private DirectoryInfo? selectedDirectory;

    private static string CachePath => Path.Combine(Main.SavePath, "PackBuilder", "mod_control_panel.json");

    public event Action<ModProjectView>? OnEdit;

    public override void OnInitialize()
    {
        base.OnInitialize();

        // Cache
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

        // Base settings
        BackgroundColor = new Color(33, 43, 79) * 0.8f;

        Width.Set(800f, 0f);
        Height.Set(500f, 0f);
        MinWidth.Set(400f, 0f);
        MinHeight.Set(200f, 0f);

        // Container
        var containerElement = new UIElement();
        {
            containerElement.Width.Set(0f, 1f);
            containerElement.Height.Set(0f, 1f);
            containerElement.IgnoresMouseInteraction = true;
        }
        Append(containerElement);

        // Top bar
        var topBarContainer = new UIGrid();
        {
            topBarContainer.ManualSortMethod = _ => { };
            topBarContainer.Width.Set(0f, 1f);
            topBarContainer.Height.Set(28f, 0f);
            // topBarContainer.IgnoresMouseInteraction = true;
        }
        Append(topBarContainer);

        var projects = ModProjectProvider.ModSourcesViews.ToList();
        var selectedProject = GetProjectFromDirectory(projects, cacheData.LastOpenedSource);

        const float regular_button_width = 28f;
        const float top_bar_padding = 4f;
        modNameDropDown = new ModNameDropDown(selectedProject);
        {
            modNameDropDown.Width.Set(0f, 1f);
            modNameDropDown.Height.Set(0f, 1f);
            modNameDropDown.WithFadedMouseOver();
            modNameDropDown.OnLeftClick += OpenOrCloseModNameGrid;
            modNameDropDown.OnProjectSelected += OnProjectSelected;
        }
        directoryDisplay = new DirectoryDisplay(modNameDropDown);
        {
            directoryDisplay.Left.Set(top_bar_padding, 0f);
            directoryDisplay.Width.Set(-top_bar_padding - (top_bar_padding + regular_button_width) * 4f, 1f);
            directoryDisplay.Height.Set(0f, 1f);
        }
        topBarContainer.Add(directoryDisplay);

        buildButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/BuildMedium", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Build")
        );
        {
            buildButton.OnLeftClick += (_, _) =>
            {
                // TODO: Some logging?
                if (modNameDropDown?.SelectedProject is not { } project)
                {
                    return;
                }

                SteamedWraps.StopPlaytimeTracking();
                Main.menuMode = 10;
                Main.gameMenu = true;
                WorldGen.SaveAndQuit(
                    () =>
                    {
                        Interface.buildMod.Build(project.Directory, reload: true);
                    }
                );
            };
        }
        topBarContainer.Add(buildButton);

        addButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/AddMedium", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Add")
        );
        {
            addButton.OnLeftClick += AddButton_OnLeftClick;
        }
        topBarContainer.Add(addButton);

        editButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/EditMedium", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Edit")
        );
        {
            editButton.OnLeftClick += EditButton_OnLeftClick;
        }
        topBarContainer.Add(editButton);

        openButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/OpenMedium", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Open")
        );
        {
            openButton.OnLeftClick += OpenButton_OnLeftClick;
        }
        topBarContainer.Add(openButton);

        var yOffset = topBarContainer.Height.Pixels;

        // Divider
        yOffset += 4f;
        var divider1 = new UIHorizontalSeparator();
        {
            divider1.Width = StyleDimension.FromPixelsAndPercent(0f, 1f);
            divider1.HAlign = 0.5f;
            divider1.Top = StyleDimension.FromPixels(yOffset);
            divider1.Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f;
        }
        Append(divider1);

        // Main window

        yOffset += 8f;
        modifiersList = new UIList();
        {
            modifiersList.Top.Set(yOffset, 0f);
            modifiersList.Width.Set(0f, 1f);
            modifiersList.Height.Set(-modifiersList.Top.Pixels, 1f);
        }
        Append(modifiersList);

        var scrollbar = new UIScrollbar();
        {
            scrollbar.Top.Set(yOffset + 6f, 0f);
            scrollbar.Height.Set(-modifiersList.Top.Pixels - 18f + 6f, 1f);
            scrollbar.Left.Set(-18f, 1f);
        }
        Append(scrollbar);
        modifiersList.SetScrollbar(scrollbar);
        modifiersList.Width.Set(-scrollbar.Width.Pixels, 1f);

        // UpdateModifiersList();
        OnProjectSelected(selectedProject);

        projectViewThing = new ModNameSelectionGrid(projects, selectedProject);
        {
            projectViewThing.Width.Set(0f, 1f);
            projectViewThing.Height.Set(0f, 1f);
            projectViewThing.OnLeftClick += ProjectViewThing_OnLeftClick;
            projectViewThing.OnClickingOption += ProjectViewThing_OnClickingOption;

            projectViewThing.Panel?.Width = modNameDropDown.Width;
            projectViewThing.Panel?.MaxWidth = new StyleDimension(300f, 0f);
            projectViewThing.Panel?.Height.Pixels -= topBarContainer.Height.Pixels + 4f;
            projectViewThing.Panel?.Top.Pixels = topBarContainer.Height.Pixels + 4f;
        }
        // Append(projectViewThing);

        // Resize corner
        var resizablePanelButton = new ResizablePanelButton();
        {
            resizablePanelButton.Left.Pixels += PaddingLeft;
            resizablePanelButton.Top.Pixels += PaddingTop;
        }
        Append(resizablePanelButton);
    }

    private void OnProjectSelected(ModProjectView? obj)
    {
        if (obj is { } project)
        {
            RefreshFileGraph(project.Directory);
            // UpdateModifiersList(selectedDirectory);
        }
        else
        {
            fileGraph?.Dispose();
            UpdateModifiersList(null);
        }
    }

    private void RefreshFileGraph(string baseDirectory)
    {
        fileGraph?.Dispose();
        fileGraph = new FileGraph(baseDirectory, PackBuilderType.Extensions.Values.Select(x => x + ".json").ToArray());
        {
            // Add new files to UI
            fileGraph.FileAdded += entry =>
            {
                if (!ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };

            // Remove old files from UI
            fileGraph.FileRemoved += entry =>
            {
                if (!ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };

            // Update modified files in UI, not too important
            fileGraph.FileChanged += entry =>
            {
                if (!ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };

            // Update renamed files in UI, listen for both sides
            fileGraph.FileRenamed += (oldPath, entry) =>
            {
                if (!ListeningToEntry(oldPath) && !ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };

            // Add new directories to UI
            fileGraph.DirectoryAdded += entry =>
            {
                if (!ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };

            // Remove old directories from UI ALSO if we're in the directory,
            // then move to the nearest still-extant directory
            fileGraph.DirectoryRemoved += entry =>
            {
                if (IsSubdirOf(baseDirectory, entry.FullPath))
                {
                    // TODO: fileGraph can be null despite Roslyn thinking
                    //       otherwise
                    selectedDirectory = new DirectoryInfo(FindNearestExistingDirectory(entry.FullPath, fileGraph.Root.FullPath));
                }
                else if (!ListeningToEntry(entry))
                {
                    return;
                }

                lock (fgLock)
                {
                    needsModifierListRefresh = true;
                }
            };
        }

        selectedDirectory = new DirectoryInfo(baseDirectory);
        UpdateModifiersList(selectedDirectory);
    }

    private bool ListeningToEntry(FileSystemEntry entry)
    {
        return entry.Parent is { } parent
            && ListeningToEntry(parent.FullPath);
    }

    private bool ListeningToEntry(string path)
    {
        return selectedDirectory is not null
            && PathsEqual(path, selectedDirectory.FullName);
    }

    private static bool IsSubdirOf(string parent, string child)
    {
        parent = Path.GetFullPath(parent);
        child = Path.GetFullPath(child);

        var relative = Path.GetRelativePath(parent, child);
        return relative != "."
            && !relative.StartsWith("..", StringComparison.Ordinal)
            && !Path.IsPathRooted(relative);
    }

    private static string FindNearestExistingDirectory(string path, string root)
    {
        path = Path.GetFullPath(path);
        root = Path.GetFullPath(root)
                   .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        while (true)
        {
            if (Directory.Exists(path))
            {
                return path;
            }

            if (string.Equals(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), root, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return root;
            }

            var parent = Directory.GetParent(path);
            if (parent is null)
            {
                return root;
            }

            path = parent.FullName;
            if (!IsSubdirOf(root, path) && !PathsEqual(root, path))
            {
                return root;
            }
        }
    }

    private static bool PathsEqual(string a, string b)
    {
        return string.Equals(
            Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),
            OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal
        );
    }

    private static readonly string[] unwanted_directories = ["Properties/", "Localization/"];

    private static bool IsIgnored(string relativePath, List<string> ignoredPaths)
    {
        if (relativePath.StartsWith('.'))
        {
            return true;
        }

        if (relativePath.StartsWith("bin/") || relativePath.StartsWith("obj/"))
        {
            return true;
        }

        return ignoredPaths.Any(x => FitsMask(relativePath, x));

        static bool FitsMask(string path, string mask)
        {
            var pattern =
                '^' +
                Regex.Escape(mask.Replace(".", "__DOT__")
                                 .Replace("*", "__STAR__")
                                 .Replace("?", "__QM__"))
                     .Replace("__DOT__", "[.]")
                     .Replace("__STAR__", ".*")
                     .Replace("__QM__", ".")
              + '$';
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(path);
        }
    }

    private static bool IsUnwanted(string relativePath)
    {
        return unwanted_directories.Any(relativePath.StartsWith);
    }

    private void UpdateModifiersList(DirectoryInfo? directory)
    {
        if (modifiersList is null)
        {
            return;
        }

        // definitely know we're rebuilding it
        modifiersList.Clear();

        // probably never happens I hope tbh ngl
        if (directory is null || fileGraph is null)
        {
            return;
        }

        var hideIgnored = ModContent.GetInstance<ClientConfig>().HideIgnoredDirectories;
        var hideUnwanted = ModContent.GetInstance<ClientConfig>().HideUnwantedDirectories;
        var hideAny = hideIgnored || hideUnwanted;

        var root = fileGraph.Root.FullPath;
        var relative = Path.GetRelativePath(root, directory.FullName);
        selectedDirectory = directory;
        directoryDisplay?.Directory = relative == "." ? string.Empty : relative.Replace('\\', '/');

        // ..
        if (!PathsEqual(root, directory.FullName))
        {
            modifiersList.Add(new ReturnDirectoryElement(this));
        }

        // directories
        foreach (var entry in directory.EnumerateDirectories())
        {
            if (hideAny)
            {
                var relPath = Path.GetRelativePath(root, entry.FullName).Replace('\\', '/');
                if (!relPath.EndsWith('/'))
                {
                    relPath += '/';
                }

                if (hideIgnored && IsIgnored(relPath, modNameDropDown?.SelectedProject?.Project.Manifest.IgnoredBuildPaths ?? []))
                {
                    continue;
                }
                
                if (hideUnwanted && IsUnwanted(relPath))
                {
                    continue;
                }
            }
            modifiersList.Add(new DirectoryElement(this, entry));
        }

        // packbuilder files
        foreach (var file in directory.EnumerateFiles())
        {
            if (!fileGraph.TryGetCompatibleExtension(file.Name, out var ext))
            {
                continue;
            }

            ext = ext.ToLowerInvariant();
            if (!PackBuilderType.ExtensionsToTypes.TryGetValue(Path.GetFileNameWithoutExtension(ext).Trim('.'), out var type))
            {
                continue;
            }

            // TODO: gross project access but should be safe
            modifiersList.Add(new ModifierElement(this, modNameDropDown!.SelectedProject!.Value, file, type));
        }
    }

    public void OpenUp()
    {
        var parent = selectedDirectory?.Parent;
        if (parent is null)
        {
            return;
        }

        Utils.OpenFolder(parent.FullName);
    }

    public void NavigateUp()
    {
        if (selectedDirectory is null)
        {
            return;
        }

        selectedDirectory = selectedDirectory.Parent;
        UpdateModifiersList(selectedDirectory);
    }

    // TODO: no path sanitization, but only called by our code so whatever
    public void NavigateTo(DirectoryInfo directory)
    {
        selectedDirectory = directory;
        UpdateModifiersList(directory);
    }

    public override void Update(GameTime gameTime)
    {
        if (delayedListRefresh)
        {
            delayedListRefresh = false;

            if (fileToFocusOn is not null && modifiersList is not null)
            {
                var focus = modifiersList._items.FirstOrDefault(x => x is ModifierElement e && e.IsFocus(fileToFocusOn)) as ModifierElement;
                if (focus is not null)
                {
                    modifiersList.Goto(x => x == focus);
                    focus.EditName();
                }

                fileToFocusOn = null;
            }
        }

        lock (fgLock)
        {
            if (needsModifierListRefresh)
            {
                UpdateModifiersList(selectedDirectory);
                needsModifierListRefresh = false;
                delayedListRefresh = true;
            }
        }

        base.Update(gameTime);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        var shouldUpdateIcon = ++iconUpdateTimer >= 60;
        if (shouldUpdateIcon)
        {
            iconUpdateTimer = 0;
        }

        if (modNameDropDown?.SelectedProject is { } project)
        {
            var dir = project.Directory;
            var icon = GetOrUpdateIcon(Path.Combine(dir, "icon.png"), shouldUpdateIcon, out var error);
            var dims = GetDimensions();
            var iconPos = dims.Position() - new Vector2(80f, 0f) - new Vector2(8f, 0f);
            spriteBatch.Draw(icon.Value, iconPos, Color.White);

            if (error is not null)
            {
                var centerTextPos = iconPos + new Vector2(40f, 40f);
                var textScale = new Vector2(0.8f);
                var textSize = FontAssets.MouseText.Value.MeasureString(error);
                var textOrigin = (textSize / 2f).Floor();
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, error, centerTextPos, Color.White, 0f, textOrigin, textScale);
            }

            var displayName = project.Properties.DisplayName;
            var version = project.Properties.Version;
            var authors = project.Properties.Author;

            var textPos = dims.Position() - new Vector2(8f, 0f) + new Vector2(0f, 88f);
            DrawRightAlignedText(spriteBatch, $"{displayName} v{version}", ref textPos);
            DrawRightAlignedText(spriteBatch, Language.GetTextValue("Mods.PackBuilder.UI.AuthorBy", authors), ref textPos);
        }

        return;

        static void DrawRightAlignedText(SpriteBatch sb, string text, ref Vector2 position)
        {
            var fontSize = new Vector2(0.8f);
            var textSize = FontAssets.MouseText.Value.MeasureString(text);
            var textOrigin = new Vector2(textSize.X, 0f).Floor();
            ChatManager.DrawColorCodedStringWithShadow(sb, FontAssets.MouseText.Value, text, position, Color.White, 0f, textOrigin, fontSize);

            position.Y += textSize.Y * fontSize.Y;
        }
    }

    private static Asset<Texture2D> GetOrUpdateIcon(string path, bool periodicUpdate, out string? error)
    {
        error = null;

        // Not yet cached
        if (!icon_cache.TryGetValue(path, out var cached))
        {
            goto UpdateCache;
        }

        // Valid cache
        if (!periodicUpdate && cached.Icon is not null)
        {
            return cached.Icon;
        }

        // Update cache
    UpdateCache:
        if (!File.Exists(path))
        {
            error = "Missing icon.png";
            icon_cache[path] = (null, 0);
            return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/DefaultModIcon", AssetRequestMode.ImmediateLoad);
        }

        // Assume cache is fine if file hasn't been modified since it was found.
        var lastUpdated = File.GetLastWriteTimeUtc(path).Ticks;
        if (cached.LastUpdated >= lastUpdated && cached.Icon is not null)
        {
            return cached.Icon;
        }

        using var fs = File.OpenRead(path);
        var icon = Main.Assets.CreateUntracked<Texture2D>(fs, ".png");

        // Validate size
        if (icon.Width() != 80 || icon.Height() != 80)
        {
            error = "Must be 80x80";
            icon_cache[path] = (null, 0);
            return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/DefaultModIcon", AssetRequestMode.ImmediateLoad);
        }

        // Probably good enough
        icon_cache[path] = (icon, lastUpdated);
        return icon;
    }

    private void AddButton_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        var dropdown = new AddFileDropDown();
        dropdown.Left.Set(0, 0f);
        dropdown.Top.Set(0, 0f);
        dropdown.Width.Set(0, 1f);
        dropdown.Height.Set(0, 1f);
        dropdown.OnLeftClick += CloseAddFileDropdown;
        dropdown.OnSelected += e =>
        {
            currentDropdown?.Parent.RemoveChild(currentDropdown);
            AddFile(e);
        };
        Append(dropdown);
        currentDropdown = dropdown;
    }

    private void CloseAddFileDropdown(UIMouseEvent evt, UIElement listeningElement)
    {
        if (currentDropdown is not null)
        {
            RemoveChild(currentDropdown);
        }
    }

    private void AddFile(PackBuilderType type)
    {
        if (selectedDirectory is null)
        {
            return;
        }

        var path = FindAvailableFilename(selectedDirectory.FullName, "mod", type.Extension.ToLowerInvariant());
        File.Create(path);
        fileToFocusOn = path;
    }

    private static string FindAvailableFilename(string directory, string baseName, string extension)
    {
        var path = Path.Combine(directory, $"{baseName}.{extension}.json");
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        var i = 1;
        while (true)
        {
            path = Path.Combine(directory, $"{baseName}{i}.{extension}.json");
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return path;
            }

            i++;
        }
    }

    private void EditButton_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        if (modNameDropDown?.SelectedProject is not { } project)
        {
            return;
        }

        OnEdit?.Invoke(project);
    }

    private void OpenButton_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
    {
        if (modNameDropDown?.SelectedProject is not { } project)
        {
            return;
        }

        Utils.OpenFolder(selectedDirectory?.FullName ?? project.Directory);
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
