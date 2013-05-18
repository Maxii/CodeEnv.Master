// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInstanceIdentity.cs
// Interface for identifying the unique instance of a class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface for identifying the unique instance of a class.
    /// Derived client classes wanting GameEventManager to ID the instance should implement the interface and call
    /// IncrementInstanceCounter() from either MonoBehaviourBase or AInstanceIdentity. This method contains  
    /// <c>InstanceID = System.Threading.Interlocked.Increment(ref instanceCounter);</c>
    /// </summary>
    public interface IInstanceIdentity {

        int InstanceID { get; set; }
    }
}

