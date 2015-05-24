// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISensorRangeMonitor.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ISensorRangeMonitor {

        string FullName { get; }

        DistanceRange Range { get; }

        IUnitCmdItem ParentCommand { set; }

        Player Owner { get; }

        void Add(Sensor sensor);

        /// <summary>
        /// Removes the specified sensor. Returns <c>true</c> if this monitor
        /// is still in use (has sensors remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="sensor">The sensor.</param>
        /// <returns></returns>
        bool Remove(Sensor sensor);

        IList<IElementAttackableTarget> EnemyTargetsDetected { get; }


    }
}

