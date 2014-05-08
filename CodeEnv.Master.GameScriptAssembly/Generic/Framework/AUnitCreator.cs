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
    where ElementStatType : class, new()
    where CommandType : AUnitCommandModel {

    /// <summary>
    /// OneShot event indicating the creation of the unit has completed meaning
    /// it has been built and enabled, but its state machine is not yet operating. 
    /// This final operational step will occur once the game is running.
    /// NOTE: Currently only used by UniverseInitializer to deploy Settlements to Systems when built.
    /// </summary>
    public event Action<CommandType> onUnitBuildComplete_OneShot;

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName { get; private set; }

    protected IList<ElementStatType> _elementStats;
    protected CommandType _command;

    private HashSet<ElementCategoryType> _elementCategoriesUsed;
    protected IList<ElementType> _elements;
    private IList<IDisposable> _subscribers;
    private IGameManager _gameMgr;
    protected IPlayer _owner;

    protected override void Awake() {
        base.Awake();
        _elementStats = new List<ElementStatType>();
        _elementCategoriesUsed = new HashSet<ElementCategoryType>();

        UnitName = InitializeUnitName();
        D.Assert(isCompositionPreset == _transform.childCount > 0, "{0}.{1} Composition Preset flag is incorrect.".Inject(UnitName, GetType().Name));
    }

    protected override void Start() {
        base.Start();
        _gameMgr = References.GameManager;
        _owner = ValidateAndInitializeOwner();
        if (toDeployInRuntime) {
            GameStatus.Instance.onIsRunning_OneShot += OnGameIsRunning;
        }
        else {
            Subscribe();
        }
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _gameMgr.onCurrentStateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged() {
        DeployDuringStartup(_gameMgr.CurrentState);
    }

    private void OnGameIsRunning() {
        DeployDuringRuntime();
    }

    private void DeployDuringStartup(GameState gameState) {
        if (gameState == GetCreationGameState()) {
            //D.Assert(gameState != GameState.RunningCountdown_2);    // interferes with positioning
            CreateComposition();
            PrepareUnitForOperations();
        }
        if (gameState == GameState.Running) {
            BeginUnitOperations();
        }
    }

    private void DeployDuringRuntime() {
        CreateComposition();
        PrepareUnitForOperations(onCompleted: BeginUnitOperations);
    }

    /// <summary>
    /// Returns the GameState defining when creation should occur.
    /// </summary>
    protected abstract GameState GetCreationGameState();

    private void CreateComposition() {
        if (isCompositionPreset) {
            CreateStatsFromChildren();
        }
        else {
            CreateRandomStats();
        }
    }

    private void PrepareUnitForOperations(Action onCompleted = null) {
        _elements = MakeElements();
        EnableElements();
        _command = MakeCommand(_owner);
        EnableCommand();
        new Job(UnityUtility.WaitForFrames(1), toStart: true, onJobComplete: delegate {
            // delay 1 frame to make sure elements and command have initialized, then continue
            AddElements();
            AssignHQElement();
            OnUnitBuildComplete();
            if (onCompleted != null) {
                onCompleted();
            }
        });
    }

    private void BeginUnitOperations() {
        __InitializeCommandIntel();
        BeginElementsOperations();
        BeginCommandOperations();
        new Job(UnityUtility.WaitForFrames(1), toStart: true, onJobComplete: delegate {
            // delay 1 frame to allow Element and Command Idling_EnterState to execute
            IssueFirstUnitCommand();
            RemoveCreatorScript();
        });
    }

    private void CreateStatsFromChildren() {
        var elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>();
        foreach (var element in elements) {
            ElementCategoryType category = DeriveCategory(element);
            _elementCategoriesUsed.Add(category);
            // don't change the name as I need to derive category from it in InitializeElements
            string elementInstanceName = element.gameObject.name;
            //CreateElementStat(category, elementInstanceName);   // FIXME this should be _elementStats.Add(CreateElementStat())
            _elementStats.Add(CreateElementStat(category, elementInstanceName));
        }
    }

    private void CreateRandomStats() {
        ElementCategoryType[] validHQCategories = GetValidHQElementCategories();
        ElementCategoryType[] validCategories = GetValidElementCategories();

        int elementCount = RandomExtended<int>.Range(1, maxRandomElements);
        D.Log("{0} Element count is {1}.", UnitName, elementCount);
        for (int i = 0; i < elementCount; i++) {
            ElementCategoryType category = (i == 0) ? RandomExtended<ElementCategoryType>.Choice(validHQCategories) : RandomExtended<ElementCategoryType>.Choice(validCategories);
            _elementCategoriesUsed.Add(category);
            int elementInstanceIndex = GetCurrentCount(category) + 1;
            string elementInstanceName = category.ToString() + Constants.Underscore + elementInstanceIndex;
            _elementStats.Add(CreateElementStat(category, elementInstanceName));
        }
    }

    protected abstract ElementCategoryType[] GetValidHQElementCategories();

    protected abstract ElementCategoryType[] GetValidElementCategories();

    protected abstract ElementStatType CreateElementStat(ElementCategoryType category, string elementName);

    private IList<ElementType> MakeElements() {
        var elements = new List<ElementType>();
        foreach (var stat in _elementStats) {
            ElementType element = null;
            if (isCompositionPreset) {
                // find a preExisting element of the right category first to provide to Make
                var categoryElements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>()
                    .Where(e => DeriveCategory(e).Equals(GetCategory(stat)));
                var categoryElementsStillAvailable = categoryElements.Except(elements);
                element = categoryElementsStillAvailable.First();
                var existingElementReference = element;
                bool isElementCompatibleWithOwner = MakeElement(stat, _owner, ref element);
                if (!isElementCompatibleWithOwner) {
                    Destroy(existingElementReference.gameObject);
                }
            }
            else {
                element = MakeElement(stat, _owner);
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

    protected abstract bool MakeElement(ElementStatType stat, IPlayer owner, ref ElementType element);

    protected abstract ElementType MakeElement(ElementStatType stat, IPlayer owner);

    protected abstract CommandType MakeCommand(IPlayer owner);

    private void EnableElements() {
        _elements.ForAll(e => {
            e.enabled = true;
            (e as Component).gameObject.GetSafeInterface<IViewable>().enabled = true;
        });
    }

    private void EnableCommand() {
        _command.enabled = true;
        (_command as Component).gameObject.GetSafeInterface<IViewable>().enabled = true;
    }

    private void AddElements() {
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

    private void OnUnitBuildComplete() {
        var temp = onUnitBuildComplete_OneShot;
        if (temp != null) {
            temp(_command);
        }
        onUnitBuildComplete_OneShot = null;
    }

    protected abstract void __InitializeCommandIntel();

    private ElementCategoryType DeriveCategory(ElementType element) {
        string elementName = element.gameObject.name;
        return GameUtility.DeriveEnumFromName<ElementCategoryType>(elementName);
    }

    private int GetCurrentCount(ElementCategoryType elementCategory) {
        if (!_elementCategoriesUsed.Contains(elementCategory)) {
            return 0;
        }
        return GetStatsCount(elementCategory);
    }

    protected abstract int GetStatsCount(ElementCategoryType elementCategory);

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

    private string InitializeUnitName() {
        // usually, the name of the unit is carried by the name of the gameobject where this creator is located
        return _transform.name;
    }

    private void RemoveCreatorScript() {
        Destroy(this);
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


