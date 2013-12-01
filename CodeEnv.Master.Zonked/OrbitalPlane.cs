// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OrbitalPlane.cs
// Manages the interaction of the Orbital plane, aka the 'system', with the Player.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Manages the interaction of the Orbital plane, aka the 'system', with the Player.
/// </summary>
[System.Obsolete]
public class OrbitalPlane : StationaryItem, IZoomToFurthest {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    public float minPlaneZoomDistance = 2F;
    public float optimalPlaneFocusDistance = 400F;

    private SystemGraphics _systemGraphics;
    private SystemCreator _systemManager;

    protected override void Awake() {
        base.Awake();
        _systemManager = gameObject.GetSafeMonoBehaviourComponentInParents<SystemCreator>();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<SystemGraphics>();
        __ValidateCtxObjectSettings();
    }

    private void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    protected override IGuiHudPublisher InitializeHudPublisher() {
        return new GuiHudPublisher<SystemData>(Data);
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        //D.Log("OrbitalPlane.OnHover({0}), hoveredObject = {1}.".Inject(isOver, UICamera.hoveredObject.name));
        _systemGraphics.HighlightTrackingLabel(isOver);
    }

    public void OnPress(bool isDown) {
        if (_systemManager.IsSelected) {
            //D.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.ShowContextMenuOnPress(isDown);
        }
    }

    protected override void OnClick() {
        base.OnClick();
        if (GameInputHelper.IsLeftMouseButton()) {
            _systemManager.OnLeftClick();
        }
    }

    protected override void OnIsFocusChanged() {
        base.OnIsFocusChanged();
        _systemGraphics.AssessHighlighting();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    protected override float CalcMinimumCameraViewingDistance() {
        return minPlaneZoomDistance;
    }

    #endregion

    #region ICameraFocusable Members

    public override bool IsRetainedFocusEligible { get { return true; } }

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    protected override float CalcOptimalCameraViewingDistance() {
        return optimalPlaneFocusDistance;
    }

    #endregion

}

