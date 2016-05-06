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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Constant values specific to the game.
    /// </summary>
    public static class GameConstants {

        /// <summary>
        /// The tolerance allowed for one hour to be equal to another.
        /// <remarks>0.1 hour tolerance is about the best I can expect at an FPS as low as 25 
        /// (0.04 secs between updates to GameTime) and GameSettings.HoursPerSecond of 2.0
        /// => 0.04 * 2.0 = 0.08 hours tolerance. Granularity will be better at higher FPS, 
        /// but I can't count on it.</remarks>
        /// </summary>
        public const float HoursPrecision = 0.1F;

        public const float HoursRoundingFactor = 1F / HoursPrecision;

        public static readonly Vector3 UniverseOrigin = Vector3.zero;

        #region UILabel Icon Markers

        /*********************************************************************************
                    *   Markers used within UILabel to indicate the sprite to inline within the label's text.
                    *   Note: The UILabel's bbcode checkbox must be checked for the marker to be recognized.
                    *********************************************************************************/

        public const string IconMarker_Currency = "|currency|";

        public const string IconMarker_Beam = "|beam|";

        public const string IconMarker_Missile = "|missile|";

        public const string IconMarker_Projectile = "|projectile|";

        public const string IconMarker_Distance = "|distance|";

        public const string IconMarker_Health = "|health|";

        #endregion

    }
}

