using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using PackBuilder.Common.ModBuilding;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using Terraria.UI.Chat;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal interface IVisitor<in T>
{
    void Visit(T obj);
}

internal abstract class AbstractEditorWindow<TType, TFactory> : AbstractInterfaceWindow
    where TType : PackBuilderType, new()
    where TFactory : ModifierEditorElement<TType>
{
    public sealed class EditorWindowDropDown : UIPanel
    {
        public EditorWindowDropDown(ModifierEditorElement[] elements)
        {
            VAlign = 1f;

            BackgroundColor = new Color(35, 40, 83) * 0.5f;
            BorderColor = new Color(35, 40, 83) * 0.5f;
            IgnoresMouseInteraction = false;
            SetPadding(0f);

            BuildList(elements);
        }

        public UIPanel? Panel { get; private set; }

        public event Action<ModifierEditorElement>? OnSelectOption;

        public void BuildList(ModifierEditorElement[] elements)
        {
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

            foreach (var element in elements)
            {
                var button = new GroupOptionButton<ModifierEditorElement>(element, Language.GetText("Mods.PackBuilder.Stupid").WithFormatArgs(element.Name), null, Color.White, null, 0.8f);
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
            if (listeningElement is not GroupOptionButton<ModifierEditorElement> option)
            {
                return;
            }

            OnSelectOption?.Invoke(option.OptionValue);
        }
    }

    private readonly Queue<Action> runDuringUpdate = [];
    private readonly UIText title;

    private readonly UIElement topBarContainer;

    private EditorWindowDropDown? currentDropdown;

    public AbstractEditorWindow(BaseModifierElement element, BuilderInterfaceState state) : base(state)
    {
        Element = element;

        Width.Set(600, 0f);
        Height.Set(450, 0f);
        MinWidth.Set(400f, 0f);
        MinHeight.Set(200f, 0f);
        SetPadding(8f);

        BackgroundColor = new Color(33, 43, 79) * 0.8f;

        const float padding = 4f;
        // const float title_bar_height = 16f;
        const float divider_height = 4f;
        const float bottom_section_height = 40f;

        var yOffset = 0f;

        // Top bar
        topBarContainer = new UIElement();
        {
            topBarContainer.Width.Set(0f, 1f);
            topBarContainer.Height.Set(24f, 0f);
            // topBarContainer.IgnoresMouseInteraction = true;

            topBarContainer.OnLeftMouseDown += (evt, _) =>
            {
                if (evt.Target == topBarContainer)
                {
                    ClickThroughThisTime = true;
                }
            };
            topBarContainer.OnLeftMouseUp += (evt, _) =>
            {
                if (evt.Target == topBarContainer)
                {
                    ClickThroughThisTime = true;
                }
            };
        }
        Append(topBarContainer);

        // Title bar
        title = new UIText(GetTitle(Element.Title));
        {
            title.Left.Set(4f, 0f);
            title.Width.Set(-(28f * 2f) - 4, 1f);
            title.Height.Set(24f, 0f);
            title.TextOriginX = 0f;
            title.TextOriginY = 0.33f;
            title.HAlign = 0f;
            title.VAlign = 0f;
            title.IgnoresMouseInteraction = true;
        }
        topBarContainer.Append(title);

        var renameButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("Packbuilder/Assets/Textures/UI/RenameSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Rename")
        );
        {
            renameButton.HAlign = 1f;
            renameButton.VAlign = 0f;
            renameButton.Left.Set(-28, 0f);
            renameButton.OnLeftClick += (_, _) => EditName();
        }
        topBarContainer.Append(renameButton);

        var addButton = new UITooltipImageButton(
            ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/AddSmall", AssetRequestMode.ImmediateLoad),
            Language.GetTextValue("Mods.PackBuilder.UI.Add")
        );
        {
            addButton.HAlign = 1f;
            addButton.VAlign = 0f;
            addButton.OnLeftClick += OpenEditorWindowDropDown;
        }
        topBarContainer.Append(addButton);

        yOffset += title.Height.Pixels;

        yOffset += 4;
        var divider = new UIHorizontalSeparator();
        {
            divider.IgnoresMouseInteraction = true;
            divider.Width.Set(0f, 1f);
            divider.HAlign = 0.5f;
            divider.Top.Set(yOffset, 0);
            divider.Color = Color.Lerp(Color.White, new Color(63, 65, 151, 255), 0.85f) * 0.9f;
        }
        Append(divider);

        // Containing element for list and scrollbar
        yOffset += 8f;
        var container = new UIElement();
        {
            container.Width.Set(0f, 1f);
            // container.Height.Set(-ChangeList.Top.Pixels - 4f);
            container.Top.Set(yOffset, 0f);
        }
        Append(container);

        ChangeList = new UIList();
        {
            ChangeList.ManualSortMethod = _ => { };
            ChangeList.Left.Set(0f, 0f);
            // ChangeList.Top.Set(titleBar.Height.Pixels + 12f, 0f);
            ChangeList.Width.Set(-28f, 1f);
            // ChangeList.Height.Set(-ChangeList.Top.Pixels - 4f, 1f);
            ChangeList.Height.Set(0f, 1f);
            ChangeList.VAlign = 1f;
        }
        container.Append(ChangeList);

        var scrollBar = new UIScrollbar();
        {
            scrollBar.HAlign = 1f;
            // scrollBar.Top.Set(ChangeList.Top.Pixels + 4f, 0f);
            // scrollBar.Height.Set(-scrollBar.Top.Pixels - 6f, 1f);
            scrollBar.VAlign = 1f;
            scrollBar.Height.Set(-12f, 1f);
            scrollBar.Top.Set(-6f, 0f);
        }
        container.Append(scrollBar);
        ChangeList.SetScrollbar(scrollBar);

        var yOffsetFromBottom = 0f;

        yOffsetFromBottom += bottom_section_height;
        var bottomSectionContainer = new UIGrid();
        {
            bottomSectionContainer.ManualSortMethod = _ => { };
            bottomSectionContainer.Width.Set(0f, 1f);
            bottomSectionContainer.Height.Set(bottom_section_height, 0f);
            bottomSectionContainer.Top.Set(-yOffsetFromBottom, 1f);

            bottomSectionContainer.OnLeftMouseDown += (evt, _) =>
            {
                if (evt.Target == bottomSectionContainer || evt.Target == bottomSectionContainer._innerList)
                {
                    ClickThroughThisTime = true;
                }
            };
            bottomSectionContainer.OnLeftMouseUp += (evt, _) =>
            {
                if (evt.Target == bottomSectionContainer || evt.Target == bottomSectionContainer._innerList)
                {
                    ClickThroughThisTime = true;
                }
            };
        }
        yOffsetFromBottom += padding;
        Append(bottomSectionContainer);

        const float regular_button_width = 76f;
        const float top_bar_padding = 4f;
        var saveButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.PackBuilder.UI.Save"));
        {
            saveButton.Width.Set(regular_button_width, 0f);
            saveButton.Height.Set(0f, 1f);
            saveButton.WithFadedMouseOver();
            saveButton.OnLeftClick += (_, _) =>
            {
                if (Element is not ModifierElement e)
                {
                    return;
                }

                try
                {
                    if (Path.GetDirectoryName(e.File.FullName) is { } parentDir)
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    var serialized = Serialize(BuildFromModifiers(CreateObject()));
                    File.WriteAllText(e.File.FullName, serialized);
                }
                catch
                {
                    // TODO: real logging since this error is important
                }
            };
        }
        bottomSectionContainer.Add(saveButton);

        var closeButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.PackBuilder.UI.Close"));
        {
            closeButton.Left.Set(top_bar_padding, 0f);
            closeButton.Width.Set(regular_button_width, 0f);
            closeButton.Height.Set(0f, 1f);
            closeButton.WithFadedMouseOver();
            closeButton.OnLeftClick += (_, _) => CloseWindow();
        }
        bottomSectionContainer.Add(closeButton);

        yOffsetFromBottom += 6f;
        var divider2 = new UIHorizontalSeparator();
        {
            divider2.IgnoresMouseInteraction = true;

            divider2.Width.Set(0f, 1f);
            divider2.Top.Set(-yOffsetFromBottom + 4f, 1f);
            divider2.HAlign = 0.5f;

            divider2.Color = new Color(85, 88, 159) * 0.5f;
        }
        Append(divider2);

        yOffsetFromBottom += 6f;

        container.Height.Set(-yOffset - yOffsetFromBottom, 1f);

        // Resize corner
        var resizablePanelButton = new ResizablePanelButton();
        {
            resizablePanelButton.Left.Pixels += PaddingLeft;
            resizablePanelButton.Top.Pixels += PaddingTop;
        }
        Append(resizablePanelButton);

        if (Element is ModifierElement e)
        {
            var json = File.ReadAllText(e.File.FullName);
            var obj = Deserialize(json);
            var modifiers = DeriveModifiers(obj);
            foreach (var modifier in modifiers)
            {
                ChangeList.Add(modifier);
            }
        }

        Recalculate();
    }

    public BaseModifierElement Element { get; }

    public UIList ChangeList { get; }

    protected virtual TType CreateObject()
    {
        var type = ChangeList._items.FirstOrDefault(x => x is TFactory) as TFactory;
        return type?.CreateObject() ?? new TType();
    }

    protected virtual TType BuildFromModifiers(TType obj)
    {
        foreach (var modifier in ChangeList._items)
        {
            if (modifier is IVisitor<TType> visitor)
            {
                visitor.Visit(obj);
            }
        }

        return obj;
    }

    protected abstract IEnumerable<ModifierEditorElement> DeriveModifiers(TType obj);

    protected abstract IEnumerable<ModifierEditorElement> GetAvailableModifiers();

    protected virtual string Serialize(TType obj)
    {
        return JsonConvert.SerializeObject(obj, Formatting.Indented, PackBuilder.JsonSettings);
    }

    protected virtual TType Deserialize(string json)
    {
        return JsonConvert.DeserializeObject<TType>(json, PackBuilder.JsonSettings) ?? new TType();
    }

    public void EditName()
    {
        if (Element is not ModifierElement element)
        {
            return;
        }

        var extIdx = element.File.Name.IndexOf<char>('.');
        var ext = element.File.Name[extIdx..];
        var relativeFileName = Path.GetRelativePath(element.Project.Directory, element.File.FullName);
        {
            if (relativeFileName.EndsWith(ext, StringComparison.InvariantCultureIgnoreCase))
            {
                relativeFileName = relativeFileName[..^ext.Length];
            }
        }

        topBarContainer.RemoveChild(title);

        var input = new InputField("...");
        {
            input.Left.Set(4f, 0f);
            input.Width.Set(-(28f * 2f) - 4, 1f);
            input.Height.Set(24f, 0f);
            input.TextAlignX = 0f;
            input.HAlign = 0f;
            input.VAlign = 0f;

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
                                Append(title);
                                return;
                            }

                            try
                            {
                                var path = Path.Combine(element.Project.Directory, fullRelativePath);
                                if (Path.GetDirectoryName(path) is { } parentDir)
                                {
                                    Directory.CreateDirectory(parentDir);
                                }

                                // no overwrite because that's evil in this ui
                                element.File.MoveTo(path);

                                element.Title = Path.GetFileName(fullRelativePath);
                                element.RefreshText();
                                title._text = element.Title;
                            }
                            catch
                            {
                                // ignore
                            }
                            finally
                            {
                                Append(title);
                            }
                        }
                    );
                };
        }
        Append(input);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        while (runDuringUpdate.TryDequeue(out var act))
        {
            act.Invoke();
        }
    }

    private void OpenEditorWindowDropDown(UIMouseEvent evt, UIElement listeningElement)
    {
        var modifierList = GetAvailableModifiers().ToArray();

        var dropDown = new EditorWindowDropDown(modifierList);
        dropDown.Left.Set(0, 0f);
        dropDown.Top.Set(0, 0f);
        dropDown.Width.Set(0, 1f);
        dropDown.Height.Set(0, 1f);
        dropDown.OnLeftClick += CloseEditorWindowDropDown;
        dropDown.OnSelectOption += e =>
        {
            currentDropdown?.Parent.RemoveChild(currentDropdown);
            AddEditor(e);
        };
        Append(dropDown);
        currentDropdown = dropDown;
    }

    private void CloseEditorWindowDropDown(UIMouseEvent evt, UIElement listeningElement)
    {
        if (currentDropdown is not null)
        {
            RemoveChild(currentDropdown);
        }
    }

    private string GetTitle(string text)
    {
        if (ChatManager.GetStringSize(FontAssets.MouseText.Value, text, Vector2.One).X > 240)
        {
            return text[..16] + "...";
        }

        return text;
    }

    public void AddEditor(ModifierEditorElement e)
    {
        ChangeList.Add(e);
    }

    public void RemoveEditor(ModifierEditorElement e)
    {
        ChangeList.Add(e);
    }

    protected static TElement CreateAndPopulate<TElement, TArg>(TArg arg)
        where TElement : ModifierEditorElement<TArg>, new()
    {
        var element = new TElement();
        {
            element.Populate(arg);
        }
        return element;
    }
}
