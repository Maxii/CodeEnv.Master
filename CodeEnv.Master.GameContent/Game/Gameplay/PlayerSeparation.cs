// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerSeparation.cs
// Indicates how far away each AIPlayer's home system starts from the User's home system.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Indicates how far away each AIPlayer's home system starts from the User's home system.
    /// </summary>
    public enum PlayerSeparation {

        None,
        Close,
        Normal,
        Distant

    }
}

