using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Diagnostics.CodeAnalysis;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{    
    public class FiniteStateMachine : IFiniteStateMachine
    {    
        public class State : IState // @OPT Change base type to int
        {
            private readonly string mState;
            private readonly int mHash;

            public State(string inputState)
            { 
                mState = inputState;
                mHash = mState.GetHashCode();
            }
            public override string ToString() { return mState; }
            public override bool Equals(object obj)
            {
                return (obj as State)?.mHash == mHash;
            }
            public override int GetHashCode()
            { 
                return mHash;
            }
        }

        public class DelayedCallback
        {
            private readonly Action mCallback;
            private readonly ICoreAPI mApi;
            private long? mCallbackId;

            public DelayedCallback(ICoreAPI api, int delayMs, Action callback)
            {
                mCallback = callback;
                mApi = api;

                mCallbackId = mApi.World.RegisterCallback(Handler, delayMs);
            }

            ~DelayedCallback()
            {
                Cancel();
            }

            public void Handler(float time)
            {
                mApi.Logger.Error("[Firearms] [DelayedCallback] Handler called with time: " + time.ToString());
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
        
        private const string cStateAtributeName = "firearms.state";
        private const string cInitialStateAtribute = "initialState";

        private string mInitialState;
        private readonly Dictionary<State, Dictionary<IInput, IOperation>> mOperationsByInputAndState = new();
        private readonly Dictionary<IOperation, HashSet<State>> mStatesByOperationForTimer = new();
        private readonly Dictionary<IInput, IInput> mRepeatedInputs = new();
        private DelayedCallback mTimer;
        private DelayedCallback mRepeater;

        private CollectibleObject mCollectible;
        private ICoreAPI mApi;

        public void Init(ICoreAPI api, Dictionary<string, IOperation> operations, Dictionary<string, ISystem> systems, Dictionary<string, IInput> inputs, JsonObject behaviourAttributes, CollectibleObject collectible)
        {
            mCollectible = collectible;
            mInitialState = behaviourAttributes[cInitialStateAtribute].AsString();
            mApi = api;

            foreach (var entry in systems)
            {
                entry.Value.SetSystems(systems);
            }

            foreach (var operationEntry in operations)
            {
                IOperation operation = operationEntry.Value;

                List<Tuple<string, string, string>> transitions = operation.GetTransitions();

                Dictionary<string, IState> operationStateMapping = new Dictionary<string, IState>();
                
                foreach (var transition in transitions)
                {
                    string input = transition.Item1;
                    State initialState = new State(transition.Item2);
                    State finalState = new State(transition.Item3);

                    operationStateMapping.TryAdd(transition.Item2, initialState);
                    operationStateMapping.TryAdd(transition.Item3, finalState);

                    mOperationsByInputAndState.TryAdd(initialState, new());
                    mOperationsByInputAndState.TryAdd(finalState, new());


                    if (input == "")
                    {
                        if (!mStatesByOperationForTimer.ContainsKey(operation)) mStatesByOperationForTimer.Add(operation, new());

                        mStatesByOperationForTimer[operation].Add(initialState);
                        continue;
                    }

                    mOperationsByInputAndState[initialState].TryAdd(inputs[input], operation);
                }

                operation.SetInputsStatesSystems(inputs, operationStateMapping, systems);
            }
        }

        public bool Process(ItemSlot slot, EntityAgent player, IInput input)
        {
            if (slot?.Itemstack?.Collectible != mCollectible || player == null) return false;
            
            State state = ReadStateFrom(slot);
            if (!mOperationsByInputAndState.ContainsKey(state))
            {
                WriteStateTo(slot, new State(mInitialState));
                return false;
            }
            if (!mOperationsByInputAndState[state].ContainsKey(input))
            {
                return false;
            }

            IOperation operation = mOperationsByInputAndState[state][input];

            if (RunOperation(slot, player, operation, input, state))
            {
                TryRepeat(slot, player, input);
                return true;
            }

            return false;
        }
        public bool OnTimer(ItemSlot slot, EntityAgent player, IInput input, IOperation operation)
        {
            if (slot?.Itemstack?.Collectible != mCollectible || player == null) return false;

            State state = ReadStateFrom(slot);
            if (!mStatesByOperationForTimer.ContainsKey(operation) || !mStatesByOperationForTimer[operation].Contains(state))
            {
                return false;
            }

            if (RunOperation(slot, player, operation, input, state))
            {
                TryRepeat(slot, player, input);
                return true;
            }

            return false;
        }

        private void TryRepeat(ItemSlot slot, EntityAgent player, IInput input)
        {
            if ((input as IMouseInput)?.IsRepeatable() == true) mRepeater = new DelayedCallback(mApi, 1, () => Repeat(slot, player, input));
        }

        private void Repeat(ItemSlot slot, EntityAgent player, IInput input)
        {
            if (slot?.Itemstack?.Collectible != mCollectible || player == null) return;

            State state = ReadStateFrom(slot);
            if (!mOperationsByInputAndState.ContainsKey(state))
            {
                WriteStateTo(slot, new State(mInitialState));
                return;
            }
            if (!mOperationsByInputAndState[state].ContainsKey(input))
            {
                return;
            }

            IOperation operation = mOperationsByInputAndState[state][input];

            RunOperation(slot, player, operation, input, state);
        }

        private bool RunOperation(ItemSlot slot, EntityAgent player, IOperation operation, IInput input, State state)
        {
            if (operation.StopTimer(slot, player, state, input)) mTimer?.Cancel();

            IState newState = operation.Perform(slot, player, state, input);
            if (state.ToString() != newState.ToString())
            {
                if (mApi.Side == EnumAppSide.Server) mApi.Logger.Warning("[Firearms] [SERVER] State moved from '" + state.ToString() + "' to '" + newState.ToString() + "'."); // @DEBUG
                if (mApi.Side == EnumAppSide.Client) mApi.Logger.Warning("[Firearms] [CLIENT] State moved from '" + state.ToString() + "' to '" + newState.ToString() + "'."); // @DEBUG
                WriteStateTo(slot, newState as State);
            }

            int? timerDelayMs = operation.Timer(slot, player, state, input);

            if (timerDelayMs != null)
            {
                mTimer = new DelayedCallback(mApi, (int)timerDelayMs, () => OnTimer(slot, player, input, operation));
            }

            return input.Handled();
        }
        private State ReadStateFrom(ItemSlot slot)
        {
            State state = new State(slot.Itemstack.Attributes.GetAsString(cStateAtributeName, mInitialState));
            return state;
        }
        private void WriteStateTo(ItemSlot slot, State state)
        {
            slot.Itemstack.Attributes.SetString(cStateAtributeName, state.ToString());
            slot.MarkDirty();
        }
    }
}
