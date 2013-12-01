// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TempGameValues.cs
// Static class of common constants specific to the Unity Engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public static class TempGameValues {

        public const int MinFleetTrackingLabelShowDistance = 50;
        public const int MaxFleetTrackingLabelShowDistance = 300;

        public const int MinSystemTrackingLabelShowDistance = 100;
        public const int MaxSystemTrackingLabelShowDistance = 2500;

        /// <summary>
        /// The length in world units of a sector side along any of the axis. As a sector
        /// is a cube, all side lengths are equal.
        /// </summary>
        public const float SectorSideLength = 1200F;

        public static readonly Vector3 SectorSize = new Vector3(SectorSideLength, SectorSideLength, SectorSideLength);

        public const float SystemRadius = 120F;

        public const float StarRadius = 10F;

        public const int SystemOrbitSlots = 8;

        public static float __GetMass(ShipHull hull) {
            switch (hull) {
                case ShipHull.Fighter:
                    return 10F;
                case ShipHull.Frigate:
                    return 50F;
                case ShipHull.Destroyer:
                    return 100F;
                case ShipHull.Cruiser:
                    return 200F;
                case ShipHull.Dreadnaught:
                    return 400F;
                case ShipHull.Carrier:
                    return 500F;
                case ShipHull.Colonizer:
                case ShipHull.Science:
                case ShipHull.Scout:
                case ShipHull.Troop:
                case ShipHull.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hull));
            }
        }


    }
}


