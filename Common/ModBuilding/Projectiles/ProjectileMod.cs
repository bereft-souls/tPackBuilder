using Microsoft.Xna.Framework.Graphics;
using PackBuilder.Core.Systems;
using ReLogic.Content;
using System.Collections.Generic;
using PackBuilder.Common.BuilderInterface;
using PackBuilder.Common.BuilderInterface.Windows;
using PackBuilder.Common.BuilderInterface.Windows.EditorModals;
using Terraria.ModLoader;
using System.Linq;
using Terraria.ID;

namespace PackBuilder.Common.ModBuilding.Projectiles;

public sealed class ProjectileMod : PackBuilderType
{
    public List<string> Projectiles = [];

    public List<IProjectileChange> Changes = [];

    public override Asset<Texture2D> GetIcon()
    {
        return ModContent.Request<Texture2D>("PackBuilder/Assets/Textures/UI/ModifierIcons/Projectiles", AssetRequestMode.ImmediateLoad);
    }

    public override AbstractInterfaceWindow? CreateEditorWindow(BaseModifierElement element, BuilderInterfaceState state)
    {
        return new ProjectileModEditorWindow(element, state);
    }

    public override void Load()
    {
        if (Projectiles.Count == 0)
            throw new NoProjectilesException();

        foreach (int projectileType in Projectiles.Select(projectile => GetProjectile(projectile, Mod)))
        {
            if (projectileType != ProjectileID.None)
                ProjectileModifier.RegisterChanges(projectileType, Changes);
        }
    }
}
