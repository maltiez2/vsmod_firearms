using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace MaltiezFirearms
{
    public class ItemMultistateWeapon : Item
    {
        public const string temporaryStateKey = "weaponTemporaryState";
        public const string permanentStateKey = "weaponPermanentState";
        public const bool debugLogging = false;
        public const bool markSlotsDirty = true;

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

            int currentState = GetCurrentState(slot);
            
            InitInteraction(currentState, slot, byEntity, blockSel, entitySel, firstEvent);

            if (DoStartInterraction(slot, byEntity, blockSel, entitySel, firstEvent, currentState))
            {
                handling = EnumHandHandling.PreventDefault;
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null || slot.Itemstack == null) return false;

            int currentState = GetCurrentState(slot);

            int stepBy = StepInteraction(currentState, secondsUsed, slot, byEntity, blockSel, entitySel);

            if (stepBy <= 0) return true;
            
            bool interrupt = DoInterruptInteraction(stepBy, slot, byEntity, blockSel, entitySel, currentState);

            AdvanceTemporaryState(slot, stepBy);

            int previousTemporaryState = currentState + stepBy - 1;

            if (DoAdvancePermanentState(secondsUsed, slot, byEntity, blockSel, entitySel, previousTemporaryState))
            {
                if (debugLogging) api.Logger.Debug("OnHeldInteractStep - AdvancePermanentState from: " + currentState);

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

            int currentState = GetCurrentState(slot);

            if (debugLogging) api.Logger.Debug("OnHeldInteractStop - state: " + currentState);

            FinishInteraction(currentState, secondsUsed, slot, byEntity, blockSel, entitySel);

            if (DoResetState(secondsUsed, slot, byEntity, blockSel, entitySel, currentState))
            {
                ResetPermanentState(slot);
            }

            int resetedState = ResetTemporaryState(slot);

            ResetInteraction(resetedState, secondsUsed, slot, byEntity, blockSel, entitySel);
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
        private void SetPermanentState(ItemSlot slot, int state) // Not supported due to code rewrite
        {
            slot.Itemstack.Attributes.SetInt(permanentStateKey, state);
            slot.MarkDirty();
        }
        private void SetTemporaryState(ItemSlot slot, int state) // Not supported due to code rewrite
        {
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            slot.MarkDirty();
        }
        private void AdvancePermanentState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0); // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(permanentStateKey, state);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private void AdvanceTemporaryState(ItemSlot slot, int delta = 1)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0) + delta; // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private int ResetTemporaryState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(permanentStateKey, 0); // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(temporaryStateKey, state);
            if (markSlotsDirty) slot.MarkDirty();
            return state;
        }
        private void ResetPermanentState(ItemSlot slot)
        {
            slot.Itemstack.Attributes.SetInt(permanentStateKey, 0);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private int GetCurrentState(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetAsInt(temporaryStateKey, 0);
        }
        private int GetCurrentPermanentState(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetAsInt(permanentStateKey, 0);
        }
    }
}