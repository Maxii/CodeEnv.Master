// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnityMoveTech.cs
// Debug enum indicating what Projectile movement approach is being used - Physics or Kinematic.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Debug enum indicating what Projectile movement approach is being used - Physics or Kinematic.
    /// <remarks>Used by DebugControls when in the Editor. Moved here to allow access from classes in
    /// the GameContent namespace.</remarks>
    /// </summary>
    public enum UnityMoveTech {

        Physics,
        Kinematic

    }
}

