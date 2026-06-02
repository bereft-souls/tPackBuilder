using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace PackBuilder.Common;

internal sealed class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool DeveloperMode { get; set; } = false;

    [DefaultValue(true)]
    public bool HideIgnoredDirectories { get; set; } = true;
    
    [DefaultValue(true)]
    public bool HideUnwantedDirectories { get; set; } = true;
}
