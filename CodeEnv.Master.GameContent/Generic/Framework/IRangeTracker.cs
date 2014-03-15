// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IRangeTracker.cs
//  Interface for access to RangeTrackers.
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
    /// Interface for access to RangeTrackers.
    /// </summary>
    public interface IRangeTracker {

        /// <summary>
        /// Occurs once with <c>true</c> when the first of one or more enemies come into range and 
        /// once with <c>false</c> when there are no more enemies in range.
        /// </summary>
        event Action<bool, Guid> onEnemyInRange;

        Guid ID { get; }

        float Range { get; set; }

        Range<float> RangeSpan { get; }

        IPlayer Owner { get; set; }

        IList<IMortalItem> EnemyTargets { get; }

        IList<IMortalItem> AllTargets { get; }

        /// <summary>
        /// Attempts to acquire a random enemy target in this weapon's range. Returns
        /// <c>true</c> if successful.
        /// </summary>
        /// <param name="enemyTarget">The enemy target in range. Can be null.</param>
        /// <returns><c>true</c> if there is an enemy target in range.</returns>
        bool __TryGetRandomEnemyTarget(out IMortalItem enemyTarget);

        AElementData Data { get; set; } // TODO Temporary Just for debug messages

    }
}

