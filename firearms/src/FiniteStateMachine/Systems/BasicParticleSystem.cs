using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    internal class BasicParticles : UniqueIdFactoryObject, ISystem
    {
        private readonly Dictionary<string, SimpleParticleProperties> mParticleEffectsTypes = new();
        private readonly Dictionary<string, Tuple<string, float, Vec3f, Vec3f>> mParticleEffects = new();

        public override void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            FillParticleEffectsTypes();
            FillParticleEffects(definition["effects"]);
        }

        void ISystem.SetSystems(Dictionary<string, ISystem> systems)
        {
        }

        bool ISystem.Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            foreach (JsonObject effectCode in parameters["effects"].AsArray())
            {
                Tuple<string, float, Vec3f, Vec3f> effect = mParticleEffects[effectCode.AsString()];
                if (mParticleEffectsTypes.ContainsKey(effect.Item1))
                {
                    SpawnParticleEffect(mParticleEffectsTypes[effect.Item1], player, effect.Item3, effect.Item4, effect.Item2);
                }
            }

            return true;
        }

        bool ISystem.Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }

        private Vec3f FromCameraReferenceFrame(EntityAgent byEntity, Vec3f position)
        {
            Vec3f viewVector = byEntity.SidedPos.GetViewVector();
            Vec3f vertical = new Vec3f(0, 1, 0);
            Vec3f localZ = viewVector.Normalize();
            Vec3f localX = viewVector.Cross(vertical).Normalize();
            Vec3f localY = localX.Cross(localZ);
            return localX * position.X + localY * position.Y + localZ * position.Z;
        }
        private void FillParticleEffectsTypes()
        {
            SimpleParticleProperties fireSmoke = new SimpleParticleProperties(
                   8, 16,
                   ColorUtil.ToRgba(60, 80, 80, 80),
                   new Vec3d(0, 0, 0),
                   new Vec3d(0.4f, 0.4f, 0.4f),
                   new Vec3f(-0.4f, -0.4f, -0.4f),
                   new Vec3f(0.4f, 0.4f, 0.4f),
                   4f,
                   -0.01f,
                   1.5f,
                   2.5f,
                   EnumParticleModel.Quad
               );

            fireSmoke.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
            fireSmoke.SelfPropelled = true;

            mParticleEffectsTypes.Add("fireSmoke", fireSmoke);

            SimpleParticleProperties fireBlast = new SimpleParticleProperties(
                    15,
                    20,
                    ColorUtil.ToRgba(255, 250, 100, 50),
                    new Vec3d(),
                    new Vec3d(),
                    new Vec3f(-2f, -2f, -2f),
                    new Vec3f(2f, 2f, 2f),
                    0.1f,
                    0.3f,
                    0.6f,
                    1.0f,
                    EnumParticleModel.Cube
            );
            fireBlast.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f);
            fireBlast.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -100f);
            fireBlast.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -50f);
            fireBlast.VertexFlags = 128;

            mParticleEffectsTypes.Add("fireBlast", fireBlast);
        }
        private void FillParticleEffects(JsonObject effectsData)
        {
            JsonObject[] particleEffectsData = effectsData.AsArray();

            foreach (JsonObject particleEffect in particleEffectsData)
            {
                JsonObject position = particleEffect["position"];
                JsonObject velocity = particleEffect["velocity"];
                mParticleEffects.Add
                    (
                        particleEffect["code"].AsString(),
                        new Tuple<string, float, Vec3f, Vec3f>
                        (
                            particleEffect["type"].AsString(),
                            particleEffect["intensity"].AsFloat(1),
                            new Vec3f(position["x"].AsFloat(), position["y"].AsFloat(), position["z"].AsFloat()),
                            new Vec3f(velocity["x"].AsFloat(), velocity["y"].AsFloat(), velocity["z"].AsFloat())
                        )
                    );
            }
        }
        private void SpawnParticleEffect(SimpleParticleProperties effect, EntityAgent byEntity, Vec3f position, Vec3f velocity, float intensity = 1.0f)
        {
            Vec3f worldPosition = FromCameraReferenceFrame(byEntity, position);
            Vec3f worldVelocity = FromCameraReferenceFrame(byEntity, velocity);

            effect.MinPos = byEntity.SidedPos.AheadCopy(0).XYZ.Add(worldPosition.X, byEntity.LocalEyePos.Y + worldPosition.Y, worldPosition.Z) - effect.AddPos * 0.5;
            effect.MinVelocity = worldVelocity - effect.AddVelocity * 0.5f;

            float minQuantity = effect.MinQuantity;
            float addQuantity = effect.AddQuantity;

            effect.MinQuantity = minQuantity * intensity;
            effect.AddQuantity = addQuantity * intensity;

            byEntity.World.SpawnParticles(effect);

            effect.MinQuantity = minQuantity;
            effect.AddQuantity = addQuantity;
        }
    }
}
