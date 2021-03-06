﻿// --------------------------------------------------------------------------------------------------------------------
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

        IEnumerable<ISector> Sectors { get; }

        IEnumerable<ISector> CoreSectors { get; }

        /// <summary>
        /// Returns the SectorID that contains the provided world location.
        /// Throws an error if <c>worldLocation</c> is not within the universe or there is no Sector at that location.
        /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe and a ASector.
        /// If this is not certain, use TryGetSectorIDContaining(worldLocation) instead.</remarks>
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns></returns>
        IntVector3 GetSectorIDContaining(Vector3 worldLocation);

        /// <summary>
        /// Returns <c>true</c> if a sectorID has been assigned containing this worldLocation, <c>false</c> otherwise.
        /// <remarks>Locations outside the universe and a very small percentage inside the universe (locations in FailedRimCells) 
        /// do not have assigned SectorIDs.</remarks>
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <param name="sectorID">The resulting sectorID.</param>
        /// <returns></returns>
        bool TryGetSectorIDContaining(Vector3 worldLocation, out IntVector3 sectorID);

        /// <summary>
        /// Gets the sector containing the provided worldLocation.
        /// Throws an error if <c>worldLocation</c> is not within the universe or within a Sector.
        /// <remarks>Provided as a convenience when the client knows the location provided is inside the universe and a Sector.
        /// If this is not certain, use TryGetSectorContaining(worldLocation) instead.</remarks>
        /// </summary>
        /// <param name="worldLocation">The world point.</param>
        /// <returns></returns>
        [Obsolete]
        ISector GetSectorContaining(Vector3 worldLocation);

        ISector GetSector(IntVector3 sectorID);


    }
}

