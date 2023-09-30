using Cairo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using static MaltiezFirearms.WeaponBehavior.Prototypes.FsmPrototype;

namespace MaltiezFirearms.WeaponBehavior.Prototypes
{    
    public class FsmPrototype : IFiniteStateMachine
    {    
        public struct State : IState // @TODO @optimisation Change base type to int
        {
            private readonly string state;

            public State(string inputState) { state = inputState; }
            public override string ToString() { return state; }
        }

        public class DelayedCallback
        {
            private readonly Action mCallback;
            private readonly ICoreAPI mApi;
            private long? mCallbackId;

            public DelayedCallback(ICoreAPI api, int delay_ms, Action callback)
            {
                mCallback = callback;
                mApi = api;

                mCallbackId = mApi.World.RegisterCallback(Handler, delay_ms);
            }

            ~DelayedCallback()
            {
                Cancel();
            }

            public void Handler(float time)
            {
                mCallback();
            }

            public void Cancel()
            {
                if (mCallbackId != null)
                {
                    mApi.World.UnregisterCallback((long)mCallbackId);
                    mCallbackId = null;
                }
            }
        }
        
        private const string cStateAtributeName = "maltiezfirearms_state";
        private const string cInitialStateAtribute = "initialState";

        private string mInitialState;
        private readonly Dictionary<State, Dictionary<IInput, IOperation>> mOperationsByInputAndState = new();
        private DelayedCallback mTimer;

        private ICoreAPI mApi;

        public void Init(ICoreAPI api, Dictionary<string, IOperation> operations, Dictionary<string, IWeaponSystem> systems, Dictionary<string, IInput> inputs, JsonObject behaviourAttributes, CollectibleObject colelctible)
        {
            mInitialState = behaviourAttributes[cInitialStateAtribute].AsString();
            mApi = api;

            foreach (var operationEntry in operations)
            {
                IOperation operation = operationEntry.Value;

                List<string> operationStates = operation.GetStates();
                List<string> operationInputs = operation.GetInputs();

                operation.SetSystems(systems);
                operation.SetInputs(inputs);

                Dictionary<string, IState> operationStateMapping = new Dictionary<string, IState>();
                foreach (string state in operationStates)
                {
                    operationStateMapping.Add(state, new State(state));
                }
                operation.SetStates(operationStateMapping);

                foreach (string state in operationStates)
                {
                    State stateObj = new State(state);
                    
                    if (!mOperationsByInputAndState.ContainsKey(stateObj))
                    {
                        mOperationsByInputAndState.Add(stateObj, new());
                    }

                    foreach (string input in operationInputs)
                    {
                        mOperationsByInputAndState[stateObj].Add(inputs[input], operation);
                    }
                }
            }
        }

        public bool Process(ItemSlot weaponSlot, EntityAgent player, IInput input)
        {
            State state = ReadStateFrom(weaponSlot);
            if (!mOperationsByInputAndState.ContainsKey(state) && !mOperationsByInputAndState[state].ContainsKey(input))
            {
                return false;
            }

            mTimer?.Cancel();

            IOperation operation = mOperationsByInputAndState[state][input];
            State newState = (State)operation.Perform(weaponSlot, player, state, input);
            if (state.ToString() != newState.ToString())
            {
                mApi.Logger.Warning("[Firearms] [FsmPrototype] State moved from '" + state.ToString() + "' to '" + newState.ToString() + "'.");
                WriteStateTo(weaponSlot, newState);
            }

            int? timerDelay_ms = operation.Timer(weaponSlot, player, state, input);

            if (timerDelay_ms != null)
            {
                mTimer = new DelayedCallback(mApi, (int)timerDelay_ms, () => Process(weaponSlot, player, input));
            }

            return true;
        }

        private State ReadStateFrom(ItemSlot weaponSlot)
        {
            return new State(weaponSlot.Itemstack.Attributes.GetAsString(cStateAtributeName, mInitialState));
        }
        private void WriteStateTo(ItemSlot weaponSlot, State state)
        {
            weaponSlot.Itemstack.Attributes.SetString(cStateAtributeName, state.ToString());
            weaponSlot.MarkDirty();
        }
    }
}
