using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using System.Collections.Generic;
using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal interface ISound
    {
        void Init(JsonObject definition);
        void Play(EntityAgent byEntity);
    }

    internal class BasicSound : ISound
    {
        public const string locationAttrName = "location";
        public const string rangeAttrName = "range";
        public const string volumeAttrName = "volume";
        public const string randomizePitchAttrName = "randomizePitch";

        private AssetLocation mLocation;
        private float mRange = 32;
        private float mVolume = 1;
        private bool mRandomizePitch = true;
        
        public virtual void Init(JsonObject definition)
        {
            mLocation = new AssetLocation(definition[locationAttrName].AsString());

            InitSoundParams(definition);
        }
        public virtual void Play(EntityAgent byEntity)
        {
            PlaySound(byEntity, mLocation);
        }

        protected void InitSoundParams(JsonObject definition)
        {
            if (definition.KeyExists(rangeAttrName)) mRange = definition[rangeAttrName].AsFloat();
            if (definition.KeyExists(volumeAttrName)) mVolume = definition[volumeAttrName].AsFloat();
            if (definition.KeyExists(randomizePitchAttrName)) mRandomizePitch = definition[randomizePitchAttrName].AsBool();
        }

        protected void PlaySound(EntityAgent byEntity, AssetLocation location)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

            byEntity.World.PlaySoundAt(location: location, atEntity: byEntity, dualCallByPlayer: byPlayer, randomizePitch: mRandomizePitch, range: mRange, volume: mVolume);
        }
    }

    internal class RandomizedSound : BasicSound
    {
        private static Random sRand = new Random();
        private List<AssetLocation> mLocations = new();

        public override void Init(JsonObject definition)
        {
            foreach (string path in definition[locationAttrName].AsArray<string>())
            {
                mLocations.Add(new AssetLocation(path));
            }

            InitSoundParams(definition);
        }

        public override void Play(EntityAgent byEntity)
        {
            int locationIndex = (int)Math.Floor((decimal)(sRand.NextDouble() * (mLocations.Count - 1)));

            PlaySound(byEntity, mLocations[locationIndex]);
        }
    }
    
    public class BasicSoundSystem : UniqueIdFactoryObject, ISystem, ISoundSystem
    {
        private readonly Dictionary<string, ISound> mSounds = new();

        public const string soundsAttrName = "sounds";
        public const string soundCodeAttrName = "code";

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            JsonObject[] sounds = definition[soundsAttrName].AsArray();

            foreach (JsonObject sound in sounds)
            {
                string soundCode = sound["code"].AsString();
                if (sound[BasicSound.locationAttrName].IsArray())
                {
                    mSounds.Add(soundCode, new RandomizedSound());
                }
                else
                {
                    mSounds.Add(soundCode, new BasicSound());
                }

                mSounds[soundCode].Init(sound);
            }
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
            // Does not require access to other systems
        }
        public virtual bool Verify(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            if (parameters.KeyExists(soundCodeAttrName) && mSounds.ContainsKey(parameters[soundCodeAttrName].AsString()))
            {
                return true;
            }

            return false;
        }
        public virtual bool Process(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            string soundCode = parameters[soundCodeAttrName].AsString();
            mSounds[soundCode].Play(player);

            return true;
        }

        void ISoundSystem.PlaySound(string soundCode, ItemSlot slot, EntityAgent player)
        {
            if (mSounds.ContainsKey(soundCode)) mSounds[soundCode].Play(player);
        }
    }
}
