// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADiscernibleItem.cs
// Abstract class for Items that can change whether they are discernible by the HumanPlayer.
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
/// Abstract class for Items that can change whether they are discernible by the HumanPlayer.
/// </summary>
public abstract class ADiscernibleItem : AItem, ICameraFocusable, IHighlightable {

    private bool _isDiscernible;
    public bool IsDiscernible {
        get { return _isDiscernible; }
        protected set { SetProperty<bool>(ref _isDiscernible, value, "IsDiscernible", OnIsDiscernibleChanged); }
    }

    protected ADisplayManager DisplayMgr { get; private set; }

    protected IGameManager _gameMgr;
    protected bool _isViewMembersOnDiscernibleInitialized;

    private IGameInputHelper _inputHelper;
    private IHighlighter _highlighter;

    #region Initialization

    /// <summary>
    /// Called from Awake, initializes local references and values including Radius-related components.
    /// </summary>
    protected override void InitializeLocalReferencesAndValues() {
        base.InitializeLocalReferencesAndValues();
        _inputHelper = References.InputHelper;
        _gameMgr = References.GameManager;
    }

    /// <summary>
    /// Called from Start, initializes View-related members of this item 
    /// that can't wait until the Item first becomes discernible. 
    /// </summary>
    /// <remarks> 
    /// Overrides AItem.InitializeViewMembers() without calling base method as AItem
    /// initializes the HudManager in the base method. Discernible Items wish to defer this
    /// initialization until first discernible, aka in InitializeViewMembersOnDiscernible()..
    /// </remarks>
    protected override void InitializeViewMembers() { }

    /// <summary>
    /// Called when the Item first becomes discernible to the player, this method initializes the 
    /// View-related members of this item that are not needed until discernible.
    /// </summary>
    protected virtual void InitializeViewMembersOnDiscernible() {
        //D.Log("{0}.InitializeViewMembersOnDiscernible() called.", FullName);
        _hudManager = InitializeHudManager();

        DisplayMgr = InitializeDisplayManager();
        _subscribers.Add(DisplayMgr.SubscribeToPropertyChanged(dm => dm.InCameraLOS, OnInCameraLOSChanged));
        // always start enabled as HumanPlayerIntelCoverage must be > None for this method to be called,
        // or, in the case of SystemItem, its members coverage must be > their starting coverage
        DisplayMgr.IsDisplayEnabled = true;

        _highlighter = InitializeHighlighter();
    }

    protected abstract ADisplayManager InitializeDisplayManager();

    protected virtual IHighlighter InitializeHighlighter() {
        return new Highlighter(this);
    }

    #endregion

    #region Model Methods

    public override void CommenceOperations() {
        base.CommenceOperations();
        AssessDiscernability();
    }

    #endregion

    #region View Methods

    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            References.MainCameraControl.CurrentFocus = this;
        }
        AssessHighlighting();
    }

    protected virtual void OnInCameraLOSChanged() {
        AssessDiscernability();
    }

    protected virtual void OnIsDiscernibleChanged() {
        if (!IsDiscernible && IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_isViewMembersOnDiscernibleInitialized) {
            D.Assert(IsDiscernible);    // first time change should always be to true
            InitializeViewMembersOnDiscernible();
            _isViewMembersOnDiscernibleInitialized = true;
        }
        AssessHighlighting();
        //D.Log("{0}.IsDiscernible changed to {1}.", FullName, IsDiscernible);
    }

    /// <summary>
    /// Assesses the discernability of this item. 
    /// </summary>
    protected abstract void AssessDiscernability();

    public virtual void AssessHighlighting() {
        if (IsDiscernible && IsFocus) {
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

    #endregion

    #region Mouse Events

    protected virtual void OnHover(bool isOver) {
        //D.Log("{0}.OnHover({1}) called.", FullName, isOver);
        if (IsDiscernible && isOver) {
            ShowHud(true);
            ShowHoverHighlight(true);
            return;
        }
        ShowHud(false);
        ShowHoverHighlight(false);
    }

    protected virtual void OnClick() {
        //D.Log("{0}.OnClick() called.", FullName);
        if (IsDiscernible) {
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
        if (IsDiscernible && _inputHelper.IsLeftMouseButton) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    protected virtual void OnPress(bool isDown) {
        if (IsDiscernible && _inputHelper.IsRightMouseButton) {
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
    /// e.g. - the camera should not know the object exists when it is not discernible to the human player.
    /// </summary>
    public virtual bool IsCameraTargetEligible { get { return IsDiscernible; } }

    public abstract float MinimumCameraViewingDistance { get; }

    #endregion

    #region ICameraFocusable Members

    public abstract float OptimalCameraViewingDistance { get; }

    public virtual bool IsRetainedFocusEligible { get { return false; } }

    private bool _isFocus;
    public virtual bool IsFocus {
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

