// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipSpeedExtensions.cs
// Extension class providing values acquired externally from Xml for the Speed enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extension class providing values acquired externally from Xml for the Speed enum.
    /// </summary>
    public static class ShipSpeedExtensions {

        /// <summary>
        /// Gets the speed in units per day for this ship or fleet.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="fullSpeed">The current full speed capability of this ship or fleet.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetValue(this Speed speed, float fullSpeed) {
            switch (speed) {
                case Speed.AllStop:
                    return Constants.ZeroF;
                case Speed.Slow:
                    return 0.10F * fullSpeed;
                case Speed.OneThird:
                    return 0.25F * fullSpeed;
                case Speed.TwoThirds:
                    return 0.5F * fullSpeed;
                case Speed.Standard:
                    return 0.75F * fullSpeed;
                case Speed.Full:
                    return 1.0F * fullSpeed;
                case Speed.Flank:
                    return 1.10F * fullSpeed;
                //case Speed.FleetStandard:
                //return 0.75F * fullSpeed;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }

    }
}

