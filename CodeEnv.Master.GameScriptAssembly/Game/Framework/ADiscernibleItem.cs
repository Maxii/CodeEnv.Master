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
/// Abstract class for Items that can change whether they are discernible by the UserPlayer.
/// </summary>
public abstract class ADiscernibleItem : AItem, ICameraFocusable, IWidgetTrackable, IEffectsMgrClient, ISelectable {

    public event EventHandler<EffectSeqEventArgs> effectSeqStarting;
    public event EventHandler<EffectSeqEventArgs> effectSeqFinished;

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
        get {
            bool result = false;
            if (IsDiscernibleToUser) {  // effectively IsDiscernibleToUser && IsPrimaryMeshInMainCameraLOS
                D.AssertNotNull(DisplayMgr);
                result = DisplayMgr.IsPrimaryMeshInMainCameraLOS;
            }
            //D.Log(ShowDebugLog, "{0}.IsVisualDetailDiscernibleToUser = {1}. IsDiscernible = {2}.", DebugName, result, IsDiscernibleToUser);
            return result;
        }
    }

    public AItemCameraStat CameraStat { protected get; set; }

    private ADisplayManager _displayMgr;
    protected ADisplayManager DisplayMgr {
        get { return _displayMgr; }
        private set { SetProperty<ADisplayManager>(ref _displayMgr, value, "DisplayMgr"); }
    }

    private EffectsManager _effectsMgr;
    protected EffectsManager EffectsMgr {
        get { return _effectsMgr; }
        private set { SetProperty<EffectsManager>(ref _effectsMgr, value, "EffectsMgr"); }
    }

    /// <summary>
    /// Flag indicating whether InitializeOnFirstDiscernibleToUser() has run.
    /// </summary>
    private bool _hasInitOnFirstDiscernibleToUserRun;
    private IGameInputHelper _inputHelper;
    private ICtxControl _ctxControl;
    private IDictionary<HighlightMgrID, AHighlightManager> _highlightMgrLookup;

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
        D.Assert(IsOperational);
        _hudManager = InitializeHudManager();

        DisplayMgr = MakeDisplayManagerInstance();
        InitializeDisplayManager();
        // always start enabled as UserPlayerIntelCoverage must be > None for this method to be called,
        // or, in the case of SystemItem, its members coverage must be > their starting coverage
        DisplayMgr.IsDisplayEnabled = true;

        EffectsMgr = InitializeEffectsManager();
        _hasInitOnFirstDiscernibleToUserRun = true;
    }

    protected abstract ItemHudManager InitializeHudManager();

    protected abstract ADisplayManager MakeDisplayManagerInstance();

    protected virtual void InitializeDisplayManager() {
        DisplayMgr.Initialize();
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsInMainCameraLOS, IsInMainCameraLosPropChangedHandler));
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsPrimaryMeshInMainCameraLOS, IsVisualDetailDiscernibleToUserPropChangedHandler));
    }

    protected virtual EffectsManager InitializeEffectsManager() {
        return new EffectsManager(this);
    }

    /// <summary>
    /// Initializes the context menu. Called when the ContextMenu for this
    /// item is first used and/or when the owner changes.
    /// </summary>
    /// <param name="owner">The owner.</param>
    /// <returns></returns>
    protected abstract ICtxControl InitializeContextMenu(Player owner);

    protected abstract CircleHighlightManager InitializeCircleHighlightMgr();

    protected abstract HoverHighlightManager InitializeHoverHighlightMgr();

    protected virtual SectorViewHighlightManager InitializeSectorViewHighlightMgr() {
        throw new NotSupportedException();
    }

    #endregion

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessIsDiscernibleToUser();
    }

    /// <summary>
    /// Assesses the discernibility of this item to the user.
    /// </summary>
    protected abstract void AssessIsDiscernibleToUser();

    protected bool DoesHighlightMgrExist(HighlightMgrID mgrID) {
        return _highlightMgrLookup != null && _highlightMgrLookup.ContainsKey(mgrID);
    }

    protected AHighlightManager GetHighlightMgr(HighlightMgrID mgrID) {
        if (_highlightMgrLookup == null) {
            _highlightMgrLookup = new Dictionary<HighlightMgrID, AHighlightManager>(3, HighlightMgrIDEqualityComparer.Default);
        }
        AHighlightManager highlightMgr;
        if (!_highlightMgrLookup.TryGetValue(mgrID, out highlightMgr)) {
            switch (mgrID) {
                case HighlightMgrID.Circles:
                    highlightMgr = InitializeCircleHighlightMgr();
                    break;
                case HighlightMgrID.Hover:
                    highlightMgr = InitializeHoverHighlightMgr();
                    break;
                case HighlightMgrID.SectorView:
                    highlightMgr = InitializeSectorViewHighlightMgr();
                    break;
                case HighlightMgrID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(mgrID));
            }
            _highlightMgrLookup.Add(mgrID, highlightMgr);
        }
        return highlightMgr;
    }

    public virtual void AssessCircleHighlighting() {
        if (IsDiscernibleToUser) {
            if (IsFocus) {
                if (IsSelected) {
                    ShowCircleHighlights(CircleHighlightID.Focused, CircleHighlightID.Selected);
                    return;
                }
                ShowCircleHighlights(CircleHighlightID.Focused);
                return;
            }
            if (IsSelected) {
                ShowCircleHighlights(CircleHighlightID.Selected);
                return;
            }
        }
        ShowCircleHighlights(CircleHighlightID.None);
    }

    protected void ShowCircleHighlights(params CircleHighlightID[] circleHighlightIDs) {
        var circleHighlightMgr = GetHighlightMgr(HighlightMgrID.Circles) as CircleHighlightManager;
        if (circleHighlightIDs.Contains(CircleHighlightID.None)) {
            circleHighlightMgr.Show(false);
            return;
        }
        circleHighlightMgr.SetCirclesToShow(circleHighlightIDs);
        circleHighlightMgr.Show(true);
    }

    protected void ShowHoverHighlight(bool toShow) {
        D.Assert(IsDiscernibleToUser);
        var hoverHighlightMgr = GetHighlightMgr(HighlightMgrID.Hover);
        if (hoverHighlightMgr.IsHighlightShowing == toShow) {
            // Ngui sometimes sends two OnHover(false?) in a row
            return;
        }
        hoverHighlightMgr.Show(toShow);
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

    /// <summary>
    /// Allows derived classes to take action after the finish of an effect.
    /// This base method fires the onEffectFinished event.
    /// </summary>
    /// <param name="effectSeqID">The effect identifier.</param>
    public virtual void HandleEffectSequenceFinished(EffectSequenceID effectSeqID) {
        OnEffectSeqFinished(effectSeqID);
    }

    protected void StartEffectSequence(EffectSequenceID effectSeqID) {
        OnEffectSeqStarting(effectSeqID);
        //D.Log(ShowDebugLog, "{0} attempting to start {1} effect.", DebugName, effectSeqID.GetValueName());
        if (IsVisualDetailDiscernibleToUser) {
            //D.Log(ShowDebugLog, "{0} visual detail is discernible so starting {1} effect.", DebugName, effectSeqID.GetValueName());
            D.AssertNotNull(EffectsMgr);   // if DisplayMgr is initialized, so is EffectsMgr
            EffectsMgr.StartEffect(effectSeqID);
        }
        else {
            // Not going to show the effect. Complete the handshake so any dependencies can continue
            HandleEffectSequenceFinished(effectSeqID);
        }
    }

    protected void StopEffectSequence(EffectSequenceID effectSeqID) {
        if (EffectsMgr != null) {
            EffectsMgr.StopEffect(effectSeqID);
        }
        // if EffectsMgr never initialized, then caller of StartEffect already got its HandleEffectFinished callback
    }

    #region Event and Property Change Handlers

    private void IsFocusPropChangedHandler() {
        HandleIsFocusChanged();
    }

    protected virtual void HandleIsFocusChanged() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = this;
        }
        AssessCircleHighlighting();
    }

    private void IsSelectedPropChangedHandler() {
        HandleIsSelectedChanged();
    }

    protected virtual void HandleIsSelectedChanged() {
        if (IsSelected) {
            ShowSelectedItemHud();
            SelectionManager.Instance.CurrentSelection = this;
        }
        AssessCircleHighlighting();
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        base.HandleOwnerChanging(newOwner);
        if (_ctxControl != null) {
            D.Assert(_hasInitOnFirstDiscernibleToUserRun);
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsUser != newOwner.IsUser) {
                // Kind of owner (NoPlayer, AI or User) has changed so generate a new ctxControl -
                // aka, a change from one AI player to another does not necessitate a change
                (_ctxControl as IDisposable).Dispose();
                _ctxControl = InitializeContextMenu(newOwner);
            }
        }
    }

    private void IsInMainCameraLosPropChangedHandler() {
        HandleIsInMainCameraLosChanged();
    }

    protected virtual void HandleIsInMainCameraLosChanged() {
        AssessIsDiscernibleToUser();
    }

    private void IsDiscernibleToUserPropChangedHandler() {
        HandleIsDiscernibleToUserChanged();
    }

    protected virtual void HandleIsDiscernibleToUserChanged() {
        if (!IsDiscernibleToUser && IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_hasInitOnFirstDiscernibleToUserRun) {
            D.Assert(IsDiscernibleToUser);    // first time change should always be true
            InitializeOnFirstDiscernibleToUser();
        }
        AssessCircleHighlighting();
        //D.Log(ShowDebugLog, "{0}.IsDiscernibleToUser changed to {1}.", DebugName, IsDiscernibleToUser);
    }
    // IMPROVE deal with losing IsDiscernible while hovered or pressed

    private void IsVisualDetailDiscernibleToUserPropChangedHandler() {
        HandleIsVisualDetailDiscernibleToUserChanged();
    }

    protected virtual void HandleIsVisualDetailDiscernibleToUserChanged() { }

    void OnHover(bool isOver) {
        //D.Log(ShowDebugLog, "{0}.OnHover({1}) called at {2}.", DebugName, isOver, Utility.TimeStamp);
        HoverEventHandler(gameObject, isOver);
    }

    protected void HoverEventHandler(GameObject go, bool isOver) {
        //D.Log(ShowDebugLog, "{0} is handling an OnHover event. IsOver = {1}.", DebugName, isOver);
        if (IsOperational && IsDiscernibleToUser) {
            HandleHoveredChanged(isOver);
        }
    }

    private void HandleHoveredChanged(bool isHovered) {
        ShowHud(isHovered);
        ShowHoverHighlight(isHovered);
    }

    void OnClick() {
        ClickEventHandler(gameObject);
    }

    protected void ClickEventHandler(GameObject go) {
        HandleClick();
    }

    private void HandleClick() {
        //D.Log(ShowDebugLog, "{0} is handling an OnClick event.", DebugName);
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

    protected virtual void HandleLeftClick() {
        IsSelected = true;
    }

    protected virtual void HandleAltLeftClick() { }

    protected virtual void HandleMiddleClick() {
        IsFocus = true;
    }

    protected virtual void HandleRightClick() { }

    void OnPress(bool isDown) {
        PressEventHandler(gameObject, isDown);
    }

    protected void PressEventHandler(GameObject go, bool isDown) {
        HandlePressedChanged(isDown);
    }

    private void HandlePressedChanged(bool isPressed) {
        //D.Log(ShowDebugLog, "{0} is handling an OnPress event. IsDown = {1}.", DebugName, isPressed);
        if (IsDiscernibleToUser) {
            if (_inputHelper.IsLeftMouseButton) {
                if (isPressed) {
                    HandleLeftPress();
                }
                else {
                    HandleLeftPressRelease();
                }
            }
            else if (_inputHelper.IsMiddleMouseButton) {
                if (isPressed) {
                    HandleMiddlePress();
                }
                else {
                    HandleMiddlePressRelease();
                }
            }
            else if (_inputHelper.IsRightMouseButton) {
                if (isPressed) {
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

    protected virtual void HandleLeftPress() { }
    protected virtual void HandleMiddlePress() { }
    protected virtual void HandleRightPress() { }
    protected virtual void HandleLeftPressRelease() { }
    protected virtual void HandleMiddlePressRelease() { }
    protected virtual void HandleRightPressRelease() {
        if (!_inputMgr.IsDragging) {
            // right press release while not dragging means both press and release were over this object
            if (_ctxControl == null) {
                D.Assert(_hasInitOnFirstDiscernibleToUserRun);
                _ctxControl = InitializeContextMenu(Owner);
            }
            _ctxControl.AttemptShowContextMenu();
        }
    }

    void OnDoubleClick() {
        DoubleClickEventHandler(gameObject);
    }

    protected void DoubleClickEventHandler(GameObject go) {
        HandleDoubleClick();
    }

    private void HandleDoubleClick() {
        //D.Log(ShowDebugLog, "{0} is handling an OnDoubleClick event.", DebugName);
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

    protected virtual void HandleLeftDoubleClick() { }
    protected virtual void HandleMiddleDoubleClick() { }
    protected virtual void HandleRightDoubleClick() { }

    private void OnEffectSeqStarting(EffectSequenceID effectSeqID) {
        if (effectSeqStarting != null) {
            effectSeqStarting(this, new EffectSeqEventArgs(effectSeqID));
        }
    }

    protected void OnEffectSeqFinished(EffectSequenceID effectSeqID) {
        if (effectSeqFinished != null) {
            effectSeqFinished(this, new EffectSeqEventArgs(effectSeqID));
        }
    }

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            (_ctxControl as IDisposable).Dispose();
        }
        if (EffectsMgr != null) {
            EffectsMgr.Dispose();
        }
        CleanupHighlights();
    }

    private void CleanupHighlights() {
        var highlightMgrIDs = Enums<HighlightMgrID>.GetValues(excludeDefault: true);
        foreach (var mgrID in highlightMgrIDs) {
            if (DoesHighlightMgrExist(mgrID)) {
                var highlightMgr = GetHighlightMgr(mgrID);
                highlightMgr.Dispose();
            }
        }
    }

    #endregion

    #region Nested Classes

    public class EffectSeqEventArgs : EventArgs {

        public EffectSequenceID EffectSeqID { get; private set; }

        public EffectSeqEventArgs(EffectSequenceID effectSeqID) {
            EffectSeqID = effectSeqID;
        }
    }

    protected enum HighlightMgrID {
        None,
        Circles,
        Hover,
        SectorView
    }

    /// <summary>
    /// IEqualityComparer for HighlightMgrID. 
    /// <remarks>For use when HighlightMgrID is used as a Dictionary key as it avoids boxing from use of object.Equals.</remarks>
    /// </summary>
    private class HighlightMgrIDEqualityComparer : IEqualityComparer<HighlightMgrID> {

        public static readonly HighlightMgrIDEqualityComparer Default = new HighlightMgrIDEqualityComparer();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IEqualityComparer<HighlightMgrID> Members

        public bool Equals(HighlightMgrID value1, HighlightMgrID value2) {
            return value1 == value2;
        }

        public int GetHashCode(HighlightMgrID value) {
            return value.GetHashCode();
        }

    }

    #endregion

    #endregion

    #region Debug

    #endregion

    #region ICameraTargetable Members

    /// <summary>
    /// Indicates whether this instance is currently eligible to be a camera target for zooming, focusing or following.
    /// e.g. - the camera should not react to the object when it is not discernible to the user.
    /// </summary>
    public virtual bool IsCameraTargetEligible { get { return IsDiscernibleToUser; } }

    public float MinimumCameraViewingDistance { get { return CameraStat.MinimumViewingDistance; } }

    #endregion

    #region ICameraFocusable Members

    public float FieldOfView { get { return CameraStat.FieldOfView; } }

    //Note: protected and virtual so FleetCmdItems can override using UnitRadius
    protected float _optimalCameraViewingDistance;
    public virtual float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually
                return _optimalCameraViewingDistance;
            }
            return (CameraStat as FocusableItemCameraStat).OptimalViewingDistance;
        }
        set { SetProperty<float>(ref _optimalCameraViewingDistance, value, "OptimalCameraViewingDistance"); }
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

        float circumRadius = Mathf.Sqrt(2) * Radius / 2F;   // distance to hypotenuse of right triangle
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

    #region ISelectable Members

    private bool _isSelected;
    public bool IsSelected {
        get { return _isSelected; }
        set { SetProperty<bool>(ref _isSelected, value, "IsSelected", IsSelectedPropChangedHandler); }
    }

    #endregion

}

