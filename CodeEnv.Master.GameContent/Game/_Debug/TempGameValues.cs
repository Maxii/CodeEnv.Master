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
    using System.Collections.Generic;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    public static class TempGameValues {

        public const float __ReqdMissileTravelDistanceBeforePushover = 0.2F;

        public const float InterstellerDrag = .1F;  // should be .001F

        public const float SystemDrag = 1F; // should be .01F   // TODO not currently used

        public const int MaxLosWeaponsForAnyElement = 12;

        public const int MaxMissileWeaponsForAnyElement = 6;

        public const float MinimumFramerate = 25F;

        public const int MinTrackingLabelShowDistance = 50;
        public const int MaxTrackingLabelShowDistance = 5000;

        /// <summary>
        /// The range in degrees that a Turret's minimum barrel elevation setting can take.
        /// </summary>
        public static readonly ValueRange<float> MinimumBarrelElevationRange = new ValueRange<float>(-20F, 70F);

        /// <summary>
        /// The multiplier to apply to speed and thrust when using FasterThanLight technology.
        /// </summary>
        public const float __FtlMultiplier = 10F;

        public const float FlapsMultiplier = 100F;

        public const float DefaultShipOrbitSlotDepth = 2F;

        /// <summary>
        /// The maximum number of facilities a starbase or settlement can have.
        /// </summary>
        public const int MaxFacilitiesPerBase = 25;
        /// <summary>
        /// The radius of a Settlement or Starbase, including all of the base's facilities.
        /// TODO MaxFacilitiesPerBase and [Max]BaseRadius should be tied together.
        /// </summary>
        public const float BaseRadius = 3F;

        public const int MaxShipsPerFleet = 25;
        /// <summary>
        /// The radius of a Fleet. May not include all of the fleet's ships.
        /// TODO MaxShipsPerFleet and FleetRadius should be tied together.
        /// </summary>
        //public const float FleetRadius = 4F;

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

        // Note on MaxRadius values. Use of a static dynamically generated MaxRadius on each Item type works, but it only gets populated 
        // when a item is built. Some items like ships and facilities may not be built until runtime so the max value is zero. As CameraControl 
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
        /// The multiplier used to determine the radius of the keepoutZone around objects. 
        /// Note: This could also be called the MinOrbitDistanceMultiplier since the radius of the
        /// keepoutzone is the same as the minimumShipOrbitDistance.
        /// </summary>
        public const float KeepoutRadiusMultiplier = 1.5F;

        /// <summary>
        /// The total number of orbit slots in a System available for planets and
        /// settlements. The orbit slot for ships around the star is a ShipOrbitSlot, not
        /// one of these SystemOrbitSlots.
        /// </summary>
        public const int TotalOrbitSlotsPerSystem = 6;

        /// <summary>
        /// The maximum number of AI Players allowed in any game instance.
        /// </summary>
        public static int MaxAIPlayers { get { return UniverseSize.Gigantic.DefaultPlayerCount() - 1; } }

        /// <summary>
        /// The colors acceptable for use by players.
        /// </summary>
        public static readonly GameColor[] AllPlayerColors = new GameColor[] {
            GameColor.Blue,
            GameColor.Red, 
            GameColor.Cyan,
            GameColor.Green,
            GameColor.Magenta,
            GameColor.Yellow,
            GameColor.Brown, 
            GameColor.Purple,
            GameColor.Teal,
            GameColor.DarkGreen
        };

        public static float __GetHullMass(FacilityHullCategory hullCat) {
            switch (hullCat) {
                case FacilityHullCategory.CentralHub:
                    return 10000F;
                case FacilityHullCategory.Defense:
                case FacilityHullCategory.Factory:
                    return 5000F;
                case FacilityHullCategory.ColonyHab:
                case FacilityHullCategory.Economic:
                case FacilityHullCategory.Barracks:
                case FacilityHullCategory.Laboratory:
                    return 2000F;
                case FacilityHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        public static float __GetHullMass(ShipHullCategory hullCat) {
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    return 50F;
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return 100F;
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Science:
                    return 200F;
                case ShipHullCategory.Dreadnaught:
                case ShipHullCategory.Troop:
                    return 400F;
                case ShipHullCategory.Carrier:
                    return 500F;
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
        }

        public static float __GetEngineMass(ShipHullCategory hull) {
            return __GetHullMass(hull) * 0.10F;
        }

        public static float __GetFullStlThrust(ShipHullCategory hull) { // generates StlSpeed ~ 1.5 - 3 units/hr;  planetoids ~ 0.1 units/hour, so Slow min = 0.15 units/hr
            switch (hull) {
                case ShipHullCategory.Frigate:
                    return UnityEngine.Random.Range(5F, 15F);
                case ShipHullCategory.Destroyer:
                case ShipHullCategory.Support:
                    return UnityEngine.Random.Range(10F, 30F);
                case ShipHullCategory.Cruiser:
                case ShipHullCategory.Colonizer:
                case ShipHullCategory.Science:
                    return UnityEngine.Random.Range(20F, 60F);
                case ShipHullCategory.Dreadnaught:
                case ShipHullCategory.Troop:
                    return UnityEngine.Random.Range(40F, 120F);
                case ShipHullCategory.Carrier:
                    return UnityEngine.Random.Range(50F, 150F);
                case ShipHullCategory.Scout:
                case ShipHullCategory.Fighter:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hull));
            }
        }



        public static readonly Player NoPlayer = new NoPlayer();

        public static readonly ResourceYield NoResources = default(ResourceYield);

        public static readonly CombatStrength NoCombatStrength = default(CombatStrength);


        public static GameColor SelectedColor { get { return GameColor.Green; } }

        public static GameColor FocusedColor { get { return GameColor.Yellow; } }

        public static GameColor GeneralHighlightColor { get { return GameColor.White; } }

        public static GameColor SectorHighlightColor { get { return GameColor.Yellow; } }


        public static GameColor DisabledColor { get { return GameColor.Gray; } }

        /// <summary>
        /// The name of the sprite (texture) used as a frame around an image.
        /// </summary>
        public const string ImageFrameSpriteName = "imageFrame";

        /// <summary>
        /// The name of the only current image in MyGuiAtlas.
        /// </summary>
        public const string AnImageFilename = "image";

        /// <summary>
        /// The name of the sprite (texture) used to represent an unknown image.
        /// </summary>
        public const string UnknownImageFilename = "Question1_16";

        /// <summary>
        /// The name of the UILabel GameObject used to show the title of a GuiElement.
        /// </summary>
        public const string TitleLabelName = "Title";

        /// <summary>
        /// The name of the UILabel GameObject used to show the "?" symbol.
        /// </summary>
        public const string UnknownLabelName = "Unknown";

    }
}


