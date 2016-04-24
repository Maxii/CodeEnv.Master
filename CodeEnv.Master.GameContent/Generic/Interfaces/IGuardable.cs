// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IGuardable.cs
// Interface for Items that can be guarded at GuardStations.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that can be guarded at GuardStations.
    /// </summary>
    public interface IGuardable : INavigable {

        IList<StationaryLocation> GuardStations { get; }

        bool IsGuardingAllowedBy(Player player);

    }
}

