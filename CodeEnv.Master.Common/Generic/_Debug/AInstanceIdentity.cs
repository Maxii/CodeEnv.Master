// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInstanceIdentity.cs
// Abstract class in support of IInstanceIdentity identifying the unique instance of a class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Abstract class in support of IInstanceIdentity identifying the unique instance of a class.
    /// Derived client classes wanting to ID the instance in debug logs should implement the IInstanceIdentity interface and call
    /// IncrementInstanceCounter() in this base class. 
    /// </summary>
    public abstract class AInstanceIdentity : IInstanceIdentity {

        private static int _instanceCounter = 0;
        public int InstanceID { get; private set; }

        protected void IncrementInstanceCounter() {
            InstanceID = System.Threading.Interlocked.Increment(ref _instanceCounter);
        }

    }
}

