using PackBuilder.Common.ModBuilding.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    internal class ProjectileModifier : ModSystem
    {
        public static Dictionary<int, List<IProjectileChange>> ProjectileMods { get; } = [];

        /// <summary>
        /// Registers changes for a given projectile type.
        /// </summary>
        public static void RegisterChanges(int projectileType, params IEnumerable<IProjectileChange> changes)
        {
            ProjectileMods.TryAdd(projectileType, []);
            ProjectileMods[projectileType].AddRange(changes);
        }

        [Autoload(false)]
        [LateLoad]
        internal class PackBuilderProjectile : GlobalProjectile
        {
            public override void SetDefaults(Projectile entity) => ApplyChanges(entity);

            public static void ApplyChanges(Projectile projectile)
            {
                if (ProjectileMods.TryGetValue(projectile.type, out var value))
                    value.ForEach(c => c.ApplyTo(projectile));
            }
        }
    }
}
