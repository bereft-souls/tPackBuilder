using Terraria;

namespace PackBuilder.Common.ModBuilding.Projectiles;

internal interface IProjectileChange
{
    public void ApplyTo(Projectile projectile);
}
