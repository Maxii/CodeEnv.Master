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
    /// Derived client classes wanting GameEventManager to ID the instance should implement the IInstanceIdentity interface and call
    /// IncrementInstanceCounter() from either MonoBehaviourBase or this base class. This method contains  
    /// <c>InstanceID = System.Threading.Interlocked.Increment(ref instanceCounter);</c>
    /// </summary>
    public abstract class AInstanceIdentity {

        private static int instanceCounter = 0;
        public int InstanceID { get; set; }

        protected void IncrementInstanceCounter() {
            InstanceID = System.Threading.Interlocked.Increment(ref instanceCounter);
        }

    }
}

