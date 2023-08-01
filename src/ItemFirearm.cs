using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace MaltiezFirearms
{
    public class ItemFirearm : ItemMultistateWeapon
    {
        protected struct OperationRequirement
        {
            public string code;
            public int amount;
            public int durability;
            public int offHand;

            public OperationRequirement(string code, int amount, int durability, int offHand)
            {
                this.code = code;
                this.amount = amount;
                this.durability = durability;
                this.offHand = offHand;
            }
        }
        
        private static Random Rand = new Random();

        private const string projectileStackKey = "projectileStack";

        private static Dictionary<string, SimpleParticleProperties> ParticleEffects = new Dictionary<string, SimpleParticleProperties>();

        private ModelTransform TpOperationTransform = null;

        static ItemFirearm()
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

            ParticleEffects.Add("fireSmoke", fireSmoke);

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

            ParticleEffects.Add("fireBlast", fireBlast);
        }

        // Interaction
        public override void InitInteraction(int currentState, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return;
            }
            
            byEntity.Attributes.SetBool("stopAction", false);
            byEntity.Attributes.SetBool("weaponEmpty", false);
            byEntity.Attributes.SetBool("weaponFiring", false);
            byEntity.Attributes.SetFloat("previousSecondsUsed", 0);
            byEntity.Attributes.SetFloat("attackStartTime", 0);
            ResetAnimationStartTimes(slot, byEntity);
        }
        public override int StepInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return 0;
            }

            if (byEntity.Attributes.GetBool("stopAction")) return 0;

            float previousSecondsUsed = byEntity.Attributes.GetFloat("previousSecondsUsed");

            if (currentState < slot.Itemstack.Collectible.Attributes["loadedStage"].AsInt())
            {
                return Reload(secondsUsed - previousSecondsUsed, slot, byEntity, currentState);
            }

            Aim(secondsUsed - previousSecondsUsed, slot, byEntity, currentState);

            if (Aimed(secondsUsed, slot, byEntity) && !byEntity.Attributes.GetBool("weaponEmpty") && (byEntity.Controls.LeftMouseDown || byEntity.Attributes.GetBool("weaponFiring")))
            {
                if (!byEntity.Attributes.GetBool("weaponFiring"))
                {
                    byEntity.Attributes.SetFloat("attackStartTime", secondsUsed);
                }

                return Attack(secondsUsed - byEntity.Attributes.GetFloat("attackStartTime"), slot, byEntity, currentState);
            }

            return 0;
        }
        public override bool CancelInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return true;
            }

            return !byEntity.Attributes.GetBool("weaponFiring");
        }
        public override void ResetInteraction(int currentState, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return;
            }

            (byEntity as EntityPlayer).Stats.Set("walkspeed", "maltiezfirearms", 0f, true);
            byEntity.AnimManager.StopAnimation("bowaim");
            SetReadyVariant(slot, byEntity, currentState);
            PlayFpTransformation(byEntity, new Vec3f(0, 0, 0), new Vec3f(0, 0, 0));
            TpOperationTransform = null;
        }

        // Conditions
        protected override bool DoAdvancePermanentState(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, int currentState)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return true;
            }

            int loadedStage = slot.Itemstack.Collectible.Attributes["loadedStage"].AsInt();
            JsonObject[] loadingStages = slot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            if (loadingStages.Length <= currentState || currentState >= loadedStage) return true;

            return loadingStages[currentState]["advanceReloadState"].AsBool(true);
        }
        protected override bool DoResetState(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, int currentState)
        {
            if (slot == null || slot.Itemstack == null || byEntity == null)
            {
                api.Logger.Error("[Firearms] [InitInteraction] 'slot' or 'byEntity' is null");
                return true;
            }

            return byEntity.Attributes.GetBool("weaponEmpty", false);
        }
        
        // Sync
        protected bool CheckForDesyncProblem(ItemSlot weaponSlot, int currentState, string method)
        {
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            if (currentState >= loadingStages.Length)
            {
                string weaponName = "null";
                if (weaponSlot != null) weaponName = weaponSlot.GetStackName();
                api.Logger.Warning("[Firearms] [" + method + "] Weapon current state is higher then amount of operation stages it has. Weapon: " + weaponName + ", state: " + currentState.ToString());
                UnstuckState(weaponSlot);
                return true;
            }

            return false;
        }

        // Actions
        public int Attack(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            if (CheckForDesyncProblem(weaponSlot, currentState, "Attack")) return 0;

            byEntity.Attributes.SetBool("weaponFiring", true);

            Tuple<int, int, float> operationParameters = GetOperationParameters(weaponSlot, currentState);

            if (operationParameters == null) return 0;

            int firingStage = weaponSlot.Itemstack.Collectible.Attributes["firingStage"].AsInt();
            int renderVariant = SetOperationVariant(secondsUsed, weaponSlot, byEntity, firingStage - 1);
            PlayOperationSound(renderVariant, weaponSlot, byEntity);

            if (renderVariant == operationParameters.Item2)
            {
                Fire(secondsUsed, weaponSlot, byEntity);
            }

            if (secondsUsed >= operationParameters.Item3) byEntity.Attributes.SetBool("weaponFiring", false);

            if (byEntity.Attributes.GetBool("weaponEmpty")) return 1;

            return 0;
        }
        public void Aim(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            if (CheckForDesyncProblem(weaponSlot, currentState, "Aim")) return;

            (byEntity as EntityPlayer).Stats.Set("walkspeed", "maltiezfirearms", weaponSlot.Itemstack.Collectible.Attributes["aimingWalkspeed"].AsFloat(1), true);
            PlayAimAnimation(secondsUsed, byEntity, weaponSlot);
        }
        public int Reload(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            if (CheckForDesyncProblem(weaponSlot, currentState, "Reload")) return 0;

            OperationRequirement unfulfilledRequirement = new OperationRequirement();

            if (!CanReload(weaponSlot, byEntity, currentState, ref unfulfilledRequirement))
            {
                IServerPlayer serverPlayer = (byEntity as EntityPlayer).Player as IServerPlayer;
                if (serverPlayer != null)
                {
                    ((byEntity as EntityPlayer).Player as IServerPlayer).SendMessage(GlobalConstants.InfoLogChatGroup, "Cant reload, unfulfilled requirements: " + unfulfilledRequirement.code, EnumChatType.Notification); // TODO
                }

                //(byEntity as EntityPlayer).talkUtil.Talk(EnumTalkType.IdleShort); // could be annoying
                byEntity.Attributes.SetBool("stopAction", true);
                return 0;
            }

            SetOperationWalkSpeed(weaponSlot, byEntity, currentState);
            int renderVariant = SetOperationVariant(secondsUsed, weaponSlot, byEntity, currentState);
            PlayOperationSound(renderVariant, weaponSlot, byEntity);

            if (GetReloadTimeSeconds(weaponSlot, currentState) >= (secondsUsed) || !UseRequiredAmmo(weaponSlot, byEntity, currentState)) return 0;

            if (GetStageInterruptionFlag(weaponSlot, currentState)) byEntity.Attributes.SetBool("stopAction", true);

            byEntity.Attributes.SetFloat("previousSecondsUsed", secondsUsed + byEntity.Attributes.GetFloat("previousSecondsUsed"));
            byEntity.Attributes.SetBool("stopReload", true);
            
            return 1;
        }

        // Firearm specific
        protected bool CanReload(ItemSlot weaponSlot, EntityAgent byEntity, int currentState, ref OperationRequirement unfulfilledRequirement)
        {
            if (CheckForDesyncProblem(weaponSlot, currentState, "CanReload")) return false;

            List<OperationRequirement> requirements = GetRequiredAmmo(weaponSlot, currentState);

            foreach (OperationRequirement requirement in requirements)
            {
                if ((GetNextRequirement(byEntity, requirement) == null) != (requirement.offHand == 0))
                {
                    unfulfilledRequirement = requirement; // TODO
                    return false;
                }
            }

            return true;
        }
        protected ItemSlot GetNextRequirement(EntityAgent byEntity, OperationRequirement requirement)
        {
            ItemSlot slot = null;
            
            if (requirement.offHand >= 0)
            {
                if (byEntity.LeftHandItemSlot.Itemstack == null) return null;
                if (!byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path.StartsWith(requirement.code)) return null;
                if (byEntity.RightHandItemSlot.StackSize < requirement.amount) return null;
                if (requirement.durability > 0 && byEntity.LeftHandItemSlot.Itemstack.Item.GetRemainingDurability(byEntity.LeftHandItemSlot.Itemstack) < requirement.durability) return null;

                return byEntity.LeftHandItemSlot;
            }

            byEntity.WalkInventory((invslot) =>
            {
                if (invslot is ItemSlotCreative) return true;

                if (invslot.Itemstack != null && invslot.Itemstack.Collectible.Code.Path.StartsWith(requirement.code) && invslot.Itemstack.StackSize >= requirement.amount)
                {
                    if (requirement.durability > 0 && invslot.Itemstack.Item.GetRemainingDurability(invslot.Itemstack) < requirement.durability)
                    {
                        return true;
                    }
                    
                    slot = invslot;
                    return false;
                }

                return true;
            });

            return slot;
        }
        protected bool Fire(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            ItemStack projectileStack = GetProjectileFromWeapon(weaponSlot);

            weaponSlot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, weaponSlot);

            Vec3d projectilePosition = ProjectilePosition(weaponSlot, byEntity);
            Vec3d projectileVelocity = ProjectileVelocity(weaponSlot, byEntity);
            float projectileDamage = ProjectileDamage(weaponSlot);

            SpawnProjectile(byEntity, projectilePosition, projectileVelocity, projectileDamage, projectileStack);
            SpawnParticleEffects(byEntity, weaponSlot);

            byEntity.Attributes.SetBool("weaponEmpty", true);

            return true;
        }
        protected bool Aimed(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            return secondsUsed >= weaponSlot.Itemstack.Collectible.Attributes["aimingDuration"].AsFloat(0);
        }
        protected void SpawnProjectile(EntityAgent byEntity, Vec3d position, Vec3d velocity, float damage, ItemStack projectileStack)
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
        protected Vec3d ProjectilePosition(ItemSlot weaponSlot, EntityAgent byEntity)
        {
            JsonObject positionData = weaponSlot.Itemstack.Collectible.Attributes["muzzlePosition"];
            Vec3f position = new Vec3f(positionData["x"].AsFloat(), positionData["y"].AsFloat(), positionData["z"].AsFloat());
            Vec3f worldPosition = FromCameraReferenceFrame(byEntity, position);
            return byEntity.SidedPos.AheadCopy(0).XYZ.Add(worldPosition.X, byEntity.LocalEyePos.Y + worldPosition.Y, worldPosition.Z);
        }
        protected Vec3d ProjectileVelocity(ItemSlot weaponSlot, EntityAgent byEntity)
        {
            float muzzleVelocity = 2;

            if (weaponSlot.Itemstack.Collectible.Attributes != null)
            {
                muzzleVelocity *= weaponSlot.Itemstack.Collectible.Attributes["velocityMultiplier"].AsFloat(1);
            }

            if (weaponSlot.Itemstack.Collectible.Attributes != null)
            {
                muzzleVelocity *= GetProjectileFromWeapon(weaponSlot).Collectible.Attributes["velocityMultiplier"].AsFloat(1);
            }

            float dispersion = weaponSlot.Itemstack.Collectible.Attributes["dispersionMOA"].AsFloat(0.001f);

            double rndpitch = 2 * (Rand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion;
            double rndyaw = 2 * (Rand.NextDouble() - 0.5) * (Math.PI / 180 / 60) * dispersion;

            Vec3d pos = byEntity.ServerPos.XYZ.Add(0, byEntity.LocalEyePos.Y, 0);
            Vec3d aheadPos = pos.AheadCopy(1, byEntity.SidedPos.Pitch + rndpitch, byEntity.SidedPos.Yaw + rndyaw);
            return (aheadPos - pos) * muzzleVelocity;
        }
        protected float ProjectileDamage(ItemSlot weaponSlot)
        {
            float damage = 0;

            if (weaponSlot.Itemstack.Collectible.Attributes != null)
            {
                damage += weaponSlot.Itemstack.Collectible.Attributes["damage"].AsFloat(0);
            }

            ItemStack ammunition = GetProjectileFromWeapon(weaponSlot);
            if (ammunition != null)
            {
                damage += ammunition.Collectible.Attributes["damage"].AsFloat(0);
            }

            return damage;
        }
        protected bool IsLoaded(ItemSlot weaponSlot, int currentState)
        {
            int loadedStage = weaponSlot.Itemstack.Collectible.Attributes["loadedStage"].AsInt();

            return currentState >= loadedStage;
        }
        protected List<OperationRequirement> GetRequiredAmmo(ItemSlot weaponSlot, int currentState)
        {
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            if (loadingStages.Length <= currentState) return new List<OperationRequirement>();

            JsonObject[] requirements = loadingStages[currentState]["requirements"].AsArray();

            List<OperationRequirement> output = new List<OperationRequirement>(requirements.Length);

            for (int index = 0; index < requirements.Length; index++)
            {
                string code = requirements[index]["code"].AsString();
                int amount = requirements[index]["amount"].AsInt();
                int durability = -1;
                if (requirements[index].KeyExists("durability"))
                {
                    durability = requirements[index]["durability"].AsInt(-1);
                }
                int offHand = -1;
                if (requirements[index].KeyExists("offhand"))
                {
                    offHand = requirements[index]["offhand"].AsInt(-1);
                }

                output.Add(new OperationRequirement(code, amount, durability, offHand));
            }

            return output;
        }
        protected bool UseRequiredAmmo(ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            string projectileType = weaponSlot.Itemstack.Collectible.Attributes["projectile"].AsString();

            List<OperationRequirement> requirements = GetRequiredAmmo(weaponSlot, currentState);

            foreach (OperationRequirement requirement in requirements)
            {
                if ((GetNextRequirement(byEntity, requirement) == null) != (requirement.offHand == 0)) return false;
            }

            foreach (OperationRequirement requirement in requirements)
            {
                ItemSlot ammoSlot = GetNextRequirement(byEntity, requirement);

                if (ammoSlot == null) continue;

                if (projectileType == requirement.code)
                {
                    SetProjectileForWeapon(ammoSlot.TakeOut(requirement.amount), weaponSlot); // TODO Support multiple ammunition
                    ammoSlot.MarkDirty();
                }
                else if (requirement.durability > 0)
                {
                    ammoSlot.Itemstack.Item.DamageItem(byEntity.World, byEntity, ammoSlot, requirement.durability);
                    ammoSlot.MarkDirty();
                }
                else if (requirement.durability < 0 && requirement.amount > 0)
                {
                    ammoSlot.TakeOut(requirement.amount);
                    ammoSlot.MarkDirty();
                }
            }

            return true;
        }
        protected float GetReloadTimeSeconds(ItemSlot weaponSlot, int currentState)
        {
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            if (loadingStages.Length <= currentState) return 0;

            return loadingStages[currentState]["lengthInSeconds"].AsFloat();
        }
        protected void SetProjectileForWeapon(ItemStack amunition, ItemSlot weaponSlot) // TODO Support multiple projectiles and add checks for nulls
        {
            weaponSlot.Itemstack.Attributes.SetItemstack(projectileStackKey, amunition);
        }
        protected ItemStack GetProjectileFromWeapon(ItemSlot weaponSlot) // TODO Support multiple projectiles and add checks for nulls
        {
            ItemStack itemStack = weaponSlot.Itemstack.Attributes.GetItemstack(projectileStackKey);

            if (api.World != null && itemStack != null)
            {
                itemStack.ResolveBlockOrItem(api.World);
            }

            return itemStack;
        }
        protected bool GetStageInterruptionFlag(ItemSlot weaponSlot, int stage)
        {
            if (stage < 0) return false;
            
            int loadedStage = weaponSlot.Itemstack.Collectible.Attributes["loadedStage"].AsInt();
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            if (loadingStages.Length <= stage || stage >= loadedStage) return false;

            return loadingStages[stage]["interruptInteraction"].AsBool(true);
        }
        protected Tuple<int, int, float> GetOperationParameters(ItemSlot weaponSlot, int currentState)
        {
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            int lastVariant = loadingStages[currentState]["lastVariant"].AsInt();
            int firstVariant = loadingStages[currentState]["firstVariant"].AsInt();
            float reloadTime = loadingStages[currentState]["lengthInSeconds"].AsFloat();

            return new Tuple<int, int, float>(firstVariant, lastVariant, reloadTime);
        }
        protected void SetOperationWalkSpeed(ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();

            float walkSpeed = loadingStages[currentState]["walkspeed"].AsFloat();
            (byEntity as EntityPlayer).Stats.Set("walkspeed", "maltiezfirearms", walkSpeed, true);
        }

        // Animations
        protected Vec3f FromCameraReferenceFrame(EntityAgent byEntity, Vec3f position)
        {
            Vec3f viewVector = byEntity.SidedPos.GetViewVector();
            Vec3f vertical = new Vec3f(0, 1, 0);
            Vec3f localZ = viewVector.Normalize();
            Vec3f localX = viewVector.Cross(vertical).Normalize();
            Vec3f localY = localX.Cross(localZ);
            return localX * position.X + localY * position.Y + localZ * position.Z;
        }
        
        // Animations - Variants
        protected int SetOperationVariant(float secondsUsed, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            Tuple<int, int, float> operationParameters = GetOperationParameters(weaponSlot, currentState);

            if (operationParameters == null) return 0;

            int firstVariant = operationParameters.Item1;
            int lastVariant = operationParameters.Item2;
            float operationTime = operationParameters.Item3;
            float progress = GameMath.Clamp(secondsUsed / operationTime, 0, 1);

            int renderVariant = firstVariant + (int)Math.Floor(progress * (lastVariant - firstVariant));
            
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                PlayOperationAnimation(secondsUsed, renderVariant, weaponSlot, byEntity, currentState);
            }

            if (SetRenderVariant(renderVariant, weaponSlot, byEntity))
            {
                return renderVariant;
            }

            return 0;
        }
        protected void SetFiredVariant(ItemSlot weaponSlot, EntityAgent byEntity)
        {
            int renderVariant = weaponSlot.Itemstack.Collectible.Attributes["firedVariant"].AsInt();

            SetRenderVariant(renderVariant, weaponSlot, byEntity);
        }
        protected void SetReadyVariant(ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            if (CheckForDesyncProblem(weaponSlot, currentState, "SetReadyVariant")) return;

            if (IsLoaded(weaponSlot, currentState))
            {
                int renderVariant = weaponSlot.Itemstack.Collectible.Attributes["readyVariant"].AsInt();
                SetRenderVariant(renderVariant, weaponSlot, byEntity);
                return;
            }

            JsonObject[] loadingStages = weaponSlot.Itemstack.Collectible.Attributes["operationStages"].AsArray();
            
            if (currentState == 0)
            {
                SetRenderVariant(0, weaponSlot, byEntity);
                return;
            }

            int lastVariant = loadingStages[currentState - 1]["lastVariant"].AsInt();

            SetRenderVariant(lastVariant, weaponSlot, byEntity);
        }
        protected bool SetRenderVariant(int renderVariant, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            int prevRenderVariant = weaponSlot.Itemstack.Attributes.GetInt("renderVariant", 0);

            weaponSlot.Itemstack.TempAttributes.SetInt("renderVariant", renderVariant);
            weaponSlot.Itemstack.Attributes.SetInt("renderVariant", renderVariant);

            if (prevRenderVariant == renderVariant) return false;

            if (byEntity is EntityPlayer && (byEntity as EntityPlayer).Player != null) (byEntity as EntityPlayer).Player.InventoryManager.BroadcastHotbarSlot();

            return true;
        }
        
        // Animations - Particles
        protected List<Tuple<string, float, Vec3f, Vec3f>> GetParticleEffects(ItemSlot weaponSlot)
        {
            List<Tuple<string, float, Vec3f, Vec3f>> particleEffects = new List<Tuple<string, float, Vec3f, Vec3f>>();

            JsonObject[] particleEffectsData = weaponSlot.Itemstack.Collectible.Attributes["particleEffects"].AsArray();

            foreach (JsonObject particleEffect in particleEffectsData)
            {
                JsonObject position = particleEffect["position"];
                JsonObject velocity = particleEffect["velocity"];
                particleEffects.Add(new Tuple<string, float, Vec3f, Vec3f>(
                    particleEffect["type"].AsString(),
                    particleEffect["intensity"].AsFloat(1),
                    new Vec3f(position["x"].AsFloat(), position["y"].AsFloat(), position["z"].AsFloat()),
                    new Vec3f(velocity["x"].AsFloat(), velocity["y"].AsFloat(), velocity["z"].AsFloat())
                    ));
            }

            return particleEffects;
        }
        protected void SpawnParticleEffects(EntityAgent byEntity, ItemSlot weaponSlot)
        {
            List<Tuple<string, float, Vec3f, Vec3f>> particleEffects = GetParticleEffects(weaponSlot);

            foreach (Tuple<string, float, Vec3f, Vec3f> effect in particleEffects)
            {
                if (ParticleEffects.ContainsKey(effect.Item1))
                {
                    SpawnParticleEffect(ParticleEffects.Get(effect.Item1), byEntity, effect.Item3 , effect.Item4, effect.Item2);
                }
            }
        }
        protected void SpawnParticleEffect(SimpleParticleProperties effect, EntityAgent byEntity, Vec3f position, Vec3f velocity, float intensity = 1.0f)
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
        
        // Animations - Transformations
        protected Vec3f WeaponAimPositionDelta(ItemSlot weaponSlot)
        {
            JsonObject positionData = weaponSlot.Itemstack.Collectible.Attributes["aimingTransform"]["fpTransform"][""];
            return new Vec3f(positionData["x"].AsFloat(), positionData["y"].AsFloat(), positionData["z"].AsFloat());
        }
        protected void PlayFpTransformation(EntityAgent byEntity, Vec3f position, Vec3f rotation)
        {
            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Translation.Set(position.X, position.Y, position.Z);
            modelTransform.Rotation.Set(rotation.X, rotation.Y, rotation.Z);
            byEntity.Controls.UsingHeldItemTransformAfter = modelTransform;
        }
        protected void SetTpTransformation(float animationProgress, JsonObject operationModelTransform)
        {
            JsonObject translation = operationModelTransform["tpTransform"]["translation"];
            JsonObject rotation = operationModelTransform["tpTransform"]["rotation"];
            JsonObject origin = operationModelTransform["tpTransform"]["origin"];

            ModelTransform modelTransform = new ModelTransform();
            modelTransform.EnsureDefaultValues();
            modelTransform.Translation.Set(animationProgress * translation["x"].AsFloat(), animationProgress * translation["y"].AsFloat(), animationProgress * translation["z"].AsFloat());
            modelTransform.Rotation.Set(animationProgress * rotation["x"].AsFloat(), animationProgress * rotation["y"].AsFloat(), animationProgress * rotation["z"].AsFloat());
            modelTransform.Origin.Set(animationProgress * origin["x"].AsFloat(), animationProgress * origin["y"].AsFloat(), animationProgress * origin["z"].AsFloat());
            modelTransform.Scale = operationModelTransform["scale"].AsFloat(1);
            TpOperationTransform = modelTransform;
        }
        protected void SetAimTpTransformation(float animationProgress, ItemSlot weaponSlot)
        {
            JsonObject aimTransform = weaponSlot.Itemstack.Collectible.Attributes["aimingTransform"]["tpTransform"];

            TpOperationTransform = GetTransform(animationProgress, aimTransform);
        }
        protected void SetAimFpTransformation(float animationProgress, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            JsonObject aimTransform = weaponSlot.Itemstack.Collectible.Attributes["aimingTransform"]["fpTransform"];

            byEntity.Controls.UsingHeldItemTransformAfter = GetTransform(animationProgress, aimTransform);
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
        protected void PlayAimAnimation(float secondsUsed, EntityAgent byEntity, ItemSlot weaponSlot)
        {
            float aimProgress = GameMath.Clamp(secondsUsed / weaponSlot.Itemstack.Collectible.Attributes["aimingDuration"].AsFloat(0), 0, 1);

            byEntity.AnimManager.StartAnimation("bowaim");
            SetAimTpTransformation(aimProgress, weaponSlot);

            if (byEntity.World.Side == EnumAppSide.Client)
            {
                SetAimFpTransformation(aimProgress, weaponSlot, byEntity);
            }
        }
        protected void PlayOperationAnimation(float secondsUsed, int renderVariant, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            JsonObject[] operationModelTransformations = weaponSlot.Itemstack.Collectible.Attributes["operationModelTransformations"].AsArray();

            foreach (JsonObject operationModelTransform in operationModelTransformations)
            {
                if (renderVariant < operationModelTransform["start-animation"].AsInt() || renderVariant >= operationModelTransform["stop-animation"].AsInt()) continue;

                float animationProgress = GetReloadAnimationProgress(secondsUsed, renderVariant, operationModelTransform, weaponSlot, byEntity, currentState);

                Vec3f position = new Vec3f(operationModelTransform["fpTransform"]["translation"]["x"].AsFloat(), operationModelTransform["fpTransform"]["translation"]["y"].AsFloat(), operationModelTransform["fpTransform"]["translation"]["z"].AsFloat());
                Vec3f rotation = new Vec3f(operationModelTransform["fpTransform"]["rotation"]["x"].AsFloat(), operationModelTransform["fpTransform"]["rotation"]["y"].AsFloat(), operationModelTransform["fpTransform"]["rotation"]["z"].AsFloat());

                PlayFpTransformation(byEntity, position * animationProgress, rotation * animationProgress);
                SetTpTransformation(animationProgress, operationModelTransform);
            }
        }
        protected float GetReloadAnimationProgress(float secondsUsed, int renderVariant, JsonObject loadingModelPosition, ItemSlot weaponSlot, EntityAgent byEntity, int currentState)
        {
            int startAnimation = loadingModelPosition["start-animation"].AsInt();
            int startHold = loadingModelPosition["start-hold"].AsInt();
            int stopHold = loadingModelPosition["stop-hold"].AsInt();
            int stopAnimation = loadingModelPosition["stop-animation"].AsInt();
            float currentDuration = GetAnimationTimeInSeconds(secondsUsed, renderVariant, startAnimation, stopAnimation, byEntity);
            float animationSpeed = GetAnimationSpeedInSecondsPerVariant(weaponSlot, currentState);

            if (animationSpeed <= 0) return 1;
            
            float totalDuration = (stopAnimation - startAnimation) * animationSpeed;

            if (startHold <= renderVariant && renderVariant < stopHold)
            {
                return 1;
            }
            if (renderVariant < startAnimation || stopAnimation <= renderVariant)
            {
                return 0;
            }
            if (renderVariant < startHold)
            {
                float startDuration = totalDuration * (startHold - startAnimation) / (stopAnimation - startAnimation);
                return currentDuration / startDuration;
            }
            if (renderVariant >= stopHold)
            {
                float stopDuration = totalDuration * (stopAnimation - stopHold) / (stopAnimation - startAnimation);
                return (totalDuration - currentDuration) / stopDuration;
            }

            return 0;
        }
        protected float GetAnimationTimeInSeconds(float secondsUsed, int renderVariant, int startAnimation, int stopAnimation, EntityAgent byEntity)
        {
            if (renderVariant < startAnimation)
            {
                byEntity.Attributes.SetFloat("operationAnimationStartTime-" + startAnimation.ToString(), secondsUsed);
                return 0;
            }

            float startTime = byEntity.Attributes.GetFloat("operationAnimationStartTime-" + startAnimation.ToString(), secondsUsed);
            if (startTime <= 0)
            {
                byEntity.Attributes.SetFloat("operationAnimationStartTime-" + startAnimation.ToString(), secondsUsed);
                startTime = secondsUsed;
            }

            if (renderVariant == stopAnimation)
            {
                byEntity.Attributes.SetFloat("operationAnimationStartTime-" + startAnimation.ToString(), -1);
            }

            return secondsUsed - startTime;
        }
        protected void ResetAnimationStartTimes(ItemSlot weaponSlot, EntityAgent byEntity)
        {
            JsonObject[] loadingModelPositions = weaponSlot.Itemstack.Collectible.Attributes["operationModelTransformations"].AsArray();

            foreach (JsonObject loadingModelPosition in loadingModelPositions)
            {
                int startVariant = loadingModelPosition["start-animation"].AsInt();
                byEntity.Attributes.SetFloat("operationAnimationStartTime-" + startVariant.ToString(), -1);
            }
        }
        protected float GetAnimationSpeedInSecondsPerVariant(ItemSlot weaponSlot, int currentState)
        {
            Tuple<int, int, float> reloadParameters = GetOperationParameters(weaponSlot, currentState);

            if (reloadParameters == null) return 0;

            int firstVariant = reloadParameters.Item1;
            int lastVariant = reloadParameters.Item2;
            float reloadTime = reloadParameters.Item3;

            return reloadTime / (lastVariant - firstVariant);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            if (target == EnumItemRenderTarget.HandTp && TpOperationTransform != null)
            {
                renderinfo.Transform = renderinfo.Transform.Clone();
                renderinfo.Transform.Translation.X += TpOperationTransform.Translation.X;
                renderinfo.Transform.Translation.Y += TpOperationTransform.Translation.Y;
                renderinfo.Transform.Translation.Z += TpOperationTransform.Translation.Z;
                renderinfo.Transform.Rotation.X += TpOperationTransform.Rotation.X;
                renderinfo.Transform.Rotation.Y += TpOperationTransform.Rotation.Y;
                renderinfo.Transform.Rotation.Z += TpOperationTransform.Rotation.Z;
            }

            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }

        // Sound
        protected void PlaySound(string type, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            JsonObject sounds = weaponSlot.Itemstack.Collectible.Attributes["sounds"];
            if (!sounds.KeyExists(type)) return;
            JsonObject sound = sounds[type];
            JsonObject[] locations = sound["location"].AsArray();
            int locationIndex = (int)Math.Floor((decimal)(Rand.NextDouble() * (locations.Length - 1)));

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID);
            byEntity.World.PlaySoundAt(new AssetLocation(locations[locationIndex].AsString()), byEntity, byPlayer, false, sound["range"].AsFloat(32), sound["volume"].AsFloat(1));
        }
        protected void PlayOperationSound(int renderVariant, ItemSlot weaponSlot, EntityAgent byEntity)
        {
            JsonObject sounds = weaponSlot.Itemstack.Collectible.Attributes["operationSounds"];

            if (!sounds.KeyExists(renderVariant.ToString())) return;

            PlaySound(sounds[renderVariant.ToString()].AsString(), weaponSlot, byEntity);
        }
    }

    public class ItemMusket : ItemFirearm // TODO Temporary throwaway class, will be replaced with behaviors and attachment system
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!byEntity.Controls.CtrlKey || !byEntity.Controls.ShiftKey || blockSel != null || entitySel != null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            handling = EnumHandHandling.PreventDefault;

            if (!slot.Itemstack.Collectible.Code.Path.Contains("bayonet"))
            {
                if (byEntity.LeftHandItemSlot == null || byEntity.LeftHandItemSlot.Itemstack == null || byEntity.LeftHandItemSlot.Itemstack.StackSize == 0) return;
                if (!byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path.Contains("bayonet")) return;
                
                ItemStack bayonet = byEntity.LeftHandItemSlot.TakeOut(1);
                TryChangeVariant(slot.Itemstack, api, "attachment", "bayonet");
                slot.Itemstack.Attributes.SetItemstack("attachmentStack", bayonet);

                slot.MarkDirty();
                byEntity.LeftHandItemSlot.MarkDirty();
            }
            else
            {
                if (byEntity.LeftHandItemSlot != null && byEntity.LeftHandItemSlot.Itemstack != null && byEntity.LeftHandItemSlot.Itemstack.StackSize != 0 && byEntity.LeftHandItemSlot.Itemstack.Collectible.Code.Path.Contains("bayonet")) return;

                ItemStack bayonet = slot.Itemstack.Attributes.GetItemstack("attachmentStack");
                if (bayonet == null || bayonet.StackSize <= 0 || bayonet.Item == null) return;

                if (byEntity != null && byEntity.TryGiveItemStack(bayonet))
                {
                    TryChangeVariant(slot.Itemstack, api, "attachment", "none");
                    slot.MarkDirty();
                }
            }
        }
        public void TryChangeVariant(ItemStack stack, ICoreAPI api, string variantName, string variantValue, bool saveAttributes = true) // Author: Dana (VS discord server)
        {
            if (!stack.Collectible.Variant.ContainsKey(variantName)) return;

            var clonedAttributes = stack.Attributes.Clone();

            var newStack = new ItemStack();

            switch (stack.Collectible.ItemClass)
            {
                case EnumItemClass.Block:
                    newStack = new ItemStack(api.World.GetBlock(stack.Collectible.CodeWithVariant(variantName, variantValue)));
                    break;

                case EnumItemClass.Item:
                    newStack = new ItemStack(api.World.GetItem(stack.Collectible.CodeWithVariant(variantName, variantValue)));
                    break;
            }

            if (saveAttributes) newStack.Attributes = clonedAttributes;

            stack.SetFrom(newStack);
        }
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            byEntity.Attributes.SetInt("didattack", 0);
            byEntity.World.RegisterCallback(delegate
            {
                IPlayer player = (byEntity as EntityPlayer).Player;
                if (player != null && byEntity.Controls.HandUse == EnumHandInteract.HeldItemAttack)
                {
                    float pitchModifier = (byEntity as EntityPlayer).talkUtil.pitchModifier;
                    player.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/strike"), player.Entity, player, pitchModifier * 0.9f + (float)api.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);
                }
            }, 464);
            handling = EnumHandHandling.PreventDefault;
        }
        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }
        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            float num = 0f - Math.Min(0.8f, 3f * secondsPassed);
            float num2 = Math.Min(1.2f, 20f * Math.Max(0f, secondsPassed - 0.25f));
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                IClientWorldAccessor clientWorldAccessor = byEntity.World as IClientWorldAccessor;
                ModelTransform modelTransform = new ModelTransform();
                modelTransform.EnsureDefaultValues();
                float num3 = num2 + num;
                float num4 = Math.Min(0.2f, 1.5f * secondsPassed);
                float num5 = Math.Max(0f, 2f * (secondsPassed - 1f));
                if (secondsPassed > 0.4f)
                {
                    num3 = Math.Max(0f, num3 - num5);
                }

                num4 = Math.Max(0f, num4 - num5);

                if (slot.Itemstack.Collectible.Code.Path.Contains("bayonet"))
                {
                    modelTransform.Translation.Set(-1f * num3 + 0.2f, num4 * 0.4f, (0f - num3) * 0.8f * 2.6f);
                    modelTransform.Rotation.Set((0.3f - num3) * 9f, num3 * 5f, (0f - num3) * 30f);
                }
                else
                {
                    modelTransform.Translation.Set(0f * (-0.5f + num3), num4 * -3.0f - 0.5f, (-1.0f - num3) * 0.8f * 2.6f);
                    modelTransform.Rotation.Set((-20.0f - num3) * 9f, num3 * 5f - 10f, (0f - num3) * 30f);
                }
                
                byEntity.Controls.UsingHeldItemTransformAfter = modelTransform;
                if (num2 > 1.15f && byEntity.Attributes.GetInt("didattack") == 0)
                {
                    clientWorldAccessor.TryAttackEntity(entitySel);
                    byEntity.Attributes.SetInt("didattack", 1);
                    clientWorldAccessor.AddCameraShake(0.25f);
                }
            }

            return secondsPassed < 1.2f;
        }
        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
        }
    }
}