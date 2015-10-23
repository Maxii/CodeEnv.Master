// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrderSourceExtensions.cs
// Extensions for the OrderSource enum.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Extensions for the OrderSource enum.
    /// </summary>
    public static class OrderSourceExtensions {

        /// <summary>
        /// The speeds that are valid when issued by OrderSource.UnitCommand.
        /// </summary>
        private static IList<Speed> _validFleetSpeeds = new List<Speed>() {
            Speed.FleetSlow,
            Speed.FleetOneThird,
            Speed.FleetTwoThirds,
            Speed.FleetStandard,
            Speed.FleetFull
        };

        /// <summary>
        /// The speeds that are valid when issued by OrderSource.ElementCaptain.
        /// </summary>
        private static IList<Speed> _validShipSpeeds = new List<Speed>() {
            Speed.EmergencyStop,
            Speed.Stop,
            Speed.Docking,
            Speed.StationaryOrbit,
            Speed.MovingOrbit,
            Speed.Slow, 
            Speed.OneThird,
            Speed.TwoThirds,
            Speed.Standard,
            Speed.Full
        };

        /// <summary>
        /// Validates that the provided speed is valid from this orderSource.
        /// Note: Speed.None is not valid for either orderSource.
        /// </summary>
        /// <param name="orderSource">The order source.</param>
        /// <param name="speed">The speed.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public static void ValidateSpeed(this OrderSource orderSource, Speed speed) {
            switch (orderSource) {
                case OrderSource.ElementCaptain:
                    Arguments.Validate(_validShipSpeeds.Contains(speed));
                    break;
                case OrderSource.UnitCommand:
                    Arguments.Validate(_validFleetSpeeds.Contains(speed));
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(orderSource));
            }
        }

    }
}

