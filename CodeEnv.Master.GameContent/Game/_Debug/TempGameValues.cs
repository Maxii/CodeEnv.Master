﻿// --------------------------------------------------------------------------------------------------------------------
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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    public static class TempGameValues {

        public const string __GameMgrProgressCheckJobName = "GameMgrProgressCheckJob";

        public const bool DoSettlementsActivelyOrbit = false;

        public const float __MaxShipMoveDistanceFromFleetCmdSqrd = 22500F;   // 150

        /// <summary>
        /// The scaler to use on selected element values when an Element has construction underway.
        /// </summary>
        public const float UnderConstructionValuesScaler = 0.25F;

        /// <summary>
        /// The multiplier to use with a Design's ConstructionCost when calculating the minimum
        /// allowed ConstructionCost for an Element Refit.
        /// </summary>
        public const float MinRefitConstructionCostFactor = 0.25F;

        /// <summary>
        /// The multiplier to use with a Design's ConstructionCost when calculating the minimum
        /// allowed ConstructionCost for an Element Disband.
        /// </summary>
        public const float MinDisbandConstructionCostFactor = 0.25F;

        /// <summary>
        /// The maximum range of any weapon in a base.
        /// </summary>
        public const float __MaxBaseWeaponsRangeDistance = 25F;

        /// <summary>
        /// The maximum range of any weapon in a fleet.
        /// </summary>
        public const float __MaxFleetWeaponsRangeDistance = 20F;

        /// <summary>
        /// TEMP The minimum weapons range distance. Used for warning if weapons range is not sufficient.
        /// <remarks>Bit of overkill as doesn't account for distance from item to closest surface.</remarks>
        /// </summary>
        public static float __MinWeaponsRangeDistance
            = Mathf.Max(LargestPlanetoidObstacleZoneRadius, LargestFacilityObstacleZoneRadius, LargestShipCollisionDetectionZoneRadius)
            + LargestShipCollisionDetectionZoneRadius;

        /// <summary>
        /// The Game's minimum allowed turn rate of a ship in degrees per hour.
        /// <remarks>Used to calculate the lowest turn rate in degrees per frame 
        /// that a ship can have. Prevents a ship from correcting its heading too often
        /// when following a course which can cause the ChangeHeadingJob to constantly 
        /// kill and recreate itself, thereby never turning.</remarks>
        /// </summary>
        public const float MinimumTurnRate = 90F;

        public const float WaypointCloseEnoughDistance = 2F;

        public const float ObstacleCheckRayLengthBuffer = 0.1F;

        public const float __ReqdLaunchVehicleTravelDistanceBeforePushover = 0.2F;

        public const float __AllowedTurnTimeBufferFactor = 1.30F;   // 11.6.16 1.2 -> 1.3

        public const int MaxTurretHullMounts = 12;
        public const int MaxSiloHullMounts = 6;
        public const int MaxSensorMounts = 4;
        public const int MaxSkinMounts = 6;
        public const int MaxScreenMounts = 10;
        public const int MaxFlexMounts = 3;

        public const int __MaxElementPassiveCMs = 6;
        public const int __MaxElementActiveCMs = 6;
        public const int __MaxElementShieldGenerators = 4;
        /// <summary>
        /// The maximum number of SR sensors allowed for any element, including both reqd and optional sensors.
        /// </summary>
        public const int __MaxElementSensors = 4;
        public const int __MaxCmdPassiveCMs = 4;
        /// <summary>
        /// The maximum number of MR and LR sensors allowed for any cmd, including both reqd and optional sensors.
        /// </summary>
        public const int __MaxCmdSensors = 4;

        public const float MinimumFramerate = 25F;

        public const int MinTrackingLabelShowDistance = 50;
        public const int MaxTrackingLabelShowDistance = 5000;

        /// <summary>
        /// The number of hours between FiringSolutionChecks for Weapons and ActiveCountermeasures.
        /// </summary>
        public const float HoursBetweenFiringSolutionChecks = 1F;

        /// <summary>
        /// The range in degrees that a Turret's minimum barrel elevation setting can take.
        /// </summary>
        public static readonly ValueRange<float> MinimumBarrelElevationRange = new ValueRange<float>(-20F, 70F);

        public static EquipmentCategory[] EquipCatsSupportedByElementDesigner = {
                                                                                    EquipmentCategory.AssaultWeapon,
                                                                                    EquipmentCategory.MissileWeapon,
                                                                                    EquipmentCategory.BeamWeapon,
                                                                                    EquipmentCategory.ProjectileWeapon,
                                                                                    EquipmentCategory.ActiveCountermeasure,
                                                                                    EquipmentCategory.PassiveCountermeasure,
                                                                                    EquipmentCategory.SRSensor,
                                                                                    EquipmentCategory.ShieldGenerator
                                                                                };

        public static EquipmentCategory[] EquipCatsSupportedByCmdModuleDesigner =   {
                                                                                        EquipmentCategory.PassiveCountermeasure,
                                                                                        EquipmentCategory.MRSensor,
                                                                                        EquipmentCategory.LRSensor
                                                                                    };

        public static ShipHullCategory[] ShipHullCategoriesInUse =  {
                                                                        ShipHullCategory.Carrier,
                                                                        ShipHullCategory.Colonizer,
                                                                        ShipHullCategory.Cruiser,
                                                                        ShipHullCategory.Destroyer,
                                                                        ShipHullCategory.Dreadnought,
                                                                        ShipHullCategory.Frigate,
                                                                        ShipHullCategory.Investigator,
                                                                        ShipHullCategory.Support,
                                                                        ShipHullCategory.Troop
                                                                    };

        public static FacilityHullCategory[] FacilityHullCategoriesInUse =  {
                                                                                FacilityHullCategory.Barracks,
                                                                                FacilityHullCategory.CentralHub,
                                                                                FacilityHullCategory.ColonyHab,
                                                                                FacilityHullCategory.Defense,
                                                                                FacilityHullCategory.Economic,
                                                                                FacilityHullCategory.Factory,
                                                                                FacilityHullCategory.Laboratory
                                                                            };

        public static Formation[] AcceptableFleetFormations =   {
                                                                    Formation.Diamond,
                                                                    Formation.Globe,
                                                                    Formation.Plane,
                                                                    Formation.Spread,
                                                                    Formation.Wedge
                                                                };

        public static Formation[] AcceptableBaseFormations =    {
                                                                    Formation.Diamond,
                                                                    Formation.Globe,
                                                                    Formation.Plane,
                                                                    Formation.Spread
                                                                };

        public static ShipHullCategory[] DesiredExplorationShipCategories = {
                                                                                ShipHullCategory.Investigator,
                                                                                ShipHullCategory.Scout,
                                                                                ShipHullCategory.Frigate,
                                                                                ShipHullCategory.Destroyer
                                                                            };

        public const string __FleetCmdDesignName_Basic = "FleetCmdDesign_Basic";

        public const string EmptyFleetCmdTemplateDesignName = "EmptyFleetCmdTemplateDesign";
        public const string EmptyStarbaseCmdTemplateDesignName = "EmptyStarbaseCmdTemplateDesign";
        public const string EmptySettlementCmdTemplateDesignName = "EmptySettlementCmdTemplateDesign";

        #region Ship Speed

        /// <summary>
        /// The fastest ship speed value in units per hour allowed under propulsion. Typically
        /// realized in OpenSpace. 1.23.17 Currently used only for debug.
        /// </summary>
        public const float __ShipMaxSpeedValue = 40F;

        /// <summary>
        /// The maximum allowed orbit speed in units per hour of a Planet.
        /// Used to warn if orbit period causes this speed to be exceeded.
        /// <remarks>4.12.18 Moons have speeds up to 0.1272, Planets up to 0.0997 with RelativeOrbitSpeed = 1.0,
        /// 0.0399 with RelativeOrbitSpeed = 0.4.
        /// Moons can exceed this value as Ship's don't try to 'catch' Moons as Ships no longer 'close orbit' moons.</remarks>
        /// <remarks>IDEA: Derive OrbitPeriod of each planetoid from this value?</remarks>
        /// <remarks>Originally derived from reporting each planetoids orbit speed.</remarks>
        /// </summary>
        public const float __MaxPlanetOrbitSpeed = 0.04F;

        /// <summary>
        /// The slowest ship speed value allowed under propulsion. Set above __MaxPlanetOrbitSpeed
        /// to make sure a ship can catch a planet moving directly away from it.
        /// </summary>
        public const float ShipMinimumSpeedValue = __MaxPlanetOrbitSpeed * 1.25F;   // 0.05

        /// <summary>
        /// The relative speed in units per hour of FTL engines as compared to STL engines at the same <c>Speed</c> setting, aka Speed.Standard, etc.
        /// </summary>
        public const float StlToFtlSpeedFactor = 4F;    // 4.12.18 5 -> 4

        /// <summary>
        /// The relative density of matter in a System or Nebula compared to OpenSpace.
        /// <remarks>Effects a ship hull's current drag value. A ship's top speed in units per hour in a 
        /// System or Nebula is reduced by this factor as compared to OpenSpace.</remarks>
        /// </summary>
        public const float SystemNebulaToOpenSpaceDensityFactor = 2.5F; // 4.12.18 5 -> 2.5

        /// <summary>
        /// The relative orbit rate of planets around a star.
        /// <remarks>See 4.12.18 Notebook workup.</remarks>
        /// <remarks>Value coordinated with EquipmentStatFactory.__GetMaxAttainableSpeed(level) so that planets in a system can be caught
        /// by ships even at Speed.ThrustersOnly with Level.One STL Engines.</remarks>
        /// </summary>
        public const float RelativeOrbitRateOfPlanets = 0.4F;

        public const float RelativeOrbitRateOfMoons = 1.0F;

        public const float RelativeOrbitRateOfShips = 1.0F;

        /// <summary>
        /// The highest FullSpeed value (Units per hour) a ship can have, aka EngineStat Level.Five, FullSpeed, FTL in OpenSpace.
        /// </summary>
        public const float __TargetFtlOpenSpaceFullSpeed = 40F;

        // Note: Drag variations handled by Topography.GetRelativeDensity(), aka InSystemDrag = OpenSpaceDrag x 5F

        #endregion

        #region Close Orbit Values    

        /// <summary>
        /// The depth of a ship close orbit slot.
        /// <remarks>4.9.16 increased from 1.0F after allowing ship APilot to change speed.
        /// As all of ship (including detection zone) needs to be within the slot, the remaining slot
        /// arrival depth forced ships to go too slow and couldn't catch moving planets.</remarks>
        /// </summary>
        public const float ShipCloseOrbitSlotDepth = 1.5F;

        /// <summary>
        /// Value to add to the Planet's Radius when setting the CloseOrbitInnerRadius surrounding the moon.
        /// </summary>
        public const float PlanetCloseOrbitInnerRadiusAdder = 1F;


        #endregion

        #region Repair Capacity Values

        public const float RepairCapacityBaseline_FormationStation_Element = 1F;
        public const float RepairCapacityBaseline_Planet_Element = 2F;
        public const float RepairCapacityBaseline_Base_Element = 3F;
        public const float RepairCapacityBaseline_FormationStation_CmdModule = 0.1F;
        public const float RepairCapacityBaseline_Planet_CmdModule = 0.2F;
        public const float RepairCapacityBaseline_Base_CmdModule = 1F;
        public const float RepairCapacityFactor_HighOrbit = 1.5F;
        public const float RepairCapacityFactor_CloseOrbit = 2F;
        public const float RepairCapacityFactor_Hanger = 3F;

        #endregion

        public const float ProductionCostBuyoutMultiplier = 2.0F;

        /// <summary>
        /// The maximum number of facilities a starbase or settlement can have.
        /// </summary>
        public const int MaxFacilitiesPerBase = 25;

        public const int MaxShipsPerFleet = 25;

        #region Radii
        // Note on MaxRadius values. Use of a static dynamically generated MaxRadius on each Item type works, but it only gets populated 
        // when a item is built. Some items like ships and facilities may not be built until runtime so the max value is zero. As CameraControl 
        // needs these values during startup to set the layer culling distances, I had to return to these values for now.

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

        public const float StarRadius = 10F;

        [System.Obsolete("Not currently used")]
        public const float MaxPlanetoidRadius = 5.0F;   // range is 0.2 (smallest moon) - 5.0 (Gas Giant)

        public const float LargestPlanetoidObstacleZoneRadius = 6.1F; // Gas Giant = 6.0, determined by closeOrbitInnerRadius

        /// <summary>
        /// Value to add to the Moon's Radius when setting the radius of the ObstacleZone surrounding the moon.
        /// </summary>
        public const float MoonObstacleZoneRadiusAdder = 1F;

        public const float MaxFacilityRadius = 0.35F;  // range is 0.17 - 0.3465 CentralHub (half of HullDimension.magnitude of 0.693)

        /// <summary>
        /// The multiplier used to determine the radius of a Facility's ObstacleZoneColliderZone.
        /// <remarks>Typically applied to a facility's radius.</remarks>
        /// </summary>
        public const float FacilityObstacleZoneRadiusMultiplier = 2.0F;

        public static readonly float LargestFacilityObstacleZoneRadius   // Central Hub = 0.70
            = MaxFacilityRadius * FacilityObstacleZoneRadiusMultiplier;

        // 11.5.17 Primary use waiting on facilities ability to move
        [System.Obsolete("Not currently used")]
        public const float FacilityCollisionDetectionMonitorRangeMultiplier = 1.2F;

        // 11.5.17 Primary use waiting on facilities ability to move
        [System.Obsolete("Not currently used")]
        public static readonly float LargestFacilityCollisionDetectionZoneRadius    // CentralHub = 0.84
            = LargestFacilityObstacleZoneRadius * FacilityCollisionDetectionMonitorRangeMultiplier;

        /// <summary>
        /// The radius of each FormationStation in a starbase or settlement formation = 1.68.
        /// <remarks>UNDONE FormationPlaceholder cells should be adjusted to have edges of more than double this value to space formation
        /// stations so they don't overlap. 11.5.17 Currently 1.5 just like fleet.</remarks>
        /// </summary>
        [System.Obsolete("Not currently used")]
        public static readonly float BaseFormationStationRadius = LargestFacilityCollisionDetectionZoneRadius * 2F; // CentralHub = 1.68

        [System.Obsolete("Not currently used as FormationGridOrganizer cell size is set in editor")]    // 11.5.17 Currently using 1.5
        public static readonly float MinBaseFormationGridSideLength = BaseFormationStationRadius * 2F;  // 3.36


        [System.Obsolete("Not currently used as FormationGridOrganizer cell size is set in editor")]        // 11.5.17 Currently using 0.75
        public static readonly float MinHangerFormationGridSizeLength = MaxFacilityRadius + MaxShipRadius;  // 0.525

        public const float MaxShipRadius = 0.175F;   // range is 0.03 - 0.1705 Carrier (half of HullDimension.magnitude of 0.341)

        /// <summary>
        /// The multiplier used to determine the range of a Ship's CollisionDetectionMonitor.
        /// <remarks>Typically applied to a ship's radius.</remarks>
        /// </summary>
        public const float ShipCollisionDetectionMonitorRangeMultiplier = 2.0F;

        public static readonly float LargestShipCollisionDetectionZoneRadius      // Carrier = 0.35 // Smallest = Frigate 0.06
            = MaxShipRadius * ShipCollisionDetectionMonitorRangeMultiplier;

        /// <summary>
        /// The radius of each FormationStation in a fleet formation = 0.7.
        /// <remarks>FormationPlaceholder cells have been adjusted to have edges of more than double this value to space formation
        /// stations so they don't overlap. 11.5.17 Currently 1.5.</remarks>
        /// </summary>
        public static readonly float FleetFormationStationRadius = LargestShipCollisionDetectionZoneRadius * 2F;    // Carrier = 0.70

        [System.Obsolete("Not currently used as FormationGridOrganizer cell size is set in editor")]        // 11.5.17 Currently using 1.5
        public static readonly float MinFleetFormationGridSideLength = FleetFormationStationRadius * 2F;    // 1.40

        /// <summary>
        /// The multiplier used to determine the ClearanceRadius around an Element.
        /// <remarks>For a ship, the multiplier is applied to the CollisionDetectionZoneRadius.
        /// For a facility, it is applied to the ObstacleZoneRadius.</remarks>
        /// </summary>
        public const float ElementClearanceRadiusMultiplier = 5F;

        #endregion

        #region Culling distances and assignments

        /// <summary>
        /// The layer the UCenter mesh uses to automatically have the camera cull it based on distance.
        /// <remarks>The camera does not auto cull based on distance if Layers.TransparentFX.</remarks>
        /// </summary>
        public const Layers UCenterMeshCullLayer = Layers.TransparentFX;

        /// <summary>
        /// The layer System meshes use to automatically have the camera cull them based on distance.
        /// <remarks>A 'System mesh' is the orbital plane of a system.</remarks>
        /// </summary>
        public const Layers SystemMeshCullLayer = Layers.SystemOrbitalPlane;

        /// <summary>
        /// The layer Cmd meshes use to automatically have the camera cull them based on distance.
        /// <remarks>A 'Cmd mesh' is the highlight bubble that surrounds the HQ element.</remarks>
        /// </summary>
        public const Layers CmdMeshCullLayer = Layers.Cull_200;

        /// <summary>
        /// The layer Star meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers StarMeshCullLayer = Layers.Cull_3000;

        /// <summary>
        /// The layer larger planet meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers LargerPlanetMeshCullLayer = Layers.Cull_400;

        /// <summary>
        /// The layer smaller planet meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers SmallerPlanetMeshCullLayer = Layers.Cull_200;

        /// <summary>
        /// The layer moon meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers MoonMeshCullLayer = Layers.Cull_200;

        /// <summary>
        /// The layer larger facility meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers LargerFacilityMeshCullLayer = Layers.Cull_15;

        /// <summary>
        /// The layer smaller facility meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers SmallerFacilityMeshCullLayer = Layers.Cull_8;

        /// <summary>
        /// The layer the smallest ship meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers SmallestShipMeshCullLayer = Layers.Cull_1;

        /// <summary>
        /// The layer small ship meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers SmallShipMeshCullLayer = Layers.Cull_2;

        /// <summary>
        /// The layer medium ship meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers MediumShipMeshCullLayer = Layers.Cull_3;

        /// <summary>
        /// The layer Large ship meshes use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers LargeShipMeshCullLayer = Layers.Cull_4;

        /// <summary>
        /// The layer Cmd icons use to automatically have the camera cull them based on distance.
        /// <remarks>The camera does not auto cull based on distance if Layers.TransparentFX.</remarks>
        /// </summary>
        public const Layers CmdIconCullLayer = Layers.TransparentFX;

        /// <summary>
        /// The layer Star icons use to automatically have the camera cull them based on distance.
        /// <remarks>The camera does not auto cull based on distance if Layers.TransparentFX.</remarks>
        /// </summary>
        public const Layers StarIconCullLayer = Layers.TransparentFX;

        /// <summary>
        /// The layer Planet icons use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers PlanetIconCullLayer = Layers.Cull_1000;

        /// <summary>
        /// The layer Facility icons use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers FacilityIconCullLayer = Layers.Cull_200;

        /// <summary>
        /// The layer Ship icons use to automatically have the camera cull them based on distance.
        /// </summary>
        public const Layers ShipIconCullLayer = Layers.Cull_200;

        /// <summary>
        /// The smallest CullDistance &lt;&lt; 1 unit.
        /// TODO The distance where fighter and ordnance meshes disappear, replaced by an Icon.
        /// </summary>
        public const float CullDistance_Tiny = 0.5F;

        /// <summary>
        /// CullDistance of 1 unit.
        /// The distance where the Frigate class of ship meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_1 = 1F;

        /// <summary>
        /// CullDistance of 2 units.
        /// The distance where the Destroyer class of ship meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_2 = 2F;

        /// <summary>
        /// CullDistance of 3 units.
        /// The distance where the Cruiser class of ship meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_3 = 3F;

        /// <summary>
        /// CullDistance of 4 units. 
        /// The distance where larger ship classes (Dreadnought and Carrier) meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_4 = 4F;

        /// <summary>
        /// CullDistance of 8 units. 
        /// The distance where smaller facility meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_8 = 8F;

        /// <summary>
        /// CullDistance of 15 units.
        /// The distance where larger facility meshes disappear, replaced by the Element Icon.
        /// </summary>
        public const float CullDistance_15 = 15F;

        /// <summary>
        /// CullDistance of 200 units.
        /// The distance where moon, smaller planet meshes and Element Icons disappear. 
        /// Planets are replaced by the Planet Icon. Moon meshes are not replaced by icons.
        /// </summary>
        public const float CullDistance_200 = 200F;

        /// <summary>
        /// CullDistance of 400 units. 
        /// The distance where larger planet meshes disappear, replaced by the Planet Icon.
        /// </summary>
        public const float CullDistance_400 = 400F;

        /// <summary>
        /// CullDistance of 1000 units.
        /// The distance where planet Icons disappear.
        /// </summary>
        public const float CullDistance_1000 = 1000F;

        /// <summary>
        /// CullDistance of 3000 units. 
        /// Typically the distance where the star and orbital plane meshes disappear, replaced by the Star Icon.
        /// </summary>
        public const float CullDistance_3000 = 3000F;

        public const float __CullDistance_6000 = 6000F;
        public const float __CullDistance_10000 = 10000F;
        public const float __CullDistance_20000 = 20000F;

        #endregion

        /// <summary>
        /// The distance from the center of an obstacle to the side of the box of graphPoint vertices surrounding it. 
        /// The distance is a value between 0.0 and 1.0.  0 represents a box whose side is <c>ObstacleRadius</c> away 
        /// from the center of the obstacle, aka a box that bounds a sphere of size ObstacleRadius.
        /// 1.0 represents a box whose side is <c>SectorSideLength</c> from the center of the obstacle.
        /// </summary>
        public const float PathGraphPointPercentDistanceAroundObstacles = 0.1F;

        /// <summary>
        /// The total number of orbit slots in a System available for planets and
        /// settlements. The orbit slot for ships around the star is a ShipOrbitSlot, not
        /// one of these SystemOrbitSlots.
        /// </summary>
        public const int TotalOrbitSlotsPerSystem = 6;

        public const int MaxPlayers = 8;

        /// <summary>
        /// The maximum number of AI Players allowed in any game instance.
        /// </summary>
        public static int MaxAIPlayers = MaxPlayers - 1;

        /// <summary>
        /// The colors acceptable for use by players.
        /// </summary>
        public static readonly GameColor[] AllPlayerColors = new GameColor[] {
            GameColor.LightBlue,
            GameColor.Red,
            GameColor.Aqua,
            GameColor.Lime,
            GameColor.Magenta,
            GameColor.Yellow,
            GameColor.Silver,
            GameColor.Orange,
            GameColor.Green
        };

        private static Player _noPlayer;
        public static Player NoPlayer {
            get {
                _noPlayer = _noPlayer ?? new NoPlayer();    // lazy initialize to avoid creating before GameReferences populated with values
                return _noPlayer;
            }
        }

        private static Hero _noHero;
        public static Hero NoHero {
            get {
                _noHero = _noHero ?? new NoHero();
                return _noHero;
            }
        }

        private static ConstructionTask _noConstruction;
        public static ConstructionTask NoConstruction {
            get {
                _noConstruction = _noConstruction ?? new NoConstructionTask();
                return _noConstruction;
            }
        }

        private static ResearchTask _noResearch;
        public static ResearchTask NoResearch {
            get {
                _noResearch = _noResearch ?? new NoResearchTask();
                return _noResearch;
            }
        }


        public static readonly ResourcesYield NoResources = default(ResourcesYield);

        public static readonly CombatStrength NoCombatStrength = default(CombatStrength);


        public const GameColor SelectedColor = GameColor.Green;

        public const GameColor FocusedColor = GameColor.Yellow;

        public const GameColor GeneralHighlightColor = GameColor.White;

        public const GameColor SectorHighlightColor = GameColor.Yellow;

        public const GameColor HoveredHighlightColor = GameColor.Green;

        public const GameColor DisabledColor = GameColor.Gray;

        public const GameColor UnknownHealthColor = DisabledColor;

        public const GameColor ResearchQueuedColor = GameColor.Yellow;

        public const GameColor CurrentResearchColor = GameColor.Green;

        /// <summary>
        /// The name of the sprite (texture) used as a frame around an image.
        /// </summary>
        public const string ImageFrameFilename = "imageFrame";

        /// <summary>
        /// The name of the only current image in MyGuiAtlas.
        /// </summary>
        public const string AnImageFilename = "image";

        /// <summary>
        /// The name of the sprite (texture) used to represent an unknown image.
        /// </summary>
        public const string UnknownImageFilename = "Question1_16";

        /// <summary>
        /// The name of the sprite (texture) used to represent an empty image.
        /// </summary>
        public const string EmptyImageFilename = ImageFrameFilename;  // TODO

        /// <summary>
        /// The name of the UILabel GameObject used to show the title of a GuiElement.
        /// </summary>
        public const string TitleLabelName = "Title";

        /// <summary>
        /// The name of the UILabel GameObject used to show the "?" symbol.
        /// </summary>
        public const string UnknownLabelName = "Unknown";

        public const string FleetImageFilename = "flying_12";

        public const string StarbaseImageFilename = "ocean3_12";

        public const string SettlementImageFilename = "multiple25_12";


        public const string FoodIconFilename = "sprig2_12";
        public const string ProductionIconFilename = "hammer2_12";
        public const string IncomeIconFilename = "coins15_12";
        public const string ExpenseIconFilename = IncomeIconFilename;
        public const string NetIncomeIconFilename = IncomeIconFilename;
        public const string ScienceIconFilename = "microscope23_12";
        public const string CultureIconFilename = "Star1_12";

        public const string PopulationIconFilename = "Team1_12";
        public const string ReworkIconFilename = "Wrench1_12";    // shows on black space background

        public const string DefensiveStrengthIconFilename = "shield94_12";
        public const string OffensiveStrengthIconFilename = "lightning24_12";

        public const string BeamIconFilename = "Beam1_12";
        public const string ProjectileIconFilename = "Projectile1_12";
        public const string MissileIconFilename = "Missile1_12";
        public const string AssaultVehicleIconFilename = "flying_12";


    }
}


