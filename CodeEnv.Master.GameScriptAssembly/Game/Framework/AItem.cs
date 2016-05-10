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
public abstract class AItem : AMonoBase, IItem, IShipNavigable {

    /// <summary>
    /// Occurs when the owner of this <c>AItem</c> is about to change.
    /// The new incoming owner is the <c>Player</c> provided in the EventArgs.
    /// </summary>
    public event EventHandler<OwnerChangingEventArgs> ownerChanging;

    /// <summary>
    /// Occurs when the owner of this <c>AItem</c> has changed.
    /// </summary>
    public event EventHandler ownerChanged;

    /// <summary>
    /// Debug flag in editor indicating whether to show the D.Log for this item.
    /// <remarks>Requires #define DEBUG_LOG for D.Log() to be compiled.</remarks>
    /// </summary>
    [SerializeField]
    private bool _showDebugLog = false;
    public bool ShowDebugLog {
        get { return _showDebugLog; }
        set { SetProperty<bool>(ref _showDebugLog, value, "ShowDebugLog"); }
    }

    private AItemData _data;
    public AItemData Data {
        get { return _data; }
        set {
            D.Assert(_data == null, "{0}.{1}.Data can only be set once.".Inject(FullName, GetType().Name));
            SetProperty<AItemData>(ref _data, value, "Data", DataPropSetHandler);
        }
    }

    public virtual Topography Topography { get { return Data.Topography; } }

    public bool IsHudShowing {
        get { return _hudManager != null && _hudManager.IsHudShowing; }
    }

    /// <summary>
    /// Indicates whether this item has commenced operations, and if
    /// it is a MortalItem, that it is not dead. Set to false to initiate death.
    /// </summary>
    public bool IsOperational {
        get { return Data != null ? Data.IsOperational : false; }
        protected set { Data.IsOperational = value; }
    }

    /// <summary>
    /// The name to use for display in the UI.
    /// </summary>
    public virtual string DisplayName { get { return Name; } }

    public string FullName {
        get {
            if (Data == null) {
                return "{0}(from transform)".Inject(transform.name);
            }
            return Data.FullName;
        }
    }

    private string _name;
    public string Name {
        get {
            D.Assert(_name != null);
            return _name;
        }
        set { SetProperty<string>(ref _name, value, "Name", NamePropChangedHandler); }
    }

    public Vector3 Position { get { return transform.position; } }

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

    protected sealed override void Awake() {
        base.Awake();
        InitializeOnAwake();
        Subscribe();
        enabled = false;
    }

    protected virtual void InitializeOnAwake() {
        _inputMgr = References.InputManager;
        _gameMgr = References.GameManager;
    }

    protected virtual void Subscribe() {
        _subscriptions = new List<IDisposable>();
        _subscriptions.Add(_inputMgr.SubscribeToPropertyChanged<IInputManager, GameInputMode>(inputMgr => inputMgr.InputMode, InputModePropChangedHandler));
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
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OwnerPropChangingHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OwnerPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, bool>(d => d.IsOperational, IsOperationalPropChangedHandler));
    }

    protected sealed override void Start() {
        base.Start();
    }

    /// <summary>
    /// The final Initialization opportunity. The first method called from CommenceOperations,
    /// BEFORE IsOperational is set to true.
    /// </summary>
    protected virtual void FinalInitialize() { }

    #endregion

    /// <summary>
    /// Called when the Item should start operations, typically once the game is running.
    /// </summary>
    public virtual void CommenceOperations() {
        FinalInitialize();
        Data.CommenceOperations();
    }

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

    protected virtual void HandleInputModeChanged(GameInputMode inputMode) {
        if (IsHudShowing) {
            switch (inputMode) {
                case GameInputMode.NoInput:
                case GameInputMode.PartialPopup:
                case GameInputMode.FullPopup:
                    D.Log(ShowDebugLog, "InputMode changed to {0}. {1} is no longer showing HUD.", inputMode.GetValueName(), FullName);
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

    #region Event and Property Change Handlers

    private void DataPropSetHandler() {
        InitializeOnData();
        SubscribeToDataValueChanges();
    }

    private void NamePropChangedHandler() {
        transform.name = Name;
    }

    protected virtual void IsOperationalPropChangedHandler() {
        enabled = IsOperational;
    }

    protected virtual void OwnerPropChangingHandler(Player newOwner) {
        OnOwnerChanging(newOwner);
    }

    protected virtual void OwnerPropChangedHandler() {
        OnOwnerChanged();
    }

    private void InputModePropChangedHandler() {
        HandleInputModeChanged(_inputMgr.InputMode);
    }

    private void OnOwnerChanging(Player newOwner) {
        if (ownerChanging != null) {
            ownerChanging(this, new OwnerChangingEventArgs(newOwner));
        }
    }

    private void OnOwnerChanged() {
        if (ownerChanged != null) {
            ownerChanged(this, new EventArgs());
        }
    }

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

    #region INavigable Members

    public virtual bool IsMobile { get { return false; } }

    #endregion

    #region IShipNavigable Members

    public abstract AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition);

    #endregion

}

