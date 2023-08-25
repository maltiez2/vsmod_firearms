using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using Vintagestory.Server;
using Vintagestory.API.Server;

namespace MaltiezFirearms
{
    public class ItemMultistateWeapon : Item
    {
        public const string serverTemporaryStateKey = "weaponServerTemporaryState";
        public const string clientTemporaryState = "weaponClientTemporaryState";
        public const string permanentStateKey = "weaponPermanentState";
        public const bool markSlotsDirty = true;

        private int statesNumber = 0;

        private const bool debugLogging = false;


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
            if (!SetStatesNumber(slot)) return;

            ResyncStates(slot);
            int currentState = GetCurrentState(slot);
            InitInteraction(currentState, slot, byEntity, blockSel, entitySel, firstEvent);

            if (DoStartInterraction(slot, byEntity, blockSel, entitySel, firstEvent, currentState))
            {
                handling = EnumHandHandling.PreventDefault;
            }

            api.Logger.Warning("[Firearms] [OnHeldInteractStart] set slot");
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
                AdvancePermanentState(slot);
            }

            return !interrupt;
        }
        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            bool specialCase = api.Side == EnumAppSide.Server && cancelReason == EnumItemUseCancelReason.ChangeSlot;
            bool cancelReturn = CancelInteraction(GetCurrentState(slot), secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);

            if (specialCase) OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            return cancelReturn;
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null || slot.Itemstack == null) return;

            int currentState = GetCurrentState(slot);

            FinishInteraction(currentState, secondsUsed, slot, byEntity, blockSel, entitySel);

            int resetedState = 0;
            if (DoResetState(secondsUsed, slot, byEntity, blockSel, entitySel, currentState))
            {
                ResetState(slot);
            }
            else
            {
                resetedState = ResetTemporaryState(slot);
            }

            ResetInteraction(resetedState, secondsUsed, slot, byEntity, blockSel, entitySel);

            ResyncStates(slot);
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
        protected bool SetStatesNumber(ItemSlot slot)
        {
            if (slot == null || slot.Itemstack == null || slot.Itemstack.Collectible == null || slot.Itemstack.Collectible.Attributes == null) return false;
            if (!slot.Itemstack.Collectible.Attributes.KeyExists("operationStages")) return false;
            
            statesNumber = slot.Itemstack.Collectible.Attributes["operationStages"].AsArray().Length;
            return true;
        }

        // State setters & getters
        protected void UnstuckState(ItemSlot slot)
        {
            int serverState = slot.Itemstack.Attributes.GetInt(serverTemporaryStateKey, 0);
            slot.Itemstack.TempAttributes.SetInt(clientTemporaryState, serverState);
        }
        
        private void AdvancePermanentState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(GetTemporaryStateKey(), 0); // TODO Possible race condition
            slot.Itemstack.Attributes.SetInt(permanentStateKey, state);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private void AdvanceTemporaryState(ItemSlot slot, int delta = 1)
        {
            int state = GameMath.Min(GetTemporaryStateImpl(slot) + delta, statesNumber - 1); // TODO Possible race condition
            SetTemporaryStateImpl(slot, state);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private int ResetTemporaryState(ItemSlot slot)
        {
            int state = slot.Itemstack.Attributes.GetAsInt(permanentStateKey, 0); // TODO Possible race condition
            SetTemporaryStateImpl(slot, state);
            if (markSlotsDirty) slot.MarkDirty();
            return state;
        }
        private void ResetState(ItemSlot slot)
        {
            slot.Itemstack.Attributes.SetInt(permanentStateKey, 0);
            SetTemporaryStateImpl(slot, 0);
            if (markSlotsDirty) slot.MarkDirty();
        }
        private int GetCurrentState(ItemSlot slot)
        {
            return GetTemporaryStateImpl(slot);
        }
        private string GetTemporaryStateKey()
        {
            if (api.Side == EnumAppSide.Server)
            {
                return serverTemporaryStateKey;
            }
            else
            {
                return clientTemporaryState;
            }
        }
        private int GetTemporaryStateImpl(ItemSlot slot)
        {
            if (api.Side == EnumAppSide.Server)
            {
                return slot.Itemstack.Attributes.GetInt(serverTemporaryStateKey, 0);
            }
            else
            {
                return slot.Itemstack.TempAttributes.GetInt(clientTemporaryState, 0);
            }
        }
        private void SetTemporaryStateImpl(ItemSlot slot, int state)
        {
            if (api.Side == EnumAppSide.Server)
            {
                slot.Itemstack.Attributes.SetInt(serverTemporaryStateKey, state);
            }
            else
            {
                slot.Itemstack.TempAttributes.SetInt(clientTemporaryState, state);
            }
        }
        private void ResyncStates(ItemSlot slot)
        {
            if (api.Side == EnumAppSide.Client)
            {
                int serverState = slot.Itemstack.Attributes.GetInt(serverTemporaryStateKey, 0);
                int clientState = slot.Itemstack.TempAttributes.GetInt(clientTemporaryState, 0);
                if (serverState != clientState)
                {
                    slot.Itemstack.TempAttributes.SetInt(clientTemporaryState, serverState);
                    if (debugLogging) api.Logger.Debug("ResyncStates to " + serverState);
                }
            }
        }
    }
}