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
        private IInputInterceptor mInputIterceptor;
        private JsonObject mProperties;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            mApi = api;
            mFactories = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>();
            mInputIterceptor = mApi.ModLoader.GetModSystem<FiniteStateMachineSystem>().GetInputInterceptor();

            IBehaviourAtributesParser mParser = new Framework.BehaviourAtributesParser();
            mParser.ParseDefinition(mFactories.GetOperationFactory(), mFactories.GetSystemFactory(), mFactories.GetInputFactory(), mProperties, collObj);

            mFsm = new Framework.FiniteStateMachine();
            mFsm.Init(mApi, mParser.GetOperations(), mParser.GetSystems(), mParser.GetInputs(), mProperties, collObj);

            foreach (var inputEntry in mParser.GetInputs())
            {
                mInputIterceptor.RegisterInput(inputEntry.Value, mFsm.Process, collObj);
            }
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            mProperties = properties;
        }
    }
}
