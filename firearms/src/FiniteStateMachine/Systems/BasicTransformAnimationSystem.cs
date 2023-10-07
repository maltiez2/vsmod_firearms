using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using System;
using MaltiezFirearms.FiniteStateMachine.Framework;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public class BasicTransformAnimation : UniqueIdFactoryObject, ISystem
    {
        public const string animationsAttrName = "animations";
        public const string durationAttrName = "duration";
        public const string codeAttrName = "code";
        public const string modeAttrName = "mode";
        public const string fpAnimationAttrName = "fpTransform";
        public const string tpAnimationAttrName = "tpTransform";

        private ICoreAPI mApi;
        private readonly Dictionary<string, JsonObject> mFpAnimations = new();
        private readonly Dictionary<string, JsonObject> mTpAnimations = new();
        private readonly Dictionary<string, int> mDurations = new();
        private TickBasedPlayerAnimation mTimer;
        private CollectibleObject mCollectible;

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mApi = api;
            mCollectible = collectible;

            JsonObject[] animations = definition[animationsAttrName].AsArray();
            foreach (JsonObject animation in animations)
            {
                string animationCode = animation[codeAttrName].AsString();
                mDurations.Add(animationCode, animation[durationAttrName].AsInt());
                mFpAnimations.Add(animationCode, animation[fpAnimationAttrName]);
                mTpAnimations.Add(animationCode, animation[tpAnimationAttrName]);
            }

            mTimer = new TickBasedPlayerAnimation();
        }
        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
        }
        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters[codeAttrName].AsString();

            return mFpAnimations.ContainsKey(code);
        }
        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters[codeAttrName].AsString();
            string mode = parameters[modeAttrName].AsString();
            int duration = mDurations[code];
            if (parameters.KeyExists(durationAttrName)) duration = parameters[durationAttrName].AsInt();

            if (!mFpAnimations.ContainsKey(code)) return false;

            mTimer?.Stop();
            mTimer?.Init(mApi, duration, (float progress) => PlayAnimation(progress, code, player));
            
            switch (mode)
            {
                case "forward":
                    mTimer.Play();
                    break;
                case "backward":
                    mTimer.Revert();
                    break;
                case "cancel":
                    mTimer.Stop();
                    break;
            }
            
            return true;
        }

        private void PlayAnimation(float progress, string code, EntityAgent player)
        {
            mCollectible.GetBehavior<FiniteStateMachineBehaviour>().tpTransform = GetTransform(progress, mTpAnimations[code]);
            mCollectible.GetBehavior<FiniteStateMachineBehaviour>().fpTransform = GetTransform(progress, mFpAnimations[code]);
        }

        protected ModelTransform GetTransform(float animationProgress, JsonObject transform)
        {
            JsonObject translation = transform["translation"];
            JsonObject rotation = transform["rotation"];
            JsonObject origin = transform["origin"];

            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Translation.Set(animationProgress * translation["x"].AsFloat(), animationProgress * translation["y"].AsFloat(), animationProgress * translation["z"].AsFloat());
            modelTransform.Rotation.Set(animationProgress * rotation["x"].AsFloat(), animationProgress * rotation["y"].AsFloat(), animationProgress * rotation["z"].AsFloat());
            modelTransform.Origin.Set(animationProgress * origin["x"].AsFloat(), animationProgress * origin["y"].AsFloat(), animationProgress * origin["z"].AsFloat());
            modelTransform.Scale = transform["scale"].AsFloat(1);
            return modelTransform;
        }
    }

    public class TickBasedPlayerAnimation
    {
        public const int listenerDelay = 0;

        private ICoreAPI mApi;
        private long? mCallbackId;
        private bool mForward = true;
        private Action<float> mCallback;
        private float mDuration_ms;
        private float mCurrentDuration;
        private float mCurrentProgress;

        public void Init(ICoreAPI api, int duration_ms, Action<float> callback)
        {
            mDuration_ms = (float)duration_ms / 1000;
            mApi = api;
            mCallback = callback;
        }
        public void Play()
        {
            mCurrentDuration = 0;
            mCurrentProgress = 0;
            mForward = true;
            SetAnimation(0);
            SetListener();
        }
        public void Handler(float time)
        {
            mCurrentDuration += time;
            SetAnimation(CalculateProgress(mCurrentDuration));
            if (mCurrentDuration >= mDuration_ms) StopListener();
        }
        public void Stop()
        {
            StopListener();
        }
        public void Revert()
        {
            mCurrentDuration = mDuration_ms * (1 - mCurrentProgress);
            mForward = false;
            SetAnimation(CalculateProgress(mCurrentDuration));
            SetListener();
        }

        private float CalculateProgress(float time)
        {
            float progress = time / mDuration_ms;
            progress = progress > 1 ? 1 : progress;
            mCurrentProgress = mForward ? progress : 1 - progress;
            return mCurrentProgress;
        }
        private void SetAnimation(float progress)
        {
            mCallback(progress);
        }
        private void SetListener()
        {
            StopListener();
            
            mCallbackId = mApi.World.RegisterGameTickListener(Handler, listenerDelay);
        }
        private void StopListener()
        {
            if (mCallbackId != null) mApi.World.UnregisterGameTickListener((long)mCallbackId);
            mCallbackId = null;
        }
    }
}
