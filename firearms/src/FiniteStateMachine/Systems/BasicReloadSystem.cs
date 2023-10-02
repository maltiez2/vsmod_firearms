using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{  
    internal class BasicReloadSystem : UniqueIdFactoryObject, ISystem, IAmmoSelector
    {
        public const string ammoCodeAttrName = "ammoCode";
        public const string actionAttrName = "action";
        public const string takeAction = "take";
        public const string putAction = "put";

        private ItemStack mAmmo;
        
        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            // Nothing to init
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
            // Does not require access to other systems
        }
        public virtual bool Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters[actionAttrName].AsString();
            if (action == putAction) return true;
  
            string ammoCode = parameters[ammoCodeAttrName].AsString();
            return GetAmmoSlot(player, ammoCode) != null;
        }
        public virtual bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters[actionAttrName].AsString();
            if (action == putAction)
            {
                PutAmmoBack(player);
                return true;
            }

            string ammoCode = parameters[ammoCodeAttrName].AsString();
            ItemSlot ammoSlot = GetAmmoSlot(player, ammoCode);
            if (ammoSlot == null) return false;
            
            mAmmo = ammoSlot.TakeOut(1);
            return true;
        }
        public virtual ItemStack GetSelectedAmmo()
        {
            return mAmmo;
        }

        protected ItemSlot GetAmmoSlot(EntityAgent player, string ammoCode)
        {
            ItemSlot slot = null;

            player.WalkInventory((inventorySlot) =>
            {
                if (inventorySlot is ItemSlotCreative) return true;

                if (inventorySlot.Itemstack != null && inventorySlot.Itemstack.Collectible.Code.Path.Contains(ammoCode))
                {
                    slot = inventorySlot;
                    return false;
                }

                return true;
            });

            return slot;
        }
        protected void PutAmmoBack(EntityAgent player)
        {
            if (mAmmo != null) player.TryGiveItemStack(mAmmo);
            mAmmo = null;
        }
    }
}
