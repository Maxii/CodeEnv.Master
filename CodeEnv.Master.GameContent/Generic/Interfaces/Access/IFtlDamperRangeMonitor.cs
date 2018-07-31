// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFtlDamperRangeMonitor.cs
// Interface for easy access to the FtlDamperRangeMonitor MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for easy access to the FtlDamperRangeMonitor MonoBehaviour.
    /// </summary>
    public interface IFtlDamperRangeMonitor : IRangedEquipmentMonitor {

        /// <summary>
        /// Resets this Monitor in preparation for reuse by the same Parent.
        /// <remarks>Deactivates and removes the FtlDamper, preparing the monitor for the addition of a new FtlDamper.</remarks>
        /// </summary>
        void ResetForReuse();

    }
}

