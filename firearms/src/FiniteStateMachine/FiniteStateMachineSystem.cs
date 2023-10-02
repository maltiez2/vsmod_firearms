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
        private IInputManager mInputIterceptor;

        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterCollectibleBehaviorClass("maltiezfirearms.weapon", typeof(Framework.FiniteStateMachineBehaviour));

            mOperationFactory = new Framework.Factory<IOperation, Framework.UniqueIdGeneratorForFactory>();
            mSystemFactory = new Framework.Factory<ISystem, Framework.UniqueIdGeneratorForFactory>();
            mInputFactory = new Framework.Factory<IInput, Framework.UniqueIdGeneratorForFactory>();

            RegisterSystems();
            RegisterOperations();
            RegisterInputs();

            mInputIterceptor = new Framework.InputManager(api);
        }

        public void RegisterSystems()
        {  
            mSystemFactory.RegisterType<Systems.BasicSoundSystem>("Sound");
            mSystemFactory.RegisterType<Systems.BasicReloadSystem>("Reload");
            mSystemFactory.RegisterType<Systems.BasicShootingSystem>("Shooting");
        }
        public void RegisterOperations()
        {
            mOperationFactory.RegisterType<Operations.SimpleOperation>("SimpleOperation");
        }

        public void RegisterInputs()
        {
            mInputFactory.RegisterType<Inputs.SimpleKeyPress>("SimpleKeyPress");
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
            return mInputIterceptor;
        }
    }
}
