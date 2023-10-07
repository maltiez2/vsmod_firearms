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
    internal class BasicAim : UniqueIdFactoryObject, ISystem, IAimingSystem
    {
        private static Random sRand = new Random();
        
        private TimerForAimimg mTimer;
        private float mDispersionMin;
        private float mDispersionMax;
        private long mAimTime_ms;
        private long mAimStartTime;
        private ICoreAPI mApi;
        private bool mIsAiming = false;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mDispersionMin = definition["dispersionMin_MOA"].AsFloat();
            mDispersionMax = definition["dispersionMax_MOA"].AsFloat();
            mAimTime_ms = definition["duration"].AsInt();
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
                    mAimStartTime = mApi.World.ElapsedMilliseconds;
                    if (mApi is ICoreClientAPI) mTimer.Start();
                    mIsAiming = true;
                    break;
                case "stop":
                    mIsAiming = false;
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

        DirectionOffset IAimingSystem.GetShootingDirectionOffset()
        {
            long currentTime = mApi.World.ElapsedMilliseconds;
            float aimProgress = mIsAiming ? Math.Clamp((float)(currentTime - mAimStartTime) / mAimTime_ms, 0, 1) : 0;
            float dispersion = mDispersionMax - (mDispersionMax - mDispersionMin) * aimProgress;
            float randomPitch = (float)(2 * (sRand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion);
            float randomYaw = (float)(2 * (sRand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion);
            return (randomPitch, randomYaw);
        }
    }

    public sealed class TimerForAimimg
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
