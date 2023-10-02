using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;
using System;

namespace MaltiezFirearms.FiniteStateMachine.Operations
{
    public class BasicInstant : UniqueIdFactoryObject, IOperation
    {
        public const string mainTransitionsAttrName = "states";
        public const string systemsAttrName = "systems";
        public const string initialStateAttrName = "initial";
        public const string finalStateAttrName = "final";
        public const string inputAttrName = "input";
        public const string attributesAttrName = "attributes";

        private readonly Dictionary<string, string> mStatesInitialData = new();
        private readonly Dictionary<string, JsonObject> mSystemsInitialData = new();
        private readonly List<Tuple<string, string, string>> mTransitions = new();

        private readonly Dictionary<IState, IState> mStates = new();
        private readonly Dictionary<ISystem, JsonObject> mSystems = new();
        
        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            string inputInitialData = definition[inputAttrName].AsString();

            JsonObject[] mainTransitions = definition[mainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                mStatesInitialData.Add(transition[initialStateAttrName].AsString(), transition[finalStateAttrName].AsString());
                mTransitions.Add(new(inputInitialData, transition[initialStateAttrName].AsString(), transition[finalStateAttrName].AsString()));
            }

            JsonObject[] systems = definition[systemsAttrName].AsArray();
            foreach (JsonObject system in systems)
            {
                mSystemsInitialData.Add(system["code"].AsString(), system);
            }
        }

        public List<Tuple<string, string, string>> GetTransitions()
        {
            return mTransitions;
        }

        public void SetInputsStatesSystems(Dictionary<string, IInput> inputs, Dictionary<string, IState> states, Dictionary<string, ISystem> systems)
        {
            foreach (var entry in mStatesInitialData)
            {
                mStates.Add(states[entry.Key], states[entry.Value]);
            }
            mStatesInitialData.Clear();

            foreach (var entry in mSystemsInitialData)
            {
                mSystems.Add(systems[entry.Key], entry.Value);
            }
            mSystemsInitialData.Clear();
        }

        public IState Perform(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            foreach (var entry in mSystems)
            {
                if (!entry.Key.Verify(weaponSlot, player, entry.Value[attributesAttrName]))
                {
                    return state;
                }
            }

            foreach (var entry in mSystems)
            {
                entry.Key.Process(weaponSlot, player, entry.Value[attributesAttrName]);
            }

            return mStates[state];
        }
        public int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            return null;
        }
    }
}
