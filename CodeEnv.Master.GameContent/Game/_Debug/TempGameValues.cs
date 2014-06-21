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
        /// The multiplier to apply to speed and thrust when using FasterThanLight technology.
        /// </summary>
        public const float __FtlMultiplier = 10F;

        public const float FlapsMultiplier = 100F;

        /// <summary>
        /// The maximum number of facilities a starbase or settlement can have.
        /// </summary>
        public const int MaxFacilitiesPerBase = 25;

        public const int MaxShipsPerFleet = 25;

        /// <summary>
        /// The length in world units of a sector side along any of the axis. As a sector
        /// is a cube, all side lengths are equal.
        /// </summary>
        public const float SectorSideLength = 1200F;

        /// <summary>
        /// The length in world units of the diagonal across a sector side. Sqrt(SectorSideLengthSqrd x 2).
        /// </summary>
        public static readonly float SectorSideDiagonalLength = SectorSideLength * Mathf.Sqrt(2F);    // 1697.06

        /// <summary>
        /// The length in world units of the diagonal across a sector, aka the longest distance between corners.
        /// Sqrt(SectorSideLengthSqrd + SectorSideDiagonalLengthSqrd).
        /// </summary>
        public static readonly float SectorDiagonalLength = (float)Mathf.Sqrt(Mathf.Pow(SectorSideLength, 2F) + Mathf.Pow(SectorSideDiagonalLength, 2F));    // 2078.46

        public static readonly Vector3 SectorSize = new Vector3(SectorSideLength, SectorSideLength, SectorSideLength);

        public const float SystemRadius = 120F;

        public const float UniverseCenterRadius = 50F;

        #region Max Radius values for setting culling distances

        // Note on MaxRadius values. Use of a static dynamically generated MaxRadius on each Model type works, but it only gets populated 
        // when a model is built. Some models like ships and facilities may not be built until runtime so the max value is zero. As CameraControl 
        // needs these values during startup to set the layer culling distances, I had to return to these values for now.

        public const float StarMaxRadius = 10F;    // 10 x 300(factor) = 3000(cullingDistance)

        public const float PlanetoidMaxRadius = 5.0F;  // Moons are 0.2 - 1.0, Planets 1.0 - 5.0   // 5 x 200(factor) = 1000(cullingDistance)

        public const float FacilityMaxRadius = 0.25F;  // range is 0.125 - 0.25  // .25 x 50(factor) = 12.5(cullingDistance)

        public const float ShipMaxRadius = 0.2F;   // range is 0.04 - 0.2      // 0.2 x 25(factor) = 5(cullingDistance)

        #endregion

        /// <summary>
        /// The distance from the center of an obstacle to the side of the box of graphPoint vertices surrounding it. 
        /// The distance is a value between 0.0 and 1.0.  0 represents a box whose side is <c>ObstacleRadius</c> away 
        /// from the center of the obstacle, aka a box that bounds a sphere of size ObstacleRadius.
        /// 1.0 represents a box whose side is <c>SectorSideLength</c> from the center of the obstacle.
        /// </summary>
        public const float PathGraphPointPercentDistanceAroundObstacles = 0.1F;

        /// <summary>
        /// The multiplier used to determine the radius of the keepoutZone around celestial objects.
        /// </summary>
        public const float KeepoutRadiusMultiplier = 2F;

        //public const float MaxKeepoutDiameter = UniverseCenterRadius * KeepoutRadiusMultiplier * 2F;

        /// <summary>
        /// The total number of orbit slots in a System, including those for planets,
        /// settlements and the inner-most slot reserved for ships orbiting the star.
        /// </summary>
        public const int TotalOrbitSlotsPerSystem = 7;

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

        public static readonly Race HumanPlayersRace = new Race(new RaceStat(PlayerPrefsManager.Instance.PlayerRace, "Maxii",
            "Maxii description", PlayerPrefsManager.Instance.PlayerColor));

        public static readonly Player NoPlayer = new NoPlayer();

        public static readonly XYield NoSpecialResources = default(XYield);

        public static readonly CombatStrength NoCombatStrength = default(CombatStrength);


    }
}


