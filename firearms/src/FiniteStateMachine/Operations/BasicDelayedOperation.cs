using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using System.Threading.Channels;
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
        public const string inputsAttrName = "inputs";
        public const string attributesAttrName = "attributes";
        public const string initialName = "initial";
        public const string cancelAttrName = "cancel";
        public const string finalAttrName = "final";
        public const string delayAttrName = "delay_ms";

        // Initial data for operation's logic
        private readonly List<Tuple<string, string>> mStatesInitialData = new();
        private readonly Dictionary<Tuple<string, string>, Tuple<string, Dictionary<string, JsonObject>>> mTransitionsInitialData = new();
        private readonly Dictionary<Tuple<string, string>, int?> mTimersInitialData = new();
        private readonly List<string> mInputsInitialData = new();
        private readonly List<Tuple<string, string, string>> mTriggerConditions = new();

        // Final data for operation's logic
        private readonly Dictionary<Tuple<IState, IInput>, Tuple<IState, Dictionary<ISystem, JsonObject>>> mTransitions = new();
        private readonly Dictionary<Tuple<IState, IInput>, int?> mTimers = new();

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
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

            string inputInitial = definition[inputsAttrName][initialName].AsString();
            string inputCancel = definition[inputsAttrName][cancelAttrName].AsString();
            mInputsInitialData.Add(inputInitial);
            mInputsInitialData.Add(inputCancel);
            int? timerDelay = definition.KeyExists(delayAttrName) ? definition[delayAttrName].AsInt() : null;

            JsonObject[] mainTransitions = definition[mainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                string initial = transition[initialName].AsString();
                string cancel = transition[cancelAttrName].AsString();
                string final = transition[finalAttrName].AsString();
                string intermediate = initial + "_to_" + final + "_op." + name;

                mStatesInitialData.Add(new Tuple<string, string>(initial, intermediate));
                mStatesInitialData.Add(new Tuple<string, string>(intermediate, cancel));
                mStatesInitialData.Add(new Tuple<string, string>(intermediate, final));

                mTransitionsInitialData.Add(new Tuple<string, string>(initial, inputInitial), new Tuple<string, Dictionary<string, JsonObject>>(intermediate,  systemsInitial));
                mTransitionsInitialData.Add(new Tuple<string, string>(intermediate, inputCancel), new Tuple<string, Dictionary<string, JsonObject>>(cancel, systemsCancel));
                mTransitionsInitialData.Add(new Tuple<string, string>(intermediate, inputInitial), new Tuple<string, Dictionary<string, JsonObject>>(final, systemsFinal));

                mTimersInitialData.Add(new Tuple<string, string>(initial, inputInitial), timerDelay);

                mTriggerConditions.Add(new(inputInitial, initial, intermediate));
                mTriggerConditions.Add(new(inputCancel, intermediate, cancel));
                mTriggerConditions.Add(new("", intermediate, final));
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

                Tuple <IState, IInput> transitionFrom = new(states[entry.Key.Item1], inputs[entry.Key.Item2]);
                Tuple<IState, Dictionary<ISystem, JsonObject>> transitionTo = new(states[entry.Value.Item1], transitionSystems);

                mTransitions.Add(transitionFrom, transitionTo);
            }

            foreach (var entry in mTimersInitialData)
            {
                Tuple<IState, IInput> transition = new(states[entry.Key.Item1], inputs[entry.Key.Item2]);
                mTimers.Add(transition, entry.Value);
            }

            mStatesInitialData.Clear();
            mTransitionsInitialData.Clear();
            mTimersInitialData.Clear();
            mInputsInitialData.Clear();
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
