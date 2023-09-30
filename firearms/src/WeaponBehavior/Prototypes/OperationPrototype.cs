﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{
    public class OperationPrototype : UniqueIdFactoryObject, IOperation
    {
        private Dictionary<string, IWeaponSystem> mSytems;
        private Dictionary<string, IState> mStates;
        private Dictionary<string, IInput> mInputs;

        private Dictionary<string, string> mStateToState = new Dictionary<string, string>
        {
            {"A", "Ai"},
            {"Ai", "B"},
            {"B", "A"}
        };

        public override void Init(JsonObject definition, CollectibleObject colelctible)
        {

        }

        public IState Perform(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            foreach (var entry in mStateToState)
            {
                if (state.Equals(mStates[entry.Key]) && mSytems.First().Value.Process(weaponSlot, player, null))
                {
                    return mStates[entry.Value];
                }
            }

            return state;
        }
        public int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            if ((state.Equals(mStates["A"])))
            {
                return 5000;
            }
            
            return null;
        }

        public List<string> GetStates()
        {
            return new List<string> { "A", "Ai", "B" };
        }
        public List<string> GetInputs()
        {
            return new List<string> { "testInput" };
        }
        public void SetSystems(Dictionary<string, IWeaponSystem> systems)
        {
            mSytems = systems;
        }
        public void SetStates(Dictionary<string, IState> states)
        {
            mStates = states;
        }
        public void SetInputs(Dictionary<string, IInput> inputs)
        {
            mInputs = inputs;
        }
    }
}
