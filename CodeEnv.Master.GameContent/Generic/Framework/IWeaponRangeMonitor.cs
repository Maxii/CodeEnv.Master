// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponRangeMonitor.cs
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
    public interface IWeaponRangeMonitor {

        string FullName { get; }

        DistanceRange Range { get; }

        IElementItem ParentElement { get; set; }

        IList<IElementAttackableTarget> EnemyTargets { get; }
        IList<IElementAttackableTarget> AllTargets { get; }

        void Add(Weapon weapon);

        /// <summary>
        /// Removes the specified weapon. Returns <c>true</c> if this monitor
        /// is still in use (has weapons remaining even if not operational), <c>false</c> otherwise.
        /// </summary>
        /// <param name="weapon">The weapon.</param>
        /// <returns></returns>
        bool Remove(Weapon weapon);

        bool TryGetRandomEnemyTarget(out IElementAttackableTarget enemyTarget);

    }
}

