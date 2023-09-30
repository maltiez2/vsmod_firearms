using Vintagestory.API.Common;

namespace MaltiezFirearms.WeaponBehavior
{
    public class WeaponBehaviorSystem : ModSystem, IFactoryProvider
    {
        private IFactory<IOperation> mOperationFactory;
        private IFactory<IWeaponSystem> mSystemFactory;
        private IFactory<IInput> mInputFactory;
        private IInputInterceptor mInputIterceptor;


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterCollectibleBehaviorClass("maltiezfirearms.weapon", typeof(Prototypes.WeaponBehaviorPrototype));

            mOperationFactory = new Prototypes.FactoryPrototype<IOperation, UniqueIdGeneratorForFactory>();
            mSystemFactory = new Prototypes.FactoryPrototype<IWeaponSystem, UniqueIdGeneratorForFactory>();
            mInputFactory = new Prototypes.FactoryPrototype<IInput, UniqueIdGeneratorForFactory>();

            mOperationFactory.RegisterType<Prototypes.OperationPrototype>("TestOperation");
            mSystemFactory.RegisterType<Prototypes.WeaponSystemPrototype>("TestSystem");
            mInputFactory.RegisterType<Prototypes.InputPrototype>("TestInput");

            RegisterSystems();
            RegisterOperations();
            RegisterInputs();

            mInputIterceptor = new Prototypes.InputIntercepterPrototype(api);
        }

        public void RegisterSystems()
        {  
            mSystemFactory.RegisterType<Systems.BaseSoundSystem>("Sound");
        }
        public void RegisterOperations()
        {

        }

        public void RegisterInputs()
        {

        }

        public IFactory<IOperation> GetOperationFactory()
        {
            return mOperationFactory;
        }
        public IFactory<IWeaponSystem> GetSystemFactory()
        {
            return mSystemFactory;
        }
        public IFactory<IInput> GetInputFactory()
        {
            return mInputFactory;
        }
        public IInputInterceptor GetInputInterceptor()
        {
            return mInputIterceptor;
        }
    }
}
