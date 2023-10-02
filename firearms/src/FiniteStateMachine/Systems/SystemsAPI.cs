using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public interface IAmmoSelector
    {
        ItemStack GetSelectedAmmo();
    }

    public interface IAimingSystem
    {
        Vec3f GetSpawnLocationOffset();
        Vec3f GetShootingDirectionOffset();
    }
}