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
public abstract class ADiscernibleItem : AItem, IDiscernibleItem, ICameraFocusable, IHighlightable, IEffectsClient {

    private bool _isDiscernibleToUser;
    public bool IsDiscernibleToUser {
        get { return _isDiscernibleToUser; }
        protected set { SetProperty<bool>(ref _isDiscernibleToUser, value, "IsDiscernibleToUser", OnIsDiscernibleToUserChanged); }
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
    protected bool _isViewMembersInitialized;

    private IGameInputHelper _inputHelper;
    private IHighlighter _highlighter;

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
        D.Assert(IsOperational, "{0}.InitializeOnFirstDiscernibleToUser() called when not operational.", FullName);
        _hudManager = InitializeHudManager();

        DisplayMgr = InitializeDisplayManager();
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsInMainCameraLOS, OnIsInMainCameraLosChanged));
        _subscriptions.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.IsPrimaryMeshInMainCameraLOS, OnIsVisualDetailDiscernibleToUserChanged));
        // always start enabled as UserPlayerIntelCoverage must be > None for this method to be called,
        // or, in the case of SystemItem, its members coverage must be > their starting coverage
        DisplayMgr.EnableDisplay(true);

        EffectsMgr = InitializeEffectsManager();
        _highlighter = InitializeHighlighter();
    }

    protected abstract ItemHudManager InitializeHudManager();

    protected abstract ADisplayManager InitializeDisplayManager();

    protected virtual EffectsManager InitializeEffectsManager() {
        return new EffectsManager(this);
    }

    protected virtual IHighlighter InitializeHighlighter() {
        return new Highlighter(this);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessIsDiscernibleToUser();
    }

    #endregion

    #region View Methods


    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = this;
        }
        AssessHighlighting();
    }

    protected virtual void OnIsInMainCameraLosChanged() {
        AssessIsDiscernibleToUser();
    }

    protected virtual void OnIsDiscernibleToUserChanged() {
        if (!IsDiscernibleToUser && IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_isViewMembersInitialized) {
            D.Assert(IsDiscernibleToUser);    // first time change should always be true
            InitializeOnFirstDiscernibleToUser();
            _isViewMembersInitialized = true;
        }
        AssessHighlighting();
        //D.Log("{0}.IsDiscernibleToUser changed to {1}.", FullName, IsDiscernibleToUser);
    }

    protected virtual void OnIsVisualDetailDiscernibleToUserChanged() { }

    /// <summary>
    /// Assesses the discernability of this item to the user.
    /// </summary>
    protected abstract void AssessIsDiscernibleToUser();

    public virtual void AssessHighlighting() {
        if (IsDiscernibleToUser && IsFocus) {
            ShowHighlights(HighlightID.Focused);
            return;
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

    public virtual void OnEffectFinished(EffectID effectID) { }

    protected void StartEffect(EffectID effectID) {
        if (IsVisualDetailDiscernibleToUser) {
            D.Assert(EffectsMgr != null);   // if DisplayMgr is initialized, so is EffectsMgr
            EffectsMgr.StartEffect(effectID);
        }
        else {
            // Not going to show the effect. Complete the handshake so any dependancies can continue
            OnEffectFinished(effectID);
        }
    }

    protected void StopEffect(EffectID effectID) {
        if (EffectsMgr != null) {
            EffectsMgr.StopEffect(effectID);
        }
        // if EffectsMgr never initialized, then caller of StartEffect already got its OnEffectFinished callback
    }

    #endregion

    #region Events

    protected virtual void OnHover(bool isOver) {
        //D.Log("{0}.OnHover({1}) called.", FullName, isOver);
        if (IsDiscernibleToUser && isOver) {
            ShowHud(true);
            ShowHoverHighlight(true);
            return;
        }
        ShowHud(false);
        ShowHoverHighlight(false);
    }

    protected virtual void OnClick() {
        //D.Log("{0}.OnClick() called.", FullName);
        if (IsDiscernibleToUser) {
            if (_inputHelper.IsLeftMouseButton) {
                KeyCode notUsed;
                if (_inputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    OnAltLeftClick();
                }
                else {
                    OnLeftClick();
                }
            }
            else if (_inputHelper.IsMiddleMouseButton) {
                OnMiddleClick();
            }
            else if (_inputHelper.IsRightMouseButton) {
                OnRightClick();
            }
            else {
                D.Error("{0}.OnClick() without a mouse button found.", GetType().Name);
            }
        }
    }

    protected virtual void OnLeftClick() { }

    protected virtual void OnAltLeftClick() { }

    protected virtual void OnMiddleClick() { IsFocus = true; }

    protected virtual void OnRightClick() { }

    protected virtual void OnDoubleClick() {
        if (IsDiscernibleToUser && _inputHelper.IsLeftMouseButton) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    protected virtual void OnPress(bool isDown) {
        if (IsDiscernibleToUser && _inputHelper.IsRightMouseButton) {
            OnRightPress(isDown);
        }
    }

    protected virtual void OnRightPress(bool isDown) { }

    #endregion

    #region Cleanup

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
            return Data.CameraStat.OptimalViewingDistance;
        }
        set { _optimalCameraViewingDistance = value; }  // TODO public but not currently used until implement option to right click set
        // Camera auto setting value commented out for now
    }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
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

    public virtual float HoverHighlightRadius { get { return Radius * 2F; } }

    public virtual float HighlightRadius { get { return Radius * Screen.height * 3F; } }

    #endregion

}

