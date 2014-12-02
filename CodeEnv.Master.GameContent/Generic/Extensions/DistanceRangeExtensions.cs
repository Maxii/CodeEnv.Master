// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DistanceRangeExtensions.cs
// Extension class providing values acquired externally from Xml for the DistanceRange enum.
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
    /// Extension class providing values acquired externally from Xml for the DistanceRange enum.
    /// TODO Externalize values in XML.
    /// </summary>
    public static class DistanceRangeExtensions {

        public static float GetWeaponRange(this DistanceRange weaponRange, IPlayer owner) {
            var speciesModifier = owner.Race.Species.GetWeaponRangeModifier();
            switch (weaponRange) {
                case DistanceRange.Short:
                    return 4F * speciesModifier;
                case DistanceRange.Medium:
                    return 7F * speciesModifier;
                case DistanceRange.Long:
                    return 10F * speciesModifier;
                case DistanceRange.None:
                    return Constants.ZeroF;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(weaponRange));
            }
        }

        public static float GetSensorRange(this DistanceRange sensorRange, IPlayer owner) {
            var speciesModifier = owner.Race.Species.GetSensorRangeModifier();
            switch (sensorRange) {
                case DistanceRange.Short:
                    return 120F * speciesModifier;
                case DistanceRange.Medium:
                    return 1200F * speciesModifier;
                case DistanceRange.Long:
                    return 3600F * speciesModifier;
                case DistanceRange.None:
                    return Constants.ZeroF;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sensorRange));
            }

        }

    }
}

