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

    /// <summary>
    /// Collection of tools and utilities specific to the game.
    /// </summary>
    public static class GameUtility {

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

