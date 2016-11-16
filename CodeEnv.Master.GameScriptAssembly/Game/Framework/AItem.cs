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
using System.Linq;
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
    /// Occurs when InfoAccess rights change for a player on an item, directly attributable to
    /// a change in the player's IntelCoverage of the item.
    /// <remarks>Made accessible to trigger other players to re-evaluate what they know about opponents.</remarks>
    /// </summary>
    public event EventHandler<InfoAccessChangedEventArgs> infoAccessChgd;

    private AItemData _data;
    public AItemData Data {
        get { return _data; }
        set {
            D.AssertNull(_data, "Data can only be set once.");
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

    public Topography Topography { get { return Data.Topography; } }

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
            D.AssertNotNull(_name);
            return _name;
        }
        set { SetProperty<string>(ref _name, value, "Name", NamePropChangedHandler); }
    }

    public Vector3 Position { get { return transform.position; } }

    /// <summary>
    /// The radius of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public abstract float Radius { get; }

    /// <summary>
    /// The radius of the conceptual 'globe' surrounding this Item, outside of which will be clear of interference from
    /// this item or anything normally associated with this item.
    /// </summary>
    public abstract float ClearanceRadius { get; }

    public Player Owner {
        get { return Data.Owner; }
        set { Data.Owner = value; }
    }

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

    protected PlayerAIManager OwnerAIMgr { get; private set; }  // will be null if Owner is NoPlayer

    protected AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

    protected IList<IDisposable> _subscriptions;
    protected IInputManager _inputMgr;
    protected ItemHudManager _hudManager;
    protected IGameManager _gameMgr;
    protected IJobManager _jobMgr;

    #region Initialization

    protected sealed override void Awake() {
        base.Awake();
        InitializeOnAwake();
        ShowDebugLog = InitializeDebugLog();
        Subscribe();
        enabled = false;
    }

    protected virtual void InitializeOnAwake() {
        _inputMgr = References.InputManager;
        _gameMgr = References.GameManager;
        _jobMgr = References.JobManager;
    }

    protected abstract bool InitializeDebugLog();

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
        D.AssertNotNull(_subscriptions);
        _subscriptions.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OwnerPropChangingHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OwnerPropChangedHandler));
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, bool>(d => d.IsOperational, IsOperationalPropChangedHandler));
    }

    protected sealed override void Start() {
        base.Start();
    }

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// </summary>
    public virtual void FinalInitialize() {
        Data.FinalInitialize();
        D.Assert(IsOperational);
        OwnerAIMgr = Owner != TempGameValues.NoPlayer ? _gameMgr.GetAIManagerFor(Owner) : null;
    }

    #endregion

    /// <summary>
    /// Called when the Item should begin operations.
    /// </summary>
    public virtual void CommenceOperations() {
        D.Assert(_gameMgr.IsRunning);
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
        if (Owner != TempGameValues.NoPlayer) {
            HandleAIMgrLosingOwnership();
        }
        OnOwnerChanging(newOwner);
    }

    private void OwnerPropChangedHandler() {
        HandleOwnerChanged();
    }

    protected virtual void HandleOwnerChanged() {
        OwnerAIMgr = Owner != TempGameValues.NoPlayer ? _gameMgr.GetAIManagerFor(Owner) : null;
        if (OwnerAIMgr != null) {
            HandleAIMgrGainedOwnership();
        }
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
            ownerChanged(this, EventArgs.Empty);
        }
    }

    protected void OnInfoAccessChanged(Player player) {
        if (infoAccessChgd != null) {
            infoAccessChgd(this, new InfoAccessChangedEventArgs(player));
        }
    }

    #endregion

    /// <summary>
    /// Handles AIMgr notifications when the current Owner just gained ownership of this item.
    /// <remarks>Warning: The item handler that calls this method gets subscribed to data's owner property change once data has been 
    /// assigned to this item. All Items get assigned their initial owner via the Data constructor. As a result, this method is not be 
    /// called on the initial owner change. This doesn't matter for celestial objects (planets, stars, etc) as their initial owner is 
    /// NoPlayer. Subsequent owner changes, if any, all take place during runtime when this handler will fire. However, for Unit cmds 
    /// and elements, their initial owner is an actual player assigned prior to commencing operation. Accordingly, it is the responsibility 
    /// of the UnitCreator to inform the first owner's PlayerAIMgr of their ownership using PlayerAIMgr.HandleGainedItemOwnership() just 
    /// prior to commencing operation. IMPROVE There is another way to handle this - take the owner out of the Data.Constructor and assign 
    /// the owner (including NoPlayer) just prior to commencing operation. As a result, PlayerAIMgr's knowledge of ownership would be 
    /// completely handled by the Item's OwnerChanging/Changed handlers and this exception that requires the UnitCreator to handle it 
    /// would be eliminated.</remarks>
    /// </summary>
    protected virtual void HandleAIMgrGainedOwnership() {
        D.AssertEqual(OwnerAIMgr.Owner, Owner);
        OwnerAIMgr.HandleGainedItemOwnership(this);

        IEnumerable<Player> allies;
        if (TryGetAllies(out allies)) {
            allies.ForAll(ally => {
                var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
                allyAIMgr.HandleChgdItemOwnerIsAlly(this);
            });
        }
    }

    /// <summary>
    /// Handles the condition where the current Owner of this item is about to be replaced by another owner.
    /// </summary>
    protected virtual void HandleAIMgrLosingOwnership() {
        D.AssertEqual(OwnerAIMgr.Owner, Owner);
        D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
        OwnerAIMgr.HandleLosingItemOwnership(this);
    }

    private bool TryGetAllies(out IEnumerable<Player> alliedPlayers) {
        D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
        alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
        return alliedPlayers.Any();
    }

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
        if (_subscriptions != null) {
            _subscriptions.ForAll(s => s.Dispose());
            _subscriptions.Clear();
        }
    }

    #endregion

    #region Debug

    /// <summary>
    /// Debug flag in editor indicating whether to show the D.Log for this item.
    /// <remarks>Requires #define DEBUG_LOG for D.Log() to be compiled.</remarks>
    /// </summary>
    public bool ShowDebugLog { get; private set; }

    public Player Owner_Debug { get { return Data.Owner; } }

    private const string AItemDebugLogEventMethodNameFormat = "{0}.{1}()";

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging and ShowDebugLog are true.
    /// </summary>
    public override void LogEvent() {
        if ((_debugSettings.EnableEventLogging && ShowDebugLog)) {
            string methodName = GetCallingMethodName();
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

