// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DebugUtility.cs
// Collection of tools and utilities for debugging.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Collection of tools and utilities for debugging.
    /// </summary>
    public static class DebugUtility {

        /// <summary>
        /// The minimum number of hours that must be allowed for a rotation to complete before an error is raised.
        /// <remarks>12.17.16 GameTime.HoursPrecision is currently 0.1 hours.</remarks>
        /// </summary>
        private static float MinimumRotationErrorDateHours = GameTime.HoursPrecision;

        /// <summary>
        /// Calculates and returns the latest GameDate by which this rotation should complete. 
        /// <remarks>Typically used to calculate how long to allow a rotation coroutine to run before throwing a warning or error.
        /// Use of a date in this manner handles GameSpeed changes and Pauses during the rotation.
        /// </remarks>
        /// <remarks>This version is meant for rotation coroutines that use fixed update.</remarks>
        /// </summary>
        /// <param name="rotationRateInDegreesPerHour">The rotation rate in degrees per hour.</param>
        /// <param name="maxRotationReqdInDegrees">The maximum rotation reqd in degrees.</param>
        /// <returns></returns>
        public static GameDate CalcWarningDateForRotation(float rotationRateInDegreesPerHour, float maxRotationReqdInDegrees = 180F) {
            float maxHoursReqdToCompleteRotation = maxRotationReqdInDegrees / rotationRateInDegreesPerHour;
            maxHoursReqdToCompleteRotation = Mathf.Clamp(maxHoursReqdToCompleteRotation, MinimumRotationErrorDateHours, GameTime.HoursPerDay);
            //D.Log("MaxHoursReqdToCompleteRotation of 180 degrees at {0:0.} per hour = {1:0.##}.", rotationRateInDegreesPerHour, maxHoursReqdToCompleteRotation);
            var maxDurationFromCurrentDate = new GameTimeDuration(maxHoursReqdToCompleteRotation);
            return new GameDate(maxDurationFromCurrentDate);
        }

        public static int __GetApproxFleetQty(GameSettings gameSettings) {
            if (gameSettings.__UseDebugCreatorsOnly) {
                return 3;   // HACK
            }
            int userFleetQty = gameSettings.UserStartLevel.FleetStartQty();
            int aiFleetQty = 0;
            var aiStartLevels = gameSettings.AIPlayersStartLevels;
            foreach (var aiStartLevel in aiStartLevels) {
                aiFleetQty += aiStartLevel.FleetStartQty();
            }
            if (gameSettings.__DeployAdditionalAICreators) {
                int aiPlayerQty = aiStartLevels.Length;
                aiFleetQty += aiPlayerQty * gameSettings.__AdditionalFleetCreatorQty;
            }
            return userFleetQty + aiFleetQty;
        }

        public static int __GetApproxBaseQty(GameSettings gameSettings) {
            if (gameSettings.__UseDebugCreatorsOnly) {
                return 4;   // HACK
            }
            int userBaseQty = gameSettings.UserStartLevel.StarbaseStartQty() + gameSettings.UserStartLevel.SettlementStartQty();
            int aiBaseQty = 0;
            var aiStartLevels = gameSettings.AIPlayersStartLevels;
            foreach (var aiStartLevel in aiStartLevels) {
                aiBaseQty += aiStartLevel.StarbaseStartQty() + aiStartLevel.SettlementStartQty();
            }
            if (gameSettings.__DeployAdditionalAICreators) {
                int aiPlayerQty = aiStartLevels.Length;
                aiBaseQty += aiPlayerQty * gameSettings.__AdditionalStarbaseCreatorQty + aiPlayerQty * gameSettings.__AdditionalSettlementCreatorQty;
            }
            return userBaseQty + aiBaseQty;
        }

    }
}

