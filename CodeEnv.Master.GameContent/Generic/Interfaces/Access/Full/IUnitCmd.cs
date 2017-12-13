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

        /// <summary>
        /// Fired when Availability changes to/from Available.
        /// </summary>
        event EventHandler isAvailableChanged;

        /// <summary>
        /// Fired when the receptiveness of this Unit to receiving new orders changes.
        /// </summary>
        [Obsolete]
        event EventHandler availabilityChanged;

        event EventHandler unitOutputsChanged;

        bool IsOwnerChangeUnderway { get; }

        NewOrderAvailability Availability { get; }

        /// <summary>
        /// Returns <c>true</c> if there is currently room in this Cmd for 1 element to join it.
        /// <remarks>For use only during operations (IsOperational == true) as it utilizes the FormationManager
        /// which is not initialized until after all elements have been added during construction.</remarks>
        /// </summary>
        bool IsJoinable { get; }

        /// <summary>
        /// Returns <c>true</c> if there is currently room in this Cmd for <c>elementCount</c> elements to join it.
        /// <remarks>For use only during operations (IsOperational == true) as it utilizes the FormationManager
        /// which is not initialized until after all elements have been added during construction.</remarks>
        /// </summary>
        /// <param name="elementCount">The element count.</param>
        /// <returns></returns>
        bool IsJoinableBy(int elementCount);

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

