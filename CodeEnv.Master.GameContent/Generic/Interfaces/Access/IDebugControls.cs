// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IDebugControls.cs
// Interface for access to the DebugControls MonoBehaviour.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;

    /// <summary>
    /// Interface for access to the DebugControls MonoBehaviour.
    /// </summary>
    public interface IDebugControls {

        event EventHandler validatePlayerKnowledgeNow;

        /// <summary>
        /// Indicates whether fleets should automatically explore without countervailing orders.
        /// <remarks>10.17.16 The only current source of countervailing orders are from editor fields
        /// on DebugFleetCreators.</remarks>
        /// </summary>
        bool FleetsAutoExploreAsDefault { get; }

        /// <summary>
        /// If <c>true</c> every player knows everything about every item they detect. 
        /// It DOES NOT MEAN that they have detected everything or that players have met yet.
        /// Players meet when they first detect a HQ Element owned by another player.
        /// </summary>
        bool IsAllIntelCoverageComprehensive { get; }

    }
}

