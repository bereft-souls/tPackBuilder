using PackBuilder.Common.JsonBuilding.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace PackBuilder.Core.Systems
{
    internal class ProjectileModifier : ModSystem
    {
        public static Dictionary<int, List<ProjectileChanges>> ProjectileModSets { get; } = [];

        [Autoload(false)]
        [LateLoad]
        internal class PackBuilderProjectile : GlobalProjectile
        {
            public override void SetDefaults(Projectile entity) => ApplyChanges(entity);

            public static void ApplyChanges(Projectile projectile)
            {
                if (ProjectileModSets.TryGetValue(projectile.type, out var value))
                    value.ForEach(c => c.ApplyTo(projectile));
            }
        }
    }
}
