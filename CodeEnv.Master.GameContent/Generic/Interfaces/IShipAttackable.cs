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

    using CodeEnv.Master.Common;

    /// <summary>
    /// Interface for targets that can be attacked by ships.
    /// </summary>
    public interface IShipAttackable : IElementAttackable {

        /// <summary>
        /// Returns the proxy for this target for use by a Ship's Pilot when attacking this target.
        /// The values provided allow the proxy to help the ship stay within its desired weapons range envelope relative to the target's surface.
        /// <remarks>There is no target offset as ships don't attack in formation.</remarks>
        /// </summary>
        /// <param name="desiredWeaponsRangeEnvelope">The ship's desired weapons range envelope relative to the target's surface.</param>
        /// <param name="shipCollisionDetectionRadius">The attacking ship's collision detection radius.</param>
        /// <returns></returns>
        AutoPilotDestinationProxy GetApAttackTgtProxy(ValueRange<float> desiredWeaponsRangeEnvelope, float shipCollisionDetectionRadius);

    }
}

