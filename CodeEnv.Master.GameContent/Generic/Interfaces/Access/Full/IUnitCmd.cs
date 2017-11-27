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

        event EventHandler unitOutputsChanged;

        bool IsOwnerChangeUnderway { get; }

        /// <summary>
        /// Indicates whether this operational Cmd has commenced operations.
        /// <remarks>5.20.17 Currently used to filter</remarks>
        /// </summary>
        bool __IsActivelyOperating { get; }

        bool IsAvailable { get; }

        /// <summary>
        /// Returns <c>true</c> if there is currently room in this Cmd to join it.
        /// <remarks>For use only during operations (IsOperational == true) as it 
        /// utilizes the FormationManager which is not initialized until after all
        /// elements have been added during construction.</remarks>
        /// </summary>
        bool IsJoinable { get; }

        bool IsJoinableBy(int additionalElementCount);

        int ElementCount { get; }

        string UnitName { get; set; }

        float UnitMaxFormationRadius { get; }

        float ClearanceRadius { get; }

        IUnitElement HQElement { get; }

        UnifiedSRSensorMonitor UnifiedSRSensorMonitor { get; }

        ICmdSensorRangeMonitor MRSensorMonitor { get; }

        IList<ICmdSensorRangeMonitor> SensorMonitors { get; }

        OutputsYield UnitOutputs { get; }

        void HandleRelationsChangedWith(Player chgdRelationsPlayer);

        void HandleColdWarEnemyEngagementPolicyChanged();

    }
}

