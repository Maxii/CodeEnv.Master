// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISectorGrid.cs
// Interface allowing access to the associated Unity-compiled script. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ISectorGrid {

        IEnumerable<ISector> AllSectors { get; }

        /// <summary>
        /// Returns the SectorID that contains the provided world location.
        /// Throws an error if <c>worldLocation</c> is not within the universe.
        /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe.
        /// If this is not certain, use TryGetSectorIDThatContains(worldLocation) instead.</remarks>
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns></returns>
        IntVector3 GetSectorIDThatContains(Vector3 worldPoint);

        /// <summary>
        /// Returns <c>true</c> if a sectorID has been assigned containing this worldLocation, <c>false</c> otherwise.
        /// <remarks>Locations inside the universe have assigned SectorIDs, while those outside do not.</remarks>
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <param name="sectorID">The resulting sectorID.</param>
        /// <returns></returns>
        bool TryGetSectorIDThatContains(Vector3 worldLocation, out IntVector3 sectorID);

    }
}

