using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal class BasicPlayerStats : UniqueIdFactoryObject, ISystem
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
            float value = parameters["value"].AsFloat(0);
            bool persistent = parameters["persist"].AsBool(false);

            (player as EntityPlayer).Stats.Set(code, "maltiezfirearms.system.id" + GetId(), value, persistent);

            return true;
        }

        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
    }
}
