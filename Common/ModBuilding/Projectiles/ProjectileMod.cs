using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Projectiles;

internal class ProjectileMod : PackBuilderType
{
    public List<string> Projectiles = [];

    public List<IProjectileChange> Changes = [];

    public override void Load(Mod mod)
    {
        if (Projectiles.Count == 0)
            throw new NoProjectilesException();

        // Get the projectile mod ready for factory initialization.
        foreach (string projectile in Projectiles)
        {
            int projectileType = GetProjectile(projectile);
            ProjectileModifier.RegisterChanges(projectileType, Changes);
        }
    }
}
