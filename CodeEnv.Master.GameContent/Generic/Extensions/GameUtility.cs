// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameUtility.cs
// Collection of tools and utilities specific to the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Collection of tools and utilities specific to the game.
    /// </summary>
    public static class GameUtility {

        /// <summary>
        /// Derives the enum value of type E from within the provided name. Case insensitive.
        /// </summary>
        /// <typeparam name="E"></typeparam>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static E DeriveEnumFromName<E>(string name) where E : struct {
            D.Assert(typeof(E).IsEnum, "E must be an enumerated type.");
            Arguments.ValidateForContent(name);
            return Enums<E>.GetValues().Single(e => name.Trim().ToLower().Contains(e.ToString().ToLower()));
        }

        //public static bool CheckForIncreasingSeparation(float distanceToCurrentDestinationSqrd, ref float previousDistanceSqrd) {
        //    if (distanceToCurrentDestinationSqrd > previousDistanceSqrd + TempGameValues.IncreasingSeparationDistanceTestToleranceSqrd) {
        //        return true;
        //    }
        //    if (distanceToCurrentDestinationSqrd < previousDistanceSqrd) {
        //        // while we continue to move closer to the current destination, keep previous distance current
        //        // once we start to move away, we must not update it if we want the tolerance check to catch it
        //        previousDistanceSqrd = distanceToCurrentDestinationSqrd;
        //    }
        //    return false;
        //}

    }
}

