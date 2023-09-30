﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaltiezFirearms.WeaponBehavior
{
    internal class UniqueIdGeneratorForFactory : IUniqueIdGeneratorForFactory
    {
        private const int cFacrotyIdSize = 8;
        private const int cFactoryIdShift = 32 - cFacrotyIdSize;
        public const int MaxFactoriesNumber = 1 << cFacrotyIdSize;
        public const int MaxProductsNumber = 1 << cFactoryIdShift;

        private static int sFactoryNextId = 0;
        private int mNextProductId = 0;
        private readonly int rFactoryId;

        public UniqueIdGeneratorForFactory() => rFactoryId = sFactoryNextId++;
        public int GenerateInstanceId() => rFactoryId << cFactoryIdShift + mNextProductId++;
        public int GetFacrotyid() => rFactoryId;
    }
}
