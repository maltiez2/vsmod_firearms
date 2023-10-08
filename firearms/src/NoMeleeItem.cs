using Vintagestory.API.Common;

namespace MaltiezFirearms
{
    public class NoMelee : Item
    {
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            if (entitySel != null)
            {
                handling = EnumHandHandling.PreventDefaultAction;
            }
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }

        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            return false;
        }
    }
}
