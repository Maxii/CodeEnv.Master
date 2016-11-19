// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugDiploUserRelations.cs
// The desired Diplomatic Relationship of the AI Owner with the User. Used for debug settings in the editor. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// The desired Diplomatic Relationship of the AI Owner with the User. Used for debug settings in the editor. 
    /// <remarks>Avoids offering None and Self.</remarks>
    /// </summary>
    public enum DebugDiploUserRelations {

        Alliance,
        Friendly,
        Neutral,
        ColdWar,
        War

    }
}

