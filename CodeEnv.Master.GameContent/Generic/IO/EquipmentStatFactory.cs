// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EquipmentStatFactory.cs
// Singleton. Factory that makes AEquipmentStat instances.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using UnityEngine;

    /// <summary>
    /// Singleton. Factory that makes AEquipmentStat instances.
    /// <remarks>TODO Acquire from XML values and make use of Player parameter.</remarks>
    /// <remarks>IMPROVE EquipmentCategory should be specific and include Range and FTL/STL designation to eliminate
    /// ambiguity of __MakeInstances delivering multiple stats for the same level and category.</remarks>
    /// </summary>
    public class EquipmentStatFactory : AGenericSingleton<EquipmentStatFactory> {

        private IDictionary<EquipmentCategory, IDictionary<Level, IList<AEquipmentStat>>> _statCache;

        private EquipmentStatFactory() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            CreateAllEquipStats();
        }

        public IList<AEquipmentStat> __MakeInstances(Player player, EquipmentCategory category, Level level) {
            return _statCache[category][level];
        }

        public AEquipmentStat MakeInstance(Player player, EquipmentCategory category, Level level) {
            return __MakeInstances(player, category, level).Single();
        }

        public PassiveCountermeasureStat MakeDefaultPassiveCmInstance() {
            return _statCache[EquipmentCategory.PassiveCountermeasure][Level.One].Single() as PassiveCountermeasureStat;
        }

        private void CreateAllEquipStats() {
            var allCategories = Enums<EquipmentCategory>.GetValues(excludeDefault: true);
            _statCache = new Dictionary<EquipmentCategory, IDictionary<Level, IList<AEquipmentStat>>>(allCategories.Count());

            var allLevels = Enums<Level>.GetValues(excludeDefault: true);
            foreach (var cat in allCategories) {
                var levelLookup = new Dictionary<Level, IList<AEquipmentStat>>(allLevels.Count());
                foreach (var level in allLevels) {
                    levelLookup.Add(level, CreateEquipStatsFor(cat, level));
                }
                _statCache.Add(cat, levelLookup);
            }
        }

        private IList<AEquipmentStat> CreateEquipStatsFor(EquipmentCategory equipCat, Level level) {
            IList<AEquipmentStat> stats = new List<AEquipmentStat>();
            IEnumerable<RangeCategory> allRangeCats;
            IEnumerable<ShipHullCategory> allShipHullCats;
            IEnumerable<FacilityHullCategory> allFacHullCats;
            switch (equipCat) {
                case EquipmentCategory.PassiveCountermeasure:
                    stats.Add(__CreatePassiveCmStat(level));
                    break;
                case EquipmentCategory.ActiveCountermeasure:
                    allRangeCats = Enums<RangeCategory>.GetValues(excludeDefault: true);
                    foreach (var rangeCat in allRangeCats) {
                        stats.Add(__CreateActiveCmStat(level, rangeCat));
                    }
                    break;
                case EquipmentCategory.Hull:
                    allShipHullCats = TempGameValues.ShipHullCategoriesInUse;
                    foreach (var shipHullCat in allShipHullCats) {
                        stats.Add(__CreateHullStat(level, shipHullCat));
                    }
                    allFacHullCats = TempGameValues.FacilityHullCategoriesInUse;
                    foreach (var facHullCat in allFacHullCats) {
                        stats.Add(__CreateHullStat(level, facHullCat));
                    }
                    break;
                case EquipmentCategory.Propulsion:
                    allShipHullCats = TempGameValues.ShipHullCategoriesInUse;
                    foreach (var shipHullCat in allShipHullCats) {
                        stats.Add(__CreateEngineStat(level, shipHullCat, isFtlEngine: true));
                        stats.Add(__CreateEngineStat(level, shipHullCat, isFtlEngine: false));
                    }
                    break;
                case EquipmentCategory.AssaultWeapon:
                    stats.Add(__CreateAssaultWeaponStat(level));
                    break;
                case EquipmentCategory.BeamWeapon:
                    stats.Add(__CreateBeamWeaponStat(level));
                    break;
                case EquipmentCategory.MissileWeapon:
                    stats.Add(__CreateMissileWeaponStat(level));
                    break;
                case EquipmentCategory.ProjectileWeapon:
                    stats.Add(__CreateProjectileWeaponStat(level));
                    break;
                case EquipmentCategory.Sensor:
                    stats.Add(__CreateSensorStat(level, RangeCategory.Short));
                    stats.Add(__CreateSensorStat(level, RangeCategory.Medium));
                    stats.Add(__CreateSensorStat(level, RangeCategory.Long));
                    break;
                case EquipmentCategory.ShieldGenerator:
                    stats.Add(__CreateShieldGeneratorStat(level));
                    break;
                case EquipmentCategory.FtlDampener:
                    stats.Add(__CreateFtlDampenerStat(level));
                    break;
                case EquipmentCategory.CommandModule:
                    stats.Add(__CreateFleetCmdModuleStat(level));
                    stats.Add(__CreateStarbaseCmdModuleStat(level));
                    stats.Add(__CreateSettlementCmdModuleStat(level));
                    break;
                case EquipmentCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(equipCat));
            }
            return stats;
        }

        // TEMP 
        #region XML Reader 

        private SensorStat __CreateSensorStat(Level level, RangeCategory range) {
            string name;
            bool isDamageable;
            switch (range) {
                case RangeCategory.Short:
                    name = "Proximity";
                    isDamageable = false;
                    break;
                case RangeCategory.Medium:
                    name = "Radar";
                    isDamageable = true;
                    break;
                case RangeCategory.Long:
                    name = "Array";
                    isDamageable = true;
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(range));
            }
            return new SensorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F, 1F, 1F,
                Constants.ZeroF, range, isDamageable);
        }

        private EngineStat __CreateEngineStat(Level level, ShipHullCategory hullCat, bool isFtlEngine) {
            float maxTurnRate = isFtlEngine ? UnityEngine.Random.Range(180F, 270F) : UnityEngine.Random.Range(TempGameValues.MinimumTurnRate, 180F);
            float engineSize = isFtlEngine ? 20F : 10F;
            float engineExpense = isFtlEngine ? 10 : 5;
            string engineName = isFtlEngine ? "FtlEngine" : "StlEngine";
            bool isDamageable = isFtlEngine ? true : false;

            float engineMass = hullCat.Mass() * 0.1F;
            float hitPts = __GetEngineHitPoints(hullCat, isFtlEngine);
            float fullPropulsionPower = __GetFullStlPropulsionPower(hullCat);   // FullFtlOpenSpaceSpeed ~ 30-40 units/hour, FullStlSystemSpeed ~ 1.2 - 1.6 units/hour
            if (isFtlEngine) {
                fullPropulsionPower *= TempGameValues.__StlToFtlPropulsionPowerFactor;
            }
            float constructionCost = __GetEngineConstructionCost(hullCat, isFtlEngine);

            return new EngineStat(engineName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, hullCat, fullPropulsionPower,
                maxTurnRate, engineSize, engineMass, hitPts, constructionCost, engineExpense, isDamageable, isFtlEngine);
        }

        private float __GetEngineHitPoints(ShipHullCategory hullCat, bool isFtlEngine) {
            float lowCost = 3F;
            float highCost = 5F;
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    break;
                case ShipHullCategory.Destroyer:
                    lowCost = 4F;
                    highCost = 6F;
                    break;
                case ShipHullCategory.Investigator:
                    lowCost = 5F;
                    highCost = 8F;
                    break;
                case ShipHullCategory.Support:
                    lowCost = 6F;
                    highCost = 9F;
                    break;
                case ShipHullCategory.Troop:
                    lowCost = 7F;
                    highCost = 10F;
                    break;
                case ShipHullCategory.Colonizer:
                    lowCost = 8F;
                    highCost = 12F;
                    break;
                case ShipHullCategory.Cruiser:
                    lowCost = 10F;
                    highCost = 14F;
                    break;
                case ShipHullCategory.Dreadnought:
                    lowCost = 14;
                    highCost = 18F;
                    break;
                case ShipHullCategory.Carrier:
                    lowCost = 14F;
                    highCost = 18F;
                    break;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float engineTypeMultiplier = isFtlEngine ? 1F : 1.5F;
            return UnityEngine.Random.Range(lowCost, highCost) * engineTypeMultiplier;
        }

        private float __GetEngineConstructionCost(ShipHullCategory hullCat, bool isFtlEngine) {
            float lowCost = 10F;
            float highCost = 30F;
            switch (hullCat) {
                case ShipHullCategory.Frigate:
                    lowCost = 10F;
                    highCost = 15F;
                    break;
                case ShipHullCategory.Destroyer:
                    lowCost = 12F;
                    highCost = 20F;
                    break;
                case ShipHullCategory.Investigator:
                    lowCost = 15F;
                    highCost = 25F;
                    break;
                case ShipHullCategory.Support:
                    lowCost = 15F;
                    highCost = 25F;
                    break;
                case ShipHullCategory.Troop:
                    lowCost = 20F;
                    highCost = 30F;
                    break;
                case ShipHullCategory.Colonizer:
                    lowCost = 25F;
                    highCost = 30F;
                    break;
                case ShipHullCategory.Cruiser:
                    lowCost = 25F;
                    highCost = 35F;
                    break;
                case ShipHullCategory.Dreadnought:
                    lowCost = 35;
                    highCost = 40F;
                    break;
                case ShipHullCategory.Carrier:
                    lowCost = 35F;
                    highCost = 40F;
                    break;
                case ShipHullCategory.Fighter:
                case ShipHullCategory.Scout:
                case ShipHullCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
            }
            float engineTypeMultiplier = isFtlEngine ? 1.5F : 1F;
            return UnityEngine.Random.Range(lowCost, highCost) * engineTypeMultiplier;
        }

        /// <summary>
        /// Gets the power generated by the STL engines when operating at Full capability.
        /// </summary>
        /// <param name="hull">The ship hull.</param>
        /// <returns></returns>
        private float __GetFullStlPropulsionPower(ShipHullCategory hullCat) {
            float fastestFullFtlSpeedTgt = TempGameValues.__TargetFtlOpenSpaceFullSpeed; // 40F
            float slowestFullFtlSpeedTgt = fastestFullFtlSpeedTgt * 0.75F;   // this way, the slowest Speed.OneThird speed >= Speed.Slow
            float fullFtlSpeedTgt = UnityEngine.Random.Range(slowestFullFtlSpeedTgt, fastestFullFtlSpeedTgt);
            float hullMass = hullCat.Mass(); // most but not all of the mass of the ship
            float hullOpenSpaceDrag = hullCat.Drag();

            float reqdFullFtlPower = GameUtility.CalculateReqdPropulsionPower(fullFtlSpeedTgt, hullMass, hullOpenSpaceDrag);
            return reqdFullFtlPower / TempGameValues.__StlToFtlPropulsionPowerFactor;
        }

        private ShipHullStat __CreateHullStat(Level level, ShipHullCategory hullCat) {
            float hullMass = hullCat.Mass();
            float drag = hullCat.Drag();
            float income = hullCat.Income();
            float expense = hullCat.Expense();
            float science = hullCat.Science();
            float culture = hullCat.Culture();
            float hitPts = hullCat.HitPoints();
            float constructionCost = hullCat.ConstructionCost();
            Vector3 hullDimensions = hullCat.Dimensions();
            return new ShipHullStat(hullCat, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F,
                hullMass, drag, 0F, hitPts, constructionCost, expense, new DamageStrength(2F, 2F, 2F), hullDimensions,
                science, culture, income);
        }

        private FacilityHullStat __CreateHullStat(Level level, FacilityHullCategory hullCat) {
            float food = hullCat.Food();
            float production = hullCat.Production();
            float income = hullCat.Income();
            float expense = hullCat.Expense();
            float science = hullCat.Science();
            float culture = hullCat.Culture();
            float hullMass = hullCat.Mass();
            float hitPts = hullCat.HitPoints();
            float constructionCost = hullCat.ConstructionCost();
            Vector3 hullDimensions = hullCat.Dimensions();
            return new FacilityHullStat(hullCat, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F,
                hullMass, 0F, hitPts, constructionCost, expense, new DamageStrength(2F, 2F, 2F), hullDimensions,
                science, culture, income, food, production);
        }

        private ShieldGeneratorStat __CreateShieldGeneratorStat(Level level) {
            RangeCategory rangeCat = RangeCategory.Short;
            string name = "Deflector Generator";
            float maxCharge = 20F;
            float trickleChargeRate = 1F;
            float reloadPeriod = 20F;
            DamageStrength damageMitigation = default(DamageStrength);  // none for now
            float hitPts = 1F;
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new ShieldGeneratorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
                 level, 0F, 1F, 0F, hitPts, constructionCost, Constants.ZeroF, rangeCat, maxCharge, trickleChargeRate, reloadPeriod, damageMitigation);
        }

        private FtlDampenerStat __CreateFtlDampenerStat(Level level) {
            float constructionCost = UnityEngine.Random.Range(1F, 5F);
            float hitPts = 1F;
            return new FtlDampenerStat("FtlDampener", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
                 level, 0F, 1F, 0F, hitPts, constructionCost, Constants.ZeroF, RangeCategory.Short);
        }

        private FleetCmdModuleStat __CreateFleetCmdModuleStat(Level level) {
            return new FleetCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F,
                10F, Constants.ZeroF, Constants.OneHundredPercent);
        }

        private StarbaseCmdModuleStat __CreateStarbaseCmdModuleStat(Level level) {
            int startingPop = 100;
            float startingApproval = Constants.OneHundredPercent;
            return new StarbaseCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F,
                10F, Constants.ZeroF, Constants.OneHundredPercent, startingPop, startingApproval);
        }

        private SettlementCmdModuleStat __CreateSettlementCmdModuleStat(Level level) {
            int startingPop = 100;
            float startingApproval = Constants.OneHundredPercent;
            return new SettlementCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F,
                10F, Constants.ZeroF, Constants.OneHundredPercent, startingPop, startingApproval);
        }

        private PassiveCountermeasureStat __CreatePassiveCmStat(Level level) {
            string name = string.Empty;
            DamageStrength damageMitigation;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
            float damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
            switch (damageMitigationCategory) {
                case DamageCategory.Thermal:    // TODO DamageCategory is too small a detail to have as interesting decision
                    name = "ThermalArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Structural:
                    name = "ProjectileArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Incursion:
                    name = "SecuritySystems";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.None:
                    name = "GeneralArmor";
                    damageMitigation = new DamageStrength(2F, 2F, 2F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
            }
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level,
                0F, 0F, 0F, 2F, constructionCost, Constants.ZeroF, damageMitigation);
        }

        private ActiveCountermeasureStat __CreateActiveCmStat(Level level, RangeCategory rangeCat) {
            string name = string.Empty;
            WDVStrength[] interceptStrengths;
            float interceptAccuracy;
            float reloadPeriod;    // TODO DamageCategory is too small a detail to have as interesting decision
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageMitigationValue = UnityEngine.Random.Range(1F, 2F);
            switch (rangeCat) {
                case RangeCategory.Short:
                    name = "CIWS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Projectile, 0.2F),
                        new WDVStrength(WDVCategory.Missile, 0.5F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 0.5F)
                    };
                    interceptAccuracy = 0.50F;
                    reloadPeriod = 0.2F;    //0.1
                    break;
                case RangeCategory.Medium:
                    name = "AvengerADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 3.0F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 3.0F)
                    };
                    interceptAccuracy = 0.80F;
                    reloadPeriod = 2.0F;
                    break;
                case RangeCategory.Long:
                    name = "PatriotADS";
                    interceptStrengths = new WDVStrength[] {
                        new WDVStrength(WDVCategory.Missile, 1.0F),
                        new WDVStrength(WDVCategory.AssaultVehicle, 1.0F)
                    };
                    interceptAccuracy = 0.70F;
                    reloadPeriod = 3.0F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new ActiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level,
                0F, 0F, 0F, 1F, constructionCost, Constants.ZeroF, rangeCat, interceptStrengths, interceptAccuracy, reloadPeriod, damageMitigation);
        }

        private MissileWeaponStat __CreateMissileWeaponStat(Level level) {
            WDVCategory deliveryVehicleCategory = WDVCategory.Missile;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);    // 0.04 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(15F, 18F);    // 10-15
            string name = "Torpedo Launcher";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = DamageCategory.Structural;
            float damageValue = UnityEngine.Random.Range(6F, 16F);  // 3-8
            float ordTurnRate = 700F;   // degrees per hour
            float ordCourseUpdateFreq = 0.4F; // course updates per hour    // 3.18.17 0.5 got turn not complete warnings
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(8F, 12F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 5F;
            float ordDrag = 0.02F;
            float hitPts = 2F;
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new MissileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F, hitPts,
                constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed,
                ordMass, ordDrag, ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy, isDamageable);
        }

        private AssaultWeaponStat __CreateAssaultWeaponStat(Level level) {
            WDVCategory deliveryVehicleCategory = WDVCategory.AssaultVehicle;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 1F);    // 0.07 - 1 degrees
            float reloadPeriod = UnityEngine.Random.Range(25F, 28F);
            string name = "Assault Launcher";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = DamageCategory.Incursion;
            float damageValue = UnityEngine.Random.Range(3F, 8F);
            float ordTurnRate = 270F;   // degrees per hour
            float ordCourseUpdateFreq = 0.4F; // course updates per hour
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(2F, 4F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 10F;
            float ordDrag = 0.03F;
            float hitPts = 2F;
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new AssaultWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F, hitPts,
                constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed, ordMass,
                ordDrag, ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy, isDamageable);
        }

        private ProjectileWeaponStat __CreateProjectileWeaponStat(Level level) {
            RangeCategory rangeCat = RangeCategory.Medium;
            float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.07 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(4F, 6F);  // 2-4
            string name = "KineticKill Projector";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = DamageCategory.Structural;
            float damageValue = UnityEngine.Random.Range(5F, 10F);   // 3-8
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVCategory deliveryVehicleCategory = WDVCategory.Projectile;
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;

            float ordMaxSpeed = UnityEngine.Random.Range(15F, 18F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
            float ordMass = 1F;
            float ordDrag = 0.01F;
            float hitPts = 2F;
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new ProjectileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F, hitPts,
                constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, ordMaxSpeed,
                ordMass, ordDrag, maxLaunchInaccuracy, isDamageable);
        }

        private BeamWeaponStat __CreateBeamWeaponStat(Level level) {
            RangeCategory rangeCat = RangeCategory.Short;
            float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.04 - 3 degrees
            float reloadPeriod = UnityEngine.Random.Range(6F, 10F); // 3-5
            float duration = UnityEngine.Random.Range(2F, 3F);  //1-2
            string name = "Phaser Projector";
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = DamageCategory.Thermal;
            float damageValue = UnityEngine.Random.Range(6F, 16F);   // 3-8
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVCategory deliveryVehicleCategory = WDVCategory.Beam;
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            bool isDamageable = true;
            float hitPts = 2F;
            float constructionCost = UnityEngine.Random.Range(1F, 5F);

            return new BeamWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, 0F, 0F, 0F, hitPts,
                constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential, duration,
                maxLaunchInaccuracy, isDamageable);
        }

        #endregion

        #region Debug

        public AEquipmentStat __GetRandomEquipmentStat() {
            var category = Enums<EquipmentCategory>.GetRandom(excludeDefault: true);
            var level = Enums<Level>.GetRandom(excludeDefault: true);
            return RandomExtended.Choice<AEquipmentStat>(_statCache[category][level]);
        }

        public string __GetRandomEquipmentStatName() {
            var category = Enums<EquipmentCategory>.GetRandom(excludeDefault: true);
            var level = Enums<Level>.GetRandom(excludeDefault: true);
            return RandomExtended.Choice<AEquipmentStat>(_statCache[category][level]).Name;
        }



        #endregion

    }
}


