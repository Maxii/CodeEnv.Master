// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitCreator.cs
//  Abstract, generic base class for Element/Command Creators.
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
/// Abstract, generic base class for Element/Command Creators.
/// </summary>
/// <typeparam name="ElementType">The Type of Element contained in the Command.</typeparam>
/// <typeparam name="ElementCategoryType">The Type holding the Element's Categories.</typeparam>
/// <typeparam name="ElementDataType">The Type of the Element's Data.</typeparam>
/// <typeparam name="ElementStatType">The Type of the Element stat.</typeparam>
/// <typeparam name="CommandType">The Type of the Command.</typeparam>
public abstract class AUnitCreator<ElementType, ElementCategoryType, ElementDataType, ElementStatType, CommandType> : ACreator, IDisposable
    where ElementType : AUnitElementModel
    where ElementCategoryType : struct
    where ElementDataType : AElementData
    where ElementStatType : struct
    where CommandType : AUnitCommandModel {

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

    private IList<IDisposable> _subscribers;
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
        _subscribers = new List<IDisposable>();
        _gameMgr.onCurrentStateChanged += OnGameStateChanged;
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
            RegisterReadinessForGameStateProgression(GameState.PrepareUnitsForDeployment, isReady: false);
            CreateElementStats();
            PrepareUnitForOperations(onCompleted: delegate {
                RegisterReadinessForGameStateProgression(GameState.PrepareUnitsForDeployment, isReady: true);
            });
        }

        if (gameState == GameState.DeployingUnits) {
            RegisterReadinessForGameStateProgression(GameState.DeployingUnits, isReady: false);
            DeployUnit();
            RegisterReadinessForGameStateProgression(GameState.DeployingUnits, isReady: true);
        }

        if (gameState == GameState.Running) {
            BeginUnitOperations();
        }
    }

    #region Delay into Runtime System

    private GameDate _delayedDateInRuntime;

    private void BuildDeployDuringStartupAndDelayBeginUnitOperations(GameState gameState) {
        D.Assert(toDelayOperations && !toDelayBuild);
        if (gameState == GameState.PrepareUnitsForDeployment) {
            RegisterReadinessForGameStateProgression(GameState.PrepareUnitsForDeployment, isReady: false);
            CreateElementStats();
            PrepareUnitForOperations(onCompleted: delegate {
                RegisterReadinessForGameStateProgression(GameState.PrepareUnitsForDeployment, isReady: true);
            });
        }

        if (gameState == GameState.DeployingUnits) {
            RegisterReadinessForGameStateProgression(GameState.DeployingUnits, isReady: false);
            DeployUnit();
            RegisterReadinessForGameStateProgression(GameState.DeployingUnits, isReady: true);
        }

        if (gameState == GameState.Running) {
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
        D.Assert(currentDate <= _delayedDateInRuntime);
        if (currentDate == _delayedDateInRuntime) {
            if (toDelayBuild) {
                D.Log("{0} is about to build, deploy and begin ops on Runtime date {1}.", UnitName, _delayedDateInRuntime);
                BuildDeployAndBeginUnitOperations();
            }
            else {
                D.Log("{0} has already been built and deployed. It is about to begin ops on Runtime date {1}.", UnitName, _delayedDateInRuntime);
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
        EnableElements();
        _command = MakeCommand(_owner);
        EnableCommand();
        UnityUtility.WaitOneToExecute(delegate {
            // delay 1 frame to make sure elements and command have initialized, then continue
            AddElements();
            AssignHQElement();
            if (onCompleted != null) {
                onCompleted();
            }
        });
    }

    protected abstract void DeployUnit();

    private void BeginUnitOperations() {
        LogEvent();
        __InitializeCommandIntel();
        BeginElementsOperations();
        BeginCommandOperations();
        UnityUtility.WaitOneToExecute((wasKilled) => {
            // delay 1 frame to allow Element and Command Idling_EnterState to execute
            IssueFirstUnitCommand();
            RemoveCreatorScript();
        });
    }

    private void BuildDeployAndBeginUnitOperations() {
        CreateElementStats();
        PrepareUnitForOperations(onCompleted: delegate {
            DeployUnit();
            if (enabled) {  // creator will be disabled if Destroyed during DeployUnit
                BeginUnitOperations();
            }
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
                    range = UnityEngine.Random.Range(2F, 4F);
                    reloadPeriod = UnityEngine.Random.Range(1F, 2F);
                    name = "Phaser";
                    strengthValue = UnityEngine.Random.Range(3F, 4F);
                    break;
                case ArmamentCategory.MissileOffense:
                    range = UnityEngine.Random.Range(4F, 6F);
                    reloadPeriod = UnityEngine.Random.Range(3F, 4F);
                    name = "Torpedo";
                    strengthValue = UnityEngine.Random.Range(5F, 6F);
                    break;
                case ArmamentCategory.ParticleOffense:
                    range = UnityEngine.Random.Range(3F, 5F);
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
            ElementCategoryType category = DeriveCategory(element);
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
        D.Log("{0} Element count is {1}.", UnitName, elementCount);
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
                    .Where(e => DeriveCategory(e).Equals(GetCategory(elementStat)));
                var categoryElementsStillAvailable = categoryElements.Except(elements);
                element = categoryElementsStillAvailable.First();
                var existingElementReference = element;
                bool isElementCompatibleWithOwner = MakeElement(elementStat, weaponStats, _owner, ref element);
                if (!isElementCompatibleWithOwner) {
                    Destroy(existingElementReference.gameObject);
                }
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

    protected abstract bool MakeElement(ElementStatType stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner, ref ElementType element);

    protected abstract ElementType MakeElement(ElementStatType stat, IEnumerable<WeaponStat> weaponStats, IPlayer owner);

    protected abstract CommandType MakeCommand(IPlayer owner);

    private void EnableElements() {
        LogEvent();
        _elements.ForAll(e => {
            e.enabled = true;
            (e as Component).gameObject.GetSafeInterface<IViewable>().enabled = true;
        });
    }

    private void EnableCommand() {
        LogEvent();
        _command.enabled = true;
        (_command as Component).gameObject.GetSafeInterface<IViewable>().enabled = true;
    }

    private void AddElements() {
        LogEvent();
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    protected abstract void AssignHQElement();

    // Element positioning and formationPosition assignments have been moved to AUnitCommandModel to support runtime adds and removals

    /// <summary>
    /// Starts the state machine of each element in this Unit.
    /// </summary>
    protected abstract void BeginElementsOperations();

    /// <summary>
    /// Starts the state machine of this Unit's Command.
    /// </summary>
    protected abstract void BeginCommandOperations();

    protected abstract void IssueFirstUnitCommand();

    protected abstract void __InitializeCommandIntel();

    private ElementCategoryType DeriveCategory(ElementType element) {
        string elementName = element.gameObject.name;
        return GameUtility.DeriveEnumFromName<ElementCategoryType>(elementName);
    }

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

    private void RegisterReadinessForGameStateProgression(GameState stateToNotProgressBeyondUntilReady, bool isReady) {
        GameEventManager.Instance.Raise(new ElementReadyEvent(this, stateToNotProgressBeyondUntilReady, isReady));
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
    }

    private void Unsubscribe() {
        if (_subscribers != null) {
            _subscribers.ForAll(d => d.Dispose());
            _subscribers.Clear();
        }
        _gameMgr.onCurrentStateChanged -= OnGameStateChanged;
    }

    #region IDisposable
    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

}


