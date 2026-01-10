using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.UI;

namespace PackBuilder.Common.BuilderInterface;

internal sealed class NewModdersShouldUseDaybreak : ModPlayer
{
    public override void ProcessTriggers(TriggersSet triggersSet)
    {
        base.ProcessTriggers(triggersSet);

        if (!BuilderInterfaceSystem.OpenKeybind?.JustPressed ?? true)
        {
            return;
        }

        BuilderInterfaceSystem.ToggleInterface();
    }
}

internal sealed class BuilderInterfaceSystem : ModSystem
{
    public static UserInterface Interface { get; } = new();

    public static BuilderInterfaceState State { get; } = new();

    public static ModKeybind? OpenKeybind { get; private set; }

    // These show/hide the *entire* UI, not individual panels within the state.

    public static void ToggleInterface()
    {
        if (Interface.CurrentState is null)
        {
            ShowInterface();
        }
        else
        {
            HideInterface();
        }
    }

    public static void ShowInterface()
    {
        Interface.SetState(State);
    }

    public static void HideInterface()
    {
        Interface.SetState(null);
    }

    public override void Load()
    {
        base.Load();

        OpenKeybind = KeybindLoader.RegisterKeybind(Mod, "OpenBuilderInterface", Keys.OemCloseBrackets);
    }

    public override void PostSetupContent()
    {
        base.PostSetupContent();

        State.Activate();
    }

    public override void UpdateUI(GameTime gameTime)
    {
        base.UpdateUI(gameTime);

        if (Interface.CurrentState is not null)
        {
            Interface.Update(gameTime);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        base.ModifyInterfaceLayers(layers);

        var idx = layers.FindIndex(x => x.Name.Equals("Vanilla: Mouse Text", StringComparison.OrdinalIgnoreCase));
        if (idx != -1)
        {
            layers.Insert(
                idx,
                new LegacyGameInterfaceLayer(
                    "PackBuilder: Builder UI",
                    () =>
                    {
                        if (Interface.CurrentState is not null)
                        {
                            Interface.Draw(Main.spriteBatch, new GameTime());
                        }

                        return true;
                    }
                )
            );
        }
    }
}
