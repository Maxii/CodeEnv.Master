// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IAssemblySupported.cs
// Interface for Items that have local assembly stations where fleets can assemble without interference from obstacles.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;

    /// <summary>
    /// Interface for Items that have local assembly stations 
    /// where fleets can assemble without interference from obstacles.
    /// </summary>
    public interface IAssemblySupported {

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

    }
}

