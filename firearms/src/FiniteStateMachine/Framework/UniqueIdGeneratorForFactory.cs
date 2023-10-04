using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    internal class UniqueIdGeneratorForFactory : IUniqueIdGeneratorForFactory
    {
        public const int factoryIdFactor = 10000;

        private static int sFactoryNextId = 0;
        private int mNextProductId = 0;
        private readonly int mFactoryId;

        public UniqueIdGeneratorForFactory() => mFactoryId = sFactoryNextId++;
        public int GenerateInstanceId() => mFactoryId * factoryIdFactor + mNextProductId++;
        public int GetFactoryId() => mFactoryId;
    }
}
