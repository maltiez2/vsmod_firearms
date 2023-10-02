using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{  
    internal class BasicReloadSystem : UniqueIdFactoryObject, ISystem, IAmmoSelector
    {
        public const string AmmoCodeAttrName = "ammoCode";
        public const string ActionAttrName = "action";
        public const string TakeAction = "take";
        public const string PutAction = "put";

        private ItemStack ammo;
        
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
            string action = parameters[ActionAttrName].AsString();
            if (action == PutAction) return true;
  
            string ammoCode = parameters[AmmoCodeAttrName].AsString();
            return GetAmmoSlot(player, ammoCode) == null;
        }
        public virtual bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters[ActionAttrName].AsString();
            if (action == PutAction)
            {
                PutAmmoBack(player);
                return true;
            }

            string ammoCode = parameters[AmmoCodeAttrName].AsString();
            ItemSlot ammoSlot = GetAmmoSlot(player, ammoCode);
            if (ammoSlot == null) return false;
            
            ammo = ammoSlot.TakeOut(1);
            return true;
        }
        public virtual ItemStack GetSelectedAmmo()
        {
            return ammo;
        }

        protected ItemSlot GetAmmoSlot(EntityAgent player, string ammoCode)
        {
            ItemSlot slot = null;

            player.WalkInventory((inventorySlot) =>
            {
                if (inventorySlot is ItemSlotCreative) return true;

                if (inventorySlot.Itemstack != null && inventorySlot.Itemstack.Collectible.Code.Path.StartsWith(ammoCode))
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
            if (ammo != null) player.TryGiveItemStack(ammo);
            ammo = null;
        }
    }
}
