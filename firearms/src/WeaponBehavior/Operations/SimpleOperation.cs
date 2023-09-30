using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Operations
{
    public class SimpleOperation : UniqueIdFactoryObject, IOperation
    {
        public override void Init(JsonObject definition, CollectibleObject colelctible)
        {
            throw new NotImplementedException();
        }

        public List<string> GetInputs()
        {
            throw new NotImplementedException();
        }

        public List<string> GetStates()
        {
            throw new NotImplementedException();
        }

        public IState Perform(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            throw new NotImplementedException();
        }

        public void SetInputs(Dictionary<string, IInput> inputs)
        {
            throw new NotImplementedException();
        }

        public void SetStates(Dictionary<string, IState> states)
        {
            throw new NotImplementedException();
        }

        public void SetSystems(Dictionary<string, IWeaponSystem> systems)
        {
            throw new NotImplementedException();
        }

        public int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            throw new NotImplementedException();
        }
    }
}
