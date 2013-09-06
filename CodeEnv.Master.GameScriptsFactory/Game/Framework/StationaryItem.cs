// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StationaryItem.cs
// Lowest level instantiable class for stationary items in the universe that the camera can focus on.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Lowest level instantiable class for stationary items in the universe that the camera can focus on.
/// </summary>
public class StationaryItem : AItem, ICameraFocusable {

    private static bool _isStaticHudPublisherFieldsInitialized;

    protected GameEventManager _eventMgr;

    protected override void Awake() {
        base.Awake();
        if (!_isStaticHudPublisherFieldsInitialized) {
            InitializeStaticHudPublisherFields();
        }
        _eventMgr = GameEventManager.Instance;
    }

    private static void InitializeStaticHudPublisherFields() {
        AGuiHudPublisher.SetGuiCursorHud(GuiCursorHud.Instance);
        GuiHudPublisher<Data>.SetFactory(GuiHudTextFactory.Instance);
        GuiHudPublisher<ShipData>.SetFactory(ShipGuiHudTextFactory.Instance);
        GuiHudPublisher<FleetData>.SetFactory(FleetGuiHudTextFactory.Instance);
        GuiHudPublisher<SystemData>.SetFactory(SystemGuiHudTextFactory.Instance);
        _isStaticHudPublisherFieldsInitialized = true;
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<Data>(Data);
    }

    protected virtual void OnClick() {
        if (GameInputHelper.IsMiddleMouseButton()) {
            OnMiddleClick();
        }
    }

    protected virtual void OnMiddleClick() {
        DeclareAsFocus();
    }

    public void DeclareAsFocus() {
        _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
    }

    protected virtual void OnIsFocusChanged() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 10.0F;

    private float _optimalCameraViewingDistance;
    public virtual float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.bounds.extents.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    private bool _isFocus;
    public virtual bool IsFocus {
        get { return _isFocus; }
        set { SetProperty<bool>(ref _isFocus, value, "IsFocus", OnIsFocusChanged); }
    }

    #endregion

}

