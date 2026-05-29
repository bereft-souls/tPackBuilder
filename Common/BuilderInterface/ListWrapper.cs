using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal abstract class ListWrapper<TElement, TList> : UIElement
{
    public UIList List { get; }

    public List<TList> Items { get; }

    public event Action? OnBackspaceNoItems;
    public event Action<TElement, int>? OnBackspace;

    protected Queue<Action> RunDuringUpdate { get; } = [];

    public ListWrapper(List<TList> items)
    {
        Items = [..items];
        List = new UIList();
        {
            List.ManualSortMethod = _ => { };
            List.HAlign = 1f;

            List.Width.Set(0, 0.5f);
            List.Height.Set(0, 1f);
        }
        Append(List);

        RepopulateList();
    }

    public override void Recalculate()
    {
        base.Recalculate();

        Height.Set(List.GetTotalHeight() + PaddingTop + PaddingBottom, 0f);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        while (RunDuringUpdate.TryDequeue(out var act))
        {
            act.Invoke();
        }
    }

    public void RepopulateList()
    {
        List.Clear();

        for (var i = 0; i < Items.Count; i++)
        {
            var item = MakeActualItem(i);
            List.Add(item);
            item.Activate();
        }

        var add = MakeAddItem();
        List.Add(add);
        add.Activate();
    }

    public void PopulateWithValues(List<TList> elements)
    {
        Items.Clear();
        Items.AddRange(elements);
        RepopulateList();
    }

    protected abstract UIElement MakeActualItem(int index);

    protected abstract UIElement MakeAddItem();

    protected abstract void AddElement(UIElement element);

    protected virtual void RemoveElement(UIElement element)
    {
        if (List.Count <= 1)
        {
            return;
        }

        var index = List._items.IndexOf(element.Parent);
        if (index < 0)
        {
            return;
        }

        if (index == 0)
        {
            Items.RemoveAt(0);

            RunDuringUpdate.Enqueue(
                () =>
                {
                    RepopulateList();
                    OnBackspaceNoItems?.Invoke();
                }
            );
            return;
        }

        var prior = List._items[index - 1];
        if (prior is not TElement priorElement)
        {
            return;
        }

        Items.RemoveAt(index - 1);

        RunDuringUpdate.Enqueue(
            () =>
            {
                RepopulateList();
                OnBackspace?.Invoke(priorElement, index);
            }
        );
    }
}

internal abstract class TextFieldListWrapper<TList> : ListWrapper<InputField, TList>
{
    public TextFieldListWrapper(List<TList> items) : base(items)
    {
        OnBackspaceNoItems += () =>
        {
            InputHelpers.WritingText = false;
        };

        OnBackspace += (priorInput, idx) =>
        {
            if (List._items[idx - 1] is not InputField grrr)
            {
                return;
            }

            grrr.Text = priorInput.Text;
            grrr.currentlyWriting = true;

            InputHelpers.CursorPositon = grrr.Text.Length;
            InputHelpers.SyncBlinkerStartTime();
            InputHelpers.WritingText = true;
        };
    }

    protected override UIElement MakeActualItem(int index)
    {
        var textInput = new InputField("...");
        {
            textInput.Width.Set(0f, 1f);
            textInput.Text = ItemToString(Items[index]);

            var capturedIndex = index;
            textInput.OnEnter += obj =>
            {
                Items[capturedIndex] = StringToItem(obj.Text);
            };

            textInput.OnAttemptedBackspace += RemoveElement;
        }
        return textInput;
    }

    protected override UIElement MakeAddItem()
    {
        var addInput = new InputField("...");
        {
            addInput.Width.Set(0f, 1f);
            addInput.OnEnter += AddElement;
            addInput.OnAttemptedBackspace += RemoveElement;
        }
        return addInput;
    }

    protected override void AddElement(UIElement element)
    {
        if (element is not InputField input)
        {
            return;
        }

        if (!input.PretendEnterWasPressed && !Keys.Enter.JustPressed)
        {
            return;
        }

        if (input.Text.Length <= 0)
        {
            RunDuringUpdate.Enqueue(
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

        Items.Add(StringToItem(input.Text));

        var textInput = new InputField("...");
        {
            textInput.Width.Set(0, 1f);
            textInput.Text = input.Text;
        }

        var index = Items.Count - 1;

        RunDuringUpdate.Enqueue(
            () =>
            {
                textInput.OnEnter += obj =>
                {
                    Items[index] = StringToItem(obj.Text);
                };

                textInput.OnAttemptedBackspace += RemoveElement;

                List.Add(textInput);
                textInput.Activate();

                input.currentlyWriting = true;
                input.Text = string.Empty;
                InputHelpers.SyncBlinkerStartTime();
                InputHelpers.WritingText = true;

                List.Remove(input);
                List.Add(input);
            }
        );
    }

    protected abstract string ItemToString(TList item);

    protected abstract TList StringToItem(string str);
}

internal sealed class StringTextFieldListWrapper(List<string> items) : TextFieldListWrapper<string>(items)
{
    protected override string ItemToString(string item)
    {
        return item;
    }

    protected override string StringToItem(string str)
    {
        return str;
    }
}

internal abstract class SelectorListWrapper<TSelector, TEntry, TList> : ListWrapper<TSelector, TList>
    where TSelector : BaseSelector<TEntry, string>, new()
    where TEntry : GridEntry
{
    protected SelectorListWrapper(List<TList> items) : base(items)
    {
        OnBackspaceNoItems += () =>
        {
            InputHelpers.WritingText = false;
        };

        OnBackspace += (priorInput, idx) =>
        {
            if (List._items[idx - 1] is not TSelector grrr)
            {
                return;
            }

            grrr.Text.Text = priorInput.Text.Text;
            grrr.Text.currentlyWriting = true;

            InputHelpers.CursorPositon = grrr.Text.Text.Length;
            InputHelpers.SyncBlinkerStartTime();
            InputHelpers.WritingText = true;
        };
    }

    protected override UIElement MakeActualItem(int index)
    {
        var selector = new TSelector();
        {
            selector.Width.Set(0f, 1f);
            selector.Text.Text = selector.Entity = ItemToString(Items[index]);

            var capturedIndex = index;
            selector.Text.OnEnter += obj =>
            {
                Items[capturedIndex] = StringToItem(obj.Text);
            };

            selector.Text.OnAttemptedBackspace += RemoveElement;
        }
        return selector;
    }

    protected override UIElement MakeAddItem()
    {
        var addSelector = new TSelector();
        {
            addSelector.Width.Set(0f, 1f);
            addSelector.Text.OnEnter += AddElement;
            addSelector.Text.OnAttemptedBackspace += RemoveElement;
        }
        return addSelector;
    }

    protected override void AddElement(UIElement element)
    {
        if (element is not InputField { Parent: TSelector selector } input)
        {
            return;
        }

        if (!input.PretendEnterWasPressed && !Keys.Enter.JustPressed)
        {
            return;
        }

        if (selector.Text.Text.Length <= 0)
        {
            RunDuringUpdate.Enqueue(
                () =>
                {
                    selector.Text.currentlyWriting = true;
                    selector.Text.Text = string.Empty;
                    InputHelpers.SyncBlinkerStartTime();
                    InputHelpers.WritingText = true;
                }
            );
            return;
        }

        Items.Add(StringToItem(selector.Text.Text));

        var selectorInput = new TSelector();
        {
            selectorInput.Width.Set(0f, 1f);
            selectorInput.Text.Text = selector.Text.Text;
        }

        var index = Items.Count - 1;

        RunDuringUpdate.Enqueue(
            () =>
            {
                selectorInput.Text.OnEnter += obj =>
                {
                    Items[index] = StringToItem(obj.Text);
                };

                selectorInput.Text.OnAttemptedBackspace += RemoveElement;

                List.Add(selectorInput);
                selectorInput.Activate();

                selector.Text.currentlyWriting = true;
                selector.Text.Text = string.Empty;
                InputHelpers.SyncBlinkerStartTime();
                InputHelpers.WritingText = true;

                List.Remove(selector);
                List.Add(selector);
            }
        );
    }

    protected abstract string ItemToString(TList item);

    protected abstract TList StringToItem(string str);
}

internal sealed class SelectorNpcWrapper : SelectorListWrapper<NpcTypeSelector, NpcTypeSelector.NpcTypeEntry, string>
{
    public SelectorNpcWrapper(List<string> items) : base(items) { }

    protected override string ItemToString(string item)
    {
        return item;
    }

    protected override string StringToItem(string str)
    {
        return str;
    }
}

internal sealed class SelectorItemWrapper : SelectorListWrapper<ItemTypeSelector, ItemTypeSelector.ItemTypeEntry, string>
{
    public SelectorItemWrapper(List<string> items) : base(items) { }

    protected override string ItemToString(string item)
    {
        return item;
    }

    protected override string StringToItem(string str)
    {
        return str;
    }
}

internal sealed class SelectorProjectileWrapper : SelectorListWrapper<ProjectileTypeSelector, ProjectileTypeSelector.ProjectileTypeEntry, string>
{
    public SelectorProjectileWrapper(List<string> items) : base(items) { }

    protected override string ItemToString(string item)
    {
        return item;
    }

    protected override string StringToItem(string str)
    {
        return str;
    }
}

internal sealed class SelectorRecipeGroupWrapper : SelectorListWrapper<RecipeGroupSelector, RecipeGroupSelector.RecipeGroupEntry, string>
{
    public SelectorRecipeGroupWrapper(List<string> items) : base(items) { }

    protected override string ItemToString(string item)
    {
        return item;
    }

    protected override string StringToItem(string str)
    {
        return str;
    }
}
