using MaltiezFirearms.FiniteStateMachine.API;

namespace MaltiezFirearms.FiniteStateMachine.Framework
{
    internal class UniqueIdGeneratorForFactory : IUniqueIdGeneratorForFactory
    {
        private const int cFactoryIdSize = 8;
        private const int cFactoryIdShift = 32 - cFactoryIdSize - 1;
        public const int maxFactoriesNumber = 1 << cFactoryIdSize;
        public const int maxProductsNumber = 1 << cFactoryIdShift;

        private static int sFactoryNextId = 0;
        private int mNextProductId = 0;
        private readonly int mFactoryId;

        public UniqueIdGeneratorForFactory() => mFactoryId = sFactoryNextId++;
        public int GenerateInstanceId() => mFactoryId << cFactoryIdShift + mNextProductId++;
        public int GetFactoryid() => mFactoryId;
    }
}
