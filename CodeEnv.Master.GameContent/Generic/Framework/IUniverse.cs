// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUniverse.cs
// Interface for easy access to the Universe folder.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Interface for easy access to the Universe folder.
    /// </summary>
    public interface IUniverse {

        /// <summary>
        /// Gets the SpaceTopography value associated with this location in worldspace.
        /// </summary>
        /// <param name="worldLocation">The world location.</param>
        /// <returns></returns>
        SpaceTopography GetSpaceTopography(Vector3 worldLocation);

    }
}

