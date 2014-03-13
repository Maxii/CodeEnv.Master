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
public abstract class AUnitCreator<ElementType, ElementCategoryType, ElementDataType, ElementStatType, CommandType> : AMonoBase, IDisposable
    where ElementType : AUnitElementModel
    where ElementCategoryType : struct
    where ElementDataType : AElementData
    where ElementStatType : class, new()
    where CommandType : AUnitCommandModel<ElementType> {

    public DiplomaticRelations OwnerRelationshipWithHuman;

    public bool IsCompleted { get; private set; }

    public int maxElements = 8;

    /// <summary>
    /// The name of the top level Unit, aka the Settlement, Starbase or Fleet name.
    /// A Unit contains a Command and one or more Elements.
    /// </summary>
    public string UnitName { get; private set; }

    protected IList<ElementStatType> _elementStats;
    protected HashSet<ElementCategoryType> _elementCategoriesUsed;
    protected UnitFactory _factory;

    protected IList<ElementType> _elements;
    protected CommandType _command;
    private IList<IDisposable> _subscribers;
    protected bool _isPreset;

    protected IGameManager _gameMgr;

    protected override void Awake() {
        base.Awake();
        _gameMgr = References.GameManager;

        _elementStats = new List<ElementStatType>();
        _elementCategoriesUsed = new HashSet<ElementCategoryType>();
        _factory = UnitFactory.Instance;

        UnitName = GetUnitName();
        _isPreset = _transform.childCount > 0;
        if (!GameStatus.Instance.IsRunning) {
            Subscribe();
        }
        else {
            Initiate();
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

    private void CreateComposition() {
        if (_isPreset) {
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

        int elementCount = RandomExtended<int>.Range(1, maxElements);
        D.Log("{0} Element count is {1}.", UnitName, elementCount);
        for (int i = 0; i < elementCount; i++) {
            ElementCategoryType category = (i == 0) ? RandomExtended<ElementCategoryType>.Choice(validHQCategories) : RandomExtended<ElementCategoryType>.Choice(validCategories);
            _elementCategoriesUsed.Add(category);
            int elementInstanceIndex = GetCurrentCount(category) + 1;
            string elementInstanceName = category.ToString() + Constants.Underscore + elementInstanceIndex;
            CreateElementStat(category, elementInstanceName);
        }
    }

    private void DeployUnit() {
        InitializeElements();
        _command = GetCommand(GetOwner());
        AddElements();
    }

    private void InitializeElements() {
        _elements = new List<ElementType>();
        ElementType element = null;
        foreach (var stat in _elementStats) {

            if (_isPreset) {
                // find a preExisting element of the right category first to provide to Make
                var categoryElements = gameObject.GetSafeMonoBehaviourComponentsInChildren<ElementType>()
                    .Where(e => DeriveCategory(e).Equals(GetCategory(stat)));
                var categoryElementsStillAvailable = categoryElements.Except(_elements);
                element = categoryElementsStillAvailable.First();

                MakeElement(stat, ref element);
            }
            else {
                element = MakeElement(stat);
            }
            // need to tell each element where this creator is located. This assures that whichever element is picked as the HQElement
            // will start with this position. The other elements positions will be adjusted from the HQElement position when the formation is formed
            element.transform.position = _transform.position;
            _elements.Add(element);
        }
    }

    protected abstract CommandType GetCommand(IPlayer owner);

    private void AddElements() {
        _elements.ForAll(e => _command.AddElement(e));
        // command IS NOT assigned as a target of each element's CameraLOSChangedRelay as that would make the CommandIcon disappear when the elements disappear
    }

    // Element positioning and formationPosition assignments have been moved to AUnitCommandModel to support runtime adds and removals

    /// <summary>
    /// Enables the Unit's elements and command which allows Start() and Initialize() to run. 
    /// Commands pick their HQ Element when they initialize. As positioning of the elements in a 
    /// formation requires knowledge of the HQ Element, this must run before positioning takes place.
    /// </summary>
    private void EnableUnit() {
        _elements.ForAll(element => element.enabled = true);
        _command.enabled = true;
        EnableViews();
    }

    protected abstract void EnableViews();
    protected abstract void CreateElementStat(ElementCategoryType category, string elementName);
    protected abstract void __InitializeCommandIntel();

    private ElementCategoryType DeriveCategory(ElementType element) {
        return Enums<ElementCategoryType>.Parse(element.gameObject.name);
    }

    private int GetCurrentCount(ElementCategoryType elementCategory) {
        if (!_elementCategoriesUsed.Contains(elementCategory)) {
            return 0;
        }
        return GetStats(elementCategory).Count;
    }

    protected abstract IList<ElementStatType> GetStats(ElementCategoryType elementCategory);

    private IPlayer GetOwner() {
        IPlayer humanPlayer = _gameMgr.HumanPlayer;
        IPlayer owner = new Player();
        switch (OwnerRelationshipWithHuman) {
            case DiplomaticRelations.Self:
                owner = humanPlayer;
                break;
            case DiplomaticRelations.Enemy:
                owner.SetRelations(humanPlayer, DiplomaticRelations.Enemy);
                humanPlayer.SetRelations(owner, DiplomaticRelations.Enemy);
                break;
            case DiplomaticRelations.Ally:
                owner.SetRelations(humanPlayer, DiplomaticRelations.Ally);
                humanPlayer.SetRelations(owner, DiplomaticRelations.Ally);
                break;
            case DiplomaticRelations.Neutral:
                owner.SetRelations(humanPlayer, DiplomaticRelations.Neutral);
                humanPlayer.SetRelations(owner, DiplomaticRelations.Neutral);
                break;
            case DiplomaticRelations.Friend:
                owner.SetRelations(humanPlayer, DiplomaticRelations.Friend);
                humanPlayer.SetRelations(owner, DiplomaticRelations.Friend);
                break;
            case DiplomaticRelations.None:
                D.WarnContext("Unit Owner not selected. Defaulting to Neutral relationship with HumanPlayer.", gameObject);
                owner.SetRelations(humanPlayer, DiplomaticRelations.Neutral);
                humanPlayer.SetRelations(owner, DiplomaticRelations.Neutral);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(OwnerRelationshipWithHuman));
        }
        return owner;
    }

    protected virtual string GetUnitName() {
        // usually, the name of the unit is carried by the name of the gameobject where this creator is located
        return _transform.name;
    }

    protected virtual void OnCompleted() {
        IsCompleted = true;
    }

    /// <summary>
    /// Returns the GameState defining when creation should occur.
    /// </summary>
    /// <returns>The GameState that triggers creation.</returns>
    protected abstract GameState GetCreationGameState();
    protected abstract ElementCategoryType GetCategory(ElementStatType stat);
    protected abstract void MakeElement(ElementStatType stat, ref ElementType element);
    protected abstract ElementType MakeElement(ElementStatType stat);
    protected abstract ElementCategoryType[] GetValidHQElementCategories();
    protected abstract ElementCategoryType[] GetValidElementCategories();

    private void RemoveCreatorScript() {
        Destroy(this);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        Unsubscribe();
        // other cleanup here including any tracking Gui2D elements
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


