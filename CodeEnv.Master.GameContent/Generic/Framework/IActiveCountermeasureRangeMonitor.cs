// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IActiveCountermeasureRangeMonitor.cs
// Interface for access to ActiveCountermeasureRangeMonitor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for access to ActiveCountermeasureRangeMonitor.
    /// </summary>
    public interface IActiveCountermeasureRangeMonitor : IRangedEquipmentMonitor {

        /// <summary>
        /// Adds the ordnance launched to the list of detected items. 
        /// Part of a workaround to allow 'detection' of ordnance launched inside the monitor's collider. 
        /// Note: Obsolete as all interceptable ordnance has a rigidbody which is detected by this monitor when the 
        /// ordnance moves, even if it first appears inside the monitor's collider.</summary>
        /// <param name="ordnance">The ordnance.</param>
        [System.Obsolete]
        void AddOrdnanceLaunchedFromInsideMonitor(IInterceptableOrdnance ordnance);

    }
}

