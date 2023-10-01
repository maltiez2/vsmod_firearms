using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common.Entities;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace MaltiezFirearms.WeaponBehavior.Systems
{
    internal interface ISound
    {
        void Init(JsonObject definition);
        void Play(EntityAgent byEntity);
    }

    internal class BaseSound : ISound
    {
        public const string LocationAttrName = "location";
        public const string RangeAttrName = "range";
        public const string VolumeAttrName = "volume";
        public const string RandomizePitchAttrName = "randomizePitch";

        private AssetLocation mLocation;
        private float mRange = 32;
        private float mVolume = 1;
        private bool mRandomizePitch = true;
        
        public virtual void Init(JsonObject definition)
        {
            mLocation = new AssetLocation(definition[LocationAttrName].AsString());

            InitSoundParams(definition);
        }
        public virtual void Play(EntityAgent byEntity)
        {
            PlaySound(byEntity, mLocation);
        }

        protected void InitSoundParams(JsonObject definition)
        {
            if (definition.KeyExists(RangeAttrName)) mRange = definition[RangeAttrName].AsFloat();
            if (definition.KeyExists(VolumeAttrName)) mVolume = definition[VolumeAttrName].AsFloat();
            if (definition.KeyExists(RandomizePitchAttrName)) mRandomizePitch = definition[RandomizePitchAttrName].AsBool();
        }

        protected void PlaySound(EntityAgent byEntity, AssetLocation location)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);

            byEntity.World.PlaySoundAt(location: location, atEntity: byEntity, dualCallByPlayer: byPlayer, randomizePitch: mRandomizePitch, range: mRange, volume: mVolume);
        }
    }

    internal class RandomizedSound : BaseSound
    {
        private static Random Rand = new Random();
        private List<AssetLocation> mLocations = new();

        public override void Init(JsonObject definition)
        {
            foreach (string path in definition[LocationAttrName].AsArray<string>())
            {
                mLocations.Add(new AssetLocation(path));
            }

            InitSoundParams(definition);
        }

        public override void Play(EntityAgent byEntity)
        {
            int locationIndex = (int)Math.Floor((decimal)(Rand.NextDouble() * (mLocations.Count - 1)));

            PlaySound(byEntity, mLocations[locationIndex]);
        }
    }
    
    public class BasicSoundSystem : UniqueIdFactoryObject, IWeaponSystem
    {
        private readonly Dictionary<string, ISound> rSounds = new();

        public const string SoundsAttrName = "sounds";
        public const string SoundCodeAttrName = "code";

        public override void Init(JsonObject definition, CollectibleObject collectible)
        {
            JsonObject[] sounds = definition[SoundsAttrName].AsArray();

            foreach (JsonObject sound in sounds)
            {
                string soundCode = sound["code"].AsString();
                if (sound[BaseSound.LocationAttrName].IsArray())
                {
                    rSounds.Add(soundCode, new RandomizedSound());
                }
                else
                {
                    rSounds.Add(soundCode, new BaseSound());
                }

                rSounds[soundCode].Init(sound);
            }
        }
        public virtual bool Verify(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            if (parameters.KeyExists(SoundCodeAttrName) && rSounds.ContainsKey(parameters[SoundCodeAttrName].AsString()))
            {
                return true;
            }

            return false;
        }
        public virtual bool Process(ItemSlot weaponSlot, EntityAgent player, JsonObject parameters)
        {
            string soundCode = parameters[SoundCodeAttrName].AsString();
            rSounds[soundCode].Play(player);

            return true;
        }
    }
}
