using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.FiniteStateMachine.Operations
{
    public interface ITransition
    {
        void Init(IState initial, IState final, Dictionary<ISystem, JsonObject> systems);

    }
    
    public class BasicDelayed : UniqueIdFactoryObject, IOperation
    {
        public const string mainTransitionsAttrName = "states";
        public const string systemsAttrName = "systems";
        public const string inputsAttrName = "inputsToHandle";
        public const string inputsToInterceptAttrName = "inputsToIntercept";
        public const string attributesAttrName = "attributes";
        public const string initialName = "initial";
        public const string cancelAttrName = "cancel";
        public const string finalAttrName = "final";
        public const string delayAttrName = "delay_ms";

        private const string cTimerInput = "";
        
        private ICoreAPI mApi;

        // Initial data for operation's logic
        private readonly List<Tuple<string, string>> mStatesInitialData = new();
        private readonly Dictionary<Tuple<string, string>, Tuple<string, Dictionary<string, JsonObject>>> mTransitionsInitialData = new();
        private readonly Dictionary<Tuple<string, string>, int?> mTimersInitialData = new();
        private readonly List<string> mInputsInitialData = new();
        private readonly List<Tuple<string, string, string>> mTriggerConditions = new();
        private readonly List<string> mInputsToPreventInitialData = new();

        // Final data for operation's logic
        private readonly Dictionary<Tuple<IState, IInput>, Tuple<IState, Dictionary<ISystem, JsonObject>>> mTransitions = new();
        private readonly Dictionary<Tuple<IState, IInput>, int?> mTimers = new();
        private readonly List<IInput> mInputsToPrevent = new();

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            foreach (JsonObject input in definition[inputsToInterceptAttrName].AsArray())
            {
                mInputsToPreventInitialData.Add(input.AsString());
            }

            mApi = api;

            Dictionary<string, JsonObject> systemsInitial = new();
            Dictionary<string, JsonObject> systemsCancel = new();
            Dictionary<string, JsonObject> systemsFinal = new();

            JsonObject[] systems = definition[systemsAttrName][initialName].AsArray();
            foreach (JsonObject system in systems)
            {
                systemsInitial.Add(system["code"].AsString(), system[attributesAttrName]);
            }
            systems = definition[systemsAttrName][cancelAttrName].AsArray();
            foreach (JsonObject system in systems)
            {
                systemsCancel.Add(system["code"].AsString(), system[attributesAttrName]);
            }
            systems = definition[systemsAttrName][finalAttrName].AsArray();
            foreach (JsonObject system in systems)
            {
                systemsFinal.Add(system["code"].AsString(), system[attributesAttrName]);
            }

            List<string> cancelInputs = new();
            if (definition[inputsAttrName][cancelAttrName].IsArray())
            {
                foreach (JsonObject cancelInput in definition[inputsAttrName][cancelAttrName].AsArray())
                {
                    cancelInputs.Add(cancelInput.AsString());
                    mInputsInitialData.Add(cancelInput.AsString());
                }
            }
            else
            {
                cancelInputs.Add(definition[inputsAttrName][cancelAttrName].AsString());
                mInputsInitialData.Add(definition[inputsAttrName][cancelAttrName].AsString());
            }

            List<string> inputsInitial = new List<string>();
            if (definition[inputsAttrName][initialName].IsArray())
            {
                foreach (JsonObject input in definition[inputsAttrName][initialName].AsArray())
                {
                    inputsInitial.Add(input.AsString());
                }
            }
            else
            {
                inputsInitial.Add(definition[initialName].AsString());
            }

            int? timerDelay = definition.KeyExists(delayAttrName) ? definition[delayAttrName].AsInt() : null;

            JsonObject[] mainTransitions = definition[mainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                string initialState = transition[initialName].AsString();
                string finalState = transition[finalAttrName].AsString();
                string cancelState = transition[cancelAttrName].AsString();
                string intermediateState = initialState + "_to_" + finalState + "_op." + code;

                mStatesInitialData.Add(new Tuple<string, string>(initialState, intermediateState));
                mStatesInitialData.Add(new Tuple<string, string>(intermediateState, finalState));
                mStatesInitialData.Add(new Tuple<string, string>(intermediateState, cancelState));

                foreach (string inputInitial in inputsInitial)  mTransitionsInitialData.Add(new Tuple<string, string>(initialState, inputInitial), new Tuple<string, Dictionary<string, JsonObject>>(intermediateState,  systemsInitial));
                foreach (string inputInitial in inputsInitial) mTransitionsInitialData.Add(new Tuple<string, string>(intermediateState, inputInitial), new Tuple<string, Dictionary<string, JsonObject>>(finalState, systemsFinal));
                foreach (string inputCancel in cancelInputs) mTransitionsInitialData.Add(new Tuple<string, string>(intermediateState, inputCancel), new Tuple<string, Dictionary<string, JsonObject>>(cancelState, systemsCancel));

                foreach (string inputInitial in inputsInitial) mTimersInitialData.Add(new Tuple<string, string>(initialState, inputInitial), timerDelay);

                foreach (string inputInitial in inputsInitial) mTriggerConditions.Add(new(inputInitial, initialState, intermediateState));
                mTriggerConditions.Add(new(cTimerInput, intermediateState, finalState));
                foreach (string inputCancel in cancelInputs) mTriggerConditions.Add(new(inputCancel, intermediateState, cancelState));

                foreach (string input in mInputsToPreventInitialData)
                {
                    mTriggerConditions.Add(new(input, intermediateState, intermediateState));
                    mStatesInitialData.Add(new Tuple<string, string>(intermediateState, intermediateState));
                    mTransitionsInitialData.Add(new Tuple<string, string>(intermediateState, input), new Tuple<string, Dictionary<string, JsonObject>>(intermediateState, new Dictionary<string, JsonObject>()));
                }
            }

        }

        public List<Tuple<string, string, string>> GetTransitions()
        {
            return mTriggerConditions;
        }

        public void SetInputsStatesSystems(Dictionary<string, IInput> inputs, Dictionary<string, IState> states, Dictionary<string, ISystem> systems)
        {
            foreach (var entry in mTransitionsInitialData)
            {
                Dictionary<ISystem, JsonObject> transitionSystems = new();
                foreach (var systemEntry in entry.Value.Item2)
                {
                    transitionSystems.Add(systems[systemEntry.Key], systemEntry.Value);
                }

                Tuple<IState, IInput> transitionFrom = new(states[entry.Key.Item1], inputs[entry.Key.Item2]);
                Tuple<IState, Dictionary<ISystem, JsonObject>> transitionTo = new(states[entry.Value.Item1], transitionSystems);

                mTransitions.Add(transitionFrom, transitionTo);
            }

            foreach (var entry in mTimersInitialData)
            {
                Tuple<IState, IInput> transition = new(states[entry.Key.Item1], inputs[entry.Key.Item2]);
                mTimers.Add(transition, entry.Value);
            }

            foreach (string input in mInputsToPreventInitialData)
            {
                mInputsToPrevent.Add(inputs[input]);
            }

            mStatesInitialData.Clear();
            mTransitionsInitialData.Clear();
            mTimersInitialData.Clear();
            mInputsInitialData.Clear();
            mInputsToPreventInitialData.Clear();
        }
        public bool StopTimer(ItemSlot slot, EntityAgent player, IState state, IInput input)
        {
            Tuple<IState, IInput> transitionId = new Tuple<IState, IInput>(state, input);

            if (!mTransitions.ContainsKey(transitionId)) return false;

            return !mInputsToPrevent.Contains(input);
        }
        public IState Perform(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            Tuple<IState, IInput> transitionId = new Tuple<IState, IInput>(state, input);

            if (!mTransitions.ContainsKey(transitionId)) return state;
            
            (IState newState, Dictionary<ISystem, JsonObject> systems) = mTransitions[transitionId];


            foreach (var entry in systems)
            {
                if (!entry.Key.Verify(weaponSlot, player, entry.Value))
                {
                    return state;
                }
            }

            foreach (var entry in systems)
            {
                entry.Key.Process(weaponSlot, player, entry.Value);
            }

            return newState;
        }
        public int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            Tuple<IState, IInput> transitionId = new Tuple<IState, IInput>(state, input);

            return mTimers.ContainsKey(transitionId) ? mTimers[transitionId] : null;
        }
    }
}
