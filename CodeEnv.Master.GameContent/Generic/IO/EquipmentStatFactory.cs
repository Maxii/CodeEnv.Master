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

    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using UnityEngine;

    /// <summary>
    /// Singleton. Factory that makes AEquipmentStat instances.
    /// <remarks>TODO Make use of Player parameter.</remarks>
    /// </summary>
    public class EquipmentStatFactory : AGenericSingleton<EquipmentStatFactory> {

        private IDictionary<EquipmentStatID, AEquipmentStat> _statCache;

        private EquipmentStatFactory() {
            Initialize();
        }

        protected override void Initialize() {
            // WARNING: Do not use Instance or _instance in here as this is still part of Constructor
            CreateAllEquipStats();
        }

        private void CreateAllEquipStats() {
            var allStats = EquipmentStatXmlReader.Instance.CreateAllStats();
            _statCache = allStats.ToDictionary(stat => stat.ID);
        }

        public AEquipmentStat MakeInstance(Player player, EquipmentCategory equipCat, Level level) {
            return MakeInstance(player, new EquipmentStatID(equipCat, level));
        }

        public AEquipmentStat MakeInstance(Player player, EquipmentStatID statID) {
            return _statCache[statID];
        }

        public PassiveCountermeasureStat GetCelestialPassiveCmInstance() {
            Level lowestPassiveCmLevel = GetLowestLevelFor(EquipmentCategory.PassiveCountermeasure);
            EquipmentStatID passiveCmID = new EquipmentStatID(EquipmentCategory.PassiveCountermeasure, lowestPassiveCmLevel);
            return _statCache[passiveCmID] as PassiveCountermeasureStat;
        }

        #region Debug

        [System.Obsolete]
        public AEquipmentStat __GetRandomEquipmentStat() {
            var category = Enums<EquipmentCategory>.GetRandom(excludeDefault: true);
            IEnumerable<EquipmentStatID> catIDs = _statCache.Keys.Where(id => id.Category == category);
            var randomCatID = RandomExtended.Choice(catIDs);
            return _statCache[randomCatID];
        }

        public Level GetLowestLevelFor(EquipmentCategory eCat) {
            return _statCache.Keys.Where(id => id.Category == eCat).Select(id => id.Level).Min();
        }

        public Level GetHighestLevelFor(EquipmentCategory eCat) {
            return _statCache.Keys.Where(id => id.Category == eCat).Select(id => id.Level).Max();
        }

        #endregion

        #region Nested Classes

        private class EquipmentStatXmlReader : AXmlReader<EquipmentStatXmlReader> {

            private string _eCategoryTagName = "EquipmentCategory";
            private string _eCategoryAttributeTagName = "EquipmentCategoryName";
            private string _levelTagName = "Level";
            private string _levelAttributeTagName = "LevelName";

            private string _eNameTagName = "Name";
            private string _eImageAtlasIDTagName = "ImageAtlasID";
            private string _eImageFilenameTagName = "ImageFilename";
            private string _eDescriptionTagName = "Description";
            private string _eSizeTagName = "Size";
            private string _eMassTagName = "Mass";
            private string _ePowerTagName = "Power";
            private string _eHitPtsTagName = "HitPoints";
            private string _eConstructCostTagName = "ConstructionCost";
            private string _eExpenseTagName = "Expense";
            private string _eDamageableTagName = "Damageable";

            private string _eMaxCmdEffectivenssTagName = "MaxCmdEffect";
            private string _eStartPopTagName = "StartPop";
            private string _eStartApprovalTagName = "StartApproval";

            private string _eAccuracyTagName = "Accuracy";
            private string _eReloadPeriodTagName = "Reload";
            private string _eMaxCharge = "MaxCharge";
            private string _eChargeRate = "ChargeRate";

            private string _eWdvStrengthContainerTagName = "WdvStrengths";   // holds WdvCategory and WdvCatValue values
            private string _eWdvCategoryTagName = "WdvCategory";
            private string _eWdvCatValueTagName = "WdvCatValue";

            private string _eDmgStrengthContainerTagName = "DmgStrengths";    // holds DamageCategory and DamageCatValue values
            private string _eDmgCategoryTagName = "DmgCategory";
            private string _eDmgCatValueTagName = "DmgCatValue";

            private string _eMaxSpeed = "MaxSpeed";
            private string _eDrag = "Drag";
            private string _eRangeCategory = "RangeCategory";
            private string _eOrdnanceMass = "OrdnanceMass";
            private string _eDuration = "Duration";

            private string _eMaxTurnRate = "MaxTurnRate";
            private string _eTurnRateConstraint = "TurnRateConstraint";

            private string _eFoodOutput = "Food";
            private string _eScienceOutput = "Science";
            private string _eProdnOutput = "Production";
            private string _eIncomeOutput = "Income";
            private string _eCultureOutput = "Culture";

            private string _eUpdateFreq = "UpdateFreq";
            private string _eHqPriority = "HqPriority";

            protected override string XmlFilename { get { return "EquipmentStatValues"; } }

            private EquipmentStatXmlReader() {
                Initialize();
            }

            internal IList<AEquipmentStat> CreateAllStats() {
                List<AEquipmentStat> allStats = new List<AEquipmentStat>();
                var allEquipCats = Enums<EquipmentCategory>.GetValues(excludeDefault: true);
                foreach (var cat in allEquipCats) {
                    var catStats = CreateStats(cat);
                    allStats.AddRange(catStats);
                }
                return allStats;
            }

            private IList<AEquipmentStat> CreateStats(EquipmentCategory equipCat) {

                IList<AEquipmentStat> stats = new List<AEquipmentStat>();
                var equipCatNodes = _xElement.Elements(_eCategoryTagName);
                foreach (var equipCatNode in equipCatNodes) {
                    var equipCatNodeAttribute = equipCatNode.Attribute(_eCategoryAttributeTagName);
                    string equipCatName = equipCatNodeAttribute.Value;
                    EquipmentCategory equipCatFound = Enums<EquipmentCategory>.Parse(equipCatName);

                    if (equipCat == equipCatFound) {
                        // found the right EquipmentCategory node
                        var levelNodes = equipCatNode.Elements(_levelTagName);
                        foreach (var levelNode in levelNodes) {
                            var levelNodeAttribute = levelNode.Attribute(_levelAttributeTagName);
                            string levelName = levelNodeAttribute.Value;
                            Level levelFound = Enums<Level>.Parse(levelName);

                            AEquipmentStat stat = null;
                            EquipmentStatID statID = new EquipmentStatID(equipCat, levelFound);
                            switch (equipCat) {
                                case EquipmentCategory.PassiveCountermeasure:
                                    stat = CreatePassiveCMStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.SRActiveCountermeasure:
                                case EquipmentCategory.MRActiveCountermeasure:
                                    stat = CreateActiveCmStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.StlPropulsion:
                                case EquipmentCategory.FtlPropulsion:
                                    stat = CreateEngineStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.BeamWeapon:
                                    stat = CreateBeamWeaponStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.ProjectileWeapon:
                                    stat = CreateProjectileWeaponStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.MissileWeapon:
                                    stat = CreateMissileWeaponStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.AssaultWeapon:
                                    stat = CreateAssaultWeaponStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.LRSensor:
                                case EquipmentCategory.MRSensor:
                                case EquipmentCategory.SRSensor:
                                    stat = CreateSensorStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.ShieldGenerator:
                                    stat = CreateShieldGenStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.FtlDampener:
                                    stat = CreateFtlDampenerStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.FleetCmdModule:
                                    stat = CreateFleetCmdModuleStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.StarbaseCmdModule:
                                    stat = CreateStarbaseCmdModuleStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.SettlementCmdModule:
                                    stat = CreateSettlementCmdModuleStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.FHullBarracks:
                                case EquipmentCategory.FHullCentralHub:
                                case EquipmentCategory.FHullColonyHab:
                                case EquipmentCategory.FHullDefense:
                                case EquipmentCategory.FHullEconomic:
                                case EquipmentCategory.FHullFactory:
                                case EquipmentCategory.FHullLaboratory:
                                    stat = CreateFacilityHullStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.SHullCarrier:
                                case EquipmentCategory.SHullColonizer:
                                case EquipmentCategory.SHullCruiser:
                                case EquipmentCategory.SHullDestroyer:
                                case EquipmentCategory.SHullDreadnought:
                                case EquipmentCategory.SHullFrigate:
                                case EquipmentCategory.SHullInvestigator:
                                case EquipmentCategory.SHullSupport:
                                case EquipmentCategory.SHullTroop:
                                    stat = CreateShipHullStat(statID, levelNode);
                                    break;
                                case EquipmentCategory.None:
                                default:
                                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(equipCat));
                            }

                            D.AssertNotNull(stat);
                            stats.Add(stat);
                        }
                        break;  // found the desired EquipmentCategory so discontinue search
                    }
                }
                if (stats.IsNullOrEmpty()) {
                    D.Error("{0} could not find Xml Node(s) for EquipmentCategory {1}.", DebugName, equipCat.GetValueName());
                }
                return stats;
            }

            private SensorStat CreateSensorStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);
                bool isDamageable = bool.Parse(levelNode.Element(_eDamageableTagName).Value);

                return new SensorStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr, hitPts, constructCost, expense, isDamageable);
            }

            private ActiveCountermeasureStat CreateActiveCmStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                WDVStrength[] interceptStrengths = GetWdvStrengths(levelNode);
                float interceptAccuracy = float.Parse(levelNode.Element(_eAccuracyTagName).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgMitigation = GetDmgStrengths(levelNode);

                return new ActiveCountermeasureStat(name, imageAtlasID, imageFilename, description, statID, size, mass,
                    pwrReqd, hitPts, constructionCost, expense, interceptStrengths, interceptAccuracy, reloadPeriod,
                    dmgMitigation);
            }

            private EngineStat CreateEngineStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);
                bool isDamageable = bool.Parse(levelNode.Element(_eDamageableTagName).Value);

                float maxTurnRate = float.Parse(levelNode.Element(_eMaxTurnRate).Value);
                float maxSpeed = float.Parse(levelNode.Element(_eMaxSpeed).Value);

                return new EngineStat(name, imageAtlasID, imageFilename, description, statID, size, mass, hitPts,
                    constructCost, expense, maxTurnRate, maxSpeed, isDamageable);
            }

            private FtlDampenerStat CreateFtlDampenerStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);
                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);

                return new FtlDampenerStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr, hitPts,
                    constructCost, expense, rangeCat);
            }

            private PassiveCountermeasureStat CreatePassiveCMStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);
                DamageStrength dmgMitigation = GetDmgStrengths(levelNode);

                return new PassiveCountermeasureStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr,
                    hitPts, constructCost, expense, dmgMitigation);
            }

            private ShieldGeneratorStat CreateShieldGenStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);
                float maxCharge = float.Parse(levelNode.Element(_eMaxCharge).Value);
                float chargeRate = float.Parse(levelNode.Element(_eChargeRate).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgMitigation = GetDmgStrengths(levelNode);
                return new ShieldGeneratorStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr,
                    hitPts, constructCost, expense, rangeCat, maxCharge, chargeRate, reloadPeriod, dmgMitigation);
            }

            private StarbaseCmdModuleStat CreateStarbaseCmdModuleStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                float maxEffectiveness = float.Parse(levelNode.Element(_eMaxCmdEffectivenssTagName).Value);
                int startingPop = int.Parse(levelNode.Element(_eStartPopTagName).Value);
                float startingApproval = float.Parse(levelNode.Element(_eStartApprovalTagName).Value);

                return new StarbaseCmdModuleStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr,
                    hitPts, constructCost, expense, maxEffectiveness, startingPop, startingApproval);
            }

            private SettlementCmdModuleStat CreateSettlementCmdModuleStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                float maxEffectiveness = float.Parse(levelNode.Element(_eMaxCmdEffectivenssTagName).Value);
                int startingPop = int.Parse(levelNode.Element(_eStartPopTagName).Value);
                float startingApproval = float.Parse(levelNode.Element(_eStartApprovalTagName).Value);

                return new SettlementCmdModuleStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr,
                    hitPts, constructCost, expense, maxEffectiveness, startingPop, startingApproval);
            }

            private FleetCmdModuleStat CreateFleetCmdModuleStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwr = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                float maxEffectiveness = float.Parse(levelNode.Element(_eMaxCmdEffectivenssTagName).Value);

                return new FleetCmdModuleStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwr,
                    hitPts, constructCost, expense, maxEffectiveness);
            }

            private BeamWeaponStat CreateBeamWeaponStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);

                WDVStrength ordDeliveryVehicleStrength = GetWdvStrengths(levelNode).Single();
                float aimInaccuracy = float.Parse(levelNode.Element(_eAccuracyTagName).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgPotential = GetDmgStrengths(levelNode);
                float duration = float.Parse(levelNode.Element(_eDuration).Value);

                return new BeamWeaponStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd,
                    hitPts, constructionCost, expense, rangeCat, ordDeliveryVehicleStrength, reloadPeriod, dmgPotential, duration, aimInaccuracy);
            }

            private ProjectileWeaponStat CreateProjectileWeaponStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);

                WDVStrength ordDeliveryVehicleStrength = GetWdvStrengths(levelNode).Single();
                float aimInaccuracy = float.Parse(levelNode.Element(_eAccuracyTagName).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgPotential = GetDmgStrengths(levelNode);
                float ordMaxSpeed = float.Parse(levelNode.Element(_eMaxSpeed).Value);
                float ordMass = float.Parse(levelNode.Element(_eOrdnanceMass).Value);
                float ordDrag = float.Parse(levelNode.Element(_eDrag).Value);

                return new ProjectileWeaponStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd,
                    hitPts, constructionCost, expense, rangeCat, ordDeliveryVehicleStrength, reloadPeriod, dmgPotential, ordMaxSpeed,
                    ordMass, ordDrag, aimInaccuracy);
            }

            private MissileWeaponStat CreateMissileWeaponStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);

                WDVStrength ordDeliveryVehicleStrength = GetWdvStrengths(levelNode).Single();
                float ordSteeringInaccuracy = float.Parse(levelNode.Element(_eAccuracyTagName).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgPotential = GetDmgStrengths(levelNode);
                float ordMaxSpeed = float.Parse(levelNode.Element(_eMaxSpeed).Value);
                float ordMass = float.Parse(levelNode.Element(_eOrdnanceMass).Value);
                float ordDrag = float.Parse(levelNode.Element(_eDrag).Value);

                float ordMaxTurnRate = float.Parse(levelNode.Element(_eMaxTurnRate).Value);
                float ordCourseUpdateFreq = float.Parse(levelNode.Element(_eUpdateFreq).Value);

                return new MissileWeaponStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd,
                    hitPts, constructionCost, expense, rangeCat, ordDeliveryVehicleStrength, reloadPeriod, dmgPotential, ordMaxSpeed,
                    ordMass, ordDrag, ordMaxTurnRate, ordCourseUpdateFreq, ordSteeringInaccuracy);
            }

            private AssaultWeaponStat CreateAssaultWeaponStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                RangeCategory rangeCat = Enums<RangeCategory>.Parse(levelNode.Element(_eRangeCategory).Value);

                WDVStrength ordDeliveryVehicleStrength = GetWdvStrengths(levelNode).Single();
                float ordSteeringInaccuracy = float.Parse(levelNode.Element(_eAccuracyTagName).Value);
                float reloadPeriod = float.Parse(levelNode.Element(_eReloadPeriodTagName).Value);

                DamageStrength dmgPotential = GetDmgStrengths(levelNode);
                float ordMaxSpeed = float.Parse(levelNode.Element(_eMaxSpeed).Value);
                float ordMass = float.Parse(levelNode.Element(_eOrdnanceMass).Value);
                float ordDrag = float.Parse(levelNode.Element(_eDrag).Value);

                float ordMaxTurnRate = float.Parse(levelNode.Element(_eMaxTurnRate).Value);
                float ordCourseUpdateFreq = float.Parse(levelNode.Element(_eUpdateFreq).Value);

                return new AssaultWeaponStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd,
                    hitPts, constructionCost, expense, rangeCat, ordDeliveryVehicleStrength, reloadPeriod, dmgPotential, ordMaxSpeed,
                    ordMass, ordDrag, ordMaxTurnRate, ordCourseUpdateFreq, ordSteeringInaccuracy);
            }

            private FacilityHullStat CreateFacilityHullStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                DamageStrength dmgMitigation = GetDmgStrengths(levelNode);
                Priority hqPriority = Enums<Priority>.Parse(levelNode.Element(_eHqPriority).Value);

                float food = float.Parse(levelNode.Element(_eFoodOutput).Value);
                float science = float.Parse(levelNode.Element(_eScienceOutput).Value);
                float income = float.Parse(levelNode.Element(_eIncomeOutput).Value);
                float culture = float.Parse(levelNode.Element(_eCultureOutput).Value);
                float prodn = float.Parse(levelNode.Element(_eProdnOutput).Value);

                return new FacilityHullStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd, hitPts, constructionCost,
                    expense, dmgMitigation, hqPriority, science, culture, income, food, prodn);
            }

            private ShipHullStat CreateShipHullStat(EquipmentStatID statID, XElement levelNode) {
                string name = levelNode.Element(_eNameTagName).Value;
                AtlasID imageAtlasID = Enums<AtlasID>.Parse(levelNode.Element(_eImageAtlasIDTagName).Value);
                string imageFilename = levelNode.Element(_eImageFilenameTagName).Value;
                string description = levelNode.Element(_eDescriptionTagName).Value;
                float size = float.Parse(levelNode.Element(_eSizeTagName).Value);
                float mass = float.Parse(levelNode.Element(_eMassTagName).Value);
                float pwrReqd = float.Parse(levelNode.Element(_ePowerTagName).Value);
                float hitPts = float.Parse(levelNode.Element(_eHitPtsTagName).Value);
                float constructionCost = float.Parse(levelNode.Element(_eConstructCostTagName).Value);
                float expense = float.Parse(levelNode.Element(_eExpenseTagName).Value);

                DamageStrength dmgMitigation = GetDmgStrengths(levelNode);
                Priority hqPriority = Enums<Priority>.Parse(levelNode.Element(_eHqPriority).Value);
                float turnRateConstraint = float.Parse(levelNode.Element(_eTurnRateConstraint).Value);

                float science = float.Parse(levelNode.Element(_eScienceOutput).Value);
                float income = float.Parse(levelNode.Element(_eIncomeOutput).Value);
                float culture = float.Parse(levelNode.Element(_eCultureOutput).Value);

                return new ShipHullStat(name, imageAtlasID, imageFilename, description, statID, size, mass, pwrReqd, hitPts, constructionCost,
                    expense, dmgMitigation, hqPriority, turnRateConstraint, science, culture, income);
            }

            private WDVStrength[] GetWdvStrengths(XElement levelNode) {
                IList<WDVStrength> strengths = new List<WDVStrength>();
                var wdvStrengthNodes = levelNode.Elements(_eWdvStrengthContainerTagName);
                foreach (var wdvStrengthNode in wdvStrengthNodes) {
                    WDVCategory wdvCat = Enums<WDVCategory>.Parse(wdvStrengthNode.Element(_eWdvCategoryTagName).Value);
                    float wdvCatValue = float.Parse(wdvStrengthNode.Element(_eWdvCatValueTagName).Value);
                    strengths.Add(new WDVStrength(wdvCat, wdvCatValue));
                }
                return strengths.ToArray();
            }

            private DamageStrength GetDmgStrengths(XElement levelNode) {
                DamageStrength dmgStrength = default(DamageStrength);
                var dmgStrengthNodes = levelNode.Elements(_eDmgStrengthContainerTagName);
                foreach (var dmgStrengthNode in dmgStrengthNodes) {
                    DamageCategory dmgCat = Enums<DamageCategory>.Parse(dmgStrengthNode.Element(_eDmgCategoryTagName).Value);
                    float dmgCatValue = float.Parse(dmgStrengthNode.Element(_eDmgCatValueTagName).Value);
                    dmgStrength += new DamageStrength(dmgCat, dmgCatValue);
                }
                return dmgStrength;
            }

        }

        #endregion

        #region HullCategory-specific EngineStats

        //[Obsolete]
        //private EngineStat __CreateEngineStat(Level level, ShipHullCategory hullCat, bool isFtlEngine) {
        //    EquipmentCategory equipCat = isFtlEngine ? EquipmentCategory.FtlPropulsion : EquipmentCategory.StlPropulsion;
        //    float maxTurnRate = isFtlEngine ? UnityEngine.Random.Range(180F, 270F) : UnityEngine.Random.Range(TempGameValues.MinimumTurnRate, 180F);
        //    float engineSize = isFtlEngine ? 20F : 10F;
        //    float engineExpense = isFtlEngine ? 10 : 5;
        //    string engineName = isFtlEngine ? "FtlEngine" : "StlEngine";

        //    float engineMass = isFtlEngine ? 10F : 5F;  // Hull mass: 50 - 500
        //    float hitPts = isFtlEngine ? 6F : 10F;
        //    float fullPropulsionPower = __GetFullStlPropulsionPower(hullCat);   // FullFtlOpenSpaceSpeed ~ 30-40 units/hour, FullStlSystemSpeed ~ 1.2 - 1.6 units/hour
        //    if (isFtlEngine) {
        //        fullPropulsionPower *= TempGameValues.__StlToFtlPropulsionPowerFactor;
        //    }
        //    float constructionCost = __GetEngineConstructionCost(hullCat, isFtlEngine);

        //    return new EngineStat(engineName, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", level, fullPropulsionPower,
        //        maxTurnRate, engineSize, engineMass, hitPts, constructionCost, engineExpense, equipCat);
        //}

        //[Obsolete]
        //private float __GetEngineHitPoints(ShipHullCategory hullCat, bool isFtlEngine) {
        //    float lowCost = 3F;
        //    float highCost = 5F;
        //    switch (hullCat) {
        //        case ShipHullCategory.Frigate:
        //            break;
        //        case ShipHullCategory.Destroyer:
        //            lowCost = 4F;
        //            highCost = 6F;
        //            break;
        //        case ShipHullCategory.Investigator:
        //            lowCost = 5F;
        //            highCost = 8F;
        //            break;
        //        case ShipHullCategory.Support:
        //            lowCost = 6F;
        //            highCost = 9F;
        //            break;
        //        case ShipHullCategory.Troop:
        //            lowCost = 7F;
        //            highCost = 10F;
        //            break;
        //        case ShipHullCategory.Colonizer:
        //            lowCost = 8F;
        //            highCost = 12F;
        //            break;
        //        case ShipHullCategory.Cruiser:
        //            lowCost = 10F;
        //            highCost = 14F;
        //            break;
        //        case ShipHullCategory.Dreadnought:
        //            lowCost = 14;
        //            highCost = 18F;
        //            break;
        //        case ShipHullCategory.Carrier:
        //            lowCost = 14F;
        //            highCost = 18F;
        //            break;
        //        case ShipHullCategory.Fighter:
        //        case ShipHullCategory.Scout:
        //        case ShipHullCategory.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        //    }
        //    float engineTypeMultiplier = isFtlEngine ? 1F : 1.5F;
        //    return UnityEngine.Random.Range(lowCost, highCost) * engineTypeMultiplier;
        //}

        //[Obsolete]
        //private float __GetEngineConstructionCost(ShipHullCategory hullCat, bool isFtlEngine) {
        //    float lowCost = 10F;
        //    float highCost = 30F;
        //    switch (hullCat) {
        //        case ShipHullCategory.Frigate:
        //            lowCost = 10F;
        //            highCost = 15F;
        //            break;
        //        case ShipHullCategory.Destroyer:
        //            lowCost = 12F;
        //            highCost = 20F;
        //            break;
        //        case ShipHullCategory.Investigator:
        //            lowCost = 15F;
        //            highCost = 25F;
        //            break;
        //        case ShipHullCategory.Support:
        //            lowCost = 15F;
        //            highCost = 25F;
        //            break;
        //        case ShipHullCategory.Troop:
        //            lowCost = 20F;
        //            highCost = 30F;
        //            break;
        //        case ShipHullCategory.Colonizer:
        //            lowCost = 25F;
        //            highCost = 30F;
        //            break;
        //        case ShipHullCategory.Cruiser:
        //            lowCost = 25F;
        //            highCost = 35F;
        //            break;
        //        case ShipHullCategory.Dreadnought:
        //            lowCost = 35;
        //            highCost = 40F;
        //            break;
        //        case ShipHullCategory.Carrier:
        //            lowCost = 35F;
        //            highCost = 40F;
        //            break;
        //        case ShipHullCategory.Fighter:
        //        case ShipHullCategory.Scout:
        //        case ShipHullCategory.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(hullCat));
        //    }
        //    float engineTypeMultiplier = isFtlEngine ? 1.5F : 1F;
        //    return UnityEngine.Random.Range(lowCost, highCost) * engineTypeMultiplier;
        //}

        //[Obsolete]
        //private float __GetFullStlPropulsionPower(ShipHullCategory hullCat) {
        //    float fastestFullFtlSpeedTgt = TempGameValues.__TargetFtlOpenSpaceFullSpeed; // 40F
        //    float slowestFullFtlSpeedTgt = fastestFullFtlSpeedTgt * 0.75F;   // this way, the slowest Speed.OneThird speed >= Speed.Slow
        //    float fullFtlSpeedTgt = UnityEngine.Random.Range(slowestFullFtlSpeedTgt, fastestFullFtlSpeedTgt);
        //    float hullMass = hullCat.Mass(); // most but not all of the mass of the ship
        //    float hullOpenSpaceDrag = hullCat.Drag();

        //    float reqdFullFtlPower = GameUtility.CalculateReqdPropulsionPower(fullFtlSpeedTgt, hullMass, hullOpenSpaceDrag);
        //    return reqdFullFtlPower / TempGameValues.__StlToFtlPropulsionPowerFactor;
        //}

        #endregion

        #region Deprecated Temporary XML Reader 

        //[Obsolete]
        //private SensorStat __CreateCmdMRSensorStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.MRSensor, level);
        //    return CreateSensorStat(id, "CmdMRSensor", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description",
        //        0F, 0F, 0F, 1F, 1F, 0F, true);
        //}

        //[Obsolete]
        //private SensorStat __CreateCmdLRSensorStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.LRSensor, level);
        //    return CreateSensorStat(id, "CmdLRSensor", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description",
        //        0F, 0F, 0F, 1F, 1F, 0F, true);
        //}

        //[Obsolete]
        //private SensorStat __CreateElementSRSensorStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.SRSensor, level);
        //    return CreateSensorStat(id, "SRSensor", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description", 0F, 0F, 0F,
        //        1F, 1F, 0F, false);
        //}

        //[Obsolete]
        //private SensorStat CreateSensorStat(EquipmentStatID id, string name, AtlasID atlasID, string spriteName, string description,
        //    float size, float mass, float pwr, float hitPts, float constructionCost, float expense, bool isDamageable) {
        //    return new SensorStat(name, atlasID, spriteName, description, id, size, mass, pwr, hitPts, constructionCost, expense, isDamageable);
        //}

        //[Obsolete]
        //private EngineStat __CreateEngineStat(EquipmentCategory engineCat, Level level) {
        //    EquipmentStatID id = new EquipmentStatID(engineCat, level);
        //    bool isFtlEngine = engineCat == EquipmentCategory.FtlPropulsion;
        //    float maxTurnRate = isFtlEngine ? UnityEngine.Random.Range(180F, 270F) : UnityEngine.Random.Range(TempGameValues.MinimumTurnRate, 180F);
        //    float size = isFtlEngine ? 20F : 10F;
        //    float expense = isFtlEngine ? 10 : 5;
        //    string name = isFtlEngine ? "FtlEngine" : "StlEngine";

        //    float mass = isFtlEngine ? 10F : 5F;  // Hull mass: 50 - 500
        //    float hitPts = isFtlEngine ? 6F : 10F;
        //    float maxSpeed = __GetMaxAttainableSpeed(level, isFtlEngine);
        //    float constructionCost = UnityEngine.Random.Range(10F, 30F) * (isFtlEngine ? 1.5F : 1F);
        //    return CreateEngineStat(id, name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description", maxTurnRate, size, mass, hitPts,
        //        constructionCost, expense, maxSpeed);
        //}

        //[Obsolete]
        //private EngineStat CreateEngineStat(EquipmentStatID id, string name, AtlasID atlasID, string spriteName, string description,
        //    float maxTurnrate, float size, float mass, float hitPts, float constructionCost, float expense, float maxSpeed) {
        //    bool isDamageable = id.Category == EquipmentCategory.FtlPropulsion;
        //    return new EngineStat(name, atlasID, spriteName, description, id, size, mass, hitPts, constructionCost, expense, maxTurnrate, maxSpeed, isDamageable);
        //}

        ///// <summary>
        ///// Gets the maximum attainable speed in Topography.OpenSpace.
        ///// </summary>
        ///// <param name="level">The level.</param>
        ///// <param name="isFtlEngine">if set to <c>true</c> [is FTL engine].</param>
        ///// <returns></returns>
        ///// <exception cref="System.NotImplementedException"></exception>
        //[Obsolete]
        //private float __GetMaxAttainableSpeed(Level level, bool isFtlEngine) {
        //    float maxAttainableSpeed;
        //    switch (level) {
        //        case Level.One:
        //            maxAttainableSpeed = 16F;
        //            break;
        //        case Level.Two:
        //            maxAttainableSpeed = 20F;
        //            break;
        //        case Level.Three:
        //            maxAttainableSpeed = 25F;
        //            break;
        //        case Level.Four:
        //            maxAttainableSpeed = 32F;
        //            break;
        //        case Level.Five:
        //            maxAttainableSpeed = TempGameValues.__TargetFtlOpenSpaceFullSpeed;
        //            break;
        //        case Level.None:
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(level));
        //    }
        //    if (!isFtlEngine) {
        //        maxAttainableSpeed /= TempGameValues.StlToFtlSpeedFactor;
        //    }
        //    return maxAttainableSpeed;
        //}

        //[Obsolete]
        //private ShieldGeneratorStat __CreateShieldGeneratorStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.ShieldGenerator, level);
        //    RangeCategory rangeCat = RangeCategory.Short;
        //    string name = "Deflector Generator";
        //    float maxCharge = 20F;
        //    float trickleChargeRate = 1F;
        //    float reloadPeriod = 20F;
        //    DamageStrength damageMitigation = default(DamageStrength);  // none for now
        //    float hitPts = 1F;
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return CreateShieldGenStat(id, name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description", 0F, 0F, 0F,
        //        hitPts, constructionCost, 0F, rangeCat, maxCharge, trickleChargeRate, reloadPeriod, damageMitigation);
        //}

        //[Obsolete]
        //private ShieldGeneratorStat CreateShieldGenStat(EquipmentStatID id, string name, AtlasID atlasID, string spriteName,
        //    string description, float size, float mass, float pwr, float hitPts, float constructionCost, float expense, RangeCategory rangeCat,
        //    float maxCharge, float trickleChargeRate, float reloadPeriod, DamageStrength dmgMitigation) {
        //    return new ShieldGeneratorStat(name, atlasID, spriteName, description, id, size, mass, pwr, hitPts, constructionCost, expense, rangeCat,
        //        maxCharge, trickleChargeRate, reloadPeriod, dmgMitigation);
        //}

        //[Obsolete]
        //private FtlDampenerStat __CreateFtlDampenerStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.FtlDampener, level);
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);
        //    float hitPts = 1F;
        //    return CreateFtlDampenerStat(id, "FtlDampener", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
        //         0F, 1F, 0F, hitPts, constructionCost, 0F, RangeCategory.Short);
        //}

        //[Obsolete]
        //private FtlDampenerStat CreateFtlDampenerStat(EquipmentStatID id, string name, AtlasID atlasID, string spriteName,
        //    string description, float size, float mass, float pwr, float hitPts, float constructionCost, float expense, RangeCategory rangeCat) {
        //    return new FtlDampenerStat(name, atlasID, spriteName, description, id, size, mass, pwr, hitPts, constructionCost, expense, rangeCat);
        //}

        //[Obsolete]
        //private FleetCmdModuleStat __CreateFleetCmdModuleStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.FleetCmdModule, level);
        //    return new FleetCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id,
        //        0F, 0F, 0F, 0F, 10F, Constants.ZeroF, Constants.OneHundredPercent);
        //}

        //[Obsolete]
        //private StarbaseCmdModuleStat __CreateStarbaseCmdModuleStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.StarbaseCmdModule, level);
        //    int startingPop = 100;
        //    float startingApproval = Constants.OneHundredPercent;
        //    return new StarbaseCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id,
        //        0F, 0F, 0F, 0F, 10F, Constants.ZeroF, Constants.OneHundredPercent, startingPop, startingApproval);
        //}

        //[Obsolete]
        //private SettlementCmdModuleStat __CreateSettlementCmdModuleStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.SettlementCmdModule, level);
        //    int startingPop = 100;
        //    float startingApproval = Constants.OneHundredPercent;
        //    return new SettlementCmdModuleStat("CmdModule", AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
        //        id, 0F, 0F, 0F, 0F, 10F, Constants.ZeroF, Constants.OneHundredPercent, startingPop, startingApproval);
        //}

        //[Obsolete]
        //private PassiveCountermeasureStat __CreatePassiveCmStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.PassiveCountermeasure, level);
        //    string name = string.Empty;
        //    DamageStrength dmgMitigation;
        //    var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
        //    float damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
        //    switch (damageMitigationCategory) {
        //        case DamageCategory.Thermal:    // TODO DamageCategory is too small a detail to have as interesting decision
        //            name = "ThermalArmor";
        //            dmgMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
        //            break;
        //        case DamageCategory.Structural:
        //            name = "ProjectileArmor";
        //            dmgMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
        //            break;
        //        case DamageCategory.Incursion:
        //            name = "SecuritySystems";
        //            dmgMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
        //            break;
        //        case DamageCategory.None:
        //            name = "GeneralArmor";
        //            dmgMitigation = new DamageStrength(DamageCategory.Thermal, 2F) + new DamageStrength(DamageCategory.Structural, 2F)
        //                                                                           + new DamageStrength(DamageCategory.Incursion, 2F);
        //            break;
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
        //    }
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return CreatePassiveCMStat(id, name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F,
        //        2F, constructionCost, 0F, dmgMitigation);
        //}

        //[Obsolete]
        //private PassiveCountermeasureStat CreatePassiveCMStat(EquipmentStatID id, string name, AtlasID atlasID, string spriteName,
        //    string description, float size, float mass, float pwr, float hitPts, float constructionCost, float expense,
        //    DamageStrength dmgMitigation) {
        //    return new PassiveCountermeasureStat(name, atlasID, spriteName, description, id, size, mass, pwr, hitPts, constructionCost,
        //        expense, dmgMitigation);
        //}

        //[Obsolete]
        //private ActiveCountermeasureStat __CreateSRActiveCmStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.SRActiveCountermeasure, level);
        //    string name = "CIWS";
        //    WDVStrength[] interceptStrengths = new WDVStrength[] {
        //                new WDVStrength(WDVCategory.Projectile, 0.2F),
        //                new WDVStrength(WDVCategory.Missile, 0.5F),
        //                new WDVStrength(WDVCategory.AssaultVehicle, 0.5F)
        //            };
        //    float interceptAccuracy = 0.50F;
        //    float reloadPeriod = 0.2F;    // TODO DamageCategory is too small a detail to have as interesting decision
        //    var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
        //    float damageMitigationValue = UnityEngine.Random.Range(1F, 2F);
        //    DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return CreateActiveCMStat(id, name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F,
        //        1F, constructionCost, 0F, interceptStrengths, interceptAccuracy, reloadPeriod, damageMitigation);
        //}

        //[Obsolete]
        //private ActiveCountermeasureStat __CreateMRActiveCmStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.MRActiveCountermeasure, level);
        //    string name = "ADS";
        //    WDVStrength[] interceptStrengths = new WDVStrength[] {
        //                new WDVStrength(WDVCategory.Missile, 3.0F),
        //                new WDVStrength(WDVCategory.AssaultVehicle, 3.0F)
        //            };
        //    float interceptAccuracy = 0.80F;
        //    float reloadPeriod = 2F;    // TODO DamageCategory is too small a detail to have as interesting decision
        //    var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
        //    float damageMitigationValue = UnityEngine.Random.Range(1F, 2F);
        //    DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return CreateActiveCMStat(id, name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...",
        //        0F, 0F, 0F, 1F, constructionCost, 0F, interceptStrengths, interceptAccuracy, reloadPeriod, damageMitigation);
        //}

        //[Obsolete]
        //private ActiveCountermeasureStat CreateActiveCMStat(EquipmentStatID id, string name, AtlasID atlasID,
        //    string spriteName, string description, float size, float mass, float pwr, float hitPts, float constructionCost,
        //    float expense, WDVStrength[] interceptStrengths, float interceptAccuracy, float reloadPeriod,
        //    DamageStrength dmgMitigation) {
        //    return new ActiveCountermeasureStat(name, atlasID, spriteName, description, id, size, mass, pwr, hitPts, constructionCost, expense, interceptStrengths,
        //        interceptAccuracy, reloadPeriod, dmgMitigation);
        //}

        //[Obsolete]
        //private MissileWeaponStat __CreateMissileWeaponStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.MissileWeapon, level);

        //    RangeCategory rangeCat = RangeCategory.Long;
        //    float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);    // 0.04 - 3 degrees
        //    float reloadPeriod = UnityEngine.Random.Range(15F, 18F);    // 10-15
        //    string name = "Torpedo Launcher";
        //    float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
        //    var damageCategory = DamageCategory.Structural;
        //    float damageValue = UnityEngine.Random.Range(6F, 16F);  // 3-8
        //    float ordTurnRate = 700F;   // degrees per hour
        //    float ordCourseUpdateFreq = 0.4F; // course updates per hour    // 3.18.17 0.5 got turn not complete warnings
        //    DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
        //    WDVStrength deliveryVehicleStrength = new WDVStrength(WDVCategory.Missile, deliveryStrengthValue);

        //    float ordMaxSpeed = UnityEngine.Random.Range(8F, 12F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
        //    float ordMass = 5F;
        //    float ordDrag = 0.02F;
        //    float hitPts = 2F;
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return new MissileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id, 0F, 0F,
        //        0F, hitPts, constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod,
        //        damagePotential, ordMaxSpeed, ordMass, ordDrag, ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy);
        //}

        //[Obsolete]
        //private AssaultWeaponStat __CreateAssaultWeaponStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.AssaultWeapon, level);

        //    RangeCategory rangeCat = RangeCategory.Long; ;
        //    float maxSteeringInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 1F);    // 0.07 - 1 degrees
        //    float reloadPeriod = UnityEngine.Random.Range(25F, 28F);
        //    string name = "Assault Launcher";
        //    float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
        //    var damageCategory = DamageCategory.Incursion;
        //    float damageValue = UnityEngine.Random.Range(3F, 8F);
        //    float ordTurnRate = 270F;   // degrees per hour
        //    float ordCourseUpdateFreq = 0.4F; // course updates per hour
        //    DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
        //    WDVStrength deliveryVehicleStrength = new WDVStrength(WDVCategory.AssaultVehicle, deliveryStrengthValue);

        //    float ordMaxSpeed = UnityEngine.Random.Range(2F, 4F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
        //    float ordMass = 10F;
        //    float ordDrag = 0.03F;
        //    float hitPts = 2F;
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return new AssaultWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id, 0F, 0F,
        //        0F, hitPts, constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod,
        //        damagePotential, ordMaxSpeed, ordMass, ordDrag, ordTurnRate, ordCourseUpdateFreq, maxSteeringInaccuracy);
        //}

        //[Obsolete]
        //private ProjectileWeaponStat __CreateProjectileWeaponStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.ProjectileWeapon, level);
        //    RangeCategory rangeCat = RangeCategory.Medium;
        //    float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.07 - 3 degrees
        //    float reloadPeriod = UnityEngine.Random.Range(4F, 6F);  // 2-4
        //    string name = "KineticKill Projector";
        //    float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
        //    var damageCategory = DamageCategory.Structural;
        //    float damageValue = UnityEngine.Random.Range(5F, 10F);   // 3-8
        //    DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
        //    WDVStrength deliveryVehicleStrength = new WDVStrength(WDVCategory.Projectile, deliveryStrengthValue);

        //    float ordMaxSpeed = UnityEngine.Random.Range(15F, 18F);   // Ship STL MaxSpeed System = 1.6, OpenSpace = 8
        //    float ordMass = 1F;
        //    float ordDrag = 0.01F;
        //    float hitPts = 2F;
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return new ProjectileWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id, 0F,
        //        0F, 0F, hitPts, constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod,
        //        damagePotential, ordMaxSpeed, ordMass, ordDrag, maxLaunchInaccuracy);
        //}

        //[Obsolete]
        //private BeamWeaponStat __CreateBeamWeaponStat(Level level) {
        //    EquipmentStatID id = new EquipmentStatID(EquipmentCategory.BeamWeapon, level);
        //    RangeCategory rangeCat = RangeCategory.Short;
        //    float maxLaunchInaccuracy = UnityEngine.Random.Range(UnityConstants.AngleEqualityPrecision, 3F);  // 0.04 - 3 degrees
        //    float reloadPeriod = UnityEngine.Random.Range(6F, 10F); // 3-5
        //    float duration = UnityEngine.Random.Range(2F, 3F);  //1-2
        //    string name = "Phaser Projector";
        //    float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
        //    var damageCategory = DamageCategory.Thermal;
        //    float damageValue = UnityEngine.Random.Range(6F, 16F);   // 3-8
        //    DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
        //    WDVStrength deliveryVehicleStrength = new WDVStrength(WDVCategory.Beam, deliveryStrengthValue);
        //    float hitPts = 2F;
        //    float constructionCost = UnityEngine.Random.Range(1F, 5F);

        //    return new BeamWeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", id, 0F, 0F, 0F,
        //        hitPts, constructionCost, Constants.ZeroF, rangeCat, deliveryVehicleStrength, reloadPeriod, damagePotential,
        //        duration, maxLaunchInaccuracy);
        //}

        #endregion

    }
}


