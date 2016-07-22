﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorDetectable.cs
// Interface for Items that are detectable by SensorRangeMonitors.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using UnityEngine;

    /// <summary>
    /// Interface for Items that are detectable by SensorRangeMonitors.
    /// </summary>
    public interface ISensorDetectable : IDetectable {

        /// <summary>
        /// Occurs when [owner changed].
        /// <remarks>OK for client to have access to this, even if they don't have access
        /// to Owner info as long as they use the event to properly check for Owner access.</remarks>
        /// </summary>
        event EventHandler ownerChanged;

        void HandleDetectionBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat);

        void HandleDetectionLostBy(Player detectingPlayer, IUnitCmd_Ltd cmdItem, RangeCategory sensorRangeCat);
    }
}

