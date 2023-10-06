using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal class BasicPlayerAnimation : UniqueIdFactoryObject, ISystem
    {
        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
        }

        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
        }

        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters["code"].AsString();
            string type = parameters["type"].AsString();
            switch (type)
            {
                case "start":
                    player.AnimManager.StartAnimation(code);
                    break;
                case "stop":
                    player.AnimManager.StopAnimation(code);
                    break;
            }
            return true;
        }

        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
    }
}
