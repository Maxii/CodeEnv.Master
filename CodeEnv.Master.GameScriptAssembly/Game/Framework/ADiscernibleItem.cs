// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADiscernibleItem.cs
// Abstract class for Items that can change whether they are discernible by the UserPlayer.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Abstract class for Items that can change whether they are discernible by the UserPlayer.
/// </summary>
public abstract class ADiscernibleItem : AItem, IDiscernibleItem, ICameraFocusable, IHighlightable, IEffectsClient, ISelectable {

    private bool _isDiscernibleToUser;
    public bool IsDiscernibleToUser {
        get { return _isDiscernibleToUser; }
        protected set { SetProperty<bool>(ref _isDiscernibleToUser, value, "IsDiscernibleToUser", IsDiscernibleToUserPropChangedHandler); }
    }

    /// <summary>
    /// Indicates whether the visual detail of this Item is discernible to the user. 
    /// Detail here refers to the mesh(es) and animations, not to the icon, if any.
    /// </summary>
    public bool IsVisualDetailDiscernibleToUser {
        get { return DisplayMgr != null ? IsDiscernibleToUser && DisplayMgr.IsPrimaryMeshInMainCameraLOS : IsDiscernibleToUser; }
    }

    public new ADiscernibleItemData Data {
        get { return base.Data as ADiscernibleItemData; }
        set { base.Data = value; }
    }

    public ADisplayManager DisplayMgr { get; private set; }

    protected EffectsManager EffectsMgr { get; private set; }

    /// <summary>
    /// Flag indicating whether InitializeOnFirstDiscernibleToUser() has run.
    /// </summary>
    protected bool _hasInitOnFirstDiscernibleToUserRun;

    private IGameInputHelper _inputHelper;
    private IHighlighter _highlighter;
    private ICtxControl _ctxControl;

    #region Initialization

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        _inputHelper = References.InputHelper;
    }

    /// <summary>
    /// Called when the Item first becomes discernible to the user, this method initializes the 
    /// View-related members of this item that are not needed until the item is discernible to the user.
    /// </summary>
    protected virtual void InitializeOnFirstDiscernibleToUser() {
        D.Assert(!_hasInitOnFirstDiscernibleToUserRun);
        D.Assert(IsOperational, "{0}.InitializeOnFirstDiscernibleToUser() called when not operational.", FullName);
        _hudManager = InitializeHudManager();

        DisplayMgr = InitializeDisplayManager();
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsInMainCameraLOS, IsInMainCameraLosPropChangedHandler));
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsPrimaryMeshInMainCameraLOS, IsVisualDetailDiscernibleToUserPropChangedHandler));
        // always start enabled as UserPlayerIntelCoverage must be > None for this method to be called,
        // or, in the case of SystemItem, its members coverage must be > their starting coverage
        DisplayMgr.EnableDisplay(true);

        EffectsMgr = InitializeEffectsManager();
        _highlighter = InitializeHighlighter();
        _ctxControl = InitializeContextMenu(Owner);
        _hasInitOnFirstDiscernibleToUserRun = true;
    }

    protected abstract ItemHudManager InitializeHudManager();

    protected abstract ADisplayManager InitializeDisplayManager();

    protected virtual EffectsManager InitializeEffectsManager() {
        return new EffectsManager(this);
    }

    protected virtual IHighlighter InitializeHighlighter() {
        return new Highlighter(this);
    }

    protected abstract ICtxControl InitializeContextMenu(Player owner);

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessIsDiscernibleToUser();
    }

    /// <summary>
    /// Assesses the discernability of this item to the user.
    /// </summary>
    protected abstract void AssessIsDiscernibleToUser();

    public virtual void AssessHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowHighlights(HighlightID.Focused, HighlightID.Selected);
                    return;
                }
                ShowHighlights(HighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowHighlights(HighlightID.Selected);
                return;
            }
        }
        ShowHighlights(HighlightID.None);
    }

    protected void ShowHighlights(params HighlightID[] highlightIDs) {
        if (_highlighter != null) {
            _highlighter.Show(highlightIDs);
        }
    }

    protected void ShowHoverHighlight(bool toShow) {
        if (_highlighter != null) {
            _highlighter.ShowHovered(toShow);
        }
    }

    /// <summary>
    /// Shows the SelectedItemHudWindow for this ISelectable Item.
    /// </summary>
    /// <remarks>This method must be called prior to notifying SelectionMgr of the selection change. 
    /// HoveredItemHudWindow subscribes to the change and needs the SelectedItemHud to already 
    /// be resized and showing so it can position itself properly. Hiding the SelectedItemHud is 
    /// handled by the SelectionMgr when there is no longer an item selected.
    /// </remarks>
    protected abstract void ShowSelectedItemHud();

    public virtual void HandleEffectFinished(EffectID effectID) { }

    protected void StartEffect(EffectID effectID) {
        if (IsVisualDetailDiscernibleToUser) {
            D.Assert(EffectsMgr != null);   // if DisplayMgr is initialized, so is EffectsMgr
            EffectsMgr.StartEffect(effectID);
        }
        else {
            // Not going to show the effect. Complete the handshake so any dependancies can continue
            HandleEffectFinished(effectID);
        }
    }

    protected void StopEffect(EffectID effectID) {
        if (EffectsMgr != null) {
            EffectsMgr.StopEffect(effectID);
        }
        // if EffectsMgr never initialized, then caller of StartEffect already got its HandleEffectFinished callback
    }

    #region Event and Property Change Handlers

    protected virtual void IsFocusPropChangedHandler() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = this;
        }
        AssessHighlighting();
    }

    protected virtual void IsSelectedPropChangedHandler() {
        if (IsSelected) {
            ShowSelectedItemHud();
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessHighlighting();
    }

    protected override void OwnerPropChangingHandler(Player newOwner) {
        base.OwnerPropChangingHandler(newOwner);
        if (_hasInitOnFirstDiscernibleToUserRun) {
            D.Assert(_ctxControl != null);
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsUser != newOwner.IsUser) {
                // Kind of owner (NoPlayer, AI or User) has changed so generate a new ctxControl -
                // aka, a change from one AI player to another does not necessitate a change
                (_ctxControl as IDisposable).Dispose();
                _ctxControl = InitializeContextMenu(newOwner);
            }
        }
    }

    protected virtual void IsInMainCameraLosPropChangedHandler() {
        AssessIsDiscernibleToUser();
    }

    protected virtual void IsDiscernibleToUserPropChangedHandler() {
        if (!IsDiscernibleToUser && IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_hasInitOnFirstDiscernibleToUserRun) {
            D.Assert(IsDiscernibleToUser);    // first time change should always be true
            InitializeOnFirstDiscernibleToUser();
        }
        AssessHighlighting();
        //D.Log("{0}.IsDiscernibleToUser changed to {1}.", FullName, IsDiscernibleToUser);
    }

    // IMPROVE deal with losing IsDiscernible while hovered or pressed

    protected virtual void IsVisualDetailDiscernibleToUserPropChangedHandler() { }

    protected virtual void HandleHoverOver() {
        ShowHud(true);
        ShowHoverHighlight(true);
    }

    protected virtual void HandleHoverOff() {
        ShowHud(false);
        ShowHoverHighlight(false);
    }

    protected void HoverEventHandler(GameObject go, bool isOver) {
        //D.Log("{0} is handling an OnHover event. IsOver = {1}.", FullName, isOver);
        if (IsDiscernibleToUser) {
            if (isOver) {
                HandleHoverOver();
            }
            else {
                HandleHoverOff();
            }
        }
    }

    void OnHover(bool isOver) {
        HoverEventHandler(gameObject, isOver);
    }

    protected virtual void HandleLeftClick() { IsSelected = true; }
    protected virtual void HandleAltLeftClick() { }
    protected virtual void HandleMiddleClick() { IsFocus = true; }
    protected virtual void HandleRightClick() { }

    protected void ClickEventHandler(GameObject go) {
        //D.Log("{0} is handling an OnClick event.", FullName);
        if (IsDiscernibleToUser) {
            if (_inputHelper.IsLeftMouseButton) {
                KeyCode notUsed;
                if (_inputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    HandleAltLeftClick();
                }
                else {
                    HandleLeftClick();
                }
            }
            else if (_inputHelper.IsMiddleMouseButton) {
                HandleMiddleClick();
            }
            else if (_inputHelper.IsRightMouseButton) {
                HandleRightClick();
            }
            else {
                D.Error("{0}.OnClick() without a mouse button found.", GetType().Name);
            }
        }
    }

    void OnClick() {
        ClickEventHandler(gameObject);
    }

    protected virtual void HandleLeftPress() { }
    protected virtual void HandleMiddlePress() { }
    protected virtual void HandleRightPress() { }

    protected void PressEventHandler(GameObject go, bool isDown) {
        //D.Log("{0} is handling an OnPress event. IsDown = {1}.", FullName, isDown);
        if (IsDiscernibleToUser) {
            if (_inputHelper.IsLeftMouseButton) {
                if (isDown) {
                    HandleLeftPress();
                }
                else {
                    HandleLeftPressRelease();
                }
            }
            else if (_inputHelper.IsMiddleMouseButton) {
                if (isDown) {
                    HandleMiddlePress();
                }
                else {
                    HandleMiddlePressRelease();
                }
            }
            else if (_inputHelper.IsRightMouseButton) {
                if (isDown) {
                    HandleRightPress();
                }
                else {
                    HandleRightPressRelease();
                }
            }
            else {
                D.Error("{0}.OnPress() without a mouse button found.", GetType().Name);
            }
        }
    }

    void OnPress(bool isDown) {
        PressEventHandler(gameObject, isDown);
    }

    protected virtual void HandleLeftPressRelease() { }
    protected virtual void HandleMiddlePressRelease() { }
    protected virtual void HandleRightPressRelease() {
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            _ctxControl.TryShowContextMenu();
        }
    }

    protected virtual void HandleLeftDoubleClick() { }
    protected virtual void HandleMiddleDoubleClick() { }
    protected virtual void HandleRightDoubleClick() { }

    protected void DoubleClickEventHandler(GameObject go) {
        //D.Log("{0} is handling an OnDoubleClick event.", FullName);
        if (IsDiscernibleToUser) {
            if (_inputHelper.IsLeftMouseButton) {
                HandleLeftDoubleClick();
            }
            else if (_inputHelper.IsMiddleMouseButton) {
                HandleMiddleDoubleClick();
            }
            else if (_inputHelper.IsRightMouseButton) {
                HandleRightDoubleClick();
            }
            else {
                D.Error("{0}.OnDoubleClick() without a mouse button found.", GetType().Name);
            }
        }
    }

    void OnDoubleClick() {
        DoubleClickEventHandler(gameObject);
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
    }

    #endregion

    #region ICameraTargetable Members

    /// <summary>
    /// Indicates whether this instance is currently eligible to be a camera target for zooming, focusing or following.
    /// e.g. - the camera should not react to the object when it is not discernible to the user.
    /// </summary>
    public virtual bool IsCameraTargetEligible { get { return IsDiscernibleToUser; } }

    public float MinimumCameraViewingDistance { get { return Data.CameraStat.MinimumViewingDistance; } }

    #endregion

    #region ICameraFocusable Members

    public float FieldOfView { get { return Data.CameraStat.FieldOfView; } }

    //Note: protected and virtual so FleetCmdItems can override using UnitRadius
    protected float _optimalCameraViewingDistance;
    public virtual float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return (Data.CameraStat as CameraFocusableStat).OptimalViewingDistance;
        }
        set { _optimalCameraViewingDistance = value; }  //TODO public but not currently used until implement option to right click set
        // Camera auto setting value commented out for now
    }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", IsFocusPropChangedHandler); }
    }

    #endregion

    #region IWidgetTrackable Members

    public Vector3 GetOffset(WidgetPlacement placement) {

        float circumRadius = Mathf.Sqrt(2) * Radius / 2F;   // distance to hypotenus of right triangle
        switch (placement) {
            case WidgetPlacement.Above:
                return new Vector3(Constants.ZeroF, Radius, Constants.ZeroF);
            case WidgetPlacement.AboveLeft:
                return new Vector3(-circumRadius, circumRadius, Constants.ZeroF);
            case WidgetPlacement.AboveRight:
                return new Vector3(circumRadius, circumRadius, Constants.ZeroF);
            case WidgetPlacement.Below:
                return new Vector3(Constants.ZeroF, -Radius, Constants.ZeroF);
            case WidgetPlacement.BelowLeft:
                return new Vector3(-circumRadius, -circumRadius, Constants.ZeroF);
            case WidgetPlacement.BelowRight:
                return new Vector3(circumRadius, -circumRadius, Constants.ZeroF);
            case WidgetPlacement.Left:
                return new Vector3(-Radius, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Right:
                return new Vector3(Radius, Constants.ZeroF, Constants.ZeroF);
            case WidgetPlacement.Over:
                return Vector3.zero;
            case WidgetPlacement.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(placement));
        }
    }

    #endregion

    #region IHighlightable Members

    public virtual float HoverHighlightRadius { get { return Radius; } }

    public virtual float HighlightRadius { get { return Radius * Screen.height * 3F; } }

    #endregion

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", IsSelectedPropChangedHandler); }
    }

    #endregion

}

