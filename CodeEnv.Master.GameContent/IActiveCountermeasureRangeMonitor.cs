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
        /// Removes the specified countermeasure. Returns <c>true</c> if this monitor
        /// is still in use (has countermeasures remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <returns></returns>
        bool Remove(ActiveCountermeasure countermeasure);


    }
}

