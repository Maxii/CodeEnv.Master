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

        public const float UniverseCenterRadius = 50F;

        /// <summary>
        /// The radius of the star sphere.
        /// </summary>
        public const float StarRadius = 10F;    // 10 x 300(factor) = 3000(cullingDistance)

        public const float PlanetoidRadius_Max = 5.0F;  // Moons are 0.2 - 1.0, Planets 1.0 - 5.0   // 5 x 200(factor) = 1000(cullingDistance)

        public const float StarBaseRadius = 0.5F;

        public const float SettlementRadius = 1.0F; // 1 x 50(factor) = 50(cullingDistance)

        public const float ShipRadius_Max = 0.2F;   // range is 0.04 - 0.2      // 0.2 x 25(factor) = 5(cullingDistance)

        /// <summary>
        /// The multiplier used to determine the radius of the keepoutZone around celestial objects.
        /// </summary>
        public const float KeepoutRadiusMultiplier = 2.5F;

        /// <summary>
        /// The radius of the keepout zone around the center of a star. Used to avoid
        /// ships, planets and moons getting unrealistically close.
        /// </summary>
        public const float StarKeepoutRadius = StarRadius * KeepoutRadiusMultiplier;

        public const float MaxKeepoutRadius = UniverseCenterRadius * KeepoutRadiusMultiplier;

        public const int SystemOrbitSlots = 7;

        public static float __GetMass(ShipCategory hull) {
            switch (hull) {
                case ShipCategory.Fighter:
                    return 10F;
                case ShipCategory.Frigate:
                    return 50F;
                case ShipCategory.Destroyer:
                    return 100F;
                case ShipCategory.Cruiser:
                    return 200F;
                case ShipCategory.Dreadnaught:
                    return 400F;
                case ShipCategory.Carrier:
                    return 500F;
                case ShipCategory.Colonizer:
                case ShipCategory.Science:
                case ShipCategory.Scout:
                case ShipCategory.Troop:
                case ShipCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hull));
            }
        }


    }
}


