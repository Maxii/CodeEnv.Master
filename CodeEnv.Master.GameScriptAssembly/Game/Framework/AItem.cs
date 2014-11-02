// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
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
public abstract class AItem : AMonoBase, IItem, IDestinationTarget, ICameraFocusable, IWidgetTrackable, IDisposable {

    private AItemData _data;
    public AItemData Data {
        get { return _data; }
        set {
            if (_data != null) { throw new MethodAccessException("{0}.{1}.Data can only be set once.".Inject(FullName, GetType().Name)); }
            _data = value;
            _data.Transform = _transform;
            SubscribeToDataValueChanges();
        }
    }

    private bool _inCameraLOS = true;
    /// <summary>
    /// Indicates whether this item is within a Camera's Line Of Sight.
    /// Note: All items start out thinking they are in a camera's LOS. This is so IsDiscernible will properly operate
    /// during the period when a item's visual members have not yet been initialized. If and when they are
    /// initialized, the item will be notified by their CameraLosChangedListener of their actual InCameraLOS state.
    /// </summary>
    protected bool InCameraLOS {
        get { return _inCameraLOS; }
        set { SetProperty<bool>(ref _inCameraLOS, value, "InCameraLOS", OnInCameraLOSChanged); }
    }

    public IIntel PlayerIntel { get; private set; }

    public IGuiHudPublisher HudPublisher { get; private set; }

    private bool _isDiscernible;
    public bool IsDiscernible {
        get { return _isDiscernible; }
        protected set { SetProperty<bool>(ref _isDiscernible, value, "IsDiscernible", OnIsDiscernibleChanged); }
    }

    /// <summary>
    /// Property that allows each derived class to establish the size of the sphericalHighlight
    /// relative to the class's radius.
    /// </summary>
    protected virtual float SphericalHighlightSizeMultiplier { get { return 2F; } }

    public float circleScaleFactor = 3.0F;

    protected IList<IDisposable> _subscribers;
    protected bool _isCirclesRadiusDynamic = true;

    private HighlightCircle _circles;
    protected bool _isViewMembersOnDiscernibleInitialized;

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
        PlayerIntel = InitializePlayerIntel();
    }

    /// <summary>
    /// Derived classes should override this if they have a different type of IIntel than <see cref="Intel"/>.
    /// </summary>
    /// <returns></returns>
    protected virtual IIntel InitializePlayerIntel() { return new Intel(); }

    protected virtual void Subscribe() {
        _subscribers = new List<IDisposable>();
        SubscribeToPlayerIntelCoverageChanged();
        // Subscriptions to data value changes should be done with SubscribeToDataValueChanges()
    }

    protected virtual void SubscribeToPlayerIntelCoverageChanged() {
        _subscribers.Add((PlayerIntel as Intel).SubscribeToPropertyChanged<Intel, IntelCoverage>(pi => pi.CurrentCoverage, OnPlayerIntelCoverageChanged));
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
    /// Called from Start, initializes View-related members of this item.
    /// </summary>
    protected virtual void InitializeViewMembers() {
        HudPublisher = InitializeHudPublisher();
    }

    protected abstract IGuiHudPublisher InitializeHudPublisher();

    /// <summary>
    /// Called when the Item first becomes discernible to the player, this method initializes the 
    /// View-related members of this item that are not needed until discernible.
    /// </summary>
    protected abstract void InitializeViewMembersOnDiscernible();

    /// <summary>
    /// Subscribes to changes to values contained in Data. 
    /// </summary>
    protected virtual void SubscribeToDataValueChanges() {
        D.Assert(_subscribers != null);
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.Name, OnNamingChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, string>(d => d.ParentName, OnNamingChanged));
        _subscribers.Add(Data.SubscribeToPropertyChanging<AItemData, IPlayer>(d => d.Owner, OnOwnerChanging));
        _subscribers.Add(Data.SubscribeToPropertyChanged<AItemData, IPlayer>(d => d.Owner, OnOwnerChanged));
    }


    #endregion

    #region Model Methods

    /// <summary>
    /// Called when either the Item name or parentName is changed.
    /// </summary>
    protected virtual void OnNamingChanged() { }

    protected virtual void OnOwnerChanging(IPlayer newOwner) { }

    protected virtual void OnOwnerChanged() {
        if (onOwnerChanged != null) {
            onOwnerChanged(this);
        }
    }

    #endregion

    #region View Methods

    protected virtual void OnPlayerIntelCoverageChanged() {
        AssessDiscernability();
        if (HudPublisher.IsHudShowing) {
            // refresh the HUD as IntelCoverage has changed
            ShowHud(true);
        }
    }

    protected virtual void OnIsFocusChanged() {
        if (IsFocus) {
            References.CameraControl.CurrentFocus = this;
        }
        AssessHighlighting();
    }


    protected virtual void OnInCameraLOSChanged() {
        AssessDiscernability();
    }

    protected virtual void OnIsDiscernibleChanged() {
        if (!IsDiscernible && HudPublisher.IsHudShowing) {
            // lost ability to discern this object while showing the HUD so stop showing
            ShowHud(false);
        }
        if (!_isViewMembersOnDiscernibleInitialized) {
            D.Assert(IsDiscernible);    // first time change should always be to true
            InitializeViewMembersOnDiscernible();
            _isViewMembersOnDiscernibleInitialized = true;
        }
        AssessHighlighting();
    }

    public virtual void AssessDiscernability() {
        IsDiscernible = InCameraLOS && PlayerIntel.CurrentCoverage != IntelCoverage.None;
    }

    public void ShowHud(bool toShow) {
        HudPublisher.ShowHud(toShow, PlayerIntel, _transform.position);
        D.Log("{0}.ShowHud({1}) called.", FullName, toShow);
    }

    public virtual void AssessHighlighting() {
        if (!IsDiscernible) {
            Highlight(Highlights.None);
            return;
        }
        if (IsFocus) {
            Highlight(Highlights.Focused);
            return;
        }
        Highlight(Highlights.None);
    }

    protected virtual void Highlight(Highlights highlight) {
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(true, Highlights.Focused);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Focused);
                break;
            case Highlights.Selected:
            case Highlights.SelectedAndFocus:
            case Highlights.General:
            case Highlights.FocusAndGeneral:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    /// <summary>
    /// Shows or hides the highlighting circles around this item. Derived classes should override
    /// this if they wish to have the circles track a different transform besides the transform associated 
    /// with this item.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to show].</param>
    /// <param name="highlight">The highlight.</param>
    protected virtual void ShowCircle(bool toShow, Highlights highlight) {
        ShowCircle(toShow, highlight, _transform);
    }

    /// <summary>
    /// Shows or hides highlighting circles.
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [automatic show].</param>
    /// <param name="highlight">The highlight.</param>
    /// <param name="transform">The transform the circles should track.</param>
    protected void ShowCircle(bool toShow, Highlights highlight, Transform transform) {
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = CalcNormalizedCircleRadius();
            string circlesTitle = "{0} Circle".Inject(gameObject.name);
            _circles = new HighlightCircle(circlesTitle, transform, normalizedRadius, _isCirclesRadiusDynamic, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        //string showHide = toShow ? "showing" : "not showing";
        //D.Log("{0} {1} circle {2}.", gameObject.name, showHide, highlight.GetName());
        _circles.Show(toShow, (int)highlight);
    }

    private void ShowSphericalHighlight(bool toShow) {
        var sphericalHighlight = References.SphericalHighlight;
        if (sphericalHighlight != null) {  // allows deactivation of the SphericalHighlight gameObject
            if (toShow) {
                sphericalHighlight.SetTarget(this, Radius * SphericalHighlightSizeMultiplier);
            }
            sphericalHighlight.Show(toShow);
        }
    }

    protected virtual float CalcNormalizedCircleRadius() {
        return Screen.height * circleScaleFactor * Radius;
    }

    #endregion

    #region Mouse Events

    protected virtual void OnHover(bool isOver) {
        D.Log("{0}.OnHover({1}) called.", FullName, isOver);
        if (IsDiscernible && isOver) {
            ShowHud(true);
            ShowSphericalHighlight(true);
            return;
        }
        ShowHud(false);
        ShowSphericalHighlight(false);
    }

    protected virtual void OnClick() {
        D.Log("{0}.OnClick() called.", FullName);
        if (IsDiscernible) {
            var inputHelper = References.InputHelper;
            if (inputHelper.IsLeftMouseButton()) {
                KeyCode notUsed;
                if (inputHelper.TryIsKeyHeldDown(out notUsed, KeyCode.LeftAlt, KeyCode.RightAlt)) {
                    OnAltLeftClick();
                }
                else {
                    OnLeftClick();
                }
            }
            else if (inputHelper.IsMiddleMouseButton()) {
                OnMiddleClick();
            }
            else if (inputHelper.IsRightMouseButton()) {
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
        if (IsDiscernible && References.InputHelper.IsLeftMouseButton()) {
            OnLeftDoubleClick();
        }
    }

    protected virtual void OnLeftDoubleClick() { }

    protected virtual void OnPress(bool isDown) {
        if (IsDiscernible && References.InputHelper.IsRightMouseButton()) {
            OnRightPress(isDown);
        }
    }

    protected virtual void OnRightPress(bool isDown) { }

    #endregion


    #region Cleanup

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    /// <summary>
    /// Cleans up this instance.
    /// Note: all members should be tested for null before disposing as Items can be destroyed in Creators before completely initialized
    /// </summary>
    protected virtual void Cleanup() {
        if (HudPublisher != null) { (HudPublisher as IDisposable).Dispose(); }
        if (_circles != null) { _circles.Dispose(); }
        Unsubscribe();
    }

    protected virtual void Unsubscribe() {
        //if (_subscribers != null) {
        _subscribers.ForAll(s => s.Dispose());
        _subscribers.Clear();
        //}
    }

    #endregion

    #region ICameraTargetable Members

    public virtual bool IsEligible { get { return PlayerIntel.CurrentCoverage != IntelCoverage.None; } }

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

    public Transform Transform { get { return _transform; } }

    #endregion

    #region IItem Members

    public event Action<IItem> onOwnerChanged;

    public virtual string DisplayName { get { return Name; } }

    public string Name { get { return Data.Name; } }

    public virtual string FullName {
        get {
            if (Data != null) {
                return Data.FullName;
            }
            return _transform.name + "(from transform)";
        }
    }

    public Vector3 Position { get { return Data.Position; } }

    private float _radius;
    /// <summary>
    /// The radius in units of the conceptual 'globe' that encompasses this Item.
    /// </summary>
    public virtual float Radius {
        get {
            D.Assert(_radius != Constants.ZeroF, "{0}.Radius has not yet been set.".Inject(FullName));
            return _radius;
        }
        protected set { _radius = value; }
    }

    public IPlayer Owner { get { return Data.Owner; } }

    #endregion

    #region IDestinationTarget Members

    public virtual SpaceTopography Topography { get { return Data.Topography; } }

    public virtual bool IsMobile { get { return true; } }

    #endregion

    #region IDisposable

    [DoNotSerialize]
    private bool alreadyDisposed = false;

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
    /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
    /// </summary>
    /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool isDisposing) {
        // Allows Dispose(isDisposing) to be called more than once
        if (alreadyDisposed) {
            return;
        }

        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        alreadyDisposed = true;
    }

    // Example method showing check for whether the object has been disposed
    //public void ExampleMethod() {
    //    // throw Exception if called on object that is already disposed
    //    if(alreadyDisposed) {
    //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
    //    }

    //    // method content here
    //}
    #endregion

    #region Nested Classes

    public enum Highlights {

        None = -1,
        /// <summary>
        /// The item is the focus.
        /// </summary>
        Focused = 0,
        /// <summary>
        /// The item is selected..
        /// </summary>
        Selected = 1,
        /// <summary>
        /// The item is highlighted for other reasons. This is
        /// typically used on a fleet's ships when the fleet is selected.
        /// </summary>
        General = 2,
        /// <summary>
        /// The item is both selected and the focus.
        /// </summary>
        SelectedAndFocus = 3,
        /// <summary>
        /// The item is both the focus and generally highlighted.
        /// </summary>
        FocusAndGeneral = 4

    }

    #endregion
}

