// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitCmd.cs
// Interface for easy access to MonoBehaviours that are AUnitCmdItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to MonoBehaviours that are AUnitCmdItems.
    /// </summary>
    public interface IUnitCmd : IMortalItem {

        event EventHandler isAvailableChanged;

        bool IsAvailable { get; }

        string UnitName { get; }

        float UnitMaxFormationRadius { get; }

        float ClearanceRadius { get; }

        UnifiedSRSensorMonitor UnifiedSRSensorMonitor { get; }

        ICmdSensorRangeMonitor MRSensorMonitor { get; }


        IList<ICmdSensorRangeMonitor> SensorMonitors { get; }

        void HandleRelationsChangedWith(Player chgdRelationsPlayer);

        void HandleColdWarEnemyEngagementPolicyChanged();

    }
}

