using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using Vintagestory.API.Client;

namespace MaltiezFirearms.FiniteStateMachine
{
    public class FiniteStateMachineSystem : ModSystem, IFactoryProvider
    {
        private IFactory<IOperation> mOperationFactory;
        private IFactory<ISystem> mSystemFactory;
        private IFactory<IInput> mInputFactory;
        private IInputManager mInputManager;
        private IActiveSlotListener mActiveSlotListener;

        public override void Start(ICoreAPI api)
        {  
            base.Start(api);

            api.RegisterCollectibleBehaviorClass("firearms.finitestatemachine", typeof(Framework.FiniteStateMachineBehaviour));

            mOperationFactory = new Framework.Factory<IOperation, Framework.UniqueIdGeneratorForFactory>(api);
            mSystemFactory = new Framework.Factory<ISystem, Framework.UniqueIdGeneratorForFactory>(api);
            mInputFactory = new Framework.Factory<IInput, Framework.UniqueIdGeneratorForFactory>(api);

            RegisterSystems();
            RegisterOperations();
            RegisterInputs();

            mActiveSlotListener = (api.Side == EnumAppSide.Client) ? new Framework.ActiveSlotActiveListener((ICoreClientAPI)api) : null;
            mInputManager = new Framework.InputManager(api, mActiveSlotListener);
        }

        public void RegisterSystems()
        {  
            mSystemFactory.RegisterType<Systems.BasicSoundSystem>("Sound");
            mSystemFactory.RegisterType<Systems.BasicReload>("Reload");
            mSystemFactory.RegisterType<Systems.BasicShooting>("Shooting");
            mSystemFactory.RegisterType<Systems.BasicVariantsAnimation<Systems.TickBasedAnimation>>("VariantsAnimation");
            mSystemFactory.RegisterType<Systems.BasicRequirements>("Requirements");
            mSystemFactory.RegisterType<Systems.BasicTransformAnimation>("TransformAnimation");
        }
        public void RegisterOperations()
        {
            mOperationFactory.RegisterType<Operations.BasicInstant>("Instant");
            mOperationFactory.RegisterType<Operations.BasicDelayed>("Delayed");
        }

        public void RegisterInputs()
        {
            mInputFactory.RegisterType<Inputs.BasicKey>("Key");
            mInputFactory.RegisterType<Inputs.BasicMouse>("MouseKey");
            mInputFactory.RegisterType<Inputs.BasicHotkey>("Hotkey");
            mInputFactory.RegisterType<Inputs.BasicSlotBefore>("SlotChange");
            mInputFactory.RegisterType<Inputs.ItemDropped>("ItemDropped");
        }

        public IFactory<IOperation> GetOperationFactory()
        {
            return mOperationFactory;
        }
        public IFactory<ISystem> GetSystemFactory()
        {
            return mSystemFactory;
        }
        public IFactory<IInput> GetInputFactory()
        {
            return mInputFactory;
        }
        public IInputManager GetInputInterceptor()
        {
            return mInputManager;
        }
    }
}
