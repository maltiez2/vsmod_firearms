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
    public class BasicShooting : UniqueIdFactoryObject, ISystem
    {
        public const string ammoSelectorSystemAttrName = "reloadSystem";
        public const string aimingSystemAttrName = "aimingSystem";
        public const string velocityAttrName = "projectileVelocity";
        public const string damageAttrName = "projectileDamage";

        private static Random sRand = new Random();

        private string mReloadSystemName;
        private string mAimingSystemName;
        private IAmmoSelector mReloadSystem;
        private IAimingSystem mAimingSystem;
        private float mProjectileVelocity;
        private float mProjectileDamage;

        public override void Init(string name, JsonObject definition, CollectibleObject collectible, ICoreAPI api)
        {
            mReloadSystemName = definition[ammoSelectorSystemAttrName].AsString();
            mAimingSystemName = definition[aimingSystemAttrName].AsString();
            mProjectileVelocity = definition[velocityAttrName].AsFloat();
            mProjectileDamage = definition[damageAttrName].AsFloat();
        }
        public void SetSystems(Dictionary<string, ISystem> systems)
        {
            mReloadSystem = systems[mReloadSystemName] as IAmmoSelector;
            mAimingSystem = systems[mAimingSystemName] as IAimingSystem;
        }

        public bool Process(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            ItemStack ammoStack = mReloadSystem.TakeSelectedAmmo(slot);
            if (ammoStack == null) return false;

            Vec3d projectilePosition = ProjectilePosition(player, new Vec3f(0.0f, 0.0f, 0.0f));
            Vec3d projectileVelocity = ProjectileVelocity(player, mAimingSystem.GetShootingDirectionOffset());

            SpawnProjectile(ammoStack, player, projectilePosition, projectileVelocity, mProjectileDamage);

            return true;
        }
        public bool Verify(ItemSlot slot, EntityAgent player, JsonObject parameters)
        {
            return mReloadSystem.GetSelectedAmmo(slot) != null;
        }

        private Vec3d ProjectilePosition(EntityAgent player, Vec3f muzzlePosition)
        {
            Vec3f worldPosition = FromCameraReferenceFrame(player, muzzlePosition);
            return player.SidedPos.AheadCopy(0).XYZ.Add(worldPosition.X, player.LocalEyePos.Y + worldPosition.Y, worldPosition.Z);
        }
        private Vec3d ProjectileVelocity(EntityAgent player, IAimingSystem.DirectionOffset dispersion)
        {
            Vec3d pos = player.ServerPos.XYZ.Add(0, player.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, player.SidedPos.Pitch + dispersion.pitch, player.SidedPos.Yaw + dispersion.yaw);
            return (aheadPos - pos) * mProjectileVelocity;
        }
        private Vec3f FromCameraReferenceFrame(EntityAgent player, Vec3f position)
        {
            Vec3f viewVector = player.SidedPos.GetViewVector();
            Vec3f vertical = new Vec3f(0, 1, 0);
            Vec3f localZ = viewVector.Normalize();
            Vec3f localX = viewVector.Cross(vertical).Normalize();
            Vec3f localY = localX.Cross(localZ);
            return localX * position.X + localY * position.Y + localZ * position.Z;
        }
        private void SpawnProjectile(ItemStack projectileStack, EntityAgent player, Vec3d position, Vec3d velocity, float damage)
        {
            if (projectileStack?.Item?.Code == null) return;

            EntityProperties type = player.World.GetEntityType(projectileStack.Item.Code);
            var projectile = player.World.ClassRegistry.CreateEntity(type) as EntityProjectile;
            projectile.FiredBy = player;
            projectile.Damage = damage;
            projectile.ProjectileStack = projectileStack;
            projectile.DropOnImpactChance = projectileStack.Collectible.Attributes["breakChanceOnImpact"].AsFloat(0);
            projectile.ServerPos.SetPos(position);
            projectile.ServerPos.Motion.Set(velocity);
            projectile.Pos.SetFrom(projectile.ServerPos);
            projectile.World = player.World;
            projectile.SetRotation();

            player.World.SpawnEntity(projectile);
        }
    }
}
