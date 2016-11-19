// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IRangedEquipmentMonitor.cs
// Interface allowing access to RangedEquipmentMonitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using Common;

    /// <summary>
    ///  Interface allowing access to RangedEquipmentMonitors.
    /// </summary>
    public interface IRangedEquipmentMonitor : IDebugable {

        RangeCategory RangeCategory { get; }

        float RangeDistance { get; }

        /// <summary>
        /// The owner of this Ranged Equipment Monitor.
        /// <remarks>No reason to hinder access to Owner info as this interface value
        /// is only used by the equipment associated with the monitor to access range and 
        /// reload period multipliers that need to adjust when the Monitor's Item owner changes.</remarks>
        /// </summary>
        Player Owner { get; }

        bool IsOperational { get; }

        /// <summary>
        /// Initializes the range distance value in this monitor and all its equipment.
        /// </summary>
        void InitializeRangeDistance();

        /// <summary>
        /// Handles a change in relations between players. Called by the monitor's ParentItem when the
        /// DiplomaticRelationship between ParentItem.Owner and <c>otherPlayer</c> changes.
        /// </summary>
        /// <param name="otherPlayer">The other player.</param>
        void HandleRelationsChanged(Player otherPlayer);


    }
}

