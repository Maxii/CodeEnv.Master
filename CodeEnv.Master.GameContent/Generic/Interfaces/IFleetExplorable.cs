// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IFleetExplorable.cs
// Interface for Items that FleetCmds can be ordered to explore.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Interface for Items that FleetCmds can be ordered to explore.
    /// <remarks>Includes Systems, Sectors and the UniverseCenter.</remarks>
    /// </summary>
    public interface IFleetExplorable : IExplorable, IFleetNavigable {

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

    }
}

