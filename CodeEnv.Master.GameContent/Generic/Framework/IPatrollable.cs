// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPatrollable.cs
// Interface for Items that are patrollable by Fleets.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that are patrollable by Fleets.
    /// </summary>
    public interface IPatrollable : INavigableTarget {

        /// <summary>
        /// Returns a copy of the list of Patrol Points around this IPatrollable Item.
        /// <remarks>A copy allows the list to be modified without affecting the original list.</remarks>
        /// </summary>
        IList<StationaryLocation> PatrolPoints { get; }

    }
}

