using MaltiezFirearms.FiniteStateMachine.API;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MaltiezFirearms.FiniteStateMachine.Systems
{
    public interface IProjectileSpawner
    {
        bool SpawnProjectile(ItemStack projectileStack, EntityAgent byEntity, JsonObject parameters);
        bool CanSpawnProjectile(ItemStack projectileStack, EntityAgent byEntity, JsonObject parameters);
    }
    
    public class BasicShooting : UniqueIdFactoryObject, ISystem
    {
        public const string ammoSelectorSystemAttrName = "ammoSource";
        
        private IProjectileSpawner mProjectileSpawner;
        private string mReloadSystemName;
        private IAmmoSelector mReloadSystem;

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mProjectileSpawner = new BasicProjectileSpawner();
            mReloadSystemName = definition[ammoSelectorSystemAttrName].AsString();
        }
        public virtual void SetSystems(Dictionary<string, ISystem> systems)
        {
            mReloadSystem = systems[mReloadSystemName] as IAmmoSelector;
        }

        public virtual bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            ItemStack ammoStack = mReloadSystem.TakeSelectedAmmo(slot);
            return mProjectileSpawner.SpawnProjectile(ammoStack, player, parameters);
        }
        public virtual bool Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return true;
        }
    }

    public class BasicProjectileSpawner : IProjectileSpawner
    {
        private static Random sRand = new Random();

        public virtual bool SpawnProjectile(ItemStack projectileStack, EntityAgent byEntity, JsonObject parameters)
        {
            if (projectileStack == null) return false;
            
            Vec3d projectilePosition = ProjectilePosition(projectileStack, byEntity, new Vec3f(0.0f, 0.0f, 0.0f));
            Vec3d projectileVelocity = ProjectileVelocity(projectileStack, byEntity);
            float projectileDamage = projectileStack.Collectible.Attributes["damage"].AsFloat(0);

            SpawnProjectile(projectileStack, byEntity, projectilePosition, projectileVelocity, projectileDamage);

            return true;
        }
        public virtual bool CanSpawnProjectile(ItemStack projectileStack, EntityAgent byEntity, JsonObject parameters)
        {
            return projectileStack != null;
        }

        protected virtual Vec3d ProjectilePosition(ItemStack projectileStack, EntityAgent byEntity, Vec3f muzzlePosition)
        {
            Vec3f worldPosition = FromCameraReferenceFrame(projectileStack, byEntity, muzzlePosition);
            return byEntity.SidedPos.AheadCopy(0).XYZ.Add(worldPosition.X, byEntity.LocalEyePos.Y + worldPosition.Y, worldPosition.Z);
        }
        protected virtual Vec3d ProjectileVelocity(ItemStack projectileStack, EntityAgent byEntity, float dispersion = 0.001f, float muzzleVelocity = 2)
        {
            double randomPitch = 2 * (sRand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion;
            double randomYaw = 2 * (sRand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion;

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, byEntity.SidedPos.Pitch + randomPitch, byEntity.SidedPos.Yaw + randomYaw);
            return (aheadPos - pos) * muzzleVelocity;
        }
        protected virtual Vec3f FromCameraReferenceFrame(ItemStack projectileStack, EntityAgent byEntity, Vec3f position)
        {
            Vec3f viewVector = byEntity.SidedPos.GetViewVector();
            Vec3f vertical = new Vec3f(0, 1, 0);
            Vec3f localZ = viewVector.Normalize();
            Vec3f localX = viewVector.Cross(vertical).Normalize();
            Vec3f localY = localX.Cross(localZ);
            return localX * position.X + localY * position.Y + localZ * position.Z;
        }
        protected virtual void SpawnProjectile(ItemStack projectileStack, EntityAgent byEntity, Vec3d position, Vec3d velocity, float damage)
        {
            EntityProperties type = byEntity.World.GetEntityType(projectileStack.Item.Code);
            var projectile = byEntity.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
            projectile.FiredBy = byEntity;
            projectile.Damage = damage;
            projectile.ProjectileStack = projectileStack;
            projectile.DropOnImpactChance = projectileStack.Collectible.Attributes["breakChanceOnImpact"].AsFloat(0);
            projectile.ServerPos.SetPos(position);
            projectile.ServerPos.Motion.Set(velocity);
            projectile.Pos.SetFrom(projectile.ServerPos);
            projectile.World = byEntity.World;
            projectile.SetRotation();

            byEntity.World.SpawnEntity(projectile);
        }
    }
}
