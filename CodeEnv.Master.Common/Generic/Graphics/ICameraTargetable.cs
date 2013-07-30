// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraTargetable.cs
//  Interface containing values needed by a gameobject that is a target for camera movement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    /// <summary>
    /// Interface containing values needed by a gameobject that is a target for camera movement.
    /// </summary>
    public interface ICameraTargetable {

        /// <summary>
        /// Gets a value indicating whether this instance is targetable. e.g. - an ICameraTargetable
        /// instance should not be targetable if it is not supposed to be visible to the player.
        /// </summary>
        bool IsTargetable { get; }

        float MinimumCameraViewingDistance { get; }

    }
}

