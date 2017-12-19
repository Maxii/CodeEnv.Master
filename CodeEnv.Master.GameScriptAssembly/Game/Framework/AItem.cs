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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

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
public abstract class AItem : AMonoBase, IOwnerItem, IOwnerItem_Ltd, IShipNavigableDestination {

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
    protected AItemData Data {
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

    public bool IsHoveredHudShowing {
        get { return _hoveredHudManager != null && _hoveredHudManager.IsHudShowing; }
    }

    /// <summary>
    /// Indicates whether this item has commenced operations, and if
    /// it is a MortalItem, that it is not dead.
    /// </summary>
    public bool IsOperational {
        get { return Data != null ? Data.IsOperational : false; }
        protected set { Data.IsOperational = value; }
    }

    public string DebugName {
        get {
            if (Data == null) {
                return "{0}(from transform)".Inject(transform.name);
            }
            return Data.DebugName;
        }
    }

    public string Name {
        get {
            if (Data != null) {
                return Data.Name;
            }
            return transform.name + " (from transform)";
        }
        set { Data.Name = value; }
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

    public Player Owner { get { return Data.Owner; } }

    /// <summary>
    /// The PlayerAIManager for the owner of this item. 
    /// <remarks>Will be null if Owner is NoPlayer.</remarks>
    /// </summary>
    public PlayerAIManager OwnerAIMgr { get; private set; }

    protected bool IsOwnerChangeUnderway {
        get { return Data.IsOwnerChangeUnderway; }
        private set { Data.IsOwnerChangeUnderway = value; }
    }

    protected AInfoAccessController InfoAccessCntlr { get { return Data.InfoAccessCntlr; } }

    protected ItemHoveredHudManager _hoveredHudManager;
    protected IList<IDisposable> _subscriptions;
    protected IInputManager _inputMgr;
    protected IGameManager _gameMgr;
    protected IJobManager _jobMgr;
    protected IDebugControls _debugCntls;

    #region Initialization

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
        ShowDebugLog = InitializeDebugLog();
        Subscribe();
        enabled = false;
    }

    protected virtual void InitializeValuesAndReferences() {
        _inputMgr = GameReferences.InputManager;
        _gameMgr = GameReferences.GameManager;
        _jobMgr = GameReferences.JobManager;
        _debugCntls = GameReferences.DebugControls;
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
        _subscriptions.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.Name, NamePropChangedHandler));
    }

    /// <summary>
    /// Initializes the Owner's PlayerAIManager for this item.
    /// </summary>
    protected void InitializeOwnerAIManager() {
        OwnerAIMgr = Owner != TempGameValues.NoPlayer ? _gameMgr.GetAIManagerFor(Owner) : null;
    }

    protected sealed override void Start() {
        base.Start();
    }

    /// <summary>
    /// The final Initialization opportunity before CommenceOperations().
    /// <remarks>Derived classes must set IsOperational to true after all FinalInitialization is complete.</remarks>
    /// </summary>
    public virtual void FinalInitialize() {
        Data.FinalInitialize();
        InitializeOwnerAIManager();
    }

    #endregion

    /// <summary>
    /// Called when the Item should begin operations.
    /// </summary>
    public virtual void CommenceOperations() {
        if (!IsOperational) {
            D.Error("{0} should be operational before calling CommenceOperations. Did you forget to call FinalInitialize first?", DebugName);
        }
        D.Assert(_gameMgr.IsRunning);
        Data.CommenceOperations();
    }

    public void ShowHoveredHud(bool toShow) {
        if (_hoveredHudManager != null) {
            if (toShow) {
                _hoveredHudManager.ShowHud();
            }
            else {
                _hoveredHudManager.HideHud();
            }
        }
    }

    #region Event and Property Change Handlers

    private void DataPropSetHandler() {
        InitializeOnData();
        SubscribeToDataValueChanges();
    }

    private void NamePropChangedHandler() {
        HandleNameChanged();
    }

    private void IsOperationalPropChangedHandler() {
        HandleIsOperationalChanged();
    }

    private void OwnerPropChangingHandler(Player newOwner) {
        // IsOwnerChangeUnderway = true; Handled by AItemData before any change work is done
        HandleOwnerChanging(newOwner);
        OnOwnerChanging(newOwner);  // UNCLEAR 5.15.17 Send event BEFORE handling internally?
    }

    private void OwnerPropChangedHandler() {
        HandleOwnerChanged();
        OnOwnerChanged();
        IsOwnerChangeUnderway = false;  // after all change work is done, changes state in AItemData
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
            ownerChanged(this, EventArgs.Empty);
        }
    }

    protected void OnInfoAccessChanged(Player player) {
        if (infoAccessChgd != null) {
            infoAccessChgd(this, new InfoAccessChangedEventArgs(player));
        }
    }

    #endregion

    protected virtual void HandleNameChanged() {
        transform.name = Name;
    }

    private void HandleIsOperationalChanged() {
        enabled = IsOperational;
    }

    protected abstract void HandleOwnerChanging(Player newOwner);

    protected virtual void HandleOwnerChanged() {
        InitializeOwnerAIManager();
    }

    private void HandleInputModeChanged(GameInputMode inputMode) {
        if (IsHoveredHudShowing) {
            switch (inputMode) {
                case GameInputMode.NoInput:
                case GameInputMode.PartialPopup:
                case GameInputMode.FullPopup:
                    //D.Log(ShowDebugLog, "InputMode changed to {0}. {1} is no longer showing HUD.", inputMode.GetValueName(), DebugName);
                    ShowHoveredHud(false);
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

    #region Cleanup

    protected sealed override void OnDestroy() {
        base.OnDestroy();
    }

    /// <summary>
    /// Cleans up this instance.
    /// Note: most members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected override void Cleanup() {
        if (_hoveredHudManager != null) {
            _hoveredHudManager.Dispose();
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

    private const string AItemDebugLogEventMethodNameFormat = "{0}.{1}()";

    /// <summary>
    /// Logs a statement that the method that calls this has been called.
    /// Logging only occurs if DebugSettings.EnableEventLogging and ShowDebugLog are true.
    /// </summary>
    public override void LogEvent() {
        if ((__debugSettings.EnableEventLogging && ShowDebugLog)) {
            string methodName = GetCallingMethodName();
            string fullMethodName = AItemDebugLogEventMethodNameFormat.Inject(DebugName, methodName);
            Debug.Log("{0} beginning execution.".Inject(fullMethodName));
        }
    }

    public void __LogInfoAccessChangedSubscribers() {
        if (infoAccessChgd != null) {
            IList<string> targetNames = new List<string>();
            var subscribers = infoAccessChgd.GetInvocationList();
            foreach (var sub in subscribers) {
                targetNames.Add(sub.Target.ToString());
            }
            Debug.LogFormat("{0}.InfoAccessChgdSubscribers: {1}.", DebugName, targetNames.Concatenate());
        }
        else {
            Debug.LogFormat("{0}.InfoAccessChgd event has no subscribers.", DebugName);
        }
    }

    #endregion

    public sealed override string ToString() {
        return DebugName;
    }

    #region Archive

    /// <summary>
    /// Handles AIMgr notifications when the current Owner just gained ownership of this item.
    /// <remarks>Warning: The item handler that calls this method gets subscribed to data's owner property change once data has been 
    /// assigned to this item. All Items get assigned their initial owner via the Data constructor. As a result, this method is not 
    /// called on the initial owner change. This doesn't matter for celestial objects (planets, stars, etc) as their initial owner is 
    /// NoPlayer. Subsequent owner changes, if any, all take place during runtime when this handler will fire. However, for Unit cmds 
    /// and elements, their initial owner is an actual player assigned prior to commencing operation. Accordingly, it is the responsibility 
    /// of the UnitCreator to inform the first owner's PlayerAIMgr of their ownership using PlayerAIMgr.HandleGainedItemOwnership() just 
    /// prior to commencing operation. IMPROVE There is another way to handle this - take the owner out of the Data.Constructor and assign 
    /// the owner (including NoPlayer) just prior to commencing operation. As a result, PlayerAIMgr's knowledge of ownership would be 
    /// completely handled by the Item's OwnerChanging/Changed handlers and this exception that requires the UnitCreator to handle it 
    /// would be eliminated.</remarks>
    /// </summary>
    //[Obsolete]
    //protected virtual void HandleAIMgrGainedOwnership() {
    //    D.AssertEqual(OwnerAIMgr.Owner, Owner);
    //    OwnerAIMgr.HandleGainedItemOwnership(this);

    //    IEnumerable<Player> allies;
    //    if (TryGetAllies(out allies)) {
    //        allies.ForAll(ally => {
    //            var allyAIMgr = _gameMgr.GetAIManagerFor(ally);
    //            allyAIMgr.HandleChgdItemOwnerIsAlly(this);
    //        });
    //    }
    //}

    /// <summary>
    /// Handles the condition where the current Owner of this item is about to be replaced by another owner.
    /// </summary>
    //[Obsolete]
    //protected virtual void HandleAIMgrLosingOwnership() {
    //    D.AssertEqual(OwnerAIMgr.Owner, Owner);
    //    D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
    //    OwnerAIMgr.HandleLosingItemOwnership(this);
    //}

    //[Obsolete]
    //private bool TryGetAllies(out IEnumerable<Player> alliedPlayers) {
    //    D.AssertNotEqual(TempGameValues.NoPlayer, Owner);
    //    alliedPlayers = Owner.GetOtherPlayersWithRelationship(DiplomaticRelationship.Alliance);
    //    return alliedPlayers.Any();
    //}

    #endregion


    #region INavigableDestination Members

    public virtual bool IsMobile { get { return false; } }

    #endregion

    #region IShipNavigableDestination Members

    public abstract ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship);

    #endregion

    #region IItem_Ltd Members

    public Player Owner_Debug { get { return Data.Owner; } }

    public bool TryGetOwner_Debug(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            return true;
        }
        owner = null;
        return false;
    }

    public bool TryGetOwner(Player requestingPlayer, out Player owner) {
        if (InfoAccessCntlr.HasIntelCoverageReqdToAccess(requestingPlayer, ItemInfoID.Owner)) {
            owner = Data.Owner;
            if (owner != TempGameValues.NoPlayer) {
                D.Assert(owner.IsKnown(requestingPlayer), "{0}: How can {1} have access to Owner {2} without knowing them??? Frame: {3}."
                    .Inject(DebugName, requestingPlayer.DebugName, owner.DebugName, Time.frameCount));
            }
            return true;
        }
        owner = null;
        return false;
    }

    public bool IsOwnerAccessibleTo(Player player) {
        return InfoAccessCntlr.HasIntelCoverageReqdToAccess(player, ItemInfoID.Owner);
    }

    #endregion


}

