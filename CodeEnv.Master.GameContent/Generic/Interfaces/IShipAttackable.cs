// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IShipAttackable.cs
// Interface for targets that can be attacked by ships.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Interface for targets that can be attacked by ships.
    /// </summary>
    public interface IShipAttackable : IElementAttackable {

        /// <summary>
        /// Returns the proxy for this target for use by a Ship's Pilot when attacking this target.
        /// The distances provided allow the proxy to help the ship stay within its desired range envelope of the target.
        /// <remarks>There is no target offset as ships don't attack in formation.</remarks>
        /// </summary>
        /// <param name="minDesiredDistanceToTgtSurface">The minimum desired distance of this ship from the target's surface.</param>
        /// <param name="maxDesiredDistanceToTgtSurface">The maximum desired distance of this ship from the target's surface.</param>
        /// <returns></returns>
        AutoPilotDestinationProxy GetApAttackTgtProxy(float minDesiredDistanceToTgtSurface, float maxDesiredDistanceToTgtSurface);

    }
}

