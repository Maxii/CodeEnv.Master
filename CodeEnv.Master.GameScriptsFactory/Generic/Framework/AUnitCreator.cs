﻿// --------------------------------------------------------------------------------------------------------------------
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
using UnityEngine;

/// <summary>
/// Abstract, generic base class for Unit Creators.
/// </summary>
/// <typeparam name="ElementType">The Type of Element contained in the Command.</typeparam>
/// <typeparam name="ElementCategoryType">The Type holding the Element's Categories.</typeparam>
/// <typeparam name="ElementDataType">The Type of the Element's Data.</typeparam>
/// <typeparam name="ElementStatType">The Type of the Element stat.</typeparam>
/// <typeparam name="CommandType">The Type of the Command.</typeparam>
public abstract class AUnitCreator<ElementType, ElementCategoryType, ElementDataType, ElementStatType, CommandType> : ACreator
    where ElementType : AUnitElementItem
    where ElementCategoryType : struct
    where ElementDataType : AElementData
    where ElementStatType : struct
    where CommandType : AUnitCommandItem {

    private static IList<CommandType> _allUnitCommands = new List<CommandType>();
    public static IList<CommandType> AllUnitCommands { get { return _allUnitCommands; } }

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName { get { return _transform.name; } }

    protected IList<WeaponStat> _availableWeaponsStats;
    protected IList<ElementStatType> _elementStats;
    protected IList<ElementType> _elements;
    protected CommandType _command;
    protected IPlayer _owner;
    private bool _isUnitDeployed;

    private IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        D.Assert(isCompositionPreset == _transform.childCount > 0, "{0}.{1} Composition Preset flag is incorrect.".Inject(UnitName, GetType().Name));
    }

    protected override void Start() {
        base.Start();
        _gameMgr = References.GameManager;
        _owner = ValidateAndInitializeOwner();
        _availableWeaponsStats = __CreateAvailableWeaponsStats(9);
        Subscribe();
    }

    private void Subscribe() {
        _gameMgr.onCurrentStateChanged += OnGameStateChanged;
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
            CreateElementStats();
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
            CreateElementStats();
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

    private void CreateElementStats() {
        _elementStats = isCompositionPreset ? CreateElementStatsFromChildren() : CreateRandomElementStats();
    }

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
        UnityUtility.WaitOneToExecute((wasKilled) => {
            // delay 1 frame to allow Element and Command Idling_EnterState to execute
            //EnableOtherWhenRunning();
            RecordInStaticCollections();
            __SetIntelCoverage();
            __IssueFirstUnitCommand();
            RemoveCreatorScript();
        });
    }

    private void BuildDeployAndBeginUnitOperations() {
        CreateElementStats();
        PrepareUnitForOperations(onCompleted: delegate {
            _isUnitDeployed = DeployUnit();
            if (!_isUnitDeployed) {
                Destroy(gameObject);
                return;
            }
            BeginUnitOperations();
        });
    }

    #region Alternative Weapon Stats to choose from

    private static ArmamentCategory[] _offensiveArmsCategories = new ArmamentCategory[] { ArmamentCategory.BeamOffense, ArmamentCategory.MissileOffense, ArmamentCategory.ParticleOffense };

    private IList<WeaponStat> __CreateAvailableWeaponsStats(int weaponCount) {
        IList<WeaponStat> statsList = new List<WeaponStat>(weaponCount);
        for (int i = 0; i < weaponCount; i++) {
            float range = UnityEngine.Random.Range(3F, 5F);
            float reloadPeriod = UnityEngine.Random.Range(1F, 2F);
            string name = string.Empty;
            float strengthValue;
            ArmamentCategory offensiveArmsCategory = RandomExtended<ArmamentCategory>.Choice(_offensiveArmsCategories);
            switch (offensiveArmsCategory) {
                case ArmamentCategory.BeamOffense:
                    range = UnityEngine.Random.Range(5F, 7F);   // planet orbit to surface distance currently varies from 0.4 - 6.0
                    reloadPeriod = UnityEngine.Random.Range(1F, 2F);
                    name = "Phaser";
                    strengthValue = UnityEngine.Random.Range(3F, 4F);
                    break;
                case ArmamentCategory.MissileOffense:
                    range = UnityEngine.Random.Range(9F, 11F);
                    reloadPeriod = UnityEngine.Random.Range(3F, 4F);
                    name = "Torpedo";
                    strengthValue = UnityEngine.Random.Range(5F, 6F);
                    break;
                case ArmamentCategory.ParticleOffense:
                    range = UnityEngine.Random.Range(7F, 9F);
                    reloadPeriod = UnityEngine.Random.Range(2F, 3F);
                    name = "Disruptor";
                    strengthValue = UnityEngine.Random.Range(4F, 5F);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(offensiveArmsCategory));
            }
            CombatStrength strength = new CombatStrength(offensiveArmsCategory, strengthValue);
            WeaponStat weapStat = new WeaponStat(name, strength, range, reloadPeriod, 0F, 0F);
            statsList.Add(weapStat);
        }
        return statsList;
    }

    #endregion

    private IList<ElementStatType> CreateElementStatsFromChildren() {
        LogEvent();
        var elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>();
        var elementStats = new List<ElementStatType>(elements.Count());
        var elementCategoriesUsedCount = new Dictionary<ElementCategoryType, int>();
        foreach (var element in elements) {
            ElementCategoryType category = GetCategory(element);
            if (!elementCategoriesUsedCount.ContainsKey(category)) {
                elementCategoriesUsedCount.Add(category, Constants.One);
            }
            else {
                elementCategoriesUsedCount[category]++;
            }
            int elementInstanceIndex = elementCategoriesUsedCount[category];
            string elementInstanceName = category.ToString() + Constants.Underscore + elementInstanceIndex;
            elementStats.Add(CreateElementStat(category, elementInstanceName));
        }
        return elementStats;
    }

    private IList<ElementStatType> CreateRandomElementStats() {
        LogEvent();
        ElementCategoryType[] validHQCategories = GetValidHQElementCategories();
        ElementCategoryType[] validCategories = GetValidElementCategories();

        var elementCategoriesUsedCount = new Dictionary<ElementCategoryType, int>();
        int elementCount = RandomExtended<int>.Range(1, maxRandomElements);
        //D.Log("{0} Element count is {1}.", UnitName, elementCount);
        var elementStats = new List<ElementStatType>(elementCount);
        for (int i = 0; i < elementCount; i++) {
            ElementCategoryType category = (i == 0) ? RandomExtended<ElementCategoryType>.Choice(validHQCategories) : RandomExtended<ElementCategoryType>.Choice(validCategories);
            if (!elementCategoriesUsedCount.ContainsKey(category)) {
                elementCategoriesUsedCount.Add(category, Constants.One);
            }
            else {
                elementCategoriesUsedCount[category]++;
            }
            int elementInstanceIndex = elementCategoriesUsedCount[category];
            string elementInstanceName = (i == 0) ? category.ToString() : category.ToString() + Constants.Underscore + elementInstanceIndex;
            elementStats.Add(CreateElementStat(category, elementInstanceName));
        }
        return elementStats;
    }

    protected abstract ElementCategoryType[] GetValidHQElementCategories();

    protected abstract ElementCategoryType[] GetValidElementCategories();

    protected abstract ElementStatType CreateElementStat(ElementCategoryType category, string elementName);

    private IList<ElementType> MakeElements() {
        LogEvent();
        var elements = new List<ElementType>();
        foreach (var elementStat in _elementStats) {
            var weaponStats = _availableWeaponsStats.Shuffle().Take(weaponsPerElement);
            ElementType element = null;
            if (isCompositionPreset) {
                // find a preExisting element of the right category first to provide to Make
                var categoryElements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>()
                    .Where(e => GetCategory(e).Equals(GetCategory(elementStat)));
                var categoryElementsStillAvailable = categoryElements.Except(elements);
                element = categoryElementsStillAvailable.First();
                MakeElement(elementStat, weaponStats, _owner, ref element);
            }
            else {
                element = MakeElement(elementStat, weaponStats, _owner);
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

    protected abstract ElementCategoryType GetCategory(ElementStatType stat);

    protected abstract ElementCategoryType GetCategory(ElementType element);

    protected abstract void MakeElement(ElementStatType stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref ElementType element);

    protected abstract ElementType MakeElement(ElementStatType stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner);

    protected abstract CommandType MakeCommand(IPlayer owner);

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

    /// <summary>
    /// Enables selected children of the command and its elements. e.g. - cameraLosChangedListeners, WeaponTrackers,
    /// Revolve and Orbits, etc. These scripts that are enabled should only be enabled on or after IsRunning.
    /// </summary>
    //protected abstract void EnableOtherWhenRunning();

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
    protected abstract void BeginElementsOperations();

    /// <summary>
    /// Starts the state machine of this Unit's Command.
    /// </summary>
    protected abstract void BeginCommandOperations();

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

    protected virtual void __SetIntelCoverage() {
        LogEvent();

        var cmdIntel = _command.PlayerIntel;
        if (toCycleIntelCoverage) {
            new Job(__CycleIntelCoverage(cmdIntel), true);
        }
        else {
            cmdIntel.CurrentCoverage = IntelCoverage.Comprehensive;
        }
    }

    private IntelCoverage __previousCoverage;
    private IEnumerator __CycleIntelCoverage(IIntel intel) {
        intel.CurrentCoverage = IntelCoverage.None;
        yield return new WaitForSeconds(4F);
        intel.CurrentCoverage = IntelCoverage.Aware;
        __previousCoverage = IntelCoverage.Aware;
        while (true) {
            yield return new WaitForSeconds(4F);
            var proposedCoverage = Enums<IntelCoverage>.GetRandom(excludeDefault: true);
            while (proposedCoverage == __previousCoverage) {
                proposedCoverage = Enums<IntelCoverage>.GetRandom(excludeDefault: true);
            }
            intel.CurrentCoverage = proposedCoverage;
            __previousCoverage = proposedCoverage;
        }
    }

    protected abstract void __IssueFirstUnitCommand();


    private IPlayer ValidateAndInitializeOwner() {
        IPlayer humanPlayer = _gameMgr.HumanPlayer;
        if (isOwnerHuman) {
            return humanPlayer;
        }
        IPlayer aiOwner = new Player();
        switch (ownerRelationshipWithHuman) {
            case DiploStateWithHuman.Enemy:
                aiOwner.SetRelations(humanPlayer, DiplomaticRelations.Enemy);
                humanPlayer.SetRelations(aiOwner, DiplomaticRelations.Enemy);
                break;
            case DiploStateWithHuman.Ally:
                aiOwner.SetRelations(humanPlayer, DiplomaticRelations.Ally);
                humanPlayer.SetRelations(aiOwner, DiplomaticRelations.Ally);
                break;
            case DiploStateWithHuman.Friend:
                aiOwner.SetRelations(humanPlayer, DiplomaticRelations.Friend);
                humanPlayer.SetRelations(aiOwner, DiplomaticRelations.Friend);
                break;
            case DiploStateWithHuman.Neutral:
                aiOwner.SetRelations(humanPlayer, DiplomaticRelations.Neutral);
                humanPlayer.SetRelations(aiOwner, DiplomaticRelations.Neutral);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(ownerRelationshipWithHuman));
        }
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
            _gameMgr.onCurrentStateChanged -= OnGameStateChanged;
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


