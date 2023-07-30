using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace MaltiezFirearms
{
    public class ItemMultistateWeapon : Item
    {
        public const string temporaryStateKey = "weaponTemporaryState";
        public const string permanentStateKey = "weaponPermanentState";
        public const bool debugLogging = false;

        // Interaction
        public virtual void InitInteraction(int currentState, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent)
        {

        }
        public virtual int StepInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return 1;
        }
        public virtual bool CancelInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return true;
        }
        public virtual void FinishInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {

        }
        public virtual void ResetInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {

        }

        // Conditions
        protected virtual bool DoAdvancePermanentState(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, int currentState)
        {
            return true;
        }
        protected virtual bool DoInterruptInteraction(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, int currentState)
        {
            return false;
        }
        protected virtual bool DoStartInterraction(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, int currentState)
        {
            return true;
        }
        protected virtual bool DoResetState(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, int currentState)
        {
            return true;
        }

        // Event handlers
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (handling == EnumHandHandling.PreventDefault) return;

            if (debugLogging) api.Logger.Debug("OnHeldInteractStart - state: " + GetCurrentState(slot));

            InitInteraction(GetCurrentState(slot), slot, byEntity, blockSel, entitySel, firstEvent);

            if (DoStartInterraction(slot, byEntity, blockSel, entitySel, firstEvent, GetCurrentState(slot)))
            {
                handling = EnumHandHandling.PreventDefault;
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null || slot.Itemstack == null) return false;

            int stepBy = StepInteraction(GetCurrentState(slot), secondsUsed, slot, byEntity, blockSel, entitySel);

            if (stepBy <= 0) return true;

            bool interrupt = DoInterruptInteraction(stepBy, slot, byEntity, blockSel, entitySel, GetCurrentState(slot));

            AdvanceTemporaryState(slot, stepBy);
                
            if (DoAdvancePermanentState(secondsUsed, slot, byEntity, blockSel, entitySel, GetCurrentState(slot) - 1))
            {
                if (debugLogging) api.Logger.Debug("OnHeldInteractStep - AdvancePermanentState to: " + GetCurrentState(slot));

                AdvancePermanentState(slot);
            }

            return !interrupt;
        }
        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (debugLogging) api.Logger.Debug("OnHeldInteractCancel - state: " + GetCurrentState(slot));

            return CancelInteraction(GetCurrentState(slot), secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null)
            {
                api.Logger.Debug("OnHeldInteractStop - slot is null");
                return;
            }

            if (slot.Itemstack == null)
            {
                api.Logger.Debug("OnHeldInteractStop - slot is Itemstack");
                return;
            }

            if (debugLogging) api.Logger.Debug("OnHeldInteractStop - state: " + GetCurrentState(slot));

            FinishInteraction(GetCurrentState(slot), secondsUsed, slot, byEntity, blockSel, entitySel);

            if (DoResetState(secondsUsed, slot, byEntity, blockSel, entitySel, GetCurrentState(slot)))
            {
                ResetPermanentState(slot);
            }

            ResetTemporaryState(slot);

            ResetInteraction(GetCurrentState(slot), secondsUsed, slot, byEntity, blockSel, entitySel);
        }
        
        // Other
        public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity byEntity)
        {
            return null;
        }
        public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
        {
            return null;
        }

        // Supplementary
        protected void SetPermanentState(ItemSlot slot, int state)
        {
            slot.Itemstack.Attributes.SetInt(permanentStateKey, state);
            slot.MarkDirty();
        }
        protected void SetTemporaryState(ItemSlot slot, int state)
        {
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            slot.MarkDirty();
        }
        protected void AdvancePermanentState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0); // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(permanentStateKey, state);
            slot.MarkDirty();
        }
        protected void AdvanceTemporaryState(ItemSlot slot, int delta = 1)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0) + delta; // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            slot.MarkDirty();
        }
        protected void ResetTemporaryState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(permanentStateKey, 0); // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            slot.MarkDirty();
        }
        protected void ResetPermanentState(ItemSlot slot)
        {
            slot.Itemstack.Attributes.SetInt(permanentStateKey, 0);
            slot.MarkDirty();
        }
        protected int GetCurrentState(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0);
        }
        protected int GetCurrentPermanentState(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetAsInt(permanentStateKey, 0);
        }
    }
}