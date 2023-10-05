using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;
using Vintagestory.API.Client;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    public class FiniteStateMachineBehaviour : CollectibleBehavior
    {
        public FiniteStateMachineBehaviour(CollectibleObject collObj) : base(collObj)
        {

        }

        private ICoreAPI mApi;
        private IFactoryProvider mFactories;
        private IFiniteStateMachine mFsm;
        private IInputManager mInputIterceptor;
        private JsonObject mProperties;

        public ModelTransform tpTransform { get; set; }
        public ModelTransform fpTransform { get; set; }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            mApi = api;
            mFactories = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>();
            mInputIterceptor = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>().GetInputInterceptor();
            tpTransform = null;

            IBehaviourAttributesParser parser = new BehaviourAttributesParser();
            parser.ParseDefinition(mFactories.GetOperationFactory(), mFactories.GetSystemFactory(), mFactories.GetInputFactory(), mProperties, collObj);

            mFsm = new FiniteStateMachine();
            mFsm.Init(mApi, parser.GetOperations(), parser.GetSystems(), parser.GetInputs(), mProperties, collObj);

            foreach (var inputEntry in parser.GetInputs())
            {
                mInputIterceptor.RegisterInput(inputEntry.Value, mFsm.Process, collObj);
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);

            mFactories = null;
            mFsm = null;
            mInputIterceptor = null;
            mProperties = null;
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            mProperties = properties;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {            
            if (target == EnumItemRenderTarget.HandTp && tpTransform != null)
            {
                renderinfo.Transform = renderinfo.Transform.Clone();
                renderinfo.Transform.Translation.X += tpTransform.Translation.X;
                renderinfo.Transform.Translation.Y += tpTransform.Translation.Y;
                renderinfo.Transform.Translation.Z += tpTransform.Translation.Z;
                renderinfo.Transform.Rotation.X += tpTransform.Rotation.X;
                renderinfo.Transform.Rotation.Y += tpTransform.Rotation.Y;
                renderinfo.Transform.Rotation.Z += tpTransform.Rotation.Z;
            }

            if (target == EnumItemRenderTarget.HandFp && fpTransform != null)
            {
                renderinfo.Transform = renderinfo.Transform.Clone();
                renderinfo.Transform.Translation.X += fpTransform.Translation.X;
                renderinfo.Transform.Translation.Y += fpTransform.Translation.Y;
                renderinfo.Transform.Translation.Z += fpTransform.Translation.Z;
                renderinfo.Transform.Rotation.X += fpTransform.Rotation.X;
                renderinfo.Transform.Rotation.Y += fpTransform.Rotation.Y;
                renderinfo.Transform.Rotation.Z += fpTransform.Rotation.Z;
            }

            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        }
    }
}
