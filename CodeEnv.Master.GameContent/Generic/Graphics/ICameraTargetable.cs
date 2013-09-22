// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICameraTargetable.cs
//  Interface containing values needed by a gameobject that is a target supporting camera movement.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface containing values needed by a gameobject that is a target supporting camera movement.
    /// </summary>
    public interface ICameraTargetable {

        /// <summary>
        /// Gets a value indicating whether this instance is currently targetable. e.g. - an ICameraTargetable
        /// instance should not be targetable when it is not visible to the player.
        /// </summary>
        bool IsTargetable { get; }

        /// <summary>
        /// Gets the minimum camera viewing distance allowed for this ICameraTargetable object.
        /// </summary>
        float MinimumCameraViewingDistance { get; }

    }
}

