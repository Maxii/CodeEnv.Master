// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCreator.cs
//  Abstract, generic base class for Unit Creators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract, generic base class for Unit Creators.
/// </summary>
/// <typeparam name="ElementType">The Type of Element contained in the Command.</typeparam>
/// <typeparam name="ElementCategoryType">The Type holding the Element's Categories.</typeparam>
/// <typeparam name="ElementDataType">The Type of the Element's Data.</typeparam>
/// <typeparam name="ElementHullStatType">The Type of the Element's hull stat.</typeparam>
/// <typeparam name="CommandType">The Type of the Command.</typeparam>
public abstract class AUnitCreator<ElementType, ElementCategoryType, ElementDataType, ElementHullStatType, CommandType> : ACreator
    where ElementType : AUnitElementItem
    where ElementCategoryType : struct
    where ElementDataType : AUnitElementItemData
    where ElementHullStatType : AHullStat
    where CommandType : AUnitCmdItem {

    private static IList<CommandType> _allUnitCommands = new List<CommandType>();
    public static IList<CommandType> AllUnitCommands { get { return _allUnitCommands; } }

    protected static UnitFactory _factory;
    protected static string _designNameFormat = "{0}_{1}";

    /// <summary>
    /// Gets a unique design name.
    /// <remarks>Replaced static instance count approach with Random.Range() as Settlement and Starbase
    /// Facility designs can have the same name since there isn't really only one static counter when using Generic classes.</remarks>
    /// </summary>
    /// <param name="hullCategory">The hull category.</param>
    /// <returns></returns>
    private static string GetUniqueDesignName(ElementCategoryType hullCategory) {
        return _designNameFormat.Inject(hullCategory.ToString(), UnityEngine.Random.Range(Constants.Zero, int.MaxValue).ToString());
    }

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName { get { return _transform.name; } }

    /// <summary>
    /// Local list of design names keyed by hull category. Each Creator needs their own local list
    /// to make sure the designs used comply with this creator's settings for weapon count, etc.
    /// </summary>
    protected IDictionary<ElementCategoryType, IList<string>> _designNamesByHullCategory;

    protected IList<PassiveCountermeasureStat> _availablePassiveCountermeasureStats;
    protected IList<ActiveCountermeasureStat> _availableActiveCountermeasureStats;
    protected IList<ShieldGeneratorStat> _availableShieldGeneratorStats;
    protected CommandType _command;
    protected Player _owner;

    private IList<ElementHullStatType> _elementHullStats;
    private IList<ElementType> _elements;
    private IList<SensorStat> _availableSensorStats;

    private IList<WeaponStat> _availableLosWeaponStats;
    private IList<WeaponStat> _availableMissileWeaponStats;

    private bool _isUnitDeployed;
    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        D.Assert(isCompositionPreset == _transform.childCount > 0, "{0}.{1} Composition Preset flag is incorrect.".Inject(UnitName, GetType().Name));
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        if (_factory == null) {
            _factory = UnitFactory.Instance;
        }
        _gameMgr = References.GameManager;
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.onGameStateChanged += OnGameStateChanged;
        SubscribeStaticallyOnce();
    }

    /// <summary>
    /// Allows a one time static subscription to event publishers from this class.
    /// </summary>
    private static bool _isStaticallySubscribed;
    /// <summary>
    /// Subscribes this class using static event handler(s) to instance events exactly one time.
    /// </summary>
    private void SubscribeStaticallyOnce() {
        if (!_isStaticallySubscribed) {
            //D.Log("{0} is subscribing statically to {1}.", GetType().Name, _gameMgr.GetType().Name);
            _gameMgr.onSceneLoaded += CleanupStaticMembers;
            _isStaticallySubscribed = true;
        }
    }

    private void OnGameStateChanged() {
        var gameState = _gameMgr.CurrentState;
        switch (gameState) {
            case GameState.Building:
                _owner = ValidateAndInitializeOwner();
                _availableLosWeaponStats = __CreateAvailableLosWeaponStats(TempGameValues.MaxLosWeaponsForAnyElement);
                _availableMissileWeaponStats = __CreateAvailableMissileWeaponStats(TempGameValues.MaxMissileWeaponsForAnyElement);
                _availablePassiveCountermeasureStats = __CreateAvailablePassiveCountermeasureStats(9);
                _availableActiveCountermeasureStats = __CreateAvailableActiveCountermeasureStats(9);
                _availableSensorStats = __CreateAvailableSensorStats(9);
                _availableShieldGeneratorStats = __CreateAvailableShieldGeneratorStats(9);
                break;
            case GameState.Lobby:
            case GameState.Waiting:
            case GameState.BuildAndDeploySystems:
            case GameState.GeneratingPathGraphs:
            case GameState.PrepareUnitsForDeployment:
            case GameState.DeployingUnits:
            case GameState.RunningCountdown_1:
            case GameState.Running:
            case GameState.Loading:
            case GameState.Restoring:
                break;
            case GameState.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(gameState));
        }

        if (toDelayOperations) {
            if (toDelayBuild) {
                DelayBuildDeployAndBeginUnitOperations(gameState);
                return;
            }
            BuildDeployDuringStartupAndDelayBeginUnitOperations(gameState);
            return;
        }
        BuildDeployAndBeginUnitOperationsDuringStartup(gameState);
    }

    private void BuildDeployAndBeginUnitOperationsDuringStartup(GameState gameState) {
        D.Assert(!toDelayOperations);  // toDelayBuild can be true from previous editor setting
        if (gameState == GameState.PrepareUnitsForDeployment) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PrepareUnitsForDeployment, isReady: false);
            CreateElementHullStats();
            MakeAndRecordDesigns();
            PrepareUnitForOperations(onCompleted: delegate {
                _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PrepareUnitsForDeployment, isReady: true);
            });
        }

        if (gameState == GameState.DeployingUnits) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingUnits, isReady: false);
            _isUnitDeployed = DeployUnit();
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingUnits, isReady: true);
            if (!_isUnitDeployed) {
                Destroy(gameObject);
            }
        }

        if (gameState == GameState.Running && _isUnitDeployed) {
            BeginUnitOperations();
        }
    }

    #region Delay into Runtime System

    private GameDate _delayedDateInRuntime;

    private void BuildDeployDuringStartupAndDelayBeginUnitOperations(GameState gameState) {
        D.Assert(toDelayOperations && !toDelayBuild);
        if (gameState == GameState.PrepareUnitsForDeployment) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PrepareUnitsForDeployment, isReady: false);
            CreateElementHullStats();
            MakeAndRecordDesigns();
            PrepareUnitForOperations(onCompleted: delegate {
                _gameMgr.RecordGameStateProgressionReadiness(this, GameState.PrepareUnitsForDeployment, isReady: true);
            });
        }

        if (gameState == GameState.DeployingUnits) {
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingUnits, isReady: false);
            _isUnitDeployed = DeployUnit();
            _gameMgr.RecordGameStateProgressionReadiness(this, GameState.DeployingUnits, isReady: true);
            if (!_isUnitDeployed) {
                Destroy(gameObject);
            }
        }

        if (gameState == GameState.Running && _isUnitDeployed) {
            var delay = new GameTimeDuration(hourDelay, dayDelay, yearDelay);
            if (delay == default(GameTimeDuration)) {
                // delayOperations selected but delay is 0 so begin operations now
                BeginUnitOperations();
            }
            else {
                _delayedDateInRuntime = new GameDate(delay);
                GameTime.Instance.onDateChanged += OnCurrentDateChanged;
            }
        }
    }

    private void DelayBuildDeployAndBeginUnitOperations(GameState gameState) {
        D.Assert(toDelayOperations && toDelayBuild);

        if (gameState == GameState.Running) {
            var runtimeDelay = new GameTimeDuration(hourDelay, dayDelay, yearDelay);
            if (runtimeDelay == default(GameTimeDuration)) {
                // delayOperations selected but delay is 0 so begin operations now
                //D.Log("Beginning Unit Operations with 0 hours delay.");
                BuildDeployAndBeginUnitOperations();
            }
            else {
                _delayedDateInRuntime = new GameDate(runtimeDelay);
                GameTime.Instance.onDateChanged += OnCurrentDateChanged;
            }
        }
    }

    private void OnCurrentDateChanged(GameDate currentDate) {
        //D.Log("{0} for {1} received OnCurrentDateChanged({2}).", GetType().Name, UnitName, currentDate);
        D.Assert(toDelayOperations);
        if (currentDate >= _delayedDateInRuntime) {
            if (currentDate > _delayedDateInRuntime) {
                D.Warn("{0} for {1} recorded current date {2} beyond target date {3}.", GetType().Name, UnitName, currentDate, _delayedDateInRuntime);
            }
            if (toDelayBuild) {
                D.Log("{0} is about to build, deploy and begin ops on {1}.", UnitName, _delayedDateInRuntime);
                BuildDeployAndBeginUnitOperations();
            }
            else {
                D.Log("{0} has already been built and deployed. It is about to begin ops on {1}.", UnitName, _delayedDateInRuntime);
                BeginUnitOperations();
            }
            //D.Log("{0} for {1} is unsubscribing from GameTime.onDateChanged.", GetType().Name, UnitName);
            GameTime.Instance.onDateChanged -= OnCurrentDateChanged;
        }
    }

    #endregion

    private void PrepareUnitForOperations(Action onCompleted = null) {
        LogEvent();
        _elements = MakeElements();
        _command = MakeCommand(_owner);
        EnableUnit(onCompletion: delegate {
            AddElements();
            AssignHQElement();
            if (onCompleted != null) {
                onCompleted();
            }
        });
    }

    /// <summary>
    /// Deploys the unit. Returns true if successfully deployed, false
    /// if not and therefore destroyed.
    /// </summary>
    /// <returns></returns>
    protected abstract bool DeployUnit();

    private void BeginUnitOperations() {
        LogEvent();

        BeginElementsOperations();
        BeginCommandOperations();
        UnityUtility.WaitOneToExecute(() => {
            // delay 1 frame to allow Element and Command Idling_EnterState to execute
            RecordInStaticCollections();
            __IssueFirstUnitCommand();
            RemoveCreatorScript();
        });
    }

    private void BuildDeployAndBeginUnitOperations() {
        CreateElementHullStats();
        MakeAndRecordDesigns();
        PrepareUnitForOperations(onCompleted: delegate {
            _isUnitDeployed = DeployUnit();
            if (!_isUnitDeployed) {
                Destroy(gameObject);
                return;
            }
            BeginUnitOperations();
        });
    }

    #region Create Stats

    private IList<WeaponStat> __CreateAvailableMissileWeaponStats(int quantity) {
        IList<WeaponStat> statsList = new List<WeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            WDVCategory deliveryVehicleCategory = WDVCategory.Missile;

            RangeCategory rangeCat = RangeCategory.Long; ;
            float accuracy = UnityEngine.Random.Range(0.95F, Constants.OneF); ;
            float reloadPeriod = UnityEngine.Random.Range(4F, 6F); ;
            string name = "PhotonTorpedo"; ;
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(3F, 8F);
            float duration = 0F;
            float baseRangeDistance = rangeCat.GetBaseWeaponRange();
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);

            var weapStat = new WeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, rangeCat,
                baseRangeDistance, deliveryVehicleStrength, accuracy, reloadPeriod, damagePotential, duration);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<WeaponStat> __CreateAvailableLosWeaponStats(int quantity) {
        IList<WeaponStat> statsList = new List<WeaponStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            RangeCategory rangeCat;
            float accuracy;
            float reloadPeriod;
            string name;
            float deliveryStrengthValue = UnityEngine.Random.Range(6F, 8F);
            var damageCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageValue = UnityEngine.Random.Range(3F, 8F);
            float duration = 0F;
            WDVCategory deliveryVehicleCategory = Enums<WDVCategory>.GetRandomExcept(default(WDVCategory), WDVCategory.Missile);
            //WDVCategory deliveryVehicleCategory = WDVCategory.Beam;
            //WDVCategory deliveryVehicleCategory = WDVCategory.Projectile;
            switch (deliveryVehicleCategory) {
                case WDVCategory.Beam:
                    rangeCat = RangeCategory.Short;
                    accuracy = UnityEngine.Random.Range(0.90F, Constants.OneF);
                    reloadPeriod = UnityEngine.Random.Range(3F, 5F);
                    name = "Phaser";
                    duration = 2F;
                    break;
                case WDVCategory.Projectile:
                    rangeCat = RangeCategory.Medium;
                    accuracy = UnityEngine.Random.Range(0.80F, Constants.OneF);
                    reloadPeriod = UnityEngine.Random.Range(2F, 4F);
                    name = "KineticKiller";
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(deliveryVehicleCategory));
            }
            float baseRangeDistance = rangeCat.GetBaseWeaponRange();
            DamageStrength damagePotential = new DamageStrength(damageCategory, damageValue);
            WDVStrength deliveryVehicleStrength = new WDVStrength(deliveryVehicleCategory, deliveryStrengthValue);
            var weapStat = new WeaponStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, rangeCat,
                baseRangeDistance, deliveryVehicleStrength, accuracy, reloadPeriod, damagePotential, duration);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    private IList<PassiveCountermeasureStat> __CreateAvailablePassiveCountermeasureStats(int quantity) {
        IList<PassiveCountermeasureStat> statsList = new List<PassiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            DamageStrength damageMitigation;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: false);
            float damageMitigationValue = UnityEngine.Random.Range(3F, 8F);
            switch (damageMitigationCategory) {
                case DamageCategory.Thermal:
                    name = "ThermalArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Atomic:
                    name = "AtomicArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.Kinetic:
                    name = "KineticArmor";
                    damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
                    break;
                case DamageCategory.None:
                    name = "GeneralArmor";
                    damageMitigation = new DamageStrength(2F, 2F, 2F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(damageMitigationCategory));
            }
            var countermeasureStat = new PassiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F, damageMitigation);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private IList<ActiveCountermeasureStat> __CreateAvailableActiveCountermeasureStats(int quantity) {
        IList<ActiveCountermeasureStat> statsList = new List<ActiveCountermeasureStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            RangeCategory rangeCat = Enums<RangeCategory>.GetRandom(excludeDefault: true);
            WDVStrength interceptStrength;
            float interceptAccuracy;
            float engagementPercent;
            float reloadPeriod;
            var damageMitigationCategory = Enums<DamageCategory>.GetRandom(excludeDefault: true);
            float damageMitigationValue = UnityEngine.Random.Range(1F, 2F);
            switch (rangeCat) {
                case RangeCategory.Short:
                    name = "CIWS";
                    engagementPercent = 0.40F;
                    interceptStrength = new WDVStrength(WDVCategory.Projectile, 0.2F);
                    interceptAccuracy = 0.50F;
                    reloadPeriod = 0.1F;
                    break;
                case RangeCategory.Medium:
                    name = "AvengerADS";
                    engagementPercent = 0.80F;
                    interceptStrength = new WDVStrength(WDVCategory.Missile, 3.0F);
                    interceptAccuracy = 0.80F;
                    reloadPeriod = 2.0F;
                    break;
                case RangeCategory.Long:
                    name = "PatriotADS";
                    engagementPercent = 0.90F;
                    interceptStrength = new WDVStrength(WDVCategory.Missile, 1.0F);
                    interceptAccuracy = 0.70F;
                    reloadPeriod = 3.0F;
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            float baseRangeDistance = rangeCat.GetBaseActiveCountermeasureRange();
            DamageStrength damageMitigation = new DamageStrength(damageMitigationCategory, damageMitigationValue);
            var countermeasureStat = new ActiveCountermeasureStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, baseRangeDistance, interceptStrength, interceptAccuracy, reloadPeriod, damageMitigation, engagementPercent);
            statsList.Add(countermeasureStat);
        }
        return statsList;
    }

    private IList<SensorStat> __CreateAvailableSensorStats(int quantity) {
        IList<SensorStat> statsList = new List<SensorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            string name = string.Empty;
            RangeCategory rangeCat = Enums<RangeCategory>.GetRandom(excludeDefault: true);
            //DistanceRange rangeCat = DistanceRange.Long;
            switch (rangeCat) {
                case RangeCategory.Short:
                    name = "ProximityDetector";
                    break;
                case RangeCategory.Medium:
                    name = "PulseSensor";
                    break;
                case RangeCategory.Long:
                    name = "DeepScanArray";
                    break;
                case RangeCategory.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(rangeCat));
            }
            float baseRangeDistance = rangeCat.GetBaseSensorRange();
            var sensorStat = new SensorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 0F, 0F, 0F,
                rangeCat, baseRangeDistance);
            statsList.Add(sensorStat);
        }
        return statsList;
    }

    private IList<ShieldGeneratorStat> __CreateAvailableShieldGeneratorStats(int quantity) {
        IList<ShieldGeneratorStat> statsList = new List<ShieldGeneratorStat>(quantity);
        for (int i = 0; i < quantity; i++) {
            RangeCategory rangeCat = RangeCategory.Short;
            float baseRangeDistance = rangeCat.GetBaseShieldRange();
            string name = "Deflector Generator";
            float maxCharge = 20F;
            float trickleChargeRate = 1F;
            float reloadPeriod = 20F;
            DamageStrength damageMitigation = default(DamageStrength);  // none for now
            var generatorStat = new ShieldGeneratorStat(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Description...", 0F, 1F, 0F, 0F,
                rangeCat, baseRangeDistance, maxCharge, trickleChargeRate, reloadPeriod, damageMitigation);
            statsList.Add(generatorStat);
        }
        return statsList;
    }

    private void CreateElementHullStats() {
        LogEvent();
        _elementHullStats = isCompositionPreset ? CreateElementHullStatsFromChildren() : CreateRandomElementHullStats();
    }

    private IList<ElementHullStatType> CreateElementHullStatsFromChildren() {
        LogEvent();
        var elements = gameObject.GetSafeMonoBehavioursInChildren<ElementType>();
        var elementHullStats = new List<ElementHullStatType>(elements.Count());
        var elementCategoriesUsedCount = new Dictionary<ElementCategoryType, int>();
        foreach (var element in elements) {
            AHull hull = element.gameObject.GetSafeFirstMonoBehaviourInChildren<AHull>();
            ElementCategoryType category = GetCategory(hull);
            if (!elementCategoriesUsedCount.ContainsKey(category)) {
                elementCategoriesUsedCount.Add(category, Constants.One);
            }
            else {
                elementCategoriesUsedCount[category]++;
            }
            int elementInstanceIndex = elementCategoriesUsedCount[category];
            string elementInstanceName = category.ToString() + Constants.Underscore + elementInstanceIndex;
            elementHullStats.Add(CreateElementHullStat(category, elementInstanceName));
        }
        return elementHullStats;
    }

    private IList<ElementHullStatType> CreateRandomElementHullStats() {
        LogEvent();
        var elementCategoriesUsedCount = new Dictionary<ElementCategoryType, int>();
        int elementCount = RandomExtended.Range(1, maxElementsInRandomUnit);
        //D.Log("{0} Element count is {1}.", UnitName, elementCount);
        var elementHullStats = new List<ElementHullStatType>(elementCount);
        for (int i = 0; i < elementCount; i++) {
            ElementCategoryType category = (i == 0) ? RandomExtended.Choice(HQElementCategories) : RandomExtended.Choice(ElementCategories);
            if (!elementCategoriesUsedCount.ContainsKey(category)) {
                elementCategoriesUsedCount.Add(category, Constants.One);
            }
            else {
                elementCategoriesUsedCount[category]++;
            }
            int elementInstanceIndex = elementCategoriesUsedCount[category];
            string elementInstanceName = category.ToString() + Constants.Underscore + elementInstanceIndex;
            elementHullStats.Add(CreateElementHullStat(category, elementInstanceName));
        }
        return elementHullStats;
    }

    protected abstract ElementHullStatType CreateElementHullStat(ElementCategoryType hullCat, string elementName);

    #endregion

    protected abstract ElementCategoryType[] HQElementCategories { get; }

    protected abstract ElementCategoryType[] ElementCategories { get; }

    private void MakeAndRecordDesigns() {
        _designNamesByHullCategory = new Dictionary<ElementCategoryType, IList<string>>();
        foreach (var hullStat in _elementHullStats) {
            ElementCategoryType hullCategory = GetCategory(hullStat);
            int losWeaponsPerElement = GetLosWeaponQtyToInstall(hullCategory);
            var weaponStats = _availableLosWeaponStats.Shuffle().Take(losWeaponsPerElement).ToList();
            int missileWeaponsPerElement = GetMissileWeaponQtyToInstall(hullCategory);
            weaponStats.AddRange(_availableMissileWeaponStats.Shuffle().Take(missileWeaponsPerElement));

            var passiveCmStats = _availablePassiveCountermeasureStats.Shuffle().Take(passiveCMsPerElement);
            var activeCmStats = _availableActiveCountermeasureStats.Shuffle().Take(activeCMsPerElement);
            var sensorStats = _availableSensorStats.Shuffle().Take(sensorsPerElement);
            var shieldGenStats = _availableShieldGeneratorStats.Shuffle().Take(shieldGeneratorsPerElement);

            //string designName = GetNextDesignName(hullCategory);
            string designName = GetUniqueDesignName(hullCategory);
            RecordDesignName(hullCategory, designName);
            MakeAndRecordDesign(designName, hullStat, weaponStats, passiveCmStats, activeCmStats, sensorStats, shieldGenStats);
        }
    }

    private void RecordDesignName(ElementCategoryType hullCategory, string designName) {
        if (!_designNamesByHullCategory.ContainsKey(hullCategory)) {
            _designNamesByHullCategory.Add(hullCategory, new List<string>());
        }
        _designNamesByHullCategory[hullCategory].Add(designName);
    }

    protected abstract void MakeAndRecordDesign(string designName, ElementHullStatType hullStat, IEnumerable<WeaponStat> weaponStats,
        IEnumerable<PassiveCountermeasureStat> passiveCmStats, IEnumerable<ActiveCountermeasureStat> activeCmStats,
        IEnumerable<SensorStat> sensorStats, IEnumerable<ShieldGeneratorStat> shieldGenStats);

    private IList<ElementType> MakeElements() {
        LogEvent();
        var elements = new List<ElementType>();
        foreach (var hullStat in _elementHullStats) {
            ElementCategoryType hullCategory = GetCategory(hullStat);
            string designName = _designNamesByHullCategory[hullCategory].Shuffle().First();
            ElementType element = null;
            if (isCompositionPreset) {
                // find a preExisting element of the right category first to provide to Make
                var categoryElements = gameObject.GetSafeMonoBehavioursInChildren<ElementType>()
                    .Where(e => GetCategory(e.gameObject.GetSafeFirstMonoBehaviourInChildren<AHull>()).Equals(hullCategory));
                var categoryElementsStillAvailable = categoryElements.Except(elements);
                element = categoryElementsStillAvailable.First();
                PopulateElement(designName, ref element);
            }
            else {
                element = MakeElement(designName);
            }
            // Note: Need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
            // will start with this position. However, the elements here are all placed on top of each other. When the physics engine starts
            // (after element.Start() completes?), rigidbodies that are not kinematic (ships) are imparted with both linear and angular 
            // velocity from this intentional collision. This occurs before the elements are moved away from each other by being formed
            // into a formation. Accordingly, make the rigidbody kinematic here, then change the ships back when the formation is made.
            element.transform.rigidbody.isKinematic = true;
            element.transform.position = _transform.position;
            elements.Add(element);
        }
        return elements;
    }

    protected abstract ElementCategoryType GetCategory(ElementHullStatType hullStat);

    protected abstract ElementCategoryType GetCategory(AHull hull);

    /// <summary>
    /// Populates the provided element with data and mounts derived from the stats
    /// tied to the design name.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <param name="element">The element.</param>
    protected abstract void PopulateElement(string designName, ref ElementType element);

    /// <summary>
    /// Instantiates a new element derived from the design name and populates it with data and mounts
    /// derived from the stats tied to the design name.
    /// </summary>
    /// <param name="designName">Name of the design.</param>
    /// <returns></returns>
    protected abstract ElementType MakeElement(string designName);

    protected abstract CommandType MakeCommand(Player owner);

    #region Enablement

    private void EnableUnit(Action onCompletion = null) {
        LogEvent();
        _elements.ForAll(e => e.enabled = true);
        _command.enabled = true;
        UnityUtility.WaitOneToExecute(onWaitFinished: delegate {
            if (onCompletion != null) {
                onCompletion();
            }
        });
    }

    #endregion

    private void AddElements() {
        LogEvent();
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    /// <summary>
    /// Assigns the hq element to the command. The assignment itself regenerates the formation,
    /// resulting in each element assuming the proper position.
    /// Note: This method must not be called before AddElements().
    /// </summary>
    protected abstract void AssignHQElement();

    // Element positioning and formationPosition assignments have been moved to AUnitCommand to support runtime adds and removals

    /// <summary>
    /// Starts the state machine of each element in this Unit.
    /// </summary>
    private void BeginElementsOperations() {
        LogEvent();
        _elements.ForAll(e => e.CommenceOperations());
    }

    /// <summary>
    /// Starts the state machine of this Unit's Command.
    /// </summary>
    private void BeginCommandOperations() {
        LogEvent();
        _command.CommenceOperations();
    }

    /// <summary>
    /// Records the Command in its static collection holding all instances.
    /// Note: The Assert tests are here to make sure instances from a prior scene are not still present, as the collections
    /// these items are stored in are static and persist across scenes.
    /// </summary>
    private void RecordInStaticCollections() {
        _command.onDeathOneShot += OnUnitDeath;
        var cmdNamesStored = _allUnitCommands.Select(cmd => cmd.DisplayName);
        // Can't use a Contains(item) test as the new item instance will never equal the old instance from the previous scene, even with the same name
        D.Assert(!cmdNamesStored.Contains(_command.DisplayName), "{0}.{1} reports {2} already present.".Inject(UnitName, GetType().Name, _command.DisplayName));
        _allUnitCommands.Add(_command);
    }

    private int GetMissileWeaponQtyToInstall(ElementCategoryType hullCategory) {
        int maxMissilesAllowed = GetMaxMissileWeaponsAllowed(hullCategory);
        switch (missileWeaponsPerElement) {
            case WeaponLoadout.None:
                return Constants.Zero;
            case WeaponLoadout.One:
                return maxMissilesAllowed > Constants.Zero ? Constants.One : Constants.Zero;
            case WeaponLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxMissilesAllowed);
            case WeaponLoadout.Max:
                return maxMissilesAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(missileWeaponsPerElement));
        }
    }

    private int GetLosWeaponQtyToInstall(ElementCategoryType hullCategory) {
        int maxLosWeaponsAllowed = GetMaxLosWeaponsAllowed(hullCategory);
        switch (losWeaponsPerElement) {
            case WeaponLoadout.None:
                return Constants.Zero;
            case WeaponLoadout.One:
                return maxLosWeaponsAllowed > Constants.Zero ? Constants.One : Constants.Zero;
            case WeaponLoadout.Random:
                return RandomExtended.Range(Constants.Zero, maxLosWeaponsAllowed);
            case WeaponLoadout.Max:
                return maxLosWeaponsAllowed;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(missileWeaponsPerElement));
        }
    }

    protected abstract int GetMaxMissileWeaponsAllowed(ElementCategoryType hullCategory);

    protected abstract int GetMaxLosWeaponsAllowed(ElementCategoryType hullCategory);

    protected abstract void __IssueFirstUnitCommand();

    private Player ValidateAndInitializeOwner() {
        Player userPlayer = _gameMgr.UserPlayer;
        if (isOwnerUser) {
            return userPlayer;
        }
        DiplomaticRelationship desiredRelationship;
        switch (ownerRelationshipWithUser) {
            case __DiploStateWithUser.Ally:
                desiredRelationship = DiplomaticRelationship.Ally;
                break;
            case __DiploStateWithUser.Friend:
                desiredRelationship = DiplomaticRelationship.Friend;
                break;
            case __DiploStateWithUser.Neutral:
                desiredRelationship = DiplomaticRelationship.Neutral;
                break;
            case __DiploStateWithUser.ColdWar:
                desiredRelationship = DiplomaticRelationship.ColdWar;
                break;
            case __DiploStateWithUser.War:
                desiredRelationship = DiplomaticRelationship.War;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ownerRelationshipWithUser));
        }
        IEnumerable<Player> aiOwnerCandidates = _gameMgr.AIPlayers.Where(aiPlayer => aiPlayer.GetRelations(userPlayer) == desiredRelationship);

        if (!aiOwnerCandidates.Any()) {
            D.Log("{0}.{1} couldn't find an AIPlayer with desired user relationship = {2}.", UnitName, GetType().Name, desiredRelationship.GetValueName());
            desiredRelationship = DiplomaticRelationship.None;
            aiOwnerCandidates = _gameMgr.AIPlayers.Where(aiPlayer => aiPlayer.GetRelations(userPlayer) == desiredRelationship);
        }
        Player aiOwner = aiOwnerCandidates.Shuffle().First();
        D.Log("{0}.{1} picked AI Owner {2}. User relationship = {3}.", UnitName, GetType().Name, aiOwner.LeaderName, desiredRelationship.GetValueName());
        return aiOwner;
    }

    private void RemoveCreatorScript() {
        Destroy(this);
    }

    /// <summary>
    /// Removes the command from the AllUnitCommands static collection
    /// when the command dies.
    /// </summary>
    /// <param name="mortalItem">The mortal item.</param>
    private static void OnUnitDeath(IMortalItem mortalItem) {
        AllUnitCommands.Remove(mortalItem as CommandType);
    }

    protected override void Cleanup() {
        Unsubscribe();
        if (IsApplicationQuiting) {
            CleanupStaticMembers();
            UnsubscribeStaticallyOnceOnQuit();
        }
    }

    protected virtual void Unsubscribe() {
        if (_gameMgr != null) {
            _gameMgr.onGameStateChanged -= OnGameStateChanged;
        }
    }

    /// <summary>
    /// Cleans up static members of this class whose value should not persist across scenes or after quiting.
    /// UNCLEAR This is called whether the scene loaded is from a saved game or a new game. 
    /// Should static values be reset on a scene change from a saved game? 1) do the static members
    /// retain their value after deserialization, and/or 2) can static members even be serialized? 
    /// </summary>
    private static void CleanupStaticMembers() {
        _allUnitCommands.ForAll(cmd => cmd.onDeathOneShot -= OnUnitDeath);
        _allUnitCommands.Clear();
    }

    /// <summary>
    /// Unsubscribes this class from all events that use a static event handler on Quit.
    /// </summary>
    private void UnsubscribeStaticallyOnceOnQuit() {
        if (_isStaticallySubscribed) {
            if (_gameMgr != null) {
                _gameMgr.onSceneLoaded -= CleanupStaticMembers;
            }
            _isStaticallySubscribed = false;
        }
    }

}


