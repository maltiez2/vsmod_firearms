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

        public string AmmoStackAttrName = "firearms.ammo.id";
        
        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            // Nothing to init
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
            AmmoStackAttrName += GetId().ToString();
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
                PutAmmoBack(slot, player);
                return true;
            }

            string ammoCode = parameters[ammoCodeAttrName].AsString();
            ItemSlot ammoSlot = GetAmmoSlot(player, ammoCode);
            if (ammoSlot == null) return false;

            WriteAmmoStackTo(slot, ammoSlot.TakeOut(1));

            return true;
        }
        public virtual ItemStack GetSelectedAmmo(ItemSlot slot)
        {
            return ReadAmmoStackFrom(slot);
        }
        public virtual ItemStack TakeSelectedAmmo(ItemSlot slot)
        {
            return TakeAmmoStackFrom(slot);
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
        protected void PutAmmoBack(ItemSlot slot, EntityAgent player)
        {
            ItemStack ammoStack = TakeAmmoStackFrom(slot);
            if (ammoStack != null) player.TryGiveItemStack(ammoStack);
        }

        protected void WriteAmmoStackTo(ItemSlot slot, ItemStack ammoStack)
        {
            slot.Itemstack.Attributes.SetItemstack(AmmoStackAttrName, ammoStack);
        }
        protected ItemStack ReadAmmoStackFrom(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetItemstack(AmmoStackAttrName, null);
        }
        protected ItemStack TakeAmmoStackFrom(ItemSlot slot)
        {
            ItemStack ammoStack = ReadAmmoStackFrom(slot);
            slot.Itemstack.Attributes.RemoveAttribute(AmmoStackAttrName);
            return ammoStack;
        }
    }
}
