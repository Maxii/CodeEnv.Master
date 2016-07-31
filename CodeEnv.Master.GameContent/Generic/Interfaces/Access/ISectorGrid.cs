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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface allowing access to the associated Unity-compiled script. 
    /// Typically, a static reference to the script is established by GameManager in References.cs, providing access to the script from classes located in pre-compiled assemblies.
    /// </summary>
    public interface ISectorGrid {

        /// <summary>
        /// Gets the SpaceTopography value associated with this location in worldspace.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns></returns>
        Topography GetSpaceTopography(Vector3 worldLocation);

        /// <summary>
        /// Gets the index of the sector.
        /// </summary>
        /// <param name="worldPoint">The world point.</param>
        /// <returns></returns>
        Index3D GetSectorIndex(Vector3 worldPoint);


    }
}

