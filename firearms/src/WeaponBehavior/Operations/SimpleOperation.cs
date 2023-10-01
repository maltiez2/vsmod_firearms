using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.WeaponBehavior.Operations
{
    public class SimpleOperation : UniqueIdFactoryObject, IOperation
    {
        public const string MainTransitionsAttrName = "states";
        public const string SystemsAttrName = "systems";
        public const string InitialStateAttrName = "initial";
        public const string FinalStateAttrName = "final";
        public const string InputAttrName = "input";
        public const string AttributesAttrName = "attributes";

        private readonly Dictionary<string, string> mStatesInitialData = new();
        private readonly Dictionary<string, JsonObject> mSystemsInitialData = new();
        private string mInputInitialData;

        private readonly Dictionary<IState, IState> mStates = new();
        private readonly Dictionary<IWeaponSystem, JsonObject> mSystems = new();
        
        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            JsonObject[] mainTransitions = definition[MainTransitionsAttrName].AsArray();
            foreach (JsonObject transition in mainTransitions)
            {
                mStatesInitialData.Add(transition[InitialStateAttrName].AsString(), transition[FinalStateAttrName].AsString());
            }

            JsonObject[] systems = definition[SystemsAttrName].AsArray();
            foreach (JsonObject system in systems)
            {
                mSystemsInitialData.Add(system["code"].AsString(), system);
            }

            mInputInitialData = definition[InputAttrName].AsString();
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

        public void SetInputsStatesSystems(Dictionary<string, IInput> inputs, Dictionary<string, IState> states, Dictionary<string, IWeaponSystem> systems)
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
                if (!entry.Key.Verify(weaponSlot, player, entry.Value[AttributesAttrName]))
                {
                    return state;
                }
            }

            foreach (var entry in mSystems)
            {
                entry.Key.Process(weaponSlot, player, entry.Value[AttributesAttrName]);
            }

            return mStates[state];
        }
        public int? Timer(ItemSlot weaponSlot, EntityAgent player, IState state, IInput input)
        {
            return null;
        }
    }
}
