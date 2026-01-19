using Terraria;

namespace PackBuilder.Common.ModBuilding.Projectiles;

public interface IProjectileChange
{
    public void ApplyTo(Projectile projectile);
}
