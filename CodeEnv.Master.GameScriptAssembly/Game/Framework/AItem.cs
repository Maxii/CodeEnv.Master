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
                return _transform.name + "(from transform)";
            }
            return Data.FullName;
        }
    }

    public string Name { get { return Data.Name; } }

    public Vector3 Position { get { return Data.Position; } }

    public Transform Transform { get { return _transform; } }

    private float _radius;
    /// <summary>
    /// The radius of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public virtual float Radius {
        get {
            D.Assert(_radius != Constants.ZeroF, "{0}.Radius has not yet been set.".Inject(FullName));
            return _radius;
        }
        protected set { _radius = value; }
    }

    public Player Owner { get { return Data.Owner; } }

    protected IList<IDisposable> _subscribers;
    protected IInputManager _inputMgr;
    protected HudManager _hudManager;

    #region Initialization

    protected override void Awake() {
        base.Awake();
        InitializeLocalReferencesAndValues();
        Subscribe();
        enabled = false;
    }

    /// <summary>
    /// Called from Awake, initializes local references and values including Radius-related components.
    /// </summary>
    protected virtual void InitializeLocalReferencesAndValues() {
        _inputMgr = References.InputManager;
    }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        _subscribers.Add(_inputMgr.SubscribeToPropertyChanged<IInputManager, GameInputMode>(inputMgr => inputMgr.InputMode, OnInputModeChanged));
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected override void Start() {
        base.Start();
        InitializeModelMembers();
        InitializeViewMembers();
    }

    /// <summary>
    /// Called from Start, initializes Model-related members of this Item.
    /// </summary>
    protected abstract void InitializeModelMembers();

    /// <summary>
    /// Called from Start, initializes View-related members of this item 
    /// that can't wait until the Item first becomes discernible. Default
    /// implementation does nothing.
    /// </summary>
    protected virtual void InitializeViewMembers() {            // TODO AItem must override and init hud on discernible
        _hudManager = InitializeHudManager();
    }

    protected abstract HudManager InitializeHudManager();

    /// <summary>
    /// Subscribes to changes to values contained in Data. 
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() {
        D.Assert(_subscribers != null);
        _subscribers.Add(Data.SubscribeToPropertyChanging<AItemData, Player>(d => d.Owner, OnOwnerChanging));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, Player>(d => d.Owner, OnOwnerChanged));
    }

    #endregion

    #region Model Methods

    /// <summary>
    /// Called when the Item should start operations, typically once
    /// the game is running.
    /// </summary>
    public virtual void CommenceOperations() {
        IsOperational = true;
    }

    /// <summary>
    /// Indicates whether the provided <c>player</c> has investigated the item and
    /// gained knowledge of the item that is greater than the default level when the game started. 
    /// Example 1: All players start with IntelCoverage.Aware knowledge of Stars. This method would 
    /// return false if the player's IntelCoverage of the Star was not greater than Aware.
    /// Example 2: For a System, this method would return true if the player's IntelCoverage of the
    /// System's Star was greater than IntelCoverage.Aware OR the player's IntelCoverage of any of
    /// the System's Planetoids was greater than IntelCoverage.None.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public bool HasPlayerInvestigated(Player player) {
        return Data.HasPlayerInvestigated(player);
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
                _hudManager.Show(Position);
            }
            else {
                _hudManager.Hide();
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
                case GameInputMode.PartialScreenPopup:
                case GameInputMode.FullScreenPopup:
                    D.Log("InputMode changed to {0}. {1} is no longer showing HUD.", inputMode.GetName(), FullName);
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

    #region Mouse Events

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
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
    }

    #endregion

    #region INavigableTarget Members

    public virtual Topography Topography { get { return Data.Topography; } }

    public virtual bool IsMobile { get { return false; } }

    #endregion


}

