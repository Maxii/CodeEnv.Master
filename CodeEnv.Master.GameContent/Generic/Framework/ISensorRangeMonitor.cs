// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorRangeMonitor.cs
//  Interface for access to SensorRangeMonitor.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for access to SensorRangeMonitor.
    /// </summary>
    public interface ISensorRangeMonitor : IRangedEquipmentMonitor {

        IUnitCmdItem ParentItem { get; set; }

        IList<IElementAttackableTarget> AttackableEnemyTargetsDetected { get; }

        void Add(Sensor sensor);

        /// <summary>
        /// Removes the specified sensor. Returns <c>true</c> if this monitor
        /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="sensor">The sensor.</param>
        /// <returns></returns>
        bool Remove(Sensor sensor);

        /// <summary>
        /// Resets this Monitor for reuse by the parent item.
        /// </summary>
        void Reset();

    }
}

