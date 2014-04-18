// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IWeaponRangeTracker.cs
//  Interface for access to WeaponRangeTrackers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for access to WeaponRangeTrackers.
    /// </summary>
    public interface IWeaponRangeTracker {

        /// <summary>
        /// Occurs once with <c>true</c> when the first of one or more enemies come into range and 
        /// once with <c>false</c> when there are no more enemies in range.
        /// </summary>
        event Action<bool, Guid> onEnemyInRange;

        string ParentFullName { get; set; }

        Guid ID { get; }

        float Range { get; set; }

        Range<float> RangeSpan { get; }

        IPlayer Owner { get; set; }

        IList<IMortalTarget> EnemyTargets { get; }

        IList<IMortalTarget> AllTargets { get; }

        /// <summary>
        /// Attempts to acquire a random enemy target in this weapon's range. Returns
        /// <c>true</c> if successful.
        /// </summary>
        /// <param name="enemyTarget">The enemy target in range. Can be null.</param>
        /// <returns><c>true</c> if there is an enemy target in range.</returns>
        bool __TryGetRandomEnemyTarget(out IMortalTarget enemyTarget);

    }
}

