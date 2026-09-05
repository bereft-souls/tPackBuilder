using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.ModBuilding;
using PackBuilder.Common.Project;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

public abstract class BaseModifierElement : UIPanel
{
    protected BaseModifierElement(Asset<Texture2D> icon, string title)
    {
        Title = title;
        Icon = icon;

        SetPadding(0f);
        PaddingLeft = PaddingRight = 8f;
        Height.Set(40f, 0f);
        Width.Set(0f, 1f);

        this.WithFadedMouseOver();

        const float regular_button_width = 24f;
        const float top_bar_padding = 4f;
        var grid = new UIGrid();
        {
            grid.ManualSortMethod = _ => { };
            grid.Width.Set(0f, 1f);
            grid.Height.Set(0f, 1f);
            grid.ListPadding = top_bar_padding;
        }
        Append(grid);

        var buttons = GetButtons().ToArray();

        IconElement = new UIImage(Icon);
        {
            IconElement.Width.Set(32f, 0f);
            IconElement.Height.Set(32f, 0f);

            IconElement.VAlign = 0.5f;
        }
        grid.Add(IconElement);

        TitleElement = new UIText(GetDisplayTitle(), textScale: TitleScale);
        {
            /*
            TitleElement._backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/FullPanel", AssetRequestMode.ImmediateLoad);
            TitleElement._borderTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SmallPanelOutline", AssetRequestMode.ImmediateLoad);
            */

            TitleElement.Left.Set(0f, 0f);
            TitleElement.Width.Set(-IconElement.Width.Pixels - top_bar_padding - (top_bar_padding + regular_button_width) * buttons.Length, 1f);
            TitleElement.MaxWidth = TitleElement.Width;
            TitleElement.OnInternalTextChange += () =>
            {
                TitleElement.MinWidth.Set(0f, 0f);
            };
            TitleElement.Height.Set(0f, 1f);
            TitleElement.HAlign = 0f;
            TitleElement.TextOriginY = 0.5f;
            TitleElement.TextOriginX = 0f;
        }
        grid.Add(TitleElement);

        foreach (var button in buttons)
        {
            grid.Add(button);
        }
    }

    public string Title { get; set; }

    protected abstract float BaseOrder { get; }

    protected virtual float TitleScale => 1f;

    protected Asset<Texture2D> Icon { get; }

    protected UIImage IconElement { get; }

    protected UIText TitleElement { get; }

    protected abstract IEnumerable<UIElement> GetButtons();

    protected virtual string GetDisplayTitle()
    {
        return Title;
    }

    public override int CompareTo(object? obj)
    {
        if (obj is not BaseModifierElement other)
        {
            return base.CompareTo(obj);
        }

        var baseComparison = BaseOrder.CompareTo(other.BaseOrder);
        if (baseComparison != 0)
        {
            return baseComparison;
        }

        return Title.CompareTo(other.Title, StringComparison.InvariantCultureIgnoreCase);
    }
}

internal abstract class BaseDirectoryElement : BaseModifierElement
{
    protected BaseDirectoryElement(Asset<Texture2D> icon, string title) : base(icon, title)
    {
        // Height.Set(36f, 0f);
    }

    // protected override float TitleScale => 0.75f;
}

internal sealed class ReturnDirectoryElement : BaseDirectoryElement
{
    private readonly ModControlPanelWindow window;

    public ReturnDirectoryElement(ModControlPanelWindow window) : base(
        ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/ReturnDirectory"),
        Language.GetTextValue("Mods.PackBuilder.UI.ReturnDirectory")
    )
    {
        this.window = window;

        OnLeftDoubleClick += (_, _) =>
        {
            window.NavigateUp();
        };
    }

    protected override float BaseOrder => 0f;

    protected override IEnumerable<UIElement> GetButtons()
    {
        var openExternalButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/OpenExternalSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.OpenExternal")
        );
        {
            openExternalButton.VAlign = 0.5f;
            openExternalButton.OnLeftClick += (_, _) =>
            {
                window.OpenUp();
            };
        }
        yield return openExternalButton;

        var openButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/OpenSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Open")
        );
        {
            openButton.VAlign = 0.5f;
            openButton.OnLeftClick += (_, _) =>
            {
                window.NavigateUp();
            };
        }
        yield return openButton;
    }
}

internal sealed class DirectoryElement : BaseDirectoryElement
{
    private readonly DirectoryInfo directory;

    private readonly ModControlPanelWindow window;

    public DirectoryElement(ModControlPanelWindow window, DirectoryInfo directory) : base(
        ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Directory"),
        directory.Name
    )
    {
        this.window = window;
        this.directory = directory;

        OnLeftDoubleClick += (_, _) =>
        {
            window.NavigateTo(directory);
        };
    }

    protected override float BaseOrder => 1f;

    protected override IEnumerable<UIElement> GetButtons()
    {
        var openExternalButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/OpenExternalSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.OpenExternal")
        );
        {
            openExternalButton.VAlign = 0.5f;
            openExternalButton.OnLeftClick += (_, _) =>
            {
                try
                {
                    Utils.OpenFolder(directory.FullName);
                }
                catch (Exception e)
                {
                    // ignore
                }
            };
        }
        yield return openExternalButton;

        var openButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/OpenSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Open")
        );
        {
            openButton.VAlign = 0.5f;
            openButton.OnLeftClick += (_, _) =>
            {
                window.NavigateTo(directory);
            };
        }
        yield return openButton;
    }
}

internal sealed class ModifierElement : BaseModifierElement
{
    private readonly PackBuilderType singletonInstance;

    private readonly ModControlPanelWindow window;

    private readonly Queue<Action> runDuringUpdate = [];

    public ModifierElement(ModControlPanelWindow window, ModProjectView project, FileInfo file, PackBuilderType singletonInstance) : base(singletonInstance.GetIcon(), file.Name)
    {
        this.window = window;
        Project = project;
        File = file;
        this.singletonInstance = singletonInstance;

        OnLeftDoubleClick += OpenEditor;
    }

    protected override float BaseOrder => 2f;

    public ModProjectView Project { get; }

    public FileInfo File { get; }

    public bool IsFocus(string? path)
    {
        if (path is null)
        {
            return false;
        }
        
        path = Path.GetFullPath(path);
        return Path.GetFullPath(File.FullName) == path;
    }

    protected override IEnumerable<UIElement> GetButtons()
    {
        var trashButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/TrashSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Delete")
        );
        {
            trashButton.VAlign = 0.5f;
            trashButton.OnLeftClick += (_, _) =>
            {
                try
                {
                    File.Delete();
                }
                catch
                {
                    // ignore
                }
            };
        }
        yield return trashButton;

        var renameButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/RenameSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Rename")
        );
        {
            renameButton.VAlign = 0.5f;
            renameButton.OnLeftClick += (_, _) =>
            {
                EditName();
            };
        }
        yield return renameButton;

        var editButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/EditSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Edit")
        );
        {
            editButton.VAlign = 0.5f;
            editButton.OnLeftClick += OpenEditor;
        }
        yield return editButton;
    }

    protected override string GetDisplayTitle()
    {
        return GetText(base.GetDisplayTitle());
    }

    public void EditName()
    {
        var extIdx = File.Name.IndexOf<char>('.');
        var ext = File.Name[extIdx..];
        var relativeFileName = Path.GetRelativePath(Project.Directory, File.FullName);
        {
            if (relativeFileName.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                relativeFileName = relativeFileName[..^ext.Length];
            }
        }

        var titleParent = TitleElement.Parent;
        titleParent.RemoveChild(TitleElement);

        var input = new InputField("...");
        {
            input._backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/BorderlessEmptyPanel");
            input._borderTexture = TextureAssets.MagicPixel;
            input.BorderColor = Color.Transparent;

            input.Left = TitleElement.Left;
            input.Width.Set(TitleElement.GetDimensions().Width, 0f);
            input.Height.Set(-8f, 1f);
            input.HAlign = 0f;
            input.TextAlignX = 0f;
            input.VAlign = 0.5f;

            input.OnUpdate += _ =>
            {
                input.BackgroundColor = IsMouseHovering ? UICommon.DefaultUIBlueMouseOver : new Color(43, 62, 131) * 0.7f;
            };

            input.SetPadding(3f);

            input.Text = relativeFileName;
            InputHelpers.CursorPositon = relativeFileName.Length;

            input.currentlyWriting = true;

            InputHelpers.WritingText = true;
            InputHelpers.SyncBlinkerStartTime();

            input.OnEnter +=
                obj =>
                {
                    runDuringUpdate.Enqueue(
                        () =>
                        {
                            var fullRelativePath = obj.Text + ext;
                            RemoveChild(obj);

                            if (Path.IsPathRooted(fullRelativePath))
                            {
                                // no...
                                titleParent.Append(TitleElement);
                                return;
                            }

                            try
                            {
                                var path = Path.Combine(Project.Directory, fullRelativePath);
                                if (Path.GetDirectoryName(path) is { } parentDir)
                                {
                                    Directory.CreateDirectory(parentDir);
                                }

                                // no overwrite because that's evil in this ui
                                File.MoveTo(path);

                                // put here to early abort
                                Title = Path.GetFileName(fullRelativePath);
                                RefreshText();
                            }
                            catch
                            {
                                // ignore
                            }
                            finally
                            {
                                titleParent.Append(TitleElement);
                            }
                        }
                    );
                };
        }
        Append(input);
    }

    private void OpenEditor(UIMouseEvent evt, UIElement listeningElement)
    {
        var dimensions = GetDimensions();
        var modifierEditor = singletonInstance.CreateEditorWindow(this, BuilderInterfaceSystem.State);
        if (modifierEditor is null)
        {
            return;
        }

        modifierEditor.Left.Set(dimensions.X + dimensions.Width + 16, 0f);
        modifierEditor.Top.Set(dimensions.Y, 0f);
        modifierEditor.WindowId = new WindowId(Path.GetRelativePath(Project.Directory, File.FullName), Project.InternalName, Project.Directory);
        BuilderInterfaceSystem.State.AddWindowOrBringToFront(modifierEditor);
        modifierEditor.BringToFront();
    }

    public void RefreshText()
    {
        TitleElement._text = GetText(Title);
    }

    private static string GetText(string path)
    {
        var length = path.IndexOf<char>('.');

        return path[..length] + $"[c/9c9c9c:{path[length..]}]";
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        while (runDuringUpdate.TryDequeue(out var act))
        {
            act.Invoke();
        }
    }
}
