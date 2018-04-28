// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFtlDampenerRangeMonitor.cs
// Interface for easy access to the FtlDampenerRangeMonitor MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to the FtlDampenerRangeMonitor MonoBehaviour.
    /// </summary>
    public interface IFtlDampenerRangeMonitor : IRangedEquipmentMonitor {

        /// <summary>
        /// Resets this Monitor in preparation for reuse by the same Parent.
        /// <remarks>Deactivates and removes the FtlDampener, preparing the monitor for the addition of a new FtlDampener.</remarks>
        /// </summary>
        void ResetForReuse();

    }
}

