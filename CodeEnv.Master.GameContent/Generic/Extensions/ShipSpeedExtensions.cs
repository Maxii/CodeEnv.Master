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

                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }

        /// <summary>
        /// Returns the next faster speed above this one. If there is no faster
        /// speed, or speed is AllStop, then None is returned.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Speed GetFaster(this Speed speed) {
            switch (speed) {
                case Speed.AllStop:
                    return Speed.None;

                case Speed.Slow:
                    return Speed.OneThird;
                case Speed.OneThird:
                    return Speed.TwoThirds;
                case Speed.TwoThirds:
                    return Speed.Standard;
                case Speed.Standard:
                    return Speed.Full;
                case Speed.Full:
                    return Speed.Flank;
                case Speed.Flank:
                    return Speed.None;

                case Speed.FleetSlow:
                    return Speed.FleetOneThird;
                case Speed.FleetOneThird:
                    return Speed.FleetTwoThirds;
                case Speed.FleetTwoThirds:
                    return Speed.FleetStandard;
                case Speed.FleetStandard:
                    return Speed.FleetFull;
                case Speed.FleetFull:
                    return Speed.None;

                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }

        /// <summary>
        /// Returns the next slower speed below this one. If there is no slower
        /// speed (besides AllStop), or speed is AllStop, then None is returned.
        /// </summary>
        /// <param name="speed">The speed.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Speed GetSlower(this Speed speed) {
            switch (speed) {
                case Speed.AllStop:
                    return Speed.None;

                case Speed.Slow:
                    return Speed.None;
                case Speed.OneThird:
                    return Speed.Slow;
                case Speed.TwoThirds:
                    return Speed.OneThird;
                case Speed.Standard:
                    return Speed.TwoThirds;
                case Speed.Full:
                    return Speed.Standard;
                case Speed.Flank:
                    return Speed.Full;

                case Speed.FleetSlow:
                    return Speed.None;
                case Speed.FleetOneThird:
                    return Speed.FleetSlow;
                case Speed.FleetTwoThirds:
                    return Speed.FleetOneThird;
                case Speed.FleetStandard:
                    return Speed.FleetTwoThirds;
                case Speed.FleetFull:
                    return Speed.FleetStandard;

                case Speed.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(speed));
            }
        }
    }
}

