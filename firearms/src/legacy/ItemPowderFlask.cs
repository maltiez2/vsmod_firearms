using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace MaltiezFirearms
{
    public class ItemPowderFlask : Item
    {
        private const string powderStackKey = "powderStack";
        private const string powderCode = "gunpowder"; // TODO move into item json
        private const int powderToDurabilityRatio = 8; // TODO move into item json (flask OR powder)

        public override void DamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, int amount = 1)
        {
            TryRetrieveFrom(itemslot, amount);
        }

        public virtual bool ChangeDurability(ItemSlot itemslot, int amount)
        {
            ItemStack itemstack = itemslot.Itemstack;

            if (amount >= 0 && itemstack.Collectible.GetRemainingDurability(itemstack) >= itemstack.Collectible.GetMaxDurability(itemstack))
            {
                return false;
            }
            
            int remainingDurability = itemstack.Collectible.GetRemainingDurability(itemstack) + amount;
            remainingDurability = Math.Min(itemstack.Collectible.GetMaxDurability(itemstack), remainingDurability);
            
            if (remainingDurability < 0)
            {
                return false;
            }
            
            itemstack.Attributes.SetInt("durability", Math.Max(remainingDurability, 0));
            
            if (remainingDurability == 0)
            {
                itemslot.Itemstack.Attributes.SetItemstack(powderStackKey, null);
            }

            itemslot.MarkDirty();

            return true;
        }
        public override int GetRemainingDurability(ItemStack itemstack)
        {
            return (int)itemstack.Attributes.GetDecimal("durability", 0);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel != null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }


            if (!(byEntity is EntityPlayer)) return;

            handling = EnumHandHandling.PreventDefault;

            ItemSlot leftHandItemSlot = byEntity.LeftHandItemSlot;

            if (byEntity.Controls.CtrlKey)
            {
                ItemStack content = Empty(slot);

                if (content != null && !byEntity.TryGiveItemStack(content))
                {
                    ChangeDurability(slot, powderToDurabilityRatio * content.StackSize);
                    slot.Itemstack.Attributes.SetItemstack(powderStackKey, content);
                    slot.MarkDirty();
                }

                return;
            }

            if (!TryPutInto(slot, leftHandItemSlot))
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return true;
        }

        public virtual bool TryPutInto(ItemSlot container, ItemSlot filling)
        {
            if (!IsStorable(filling.Itemstack)) return false;

            if (!ChangeDurability(container, powderToDurabilityRatio)) return false;

            ItemStack fillingStack = filling.TakeOut(1);

            container.Itemstack.Attributes.SetItemstack(powderStackKey, fillingStack);

            container.MarkDirty();
            filling.MarkDirty();
            return true;
        }
        public virtual ItemStack TryRetrieveFrom(ItemSlot container, int amount = 1)
        {
            ItemStack itemStack = container.Itemstack.Attributes.GetItemstack(powderStackKey);

            if (itemStack == null) return null;

            if (!ChangeDurability(container, -amount)) return null;

            itemStack.StackSize = (int)Math.Floor((decimal)(amount / powderToDurabilityRatio));

            if (api.World != null)
            {
                itemStack.ResolveBlockOrItem(api.World);
            }

            return itemStack;
        }
        public virtual ItemStack Empty(ItemSlot container)
        {
            return TryRetrieveFrom(container, container.Itemstack.Collectible.GetRemainingDurability(container.Itemstack));
        }

        protected virtual bool IsStorable(ItemStack item)
        {
            if (item == null || item.Collectible == null) return false;

            return item.Collectible.Code.Path.StartsWith(powderCode);
        }
    }

}