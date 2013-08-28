﻿// --------------------------------------------------------------------------------------------------------------------
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
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the interaction of the Orbital plane, aka the 'system', with the Player.
/// </summary>
public class OrbitalPlane : StationaryItem, IHasContextMenu, IZoomToFurthest {

    public new SystemData Data {
        get { return base.Data as SystemData; }
        set { base.Data = value; }
    }

    public float minPlaneZoomDistance = 1.0F;
    public float optimalPlaneFocusDistance = 400F;

    private SystemGraphics _systemGraphics;
    private SystemManager _systemManager;

    protected override void Awake() {
        base.Awake();
        _systemManager = gameObject.GetSafeMonoBehaviourComponentInParents<SystemManager>();
        _systemGraphics = gameObject.GetSafeMonoBehaviourComponentInParents<SystemGraphics>();
        __ValidateCtxObjectSettings();
    }

    protected override void Start() {
        base.Start();
        PlayerIntelLevel = IntelLevel.Complete;
    }

    protected override void OnHover(bool isOver) {
        base.OnHover(isOver);
        _systemGraphics.HighlightTrackingLabel(isOver);
    }

    protected override void OnClick() {
        base.OnClick();
        if (NguiGameInput.IsLeftMouseButtonClick()) {
            _systemManager.OnLeftClick();
        }
    }

    protected override void OnIsFocusChanged() {
        _systemGraphics.ChangeHighlighting();
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
    public override float MinimumCameraViewingDistance { get { return minPlaneZoomDistance; } }

    #endregion

    #region ICameraFocusable Members

    /// <summary>
    /// Overridden because the default implementation returns a value that
    /// is a factor of the collider bounds which doesn't work for the orbital
    /// plane collider.
    /// </summary>
    public override float OptimalCameraViewingDistance { get { return optimalPlaneFocusDistance; } }

    #endregion

    #region IHasContextMenu Members

    public void __ValidateCtxObjectSettings() {
        CtxObject ctxObject = gameObject.GetSafeMonoBehaviourComponent<CtxObject>();
        D.Assert(ctxObject.contextMenu != null, "{0}.contextMenu on {1} is null.".Inject(typeof(CtxObject).Name, gameObject.name));
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
    }

    public void OnPress(bool isPressed) {
        if (_systemManager.IsSelected) {
            //Logger.Log("{0}.OnPress({1}) called.", this.GetType().Name, isPressed);
            CameraControl.Instance.ContextMenuPickHandler.OnPress(isPressed);
        }
    }

    #endregion

}

