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

    public bool IsCompleted { get; private set; }

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName { get; private set; }

    protected IList<ElementStatType> _elementStats;
    protected CommandType _command;

    private HashSet<ElementCategoryType> _elementCategoriesUsed;
    private IList<ElementType> _elements;
    private IList<IDisposable> _subscribers;
    private IGameManager _gameMgr;
    private IPlayer _owner;

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
        if (!GameStatus.Instance.IsRunning) {
            Subscribe();
        }
        else {
            Initiate(); // TODO this has never been tested
        }
    }

    private void Subscribe() {
        _subscribers = new List<IDisposable>();
        _gameMgr.onCurrentStateChanged += OnGameStateChanged;
    }

    private void Initiate() {
        CreateComposition();
        DeployUnit();
        EnableUnit();
        // FIXME need to allow time for Starts to run to choose HQ after EnableUnit() is called
        OnCompleted();
        __InitializeCommandIntel();
    }

    private void OnGameStateChanged() {
        if (_gameMgr.CurrentState == GetCreationGameState()) {
            D.Assert(_gameMgr.CurrentState != GameState.RunningCountdown_2);    // interferes with positioning
            CreateComposition();
            DeployUnit();
            EnableUnit();  // must make View operational before starting state changes within it
        }
        if (_gameMgr.CurrentState == GameState.RunningCountdown_2) {
            // allows time for Starts to run to choose HQ after EnableUnit() is called
            OnCompleted();
        }
        if (_gameMgr.CurrentState == GameState.RunningCountdown_1) {
            __InitializeCommandIntel();
        }
        if (_gameMgr.CurrentState == GameState.Running) {
            RemoveCreatorScript();
        }
    }

    /// <summary>
    /// Returns the GameState defining when creation should occur.
    /// </summary>
    /// <returns>The GameState that triggers creation.</returns>
    protected abstract GameState GetCreationGameState();

    private void CreateComposition() {
        if (isCompositionPreset) {
            CreateStatsFromChildren();
        }
        else {
            CreateRandomStats();
        }
    }

    private void CreateStatsFromChildren() {
        var elements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>();
        foreach (var element in elements) {
            ElementCategoryType category = DeriveCategory(element);
            _elementCategoriesUsed.Add(category);
            // don't change the name as I need to derive category from it in InitializeElements
            string elementInstanceName = element.gameObject.name;
            CreateElementStat(category, elementInstanceName);
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
            CreateElementStat(category, elementInstanceName);
        }
    }

    protected abstract ElementCategoryType[] GetValidHQElementCategories();

    protected abstract ElementCategoryType[] GetValidElementCategories();

    protected abstract void CreateElementStat(ElementCategoryType category, string elementName);

    private void DeployUnit() {
        InitializeElements();
        _command = GetCommand(_owner);
        AddElements();
    }

    private void InitializeElements() {
        _elements = new List<ElementType>();
        ElementType element = null;
        foreach (var stat in _elementStats) {
            if (isCompositionPreset) {
                // find a preExisting element of the right category first to provide to Make
                var categoryElements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>()
                    .Where(e => DeriveCategory(e).Equals(GetCategory(stat)));
                var categoryElementsStillAvailable = categoryElements.Except(_elements);
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
            _elements.Add(element);
        }
    }

    protected abstract ElementCategoryType GetCategory(ElementStatType stat);

    protected abstract bool MakeElement(ElementStatType stat, IPlayer owner, ref ElementType element);

    protected abstract ElementType MakeElement(ElementStatType stat, IPlayer owner);

    protected abstract CommandType GetCommand(IPlayer owner);

    private void AddElements() {
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    // Element positioning and formationPosition assignments have been moved to AUnitCommandModel to support runtime adds and removals

    /// <summary>
    /// Enables the Unit's elements and command which allows Start() and Initialize() to run. 
    /// Commands pick their HQ Element when they initialize. 
    /// </summary>
    private void EnableUnit() {
        _command.enabled = true;
        // Commands now enable their elements during initialization

        // Enable the Views of the models 
        _command.Elements.ForAll(e => (e as Component).gameObject.GetSafeInterface<IElementViewable>().enabled = true);
        _command.gameObject.GetSafeInterface<ICommandViewable>().enabled = true;
    }

    protected virtual void OnCompleted() {
        IsCompleted = true;
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


