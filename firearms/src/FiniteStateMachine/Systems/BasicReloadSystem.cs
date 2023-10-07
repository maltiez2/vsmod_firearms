using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{  
    internal class BasicReload : UniqueIdFactoryObject, ISystem, IAmmoSelector
    {
        public const string ammoCodeAttrName = "ammoCode";
        public const string actionAttrName = "action";
        public const string amountAttrName = "amount";
        public const string takeAction = "take";
        public const string putAction = "put";

        public string AmmoStackAttrName = "firearms.ammo.";

        private string mCode;
        
        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mCode = code;
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
            AmmoStackAttrName += mCode;
        }
        public bool Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters[actionAttrName].AsString();
            if (action == putAction) return true;
            int amount = 1;
            if (parameters.KeyExists(amountAttrName)) amount = parameters[amountAttrName].AsInt();

            string ammoCode = parameters[ammoCodeAttrName].AsString();
            ItemSlot ammoSlot = GetAmmoSlot(player, ammoCode);
            return ammoSlot != null && ammoSlot != slot && ammoSlot.Itemstack?.StackSize >= amount;
        }
        public bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters[actionAttrName].AsString();
            if (action == putAction)
            {
                PutAmmoBack(slot, player);
                return true;
            }
            int amount = 1;
            if (parameters.KeyExists(amountAttrName)) amount = parameters[amountAttrName].AsInt();

            string ammoCode = parameters[ammoCodeAttrName].AsString();
            ItemSlot ammoSlot = GetAmmoSlot(player, ammoCode);
            if (ammoSlot == null || ammoSlot == slot) return false;

            WriteAmmoStackTo(slot, ammoSlot.TakeOut(amount));

            return true;
        }
        public ItemStack GetSelectedAmmo(ItemSlot slot)
        {
            return ReadAmmoStackFrom(slot);
        }
        public ItemStack TakeSelectedAmmo(ItemSlot slot, int amount = 0)
        {
            return TakeAmmoStackFrom(slot, amount);
        }

        private ItemSlot GetAmmoSlot(EntityAgent player, string ammoCode)
        {
            ItemSlot slot = null;

            player?.WalkInventory((inventorySlot) =>
            {
                if (inventorySlot is ItemSlotCreative) return true;

                if (inventorySlot?.Itemstack?.Collectible?.Code.Path.StartsWith(ammoCode) == true)
                {
                    slot = inventorySlot;
                    return false;
                }

                return true;
            });

            return slot;
        }
        private void PutAmmoBack(ItemSlot slot, EntityAgent player)
        {
            ItemStack ammoStack = TakeAmmoStackFrom(slot);
            if (ammoStack != null) player.TryGiveItemStack(ammoStack);
        }

        private void WriteAmmoStackTo(ItemSlot slot, ItemStack ammoStack)
        {
            slot.Itemstack.Attributes.SetItemstack(AmmoStackAttrName, ammoStack);
            slot.MarkDirty();
        }
        private ItemStack ReadAmmoStackFrom(ItemSlot slot)
        {
            return slot.Itemstack.Attributes.GetItemstack(AmmoStackAttrName, null);
        }
        private ItemStack TakeAmmoStackFrom(ItemSlot slot, int amount = 0)
        {
            ItemStack ammoStack = ReadAmmoStackFrom(slot);
            if (amount == 0 || ammoStack.StackSize == amount)
            {
                slot.Itemstack.Attributes.RemoveAttribute(AmmoStackAttrName);
                slot.MarkDirty();
                return ammoStack;
            }

            if (ammoStack.StackSize < amount) return null;

            ItemStack takenAmmoStack = ammoStack.Clone();
            takenAmmoStack.StackSize = amount;
            ammoStack.StackSize -= amount;

            WriteAmmoStackTo(slot, ammoStack);
            return takenAmmoStack;
        }
    }
}
