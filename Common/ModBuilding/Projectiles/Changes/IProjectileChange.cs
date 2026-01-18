using Terraria;

namespace PackBuilder.Common.ModBuilding.Projectiles.Changes
{
    internal interface IProjectileChange
    {
        public void ApplyTo(Projectile projectile);
    }
}
