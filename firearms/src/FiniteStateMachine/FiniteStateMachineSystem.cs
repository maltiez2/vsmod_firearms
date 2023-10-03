using Vintagestory.API.Common;
using MaltiezFirearms.FiniteStateMachine.API;
using MaltiezFirearms.FiniteStateMachine.Systems;
using MaltiezFirearms.FiniteStateMachine.Inputs;

namespace MaltiezFirearms.FiniteStateMachine
{
    public class FiniteStateMachineSystem : ModSystem, IFactoryProvider
    {
        private IFactory<IOperation> mOperationFactory;
        private IFactory<ISystem> mSystemFactory;
        private IFactory<IInput> mInputFactory;
        private IInputManager mInputManager;

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

            mInputManager = new Framework.InputManager(api);
        }

        public void RegisterSystems()
        {  
            mSystemFactory.RegisterType<Systems.BasicSoundSystem>("Sound");
            mSystemFactory.RegisterType<Systems.BasicReload>("Reload");
            mSystemFactory.RegisterType<Systems.BasicShooting>("Shooting");
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
