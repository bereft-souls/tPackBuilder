using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PackBuilder.Common.Project;
using PackBuilder.Common.Project.ManifestFormats;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface.Windows;

internal abstract class PropertyElement : UIPanel
{
    private readonly PropertyInfo property;
    private readonly UIText label;

    public PropertyElement(object? value, PropertyInfo property)
    {
        this.property = property;

        InternalValue = value;

        SetPadding(6);

        _backgroundTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/FullPanel", AssetRequestMode.ImmediateLoad);
        _borderTexture = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/SmallPanelOutline", AssetRequestMode.ImmediateLoad);

        Width.Set(0f, 1f);
        Height.Set(40f, 0f);
        MinHeight.Set(40f, 0f);

        label = new UIText(Language.GetText($"Mods.PackBuilder.UI.BuildProperties.{property.Name}"), 0.9f);
        {
            label.Width.Set(0f, 0.4f);
            label.Height.Set(0f, 1f);

            label.MaxHeight.Set(32f, 0f);

            label.Left.Set(4f, 0f);

            label.TextOriginX = 0f;
            label.TextOriginY = 0.5f;

            label.HAlign = 0;
        }
        Append(label);
    }

    public object? InternalValue { get; set; }

    public void CommitValue(BuildManifest manifest)
    {
        if (property.SetMethod is not null)
        {
            property.SetValue(manifest, InternalValue);
        }
        else if (property.PropertyType.IsConstructedGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var clearMethod = property.PropertyType.GetMethod("Clear")!;
            var addRangeMethod = property.PropertyType.GetMethod("AddRange")!;
            clearMethod.Invoke(property.GetValue(manifest), []);
            addRangeMethod.Invoke(property.GetValue(manifest), [InternalValue]);
        }
    }
}

internal abstract class PropertyElement<T>(T value, PropertyInfo property) : PropertyElement(value, property)
{
    public T Value
    {
        get => (T)InternalValue!;
        set => InternalValue = value;
    }
}

internal sealed class StringElement : PropertyElement<string>
{
    public StringElement(string value, PropertyInfo property) : base(value, property)
    {
        var textInput = new InputField("...");
        {
            textInput.VAlign = 0.5f;
            textInput.HAlign = 1f;

            textInput.Width.Set(0, 0.5f);

            textInput.Text = value;

            textInput.OnEnter += obj =>
            {
                Value = obj.Text;
            };
        }
        Append(textInput);
    }
}

internal sealed class StringListElement : PropertyElement<List<string>>
{
    private readonly UIList list;

    private readonly Queue<Action> runDuringUpdate = [];

    public StringListElement(List<string> value, PropertyInfo property) : base(value.ToList(), property)
    {
        list = new UIList();
        {
            list.ManualSortMethod = _ => { };
            list.HAlign = 1f;

            list.Width.Set(0, 0.5f);
            list.Height.Set(0, 1f);
        }
        Append(list);

        RepopulateList();
    }

    public override void Recalculate()
    {
        base.Recalculate();

        Height.Set(list.GetTotalHeight() + PaddingTop + PaddingBottom, 0f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        while (runDuringUpdate.TryDequeue(out var act))
        {
            act.Invoke();
        }
    }

    private void RepopulateList()
    {
        list.Clear();

        for (var i = 0; i < Value.Count; i++)
        {
            var textInput = new InputField("...");
            {
                textInput.Width.Set(0, 1f);

                textInput.Text = Value[i];

                var index = i;
                textInput.OnEnter += obj =>
                {
                    Value[index] = obj.Text;
                };

                textInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;
            }
            list.Add(textInput);
            textInput.Activate();
        }

        var addElementInput = new InputField("...");
        {
            addElementInput.Width.Set(0, 1f);

            addElementInput.OnEnter += OnEnter_AddElement;

            addElementInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;
        }
        list.Add(addElementInput);
        addElementInput.Activate();

        return;

        void OnEnter_AddElement(InputField input)
        {
            if (!input.PretendEnterWasPressed && !Keys.Enter.JustPressed)
            {
                return;
            }

            if (input.Text.Length <= 0)
            {
                runDuringUpdate.Enqueue(
                    () =>
                    {
                        input.currentlyWriting = true;
                        input.Text = string.Empty;
                        InputHelpers.SyncBlinkerStartTime();
                        InputHelpers.WritingText = true;
                    }
                );
                return;
            }

            Value.Add(input.Text);

            var textInput = new InputField("...");
            {
                textInput.Width.Set(0, 1f);

                textInput.Text = input.Text;
            }

            var index = Value.Count - 1;

            runDuringUpdate.Enqueue(
                () =>
                {
                    textInput.OnEnter += obj =>
                    {
                        Value[index] = obj.Text;
                    };

                    textInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;

                    list.Add(textInput);
                    textInput.Activate();

                    input.currentlyWriting = true;
                    input.Text = string.Empty;
                    InputHelpers.SyncBlinkerStartTime();
                    InputHelpers.WritingText = true;

                    list.Remove(input);
                    list.Add(input);
                }
            );
        }

        void OnAttemptedBackspace_RemoveElement(InputField input)
        {
            if (list.Count <= 1)
            {
                return;
            }

            var index = list._items.IndexOf(input);

            if (index < 0)
            {
                return;
            }

            if (index == 0)
            {
                Value.RemoveAt(0);

                runDuringUpdate.Enqueue(
                    () =>
                    {
                        RepopulateList();

                        /*
                        InputHelpers.CursorPositon = grrr.Text.Length;
                        InputHelpers.SyncBlinkerStartTime();
                        */
                        InputHelpers.WritingText = false;
                    }
                );
                return;
            }

            var prior = list._items[index - 1];

            if (prior is not InputField priorInput)
            {
                return;
            }

            Value.RemoveAt(index - 1);

            runDuringUpdate.Enqueue(
                () =>
                {
                    RepopulateList();

                    if (list._items[index - 1] is not InputField grrr)
                    {
                        return;
                    }

                    grrr.Text = priorInput.Text;
                    grrr.currentlyWriting = true;

                    InputHelpers.CursorPositon = grrr.Text.Length;
                    InputHelpers.SyncBlinkerStartTime();
                    InputHelpers.WritingText = true;
                }
            );
        }
    }
}

internal sealed class ModReferenceListElement : PropertyElement<List<ModReference>>
{
    private readonly UIList list;

    private readonly Queue<Action> runDuringUpdate = [];

    public ModReferenceListElement(List<ModReference> value, PropertyInfo property) : base(value.ToList(), property)
    {
        list = new UIList();
        {
            list.ManualSortMethod = _ => { };
            list.HAlign = 1f;

            list.Width.Set(0, 0.5f);
            list.Height.Set(0, 1f);
        }
        Append(list);

        RepopulateList();
    }

    private static ModReference SortOfParse(string text)
    {
        if (ModReference.TryParse(text, out var modRef))
        {
            return modRef;
        }

        return new ModReference(text.Split('@', 2).FirstOrDefault() ?? "", null);
    }

    public override void Recalculate()
    {
        base.Recalculate();

        Height.Set(list.GetTotalHeight() + PaddingTop + PaddingBottom, 0f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        while (runDuringUpdate.Count > 0)
        {
            runDuringUpdate.Dequeue()();
        }
    }

    private void RepopulateList()
    {
        list.Clear();

        for (var i = 0; i < Value.Count; i++)
        {
            var textInput = new InputField("...");
            {
                textInput.Width.Set(0, 1f);

                textInput.Text = Value[i].ToString();

                var index = i;
                textInput.OnEnter += obj =>
                {
                    Value[index] = SortOfParse(obj.Text);
                };

                textInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;
            }
            list.Add(textInput);
            textInput.Activate();
        }

        var addElementInput = new InputField("...");
        {
            addElementInput.Width.Set(0, 1f);

            addElementInput.OnEnter += OnEnter_AddElement;

            addElementInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;
        }
        list.Add(addElementInput);
        addElementInput.Activate();

        return;

        void OnEnter_AddElement(InputField input)
        {
            if (!input.PretendEnterWasPressed && !Keys.Enter.JustPressed)
            {
                return;
            }

            if (input.Text.Length <= 0)
            {
                runDuringUpdate.Enqueue(
                    () =>
                    {
                        input.currentlyWriting = true;
                        input.Text = string.Empty;
                        InputHelpers.SyncBlinkerStartTime();
                        InputHelpers.WritingText = true;
                    }
                );
                return;
            }

            Value.Add(SortOfParse(input.Text));

            var textInput = new InputField("...");
            {
                textInput.Width.Set(0, 1f);

                textInput.Text = input.Text;
            }

            var index = Value.Count - 1;

            runDuringUpdate.Enqueue(
                () =>
                {
                    textInput.OnEnter += obj =>
                    {
                        Value[index] = SortOfParse(obj.Text);
                    };

                    textInput.OnAttemptedBackspace += OnAttemptedBackspace_RemoveElement;

                    list.Add(textInput);
                    textInput.Activate();

                    input.currentlyWriting = true;
                    input.Text = string.Empty;
                    InputHelpers.SyncBlinkerStartTime();
                    InputHelpers.WritingText = true;

                    list.Remove(input);
                    list.Add(input);
                }
            );
        }

        void OnAttemptedBackspace_RemoveElement(InputField input)
        {
            if (list.Count <= 1)
            {
                return;
            }

            var index = list._items.IndexOf(input);

            if (index < 0)
            {
                return;
            }

            if (index == 0)
            {
                Value.RemoveAt(0);

                runDuringUpdate.Enqueue(
                    () =>
                    {
                        RepopulateList();

                        /*
                        InputHelpers.CursorPositon = grrr.Text.Length;
                        InputHelpers.SyncBlinkerStartTime();
                        */
                        InputHelpers.WritingText = false;
                    }
                );
                return;
            }

            var prior = list._items[index - 1];

            if (prior is not InputField priorInput)
            {
                return;
            }

            Value.RemoveAt(index - 1);

            runDuringUpdate.Enqueue(
                () =>
                {
                    RepopulateList();

                    if (list._items[index - 1] is not InputField grrr)
                    {
                        return;
                    }

                    grrr.Text = priorInput.Text;
                    grrr.currentlyWriting = true;

                    InputHelpers.CursorPositon = grrr.Text.Length;
                    InputHelpers.SyncBlinkerStartTime();
                    InputHelpers.WritingText = true;
                }
            );
        }
    }
}

internal sealed class VersionElement : PropertyElement<Version>
{
    public VersionElement(Version value, PropertyInfo property) : base(value, property)
    {
        var textInput = new InputField("...");
        {
            textInput.VAlign = 0.5f;
            textInput.HAlign = 1f;

            textInput.Width.Set(0, 0.5f);

            textInput.Text = value.ToString();

            textInput.OnEnter += obj =>
            {
                if (Version.TryParse(obj.Text, out var ver))
                {
                    Value = ver;
                }
            };
        }
        Append(textInput);
    }
}

internal sealed class BoolElement : PropertyElement<bool>
{
    private sealed class ToggleImage : UIElement
    {
        public Asset<Texture2D>? Texture { get; set; }

        public Rectangle? Frame { get; set; }

        public Color Color { get; set; }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);

            if (Texture is null)
            {
                return;
            }

            var dims = this.InnerDimensions;

            spriteBatch.Draw(
                Texture.Value,
                dims,
                Frame,
                Color
            );
        }
    }

    private readonly UIText text;

    private readonly ToggleImage toggle;

    public BoolElement(bool value, PropertyInfo property) : base(value, property)
    {
        var container = new UIElement();
        {
            container.Height.Set(0f, 1f);

            container.Width.Set(0, 0.35f);

            container.VAlign = 0.5f;
            container.HAlign = 1f;

            container.OnLeftClick += OnLeftClick_UpdateValue;

            container.OnMouseOver +=
                (evt, lst) =>
                {
                    SoundEngine.PlaySound(in SoundID.MenuTick);
                    UpdateColors(evt, lst);
                };

            container.OnMouseOut += UpdateColors;
        }
        Append(container);

        text = new UIText(GetText(), 0.9f);
        {
            text.Width.Set(-30f, 1f);
            text.Height.Set(0f, 1f);

            text.Left.Set(-30, 0f);

            text.HAlign = 1f;
            text.VAlign = 0.5f;

            text.TextOriginX = 1f;
            text.TextOriginY = 0.5f;

            text.TextColor = GetColor();

            text.IgnoresMouseInteraction = true;
        }
        container.Append(text);

        toggle = new ToggleImage();
        {
            var asset = ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/Toggle", AssetRequestMode.ImmediateLoad);

            asset.Wait();

            toggle.Texture = asset;

            toggle.Frame = GetButtonFrame();

            toggle.Color = GetColor();

            toggle.HAlign = 1f;

            toggle.Width.Set(30f, 0f);
            toggle.Height.Set(30f, 0f);

            toggle.IgnoresMouseInteraction = true;
        }
        container.Append(toggle);

        return;

        void UpdateColors(UIMouseEvent evt, UIElement listeningElement)
        {
            text?.TextColor = GetColor();
            toggle?.Color = GetColor();
        }

        void OnLeftClick_UpdateValue(UIMouseEvent evt, UIElement listeningElement)
        {
            Value = !Value;

            text?.SetText(GetText());

            toggle?.Frame = GetButtonFrame();

            SoundEngine.PlaySound(in SoundID.MenuTick);
        }

        LocalizedText GetText()
        {
            return Value ? Lang.menu[126] : Lang.menu[124];
        }

        Rectangle? GetButtonFrame()
        {
            return toggle.Texture?.Value.Frame(1, 2, 0, Value.ToInt());
        }

        Color GetColor()
        {
            return container.IsMouseHovering ? Color.White : (Color.White * 0.75f);
        }
    }
}

internal sealed class PropertyEditorWindow(ModProjectView project, BuilderInterfaceState state) : AbstractInterfaceWindow(state)
{
    // No enum element as we shouldn't really support changing ModSide
    private static readonly Dictionary<Type, Type> elements_by_type = new()
    {
        { typeof(string), typeof(StringElement) },
        { typeof(bool), typeof(BoolElement) },
        { typeof(Version), typeof(VersionElement) },
        { typeof(List<string>), typeof(StringListElement) },
        { typeof(List<ModReference>), typeof(ModReferenceListElement) },
    };

    private static readonly string[] properties_in_order =
    [
        "DisplayName",
        "Author",
        "Version",
        "HomepageUrl",
        "StrongModReferences",
        "WeakModReferences",
        "SoftModReferences",
        "ModsToSortBefore",
        "ModsToSortAfter",
        "AssemblyReferences",
        // "IgnoredBuildPaths", // What use is this?
    ];

    private readonly Dictionary<PropertyInfo, PropertyElement> elements = [];

    public ModProjectView Project { get; set; } = project;

    public BuildManifest Manifest => Project.Project.Manifest;

    public event Action? OnClose;

    public override void OnInitialize()
    {
        base.OnInitialize();

        BackgroundColor = new Color(33, 43, 79) * 0.8f;

        const float padding = 4f;
        const float title_bar_height = 16f;
        const float divider_height = 4f;
        const float bottom_section_height = 40f;

        var yOffset = 0f;
        // Title bar
        var titleBar = new UIText(Language.GetTextValue("Mods.PackBuilder.UI.BuildProperties"));
        {
            titleBar.Width.Set(0f, 1f);
            titleBar.Height.Set(0f, 0f);
            titleBar.TextOriginX = 0f;
            titleBar.HAlign = 0f;
            titleBar.VAlign = 0f;
            titleBar.IgnoresMouseInteraction = true;
        }
        yOffset += title_bar_height;
        Append(titleBar);

        yOffset += 8f;
        var divider1 = new UIHorizontalSeparator();
        {
            divider1.IgnoresMouseInteraction = true;

            divider1.Width.Set(0f, 1f);
            divider1.Top.Set(yOffset, 0f);
            divider1.HAlign = 0.5f;

            divider1.Color = new Color(85, 88, 159) * 0.5f;
        }
        Append(divider1);

        // Containing element for list and scrollbar
        // yOffset += 2f;
        var container = new UIElement();
        {
            container.Width.Set(0f, 1f);
            // container.Height.Set(-yOffset - padding * 2f - bottom_section_height, 1f);
            container.Top.Set(yOffset + padding, 0f);
        }
        Append(container);

        var list = new UIList();
        {
            list.Width.Set(-24f, 1f);
            list.Height.Set(-4f, 1f);

            list.VAlign = 1f;
        }
        container.Append(list);

        var scrollbar = new UIScrollbar();
        {
            scrollbar.HAlign = 1f;
            scrollbar.VAlign = 1f;
            scrollbar.Height.Set(-16f, 1f);

            scrollbar.Top.Set(-6f, 0f);
        }
        container.Append(scrollbar);
        list.SetScrollbar(scrollbar);

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
                foreach (var property in elements.Values)
                {
                    property.CommitValue(Manifest);
                }

                WellKnownBuildManifestFormats.BuildTxt.Serialize(Manifest, Project.Project.Source);
                WellKnownBuildManifestFormats.PackBuilderTxt.Serialize(Manifest, Project.Project.Source);
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

        var editDescButton = new UITextPanel<LocalizedText>(Language.GetText("Mods.PackBuilder.UI.EditDescription"));
        {
            editDescButton.Left.Set(top_bar_padding, 0f);
            editDescButton.Width.Set(regular_button_width, 0f);
            editDescButton.Height.Set(0f, 1f);
            editDescButton.WithFadedMouseOver();
            editDescButton.OnLeftClick += (_, _) =>
            {
                var descFile = Path.Combine(Project.Directory, "description.txt");
                if (!File.Exists(descFile))
                {
                    try
                    {
                        File.Create(descFile);
                    }
                    catch
                    {
                        // No perms or a directory is named that
                        return;
                    }
                }

                Process.Start(
                    new ProcessStartInfo(descFile)
                    {
                        UseShellExecute = true,
                    }
                );
            };
        }
        bottomSectionContainer.Add(editDescButton);

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

        foreach (var propertyName in properties_in_order)
        {
            var property = typeof(BuildManifest).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property is null)
            {
                continue;
            }

            if (!elements_by_type.TryGetValue(property.PropertyType, out var elementType))
            {
                continue;
            }

            var element = (PropertyElement)Activator.CreateInstance(elementType, property.GetValue(Manifest), property)!;
            elements.Add(property, element);
            list.Add(element);
            element.Activate();
        }
    }
}
