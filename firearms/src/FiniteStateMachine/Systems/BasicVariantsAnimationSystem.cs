using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public class BasicVariantsAnimation : UniqueIdFactoryObject, ISystem
    {
        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            throw new System.NotImplementedException();
        }

        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
            throw new System.NotImplementedException();
        }

        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            throw new System.NotImplementedException();
        }

        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
