// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Facing.cs
// Direction a GameObject is facing when mounted on another GameObject.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Direction a GameObject is facing when mounted on another GameObject.
    /// </summary>
    [System.Obsolete]
    public enum Facing {

        None,
        Forward,
        Aft,
        Up,
        Down,
        Port,
        Starboard

    }
}

