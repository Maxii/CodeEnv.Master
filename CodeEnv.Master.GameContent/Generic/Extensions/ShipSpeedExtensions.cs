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
        /// Gets the speed in units per day for this ship or fleet. Either data
        /// can be null if you are certain which speed you are asking for.
        /// </summary>
        /// <param name="speed">The speed enum value.</param>
        /// <param name="fleetData">The fleet data.</param>
        /// <param name="shipData">The ship data.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static float GetValue(this Speed speed, FleetCmdData fleetData, ShipData shipData = null) {
            switch (speed) {
                case Speed.AllStop:
                    return Constants.ZeroF;

                case Speed.Slow:
                    return 0.10F * shipData.FullSpeed;
                case Speed.OneThird:
                    return 0.25F * shipData.FullSpeed;
                case Speed.TwoThirds:
                    return 0.5F * shipData.FullSpeed;
                case Speed.Standard:
                    return 0.75F * shipData.FullSpeed;
                case Speed.Full:
                    return 1.0F * shipData.FullSpeed;
                case Speed.Flank:
                    return 1.10F * shipData.FullSpeed;

                case Speed.FleetSlow:
                    return 0.10F * fleetData.FullSpeed;
                case Speed.FleetOneThird:
                    return 0.25F * fleetData.FullSpeed;
                case Speed.FleetTwoThirds:
                    return 0.5F * fleetData.FullSpeed;
                case Speed.FleetStandard:
                    return 0.75F * fleetData.FullSpeed;
                case Speed.FleetFull:
                    return 1.0F * fleetData.FullSpeed;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }

    }
}

