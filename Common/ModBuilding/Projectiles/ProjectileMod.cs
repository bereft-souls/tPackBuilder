using PackBuilder.Core.Systems;
using System.Collections.Generic;
using Terraria.ModLoader;

namespace PackBuilder.Common.ModBuilding.Projectiles;

public sealed class ProjectileMod : PackBuilderType
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

    /// <summary>
    /// Call this to manually register a <see cref="ProjectileMod"/>.
    /// </summary>
    public void Register() => Load(null!);
}
