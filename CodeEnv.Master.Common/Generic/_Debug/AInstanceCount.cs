// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInstanceCount.cs
// Abstract class in support of IInstanceCount counting the unique instances of a class.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Abstract class in support of IInstanceCount counting the unique instances of a class.
    /// Derived client classes wanting to know the instance count in debug logs should implement the IInstanceCount interface and call
    /// IncrementInstanceCounter() in this base class. 
    /// </summary>
    public abstract class AInstanceCount : IInstanceCount {

        private static int _instanceCounter = 0;

        protected void IncrementInstanceCounter() {
            InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        }

        #region IInstanceCount Members

        public int InstanceCount { get; private set; }

        #endregion

    }
}

