using PackBuilder.Common.ModBuilding.Projectiles.Changes;
using System.Collections.Generic;
using Terraria;

namespace PackBuilder.Common.ModBuilding.Projectiles
{
    internal class ProjectileChanges
    {
        public List<IProjectileChange> Changes = [];

        public VanillaProjectileChange Terraria { set => Changes.Add(value); }

        public void ApplyTo(Projectile projectile)
        {
            foreach (var change in Changes)
                change.ApplyTo(projectile);
        }
    }
}
