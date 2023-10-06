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
        public const string inputsToInterceptAttrName = "inputsToIntercept";

        private readonly Dictionary<string, string> mStatesInitialData = new();
        private readonly Dictionary<string, JsonObject> mSystemsInitialData = new();
        private readonly List<Tuple<string, string, string>> mTransitions = new();
        private readonly List<string> mInputsToPreventInitialData = new();

        private readonly Dictionary<IState, IState> mStates = new();
        private readonly Dictionary<ISystem, JsonObject> mSystems = new();
        private readonly List<IInput> mInputsToPrevent = new();

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            List<string> inputs = new List<string>();
            if (definition[inputAttrName].IsArray())
            {
                foreach (JsonObject input in definition[inputAttrName].AsArray())
                {
                    inputs.Add(input.AsString());
                }
            }
            else
            {
                inputs.Add(definition[inputAttrName].AsString());
            }

            if (definition.KeyExists(inputsToInterceptAttrName))
            {
                foreach (JsonObject input in definition[inputsToInterceptAttrName].AsArray())
                {
                    mInputsToPreventInitialData.Add(input.AsString());
                }
            }

            JsonObject[] mainTransitions = definition[mainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                mStatesInitialData.Add(transition[initialStateAttrName].AsString(), transition[finalStateAttrName].AsString());

                foreach (string input in inputs)
                {
                    mTransitions.Add(new(input, transition[initialStateAttrName].AsString(), transition[finalStateAttrName].AsString()));
                }

                foreach (string input in mInputsToPreventInitialData)
                {
                    mTransitions.Add(new(input, transition[initialStateAttrName].AsString(), transition[initialStateAttrName].AsString()));
                }
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

            foreach (string input in mInputsToPreventInitialData)
            {
                mInputsToPrevent.Add(inputs[input]);
            }
            mInputsToPreventInitialData.Clear();
        }

        public IState Perform(ItemSlot slot, EntityAgent player, IState state, IInput input)
        {
            if (mInputsToPrevent.Contains(input)) return state;
            
            foreach (var entry in mSystems)
            {
                if (!entry.Key.Verify(slot, player, entry.Value[attributesAttrName]))
                {
                    return state;
                }
            }

            foreach (var entry in mSystems)
            {
                entry.Key.Process(slot, player, entry.Value[attributesAttrName]);
            }

            return mStates[state];
        }
        public bool StopTimer(ItemSlot slot, EntityAgent player, IState state, IInput input)
        {
            return false;
        }
        public int? Timer(ItemSlot slot, EntityAgent player, IState state, IInput input)
        {
            return null;
        }
    }
}
