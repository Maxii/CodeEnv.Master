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

#define DEBUG_LOG
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

    public abstract bool IsHudShowing { get; }

    /// <summary>
    /// The name to use for display in the UI.
    /// </summary>
    public virtual string DisplayName { get { return Name; } }

    public virtual string FullName {
        get {
            if (Data != null) {
                return Data.FullName;
            }
            return _transform.name + "(from transform)";
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
        InitializeHudManager();
    }


    protected abstract void InitializeHudManager();

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

    protected virtual void OnOwnerChanging(Player newOwner) { }

    protected virtual void OnOwnerChanged() {
        if (onOwnerChanged != null) {
            onOwnerChanged(this);
        }
    }

    #endregion

    #region View Methods

    public abstract void ShowHud(bool toShow);

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

