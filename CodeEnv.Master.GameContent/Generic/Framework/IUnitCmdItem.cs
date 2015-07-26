// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitCmdItem.cs
// Interface for easy access to items that are unit commands.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for easy access to items that are unit commands.
    /// </summary>
    public interface IUnitCmdItem : IMortalItem {

        bool IsSelected { get; set; }

        IconInfo IconInfo { get; }

        bool __CheckForDamage(bool isHQElementAlive, CombatStrength elementDamageSustained, float elementDamageSeverity);

        /// <summary>
        /// Attaches one or more sensors to this command's SensorRangeMonitors.
        /// Note: Sensors are part of a Unit's elements but the monitors they attach to
        /// are children of the Command. Thus sensor range is always measured from
        /// the Command, not from the element.
        /// </summary>
        /// <param name="sensors">The sensors.</param>
        void AttachSensorsToMonitors(params Sensor[] sensors);

        /// <summary>
        /// Detaches one or more sensors from this command's SensorRangeMonitors.
        /// Note: Sensors are part of a Unit's elements but the monitors they attach to
        /// are children of the Command. Thus sensor range is always measured from
        /// the Command, not from the element.
        /// </summary>
        /// <param name="sensors">The sensors.</param>
        void DetachSensorsFromMonitors(params Sensor[] sensors);

        void OnSubordinateElementDeath(IUnitElementItem deadSubordinateElement);

    }
}

