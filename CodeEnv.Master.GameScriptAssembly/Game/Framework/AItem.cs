// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AItem.cs
// Abstract base class for all Items.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract base class for all Items.
/// </summary>
public abstract class AItem : AMonoBase, IItem, INavigableTarget {

    /// <summary>
    /// Occurs when the owner of this <c>IItem</c> is about to change.
    /// The new incoming owner is the <c>Player</c> provided.
    /// </summary>
    public event Action<IItem, Player> onOwnerChanging;

    /// <summary>
    /// Occurs when the owner of this <c>IItem</c> has changed.
    /// </summary>
    public event Action<IItem> onOwnerChanged;

    private AItemData _data;
    public AItemData Data {
        get { return _data; }
        set {
            D.Assert(_data == null, "{0}.{1}.Data can only be set once.".Inject(FullName, GetType().Name));
            _data = value;
            InitializeOnData();
            SubscribeToDataValueChanges();
        }
    }

    public bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    /// <summary>
    /// Indicates whether this item has commenced operations, and if
    /// it is a MortalItem, that it is not dead.
    /// </summary>
    public bool IsOperational { get; protected set; }

    /// <summary>
    /// The name to use for display in the UI.
    /// </summary>
    public virtual string DisplayName { get { return Name; } }

    public string FullName {
        get {
            if (Data == null) {
                return transform.name + "(from transform)";
            }
            return Data.FullName;
        }
    }

    public string Name { get { return Data.Name; } }

    public Vector3 Position { get { return Data.Position; } }

    /// <summary>
    /// The radius of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public abstract float Radius { get; }

    public Player Owner { get { return Data.Owner; } }

    protected IList<IDisposable> _subscriptions;
    protected IInputManager _inputMgr;
    protected ItemHudManager _hudManager;
    protected IGameManager _gameMgr;

    #region Initialization

    protected override void Awake() {
        base.Awake();
        InitializeLocalReferencesAndValues();
        Subscribe();
        enabled = false;
    }

    /// <summary>
    /// Called from Awake, initializes local references and values.
    /// Note: Radius-related values and components should be initialized when Radius
    /// is valid which occurs when Data is added.
    /// </summary>
    protected virtual void InitializeLocalReferencesAndValues() {
        _inputMgr = References.InputManager;
        _gameMgr = References.GameManager;
    }

    protected virtual void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_inputMgr.SubscribeToPropertyChanged<IInputManager, GameInputMode>(inputMgr => inputMgr.InputMode, OnInputModeChanged));
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    /// <summary>
    /// Called once when Data is set, clients should initialize values that require the availability of Data.
    /// </summary>
    protected abstract void InitializeOnData();

    /// <summary>
    ///  Subscribes to changes to values contained in Data. Called when Data is set.
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() {
        D.Assert(_subscriptions != null);
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OnOwnerChanging));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OnOwnerChanged));
    }

    protected sealed override void Start() {
        base.Start();
        InitializeModelMembers();
        InitializeViewMembers();
    }

    /// <summary>
    /// Called from Start, initializes Model-related members of this Item.
    /// </summary>
    protected abstract void InitializeModelMembers();

    /// <summary>
    /// Called from Start, initializes View-related members of this item that aren't
    /// initialized in some other manner. Default implementation initializes the HudManager.
    /// </summary>
    protected virtual void InitializeViewMembers() {
        _hudManager = InitializeHudManager();
    }

    protected abstract ItemHudManager InitializeHudManager();

    #endregion

    #region Model Methods

    /// <summary>
    /// Called when the Item should start operations, typically once the game is running.
    /// </summary>
    public virtual void CommenceOperations() {
        IsOperational = true;
    }

    protected virtual void OnOwnerChanging(Player newOwner) {
        if (onOwnerChanging != null) {
            onOwnerChanging(this, newOwner);
        }
    }

    protected virtual void OnOwnerChanged() {
        if (onOwnerChanged != null) {
            onOwnerChanged(this);
        }
    }

    #endregion

    #region View Methods

    public void ShowHud(bool toShow) {
        if (_hudManager != null) {
            if (toShow) {
                _hudManager.ShowHud();
            }
            else {
                _hudManager.HideHud();
            }
        }
    }

    private void OnInputModeChanged() {
        OnInputModeChanged(_inputMgr.InputMode);
    }

    protected virtual void OnInputModeChanged(GameInputMode inputMode) {
        if (IsHudShowing) {
            switch (inputMode) {
                case GameInputMode.NoInput:
                case GameInputMode.PartialPopup:
                case GameInputMode.FullPopup:
                    D.Log("InputMode changed to {0}. {1} is no longer showing HUD.", inputMode.GetValueName(), FullName);
                    ShowHud(false);
                    break;
                case GameInputMode.Normal:
                    // do nothing
                    break;
                case GameInputMode.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(inputMode));
            }
        }
    }

    #endregion

    #region Events

    #endregion

    #region Cleanup

    protected sealed override void OnDestroy() {
        base.OnDestroy();
    }

    /// <summary>
    /// Cleans up this instance.
    /// Note: most members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected override void Cleanup() {
        if (_hudManager != null) {
            _hudManager.Dispose();
        }
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        _subscriptions.ForAll(s => s.Dispose());
        _subscriptions.Clear();
    }

    #endregion

    #region INavigableTarget Members

    public virtual Topography Topography { get { return Data.Topography; } }

    public virtual bool IsMobile { get { return false; } }

    #endregion

}

