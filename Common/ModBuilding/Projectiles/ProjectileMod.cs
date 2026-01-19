using PackBuilder.Core.Systems;
using System.Collections.Generic;

namespace PackBuilder.Common.ModBuilding.Projectiles;

public sealed class ProjectileMod : PackBuilderType
{
    public List<string> Projectiles = [];

    public List<IProjectileChange> Changes = [];

    public override void Load()
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
