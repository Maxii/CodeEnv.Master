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

    using System.Collections.Generic;

    /// <summary>
    /// Interface for access to RangeTrackers.
    /// </summary>
    public interface IRangeTracker {

        float Range { get; set; }

        IPlayer Owner { get; set; }

        IList<ITarget> EnemyTargets { get; }

        IList<ITarget> AllTargets { get; }

        ITarget __GetRandomEnemyTarget();

        AElementData Data { get; set; } // TODO Temporary Just for debug messages

    }
}

