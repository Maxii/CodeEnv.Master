﻿// --------------------------------------------------------------------------------------------------------------------
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
public abstract class AItem : AMonoBase, IItem, IItem_Ltd, IShipNavigable {

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
    /// Occurs when InfoAccess rights change for a player on an item.
    /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
    /// </summary>
    public event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

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

    /// <summary>
    /// Returns <c>true</c> if this item is owned by the User, <c>false</c> otherwise.
    /// <remarks>Shortcut that avoids having to access Owner to determine. If the user player
    /// is using this method (e.g. via ContextMenus), he/she always has access rights to the answer
    /// as if they own it, they have owner access, and if they don't own it, whether they have
    /// owner access rights is immaterial as the answer will always be false. The only time the
    /// AI will use it is when I intend for the AI to "cheat", aka gang up on the user.</remarks>
    /// </summary>
    public bool IsUserOwned { get { return Owner.IsUser; } }

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

    public bool TryGetOwner(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasAccessToInfo(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            return true;
        }
        owner = null;
        return false;
    }

    public bool IsOwnerAccessibleTo(Player player) {
        return InfoAccessCntlr.HasAccessToInfo(player, ItemInfoID.Owner);
    }

    protected AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

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
    protected virtual void InitializeOnData() {
        Data.Initialize();
    }

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

    #region Event and Property Change Handlers

    private void DataPropSetHandler() {
        InitializeOnData();
        SubscribeToDataValueChanges();
    }

    private void NamePropChangedHandler() {
        transform.name = Name;
    }

    private void IsOperationalPropChangedHandler() {
        HandleIsOperationalChanged();
    }

    protected virtual void HandleIsOperationalChanged() {
        enabled = IsOperational;
    }

    private void OwnerPropChangingHandler(Player newOwner) {
        HandleOwnerChanging(newOwner);
    }

    protected virtual void HandleOwnerChanging(Player newOwner) {
        OnOwnerChanging(newOwner);
    }

    private void OwnerPropChangedHandler() {
        HandleOwnerChanged();
    }

    protected virtual void HandleOwnerChanged() {
        OnOwnerChanged();
    }

    private void InputModePropChangedHandler() {
        HandleInputModeChanged(_inputMgr.InputMode);
    }

    private void HandleInputModeChanged(GameInputMode inputMode) {
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

    protected void OnInfoAccessChanged(Player player) {
        if (infoAccessChgd != null) {
            infoAccessChgd(this, new InfoAccessChangedEventArgs(player));
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

    #region Debug

    public Player Owner_Debug { get { return Data.Owner; } }

    private const string AItemDebugLogEventMethodNameFormat = "{0}.{1}()";

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging and ShowDebugLog are true.
    /// </summary>
    public override void LogEvent() {
        if ((_debugSettings.EnableEventLogging && ShowDebugLog)) {
            string methodName = GetMethodName();
            string fullMethodName = AItemDebugLogEventMethodNameFormat.Inject(FullName, methodName);
            Debug.Log("{0} beginning execution.".Inject(fullMethodName));
        }
    }

    #endregion

    #region INavigable Members

    public virtual bool IsMobile { get { return false; } }

    #endregion

    #region IShipNavigable Members

    public abstract AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition);

    #endregion


}

