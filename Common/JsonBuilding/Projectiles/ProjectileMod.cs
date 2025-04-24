using System.Collections.Generic;

namespace PackBuilder.Common.JsonBuilding.Projectiles
{
    internal class ProjectileMod
    {
        public List<string> Projectiles = [];

        public required string Projectile { set => Projectiles.Add(value); }

        public required ProjectileChanges Changes { get; set; }
    }
}
