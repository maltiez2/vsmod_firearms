using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using MaltiezFirearms.FiniteStateMachine.API;

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

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            mApi = api;
            mFactories = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>();
            mInputIterceptor = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>().GetInputInterceptor();

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
    }
}
