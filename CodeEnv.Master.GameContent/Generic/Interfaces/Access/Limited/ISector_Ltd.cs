// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ISector_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are SectorItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are SectorItems.
    /// </summary>
    public interface ISector_Ltd : IIntelItem_Ltd {

        IntVector3 SectorID { get; }

        ISystem_Ltd System { get; }

        /// <summary>
        /// A collection of assembly stations that are local to the item.
        /// </summary>
        IList<StationaryLocation> LocalAssemblyStations { get; }

        /// <summary>
        /// Returns a random position inside the sector that is clear of any interference.
        /// The point returned is guaranteed to be inside the radius of the universe.
        /// </summary>
        /// <returns></returns>
        Vector3 GetClearRandomPointInsideSector();


    }
}

