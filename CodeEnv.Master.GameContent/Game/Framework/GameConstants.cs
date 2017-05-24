// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameConstants.cs
// Constant values specific to the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Constant values specific to the game.
    /// </summary>
    public static class GameConstants {

        public static readonly Vector3 UniverseOrigin = Vector3.zero;

        #region UILabel Icon Markers

        /*********************************************************************************
                    *   Markers used within UILabel to indicate the sprite to in line within the label's text.
                    *   Note: The UILabel's bbcode checkbox must be checked for the marker to be recognized.
                    *********************************************************************************/

        public const string IconMarker_Currency = "|currency|";

        public const string IconMarker_Beam = "|beam|";

        public const string IconMarker_Missile = "|missile|";

        public const string IconMarker_AssaultVehicle = "|missile|";    // UNDONE

        public const string IconMarker_Projectile = "|projectile|";

        public const string IconMarker_Distance = "|distance|";

        public const string IconMarker_Health = "|health|";

        #endregion

    }
}

