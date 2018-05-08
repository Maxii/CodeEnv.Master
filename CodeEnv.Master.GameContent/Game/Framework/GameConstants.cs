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

    using Common;
    using UnityEngine;

    /// <summary>
    /// Constant values specific to the game.
    /// </summary>
    public static class GameConstants {

        public const string StarNameFormat = "{0} {1}";
        public const string PlanetNameFormat = "{0}{1}";
        public const string MoonNameFormat = "{0}{1}";

        public const string CmdNameExtension = " Cmd";
        public const string CreatorExtension = " Creator";
        public const string OrbitSimulatorNameExtension = " OrbitSimulator";

        public static int[] PlanetNumbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        public static string[] MoonLetters = new string[] { "a", "b", "c", "d", "e" };



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


        #region Pooling Prefab Names

        public const string PoolPrefabName_Beam = "Beam";

        public const string PoolPrefabName_Missile = "Missile";

        public const string PoolPrefabName_PhysicsProjectile = "PhysicsProjectile";

        public const string PoolPrefabName_KinematicProjectile = "KinematicProjectile";

        public const string PoolPrefabName_AssaultVehicle = "AssaultVehicle";

        #endregion
    }
}

