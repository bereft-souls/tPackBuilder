using Terraria;

namespace PackBuilder.Common.JsonBuilding.Projectiles.Changes
{
    internal interface IProjectileChange
    {
        public void ApplyTo(Projectile projectile);
    }
}
