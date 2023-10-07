using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;
using static MaltiezFirearms.FiniteStateMachine.Systems.IAimingSystem;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal class NoSway : UniqueIdFactoryObject, ISystem
    {
        private ActionCallbackTimer mTimer;
        private ICoreAPI mApi;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mApi = api;
            mTimer = new();
            mTimer.Init(api, SetAimAnimationCrutch);
        }

        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
        }

        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string action = parameters["action"].AsString();
            switch (action)
            {
                case "start":
                    if (mApi is ICoreClientAPI) mTimer.Start();
                    break;
                case "stop":
                    if (mApi is ICoreClientAPI) mTimer.Stop();
                    break;
            }
            return true;
        }

        void SetAimAnimationCrutch(bool interact)
        {
            EntityPlayer player = ((mApi as ICoreClientAPI)?.World as ClientMain)?.EntityPlayer;

            if (player?.Controls?.HandUse == null) return;

            if (interact)
            {
                player.AnimManager.StopAnimation("placeblock");
                player.Controls.HandUse = EnumHandInteract.HeldItemInteract;
            }
            else
            {
                player.Controls.HandUse = EnumHandInteract.None;
            }
        }

        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
    }

    internal sealed class ActionCallbackTimer
    {
        private Action<bool> mCallback;
        private ICoreAPI mApi;
        private long? mCallbackId;

        public void Init(ICoreAPI api, Action<bool> callback)
        {
            mCallback = callback;
            mApi = api;
        }
        public void Start()
        {
            mCallback(true);
            SetListener();
        }
        public void Handler(float time)
        {
            mCallback(true);
            SetListener();
        }
        public void Stop()
        {
            StopListener();
            mCallback(false);
        }

        private void SetListener()
        {
            StopListener();
            mCallbackId = mApi.World.RegisterGameTickListener(Handler, 0);
        }
        private void StopListener()
        {
            if (mCallbackId != null) mApi.World.UnregisterGameTickListener((long)mCallbackId);
            mCallbackId = null;
        }
    }
}
