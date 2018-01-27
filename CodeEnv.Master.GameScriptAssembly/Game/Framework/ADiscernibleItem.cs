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
    /// Returns <c>true</c> if this item can become the selection.
    /// <remarks>9.15.17 Besides all user-owned items, discernible AI-owned Cmds, all systems and all planetoids are selectable.
    /// Planetoids are currently selectable as I want a Debug ability to tell them to die via the context menu.
    /// Discernible AI-owned Cmds are selectable as the user should be able to inspect the known contents of an AI Cmd.
    /// All systems are selectable as I want the user to have the ability of changing a system's name which will propagate
    /// to the star and all the system's planetoids.</remarks>
    /// <remarks>10.22.17 I've now made all discernible items selectable, adding in stars and AI-owned Elements.</remarks>
    /// </summary>
    protected virtual bool IsSelectable { get { return IsDiscernibleToUser; } }

    /// <summary>
    /// Flag indicating whether InitializeOnFirstDiscernibleToUser() has run.
    /// <remarks>OPTIMIZE test for null DisplayMgr instead.</remarks>
    /// </summary>
    private bool __hasInitOnFirstDiscernibleToUserRun;
    private IGameInputHelper _inputHelper;
    private ICtxControl _ctxControl;
    private IDictionary<HighlightMgrID, AHighlightManager> _highlightMgrLookup;

    #region Initialization

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        _inputHelper = GameReferences.InputHelper;
    }

    /// <summary>
    /// Called when the Item first becomes discernible to the user, this method initializes the 
    /// View-related members of this item that are not needed until the item is discernible to the user.
    /// </summary>
    protected virtual void InitializeOnFirstDiscernibleToUser() {
        D.Assert(!__hasInitOnFirstDiscernibleToUserRun);
        D.Assert(IsOperational);
        D.Assert(IsDiscernibleToUser);    // first time change should always be true

        _hoveredHudManager = InitializeHoveredHudManager();

        DisplayMgr = MakeDisplayMgrInstance();
        InitializeDisplayMgr();
        // always start enabled as UserPlayerIntelCoverage must be > None for this method to be called,
        // or, in the case of SystemItem, its members coverage must be > their starting coverage
        DisplayMgr.IsDisplayEnabled = true;

        EffectsMgr = InitializeEffectsManager();
        __hasInitOnFirstDiscernibleToUserRun = true;
        // 1.24.18 Atomically re-AssessIsDiscernibleToUser() here will not change IsDiscernibleToUser
        // as DisplayMgr.IsInMainCameraLOS starts true and remains so until the IsBecameInvisible event is triggered
    }

    protected abstract ItemHoveredHudManager InitializeHoveredHudManager();

    protected abstract ADisplayManager MakeDisplayMgrInstance();

    protected virtual void InitializeDisplayMgr() {
        DisplayMgr.Initialize();
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsInMainCameraLOS, IsInMainCameraLosPropChangedHandler));
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsPrimaryMeshInMainCameraLOS, IsVisualDetailDiscernibleToUserPropChangedHandler));
        //D.Log("{0} has initialized its DisplayMgr in Frame {1}.", DebugName, Time.frameCount);
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
        //D.Log("{0}.ShowCircleHighlights({1}) called during Frame {2}.", DebugName,
        //    circleHighlightIDs.Select(id => id.GetValueName()).Concatenate(), Time.frameCount);
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
    /// Shows the selected Item in the appropriate HUD.
    /// </summary>
    /// <returns></returns>
    protected abstract void ShowSelectedItemHud();

    /// <summary>
    /// Hides the HUD that is showing the selected Item.
    /// </summary>
    protected virtual void HideSelectedItemHud() {
        D.Assert(!IsSelected);
        GameReferences.InteractibleHudWindow.Hide();
    }

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
            if (effectSeqID == EffectSequenceID.Dying) {
                __DelayDyingEffectCompletion(); // 5.12.17 FIXME delay to keep AMortalItem.HandleDeathEffectBegun()
                // from allowing DisplayMgr to try to shutdown icons that have already been destroyed.
            }
        }
    }

    private void __DelayDyingEffectCompletion() {   // 1.13.18 Chgd from hours to seconds to eliminate perpetual delay when paused
        _jobMgr.WaitForGameplaySeconds(1F, "__DyingEffectCompletionDelayJob", (jobWasKilled) => {
            if (!jobWasKilled) {
                HandleEffectSequenceFinished(EffectSequenceID.Dying);
            }
        });
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

    private void IsSelectedPropChangedHandler() {
        HandleIsSelectedChanged();
    }

    private void IsInMainCameraLosPropChangedHandler() {
        HandleIsInMainCameraLosChanged();
    }

    private void IsDiscernibleToUserPropChangedHandler() {
        HandleIsDiscernibleToUserChanged();
    }

    private void IsVisualDetailDiscernibleToUserPropChangedHandler() {
        HandleIsVisualDetailDiscernibleToUserChanged();
    }


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

    void OnClick() {
        ClickEventHandler(gameObject);
    }

    protected void ClickEventHandler(GameObject go) {
        HandleClick();
    }

    void OnPress(bool isDown) {
        PressEventHandler(gameObject, isDown);
    }

    protected void PressEventHandler(GameObject go, bool isDown) {
        HandlePressedChanged(isDown);
    }

    void OnDoubleClick() {
        DoubleClickEventHandler(gameObject);
    }

    protected void DoubleClickEventHandler(GameObject go) {
        HandleDoubleClick();
    }

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

    protected virtual void HandleIsFocusChanged() {
        if (IsFocus) {
            GameReferences.MainCameraControl.CurrentFocus = this;
        }
        AssessCircleHighlighting();
    }

    protected virtual void HandleIsSelectedChanged() {
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = this;
            ShowSelectedItemHud();
        }
        else {
            HideSelectedItemHud();
        }
        AssessCircleHighlighting();
    }

    protected override void HandleOwnerChanging(Player newOwner) {
        if (_ctxControl != null) {
            D.Assert(__hasInitOnFirstDiscernibleToUserRun);
            if (Owner == TempGameValues.NoPlayer || newOwner == TempGameValues.NoPlayer || Owner.IsUser != newOwner.IsUser) {
                // Kind of owner (NoPlayer, AI or User) has changed so generate a new ctxControl -
                // aka, a change from one AI player to another does not necessitate a change

                // 5.5.17 No worries about being paused as owner changes are deferred until no longer paused
                _ctxControl.Dispose();
                _ctxControl = InitializeContextMenu(newOwner);
            }
        }

        if (IsSelected) {
            D.Assert(Owner.IsUser);
        }
    }

    protected override void HandleOwnerChanged() {
        base.HandleOwnerChanged();
        if (IsSelected) {
            SelectionManager.Instance.CurrentSelection = null;
        }
    }

    private void HandleIsInMainCameraLosChanged() {
        AssessIsDiscernibleToUser();
    }

    // IMPROVE deal with losing IsDiscernible while hovered or pressed
    protected virtual void HandleIsDiscernibleToUserChanged() {
        if (!IsDiscernibleToUser && IsHoveredHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHoveredHud(false);
        }
        if (!__hasInitOnFirstDiscernibleToUserRun) {
            InitializeOnFirstDiscernibleToUser();
        }
        AssessCircleHighlighting();
        //D.Log(ShowDebugLog, "{0}.IsDiscernibleToUser changed to {1}.", DebugName, IsDiscernibleToUser);
    }

    protected virtual void HandleIsVisualDetailDiscernibleToUserChanged() { }

    private void HandleHoveredChanged(bool isHovered) {
        ShowHoveredHud(isHovered);
        ShowHoverHighlight(isHovered);
    }

    private void HandleClick() {
        D.Assert(IsDiscernibleToUser);
        //D.Log(ShowDebugLog, "{0} is handling an OnClick event.", DebugName);
        if (_inputHelper.IsLeftMouseButton) {
            if (_inputHelper.IsAnyKeyHeldDown(KeyCode.LeftAlt, KeyCode.RightAlt)) {
                HandleAltLeftClick();
            }
            else if (_inputHelper.IsAnyKeyHeldDown(KeyCode.LeftControl, KeyCode.RightControl)) {
                HandleCntlLeftClick();
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
            D.Error("{0}.OnClick() without a mouse button found.", DebugName);
        }
    }

    private void HandleLeftClick() {
        if (IsSelectable) {
            IsSelected = true;
        }
    }
    protected virtual void HandleCntlLeftClick() { }
    protected virtual void HandleAltLeftClick() { }
    protected virtual void HandleMiddleClick() { IsFocus = true; }
    protected virtual void HandleRightClick() { }

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
            //D.Log(ShowDebugLog, "{0}.HandleRightPressRelease called.", DebugName);
            // right press release while not dragging means both press and release were over this object
            if (_ctxControl == null) {
                D.Assert(__hasInitOnFirstDiscernibleToUserRun);
                _ctxControl = InitializeContextMenu(Owner);
            }
            _ctxControl.AttemptShowContextMenu();
        }
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

    #region Cleanup

    protected override void Cleanup() {
        base.Cleanup();
        if (_ctxControl != null) {
            _ctxControl.Dispose();
        }
        if (EffectsMgr != null) {
            EffectsMgr.Dispose();
        }
        if (DisplayMgr != null) {
            DisplayMgr.Dispose();
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

        public string DebugName { get { return GetType().Name; } }

        public static readonly HighlightMgrIDEqualityComparer Default = new HighlightMgrIDEqualityComparer();

        public override string ToString() {
            return DebugName;
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
    /// <remarks>8.5.17 Generally, the camera should not react to the object when it is not discernible to the user.
    /// However, with the introduction of the ability to focus on a user-owned Item whose Icon representation is clicked
    /// in the Gui, this was expanded to include objects owned by the user.</remarks> 
    /// </summary>
    public bool IsCameraTargetEligible { get { return IsDiscernibleToUser || Owner.IsUser; } }

    public float MinimumCameraViewingDistance { get { return CameraStat.MinimumViewingDistance; } }

    #endregion

    #region ICameraFocusable Members

    public float FieldOfView { get { return CameraStat.FieldOfView; } }

    //Note: protected and virtual so UnitCmdItems can override using UnitRadius
    protected float _optimalCameraViewingDistance;
    public virtual float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance != Constants.ZeroF) {
                // the user has set the value manually via the context menu
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
    // <summary>
    // Indicates whether this is the currently selected Item.
    // <remarks>Usage: An Item's selection is initiated by setting this property to true which will automatically populate
    // SelectionManager's CurrentSelection property. An Item's deselection is initiated by assigning SelectionManager's 
    // CurrentSelection property to some other value, including null. DONOT directly set IsSelected to false.</remarks>
    // </summary>
    public bool IsSelected {
        get { return _isSelected; }
        set {
            if (value) { // Cmds are only selectable when discernible to user. No rqmt to be selectable when losing selection
                D.Assert(IsSelectable);
            }
            SetProperty<bool>(ref _isSelected, value, "IsSelected", IsSelectedPropChangedHandler);
        }
    }

    #endregion

}

