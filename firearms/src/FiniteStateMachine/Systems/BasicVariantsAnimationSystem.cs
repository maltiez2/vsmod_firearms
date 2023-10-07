using Vintagestory.API.Datastructures;
using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using System.Collections.Generic;
using System;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public class BasicVariantsAnimation<TAnimationPlayer> : UniqueIdFactoryObject, ISystem
        where TAnimationPlayer : IAnimationPlayer, new()
    {
        public const string animationsAttrName = "animations";
        public const string firstVariantAttrName = "firstVariant";
        public const string lastVariantAttrName = "lastVariant";
        public const string durationAttrName = "duration";
        public const string codeAttrName = "code";
        public const string soundSystemAttrName = "soundSystem";
        public const string soundsAttrName = "sounds";
        public const string variantAttrName = "variant";

        private ICoreAPI mApi;
        private readonly Dictionary<string, IAnimationPlayer.AnimationParameters> mAnimations = new();
        private readonly Dictionary<string, Dictionary<int, string>> mAnimationsSounds = new();
        private TAnimationPlayer mTimer;
        private ISoundSystem mSoundSystem;
        private string mSoundSystemId = "";

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {     
            mApi = api;

            JsonObject[] animations = definition[animationsAttrName].AsArray();
            foreach (JsonObject animation in animations)
            {
                string animationCode = animation[codeAttrName].AsString();

                mAnimations.Add(
                    animationCode,
                    (
                        animation[firstVariantAttrName].AsInt(),
                        animation[lastVariantAttrName].AsInt(),
                        animation[durationAttrName].AsInt()
                    )
                );

                mAnimationsSounds.Add(animationCode, new());

                if (animation.KeyExists(soundsAttrName))
                {
                    foreach (JsonObject soundDefinition in animation[soundsAttrName].AsArray())
                    {
                        mAnimationsSounds[animationCode].Add(soundDefinition[variantAttrName].AsInt(), soundDefinition[codeAttrName].AsString());
                    }
                }
            }

            if (definition.KeyExists(soundSystemAttrName))
            {
                mSoundSystemId = definition[soundSystemAttrName].AsString();
            }
        }
        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
            if (systems.ContainsKey(mSoundSystemId)) mSoundSystem = systems[mSoundSystemId] as ISoundSystem;
        }
        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters[codeAttrName].AsString();

            return mAnimations.ContainsKey(code);
        }
        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            string code = parameters[codeAttrName].AsString();

            if (!mAnimations.ContainsKey(code)) return false;

            mTimer?.Stop();
            mTimer = new TAnimationPlayer();
            mTimer.Init(mApi, mAnimations[code], (int variant) => SetRenderVariant(variant, slot, player, mAnimationsSounds[code]));
            mTimer.Play();

            return true;
        }

        private void SetRenderVariant(int renderVariant, ItemSlot weaponSlot, EntityAgent byEntity, Dictionary<int, string> sounds)
        {
            if (weaponSlot?.Itemstack == null) return;

            if (sounds.ContainsKey(renderVariant)) mSoundSystem?.PlaySound(sounds[renderVariant], weaponSlot, byEntity);

            int prevRenderVariant = weaponSlot.Itemstack.Attributes.GetInt("renderVariant", 0);

            weaponSlot.Itemstack.TempAttributes.SetInt("renderVariant", renderVariant);
            weaponSlot.Itemstack.Attributes.SetInt("renderVariant", renderVariant);

            if (prevRenderVariant == renderVariant) return;

            (byEntity as EntityPlayer)?.Player.InventoryManager.BroadcastHotbarSlot();
        }
    }

    public interface IAnimationPlayer
    {
        public struct AnimationParameters
        {
            public int firstVariant { get; set; }
            public int lastVariant { get; set; }
            public int duration_ms { get; set; }

            public static implicit operator AnimationParameters((int first, int last, int duration) parameters)
            {
                return new AnimationParameters() { firstVariant = parameters.first, lastVariant = parameters.last, duration_ms = parameters.duration };
            }
        }

        void Init(ICoreAPI api, AnimationParameters parameters, Action<int> callback);
        void Play();
        void Stop();
        void Revert();
    }

    public class TimerBasedAnimation : IAnimationPlayer
    {
        private Action<int> mCallback;
        private ICoreAPI mApi;
        private long? mCallbackId;

        private int mFirstVariant;
        private int mNextVariant;
        private int mLastVariant;
        private int mDelay_ms;

        public void Init(ICoreAPI api, IAnimationPlayer.AnimationParameters parameters, Action<int> callback)
        {
            mCallback = callback;
            mApi = api;
            mFirstVariant = parameters.firstVariant;
            mLastVariant = parameters.lastVariant;
            mNextVariant = parameters.firstVariant;
            mDelay_ms = (mLastVariant - mNextVariant + 1 == 0) ? parameters.duration_ms : parameters.duration_ms / (mLastVariant - mNextVariant + 1);
            
        }
        public void Play()
        {
            mCallback(mNextVariant++);
            if (mNextVariant > mLastVariant) return;
            mCallbackId = mApi.World.RegisterCallback(Handler, mDelay_ms);
        }
        public void Handler(float time)
        {
            mCallback(mNextVariant++);

            if (mNextVariant > mLastVariant) return;

            int newDelay = 2 * mDelay_ms - (int)(time * 1000);
            mCallbackId = mApi.World.RegisterCallback(Handler, newDelay);
        }
        public void Stop()
        {
            if (mCallbackId == null) return;

            mApi.World.UnregisterCallback((long)mCallbackId);
            mCallbackId = null;
        }
        public void Revert()
        {
            Stop();
            mCallback(mFirstVariant);
        }
    }

    public sealed class TickBasedAnimation : IAnimationPlayer
    {
        public const int listenerDelayGranularity = 10;
        
        private Action<int> mCallback;
        private ICoreAPI mApi;
        private long? mCallbackId;

        private int mFirstVariant;
        private int mNextVariant;
        private int mLastVariant;
        private int mDelay_ms;
        private int mTimeElapsed_ms;
        private int mListenerDelay_ms;

        public void Init(ICoreAPI api, IAnimationPlayer.AnimationParameters parameters, Action<int> callback)
        {
            mCallback = callback;
            mApi = api;
            mFirstVariant = parameters.firstVariant;
            mLastVariant = parameters.lastVariant;
            mNextVariant = parameters.firstVariant;
            mDelay_ms = (mLastVariant - mNextVariant == 0) ? parameters.duration_ms : parameters.duration_ms / (mLastVariant - mNextVariant);
            mListenerDelay_ms = (listenerDelayGranularity == 0) ? 0 : mDelay_ms / listenerDelayGranularity;
        }
        public void Play()
        {
            if (!SetVariant()) return;
            mTimeElapsed_ms = 0;
            SetListener();
        }
        public void Handler(float time)
        {
            if (!CheckTime(time)) return;
            if (!SetVariant()) return;

            SetListener();
        }
        public void Stop()
        {
            StopListener();
        }
        public void Revert()
        {
            RevertVariant();
        }

        private bool CheckTime(float time)
        {
            mTimeElapsed_ms += (int)(time * 1000);
            if (mTimeElapsed_ms < mDelay_ms) return false;
            mTimeElapsed_ms -= mDelay_ms;
            return true;
        }
        private bool SetVariant()
        {
            if (mNextVariant > mLastVariant)
            {
                StopListener();
                return false;
            }
            mCallback(mNextVariant);
            mNextVariant++;
            return true;
        }
        private void RevertVariant()
        {
            mCallback(mFirstVariant);
        }
        private void SetListener()
        {
            StopListener();
            if (mNextVariant > mLastVariant) return;
            mCallbackId = mApi.World.RegisterGameTickListener(Handler, mListenerDelay_ms);
        }
        private void StopListener()
        {
            if (mCallbackId != null) mApi.World.UnregisterGameTickListener((long)mCallbackId);
            mCallbackId = null;
        }
    }
}
