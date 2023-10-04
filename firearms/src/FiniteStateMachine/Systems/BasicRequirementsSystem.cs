using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal class BasicRequirements : UniqueIdFactoryObject, ISystem
    {
        private struct OperationRequirement
        {
            public string code { get; set; }
            public int amount { get; set; }
            public int durability { get; set; }
            public int offHand { get; set; }

            public static implicit operator OperationRequirement((string code, int amount, int durability, int offHand) parameters)
            {
                return new OperationRequirement() { code = parameters.code, amount = parameters.amount, durability = parameters.durability, offHand = parameters.offHand };
            }
        }

        private readonly Dictionary<string, JsonObject> mRequirements = new();

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            foreach(JsonObject requrementPack in definition["requirementSets"].AsArray())
            {
                mRequirements.Add(requrementPack["code"].AsString(), requrementPack["requirements"]);
            }
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
        }
        public virtual bool Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters["code"].AsString();
            return Check(player, mRequirements[code]);
        }
        public virtual bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters["code"].AsString();
            switch (parameters["type"].AsString())
            {
                case "check":
                    return Check(player, mRequirements[code]);
                case "take":
                    return Consume(player, mRequirements[code]);
                default:
                    return false;
            }
        }


        private bool Check(EntityAgent byEntity, JsonObject requirements)
        {
            List<OperationRequirement> requirementsList = GetRequirements(requirements);
            foreach (OperationRequirement requirement in requirementsList)
            {
                if ((GetNextRequirement(byEntity, requirement) == null) != (requirement.offHand == 0)) return false;
            }

            return true;
        }

        private bool Consume(EntityAgent byEntity, JsonObject requirements)
        {
            List<OperationRequirement> requirementsList = GetRequirements(requirements);
            foreach (OperationRequirement requirement in requirementsList)
            {
                if ((GetNextRequirement(byEntity, requirement) == null) != (requirement.offHand == 0)) return false;
            }

            foreach (OperationRequirement requirement in requirementsList)
            {
                ItemSlot ammoSlot = GetNextRequirement(byEntity, requirement);

                if (requirement.durability > 0)
                {
                    ammoSlot?.Itemstack.Item.DamageItem(byEntity.World, byEntity, ammoSlot, requirement.durability);
                    ammoSlot?.MarkDirty();
                }
                else if (requirement.durability < 0 && requirement.amount > 0)
                {
                    ammoSlot?.TakeOut(requirement.amount);
                    ammoSlot?.MarkDirty();
                }
            }

            return true;
        }

        private ItemSlot GetNextRequirement(EntityAgent byEntity, OperationRequirement requirement)
        {
            ItemSlot slot = null;

            if (requirement.offHand >= 0)
            {
                if (byEntity.LeftHandItemSlot.Itemstack == null) return null;
                if (!byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path.StartsWith(requirement.code)) return null;
                if (byEntity.RightHandItemSlot.StackSize < requirement.amount) return null;
                if (requirement.durability > 0 && byEntity.LeftHandItemSlot.Itemstack.Item.GetRemainingDurability(byEntity.LeftHandItemSlot.Itemstack) < requirement.durability) return null;

                return byEntity.LeftHandItemSlot;
            }

            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.Path.StartsWith(requirement.code) && invslot.Itemstack.StackSize >= requirement.amount)
                {
                    if (requirement.durability > 0 && invslot.Itemstack.Item.GetRemainingDurability(invslot.Itemstack) < requirement.durability)
                    {
                        return true;
                    }

                    slot = invslot;
                    return false;
                }

                return true;
            });

            return slot;
        }

        private List<OperationRequirement> GetRequirements(JsonObject requirements)
        {
            List<OperationRequirement> output = new List<OperationRequirement>();

            foreach (JsonObject requirement in requirements.AsArray())
            {
                string code = requirement["code"].AsString();
                int amount = requirement["amount"].AsInt(1);
                int durability = requirement["durability"].AsInt(-1);
                int offHand = requirement["offhand"].AsInt(-1);

                output.Add((code, amount, durability, offHand));
            }

            return output;
        }
    }
}
