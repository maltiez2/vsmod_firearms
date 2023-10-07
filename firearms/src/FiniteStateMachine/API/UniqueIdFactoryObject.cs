using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace MaltiezFirearms.FiniteStateMachine.API
{
    /// <summary>
    /// Implements id assignment and uses it for checks if two objects are equal (for example for the objects to be used as dictionary keys).<br/>
    /// It is recommended to use this class as a base for classes that implement <see cref="ISystem"/>, <see cref="IOperation"/> or <see cref="IInput"/>.<br/>
    /// It is also recommended to use it with <see cref="Framework.UniqueIdGeneratorForFactory"/>, as it provides unique id for objects produced by <see cref="IFactory{ProductClass}"/>.
    /// </summary>
    public abstract class UniqueIdFactoryObject : IFactoryObject // @TODO May be move to dependency injection instead of inheritance
    {
        private int? mId = null;

        public abstract void Init(string code, JsonObject definition, CollectibleObject collectible, ICoreAPI api);

        /// <summary>
        /// For setting object unique id after <see cref="Init"/> call.<br/>
        /// Id can be assigned <b>only once!</b> Subsequent assignment will not change the id.<br/>
        /// Id is used for <see cref="GetHashCode"/> and <see cref="Equals"/>, so two objects are considered the same if they have the same id.
        /// </summary>
        /// <param name="id">Unique id (uniqueness should be provided by caller of the method)</param>
        public void SetId(int id) => mId = (mId == null ? id : mId);
        /// <summary>
        /// Returns objects id if it was assigned by <see cref="SetId"/>, otherwise throws an exception while casting null to int.
        /// </summary>
        /// <returns>Objects unique id (uniqueness should be provided by caller of <see cref="SetId"/>)</returns>
        /// <exception cref="System.InvalidOperationException">Id was not assigned</exception>
        public int GetId() => (int)mId;
        /// <summary>
        /// Two objects are equal if their ids are equal<br/>
        /// If id was no assigned by <see cref="SetId"/> throws an exception while casting null to int.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Id was not assigned</exception>
        public override bool Equals(object obj) => (obj as UniqueIdFactoryObject)?.GetId() == (int)mId;
        /// <summary>
        /// Hash is equal to object's id.<br/>
        /// If id was no assigned by <see cref="SetId"/> throws an exception while casting null to int.
        /// </summary>
        /// <returns>id provided in <see cref="SetId"/></returns>
        /// <exception cref="System.InvalidOperationException">Id was not assigned</exception>
        public override int GetHashCode() => (int)mId;
    }
}
