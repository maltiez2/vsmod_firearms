using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Operations
{
    public class SimpleOperation : UniqueIdFactoryObject, IOperation
    {
        public const string mainTransitionsAttrName = "states";
        public const string systemsAttrName = "systems";
        public const string initialStateAttrName = "initial";
        public const string finalStateAttrName = "final";
        public const string inputAttrName = "input";
        public const string attributesAttrName = "attributes";

        private readonly Dictionary<string, string> mStatesInitialData = new();
        private readonly Dictionary<string, JsonObject> mSystemsInitialData = new();
        private string mInputInitialData;

        private readonly Dictionary<IState, IState> mStates = new();
        private readonly Dictionary<ISystem, JsonObject> mSystems = new();
        
        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            JsonObject[] mainTransitions = definition[mainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                mStatesInitialData.Add(transition[initialStateAttrName].AsString(), transition[finalStateAttrName].AsString());
            }

            JsonObject[] systems = definition[systemsAttrName].AsArray();
            foreach (JsonObject system in systems)
            {
                mSystemsInitialData.Add(system["code"].AsString(), system);
            }

            mInputInitialData = definition[inputAttrName].AsString();
        }

        public List<string> GetInputs()
        {
            return new List<string>() { mInputInitialData };
        }

        public List<string> GetInitialStates()
        {
            List<string> output = new();

            foreach (var entry in mStatesInitialData)
            {
                output.Add(entry.Key);
            }

            return output;
        }
        public List<string> GetFinalStates()
        {
            List<string> output = new();

            foreach (var entry in mStatesInitialData)
            {
                output.Add(entry.Value);
            }

            return output;
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
