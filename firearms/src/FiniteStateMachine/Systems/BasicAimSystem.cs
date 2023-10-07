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
                    mIsAiming = true;
                    break;
                case "stop":
                    mIsAiming = false;
                    break;
            }
            return true;
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
}
