// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IInstanceCount.cs
// Interface for counting the unique instances of a class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface for counting the unique instances of a class.
    /// Derived client classes wanting to get the count of an instance should implement the interface and call
    /// IncrementInstanceCounter() from either AMonoSingleton or AInstanceCount. This method contains  
    /// <c>InstanceCount = System.Threading.Interlocked.Increment(ref instanceCounter);</c>
    /// </summary>
    public interface IInstanceCount {

        int InstanceCount { get; }
    }
}

