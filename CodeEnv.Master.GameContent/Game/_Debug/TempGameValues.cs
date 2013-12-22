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

        public const int MinTrackingLabelShowDistance = 50;
        public const int MaxTrackingLabelShowDistance = 5000;


        /// <summary>
        /// The length in world units of a sector side along any of the axis. As a sector
        /// is a cube, all side lengths are equal.
        /// </summary>
        public const float SectorSideLength = 1200F;

        /// <summary>
        /// The length in world units of the diagonal across a sector side. Sqrt(SectorSIdeLengthSqrd x 2).
        /// </summary>
        public static readonly float SectorSideDiagonalLength = SectorSideLength * Mathf.Sqrt(2F);    // 1697.06

        /// <summary>
        /// The length in world units of the diagonal across a sector, aka the longest distance between corners.
        /// Sqrt(SectorSideLengthSqrd + SectorSideDiagonalLenghtSqrd).
        /// </summary>
        public static readonly float SectorDiagonalLength = (float)Math.Sqrt(Math.Pow(SectorSideLength, 2F) + Math.Pow(SectorSideDiagonalLength, 2F));    // 2078.46

        public static readonly Vector3 SectorSize = new Vector3(SectorSideLength, SectorSideLength, SectorSideLength);

        public const float SystemRadius = 120F;

        public const float UniverseCenterRadius = 20F;

        /// <summary>
        /// The radius of the star sphere.
        /// </summary>
        public const float StarRadius = 10F;

        /// <summary>
        /// The multiplier used to determine the radius of the keepoutZone around celestial objects.
        /// </summary>
        public const float KeepoutRadiusMultiplier = 2.5F;

        /// <summary>
        /// The radius of the keepout zone around the center of a star. Used to avoid
        /// ships, planets and moons getting unrealistically close.
        /// </summary>
        public const float StarKeepoutRadius = StarRadius * KeepoutRadiusMultiplier;

        public const int SystemOrbitSlots = 7;

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


