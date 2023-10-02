using Vintagestory.API.Common;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public interface IAmmoSelector
    {
        ItemStack GetSelectedAmmo();
    }
}