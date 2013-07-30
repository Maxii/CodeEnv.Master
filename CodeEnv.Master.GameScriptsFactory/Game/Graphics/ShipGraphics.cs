// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipGraphics.cs
// Handles graphics for a ship.  Assumes location on the same game object as the ShipCaptain.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Handles graphics for a ship.  Assumes location on the same 
/// game object as the ShipCaptain.
/// </summary>
public class ShipGraphics : AGraphics {

    public bool IsShipShowing { get; private set; }

    public int maxShowDistance;

    private Color _originalShipColor;
    private Color _hiddenShipColor;
    private Renderer _renderer;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        maxAnimateDistance = maxAnimateDistance == Constants.Zero ? AnimationSettings.Instance.MaxShipAnimateDistance : maxAnimateDistance;
        maxShowDistance = AnimationSettings.Instance.MaxShipShowDistance;
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        Target = transform;
        IsShipShowing = true;   // start true so first ShowShip(false) will toggle Renderer off if out of show range
    }

    protected override void InitializeOnStart() {
        base.InitializeOnStart();
        _originalShipColor = _renderer.material.color;
        _hiddenShipColor = new Color(_originalShipColor.r, _originalShipColor.g, _originalShipColor.b, Constants.ZeroF);
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnCameraDistance = new Component[1] { 
            gameObject.GetComponentInChildren<Animation>() 
        };
    }

    protected override int EnableBasedOnDistanceToCamera() {
        int distanceToCamera = base.EnableBasedOnDistanceToCamera();
        bool toShowShip = false;
        if (IsVisible) {
            if (distanceToCamera == Constants.Zero) {
                distanceToCamera = Target.DistanceToCameraInt();
            }
            if (distanceToCamera < maxShowDistance) {
                toShowShip = true;
            }
        }
        ShowShip(toShowShip);
        return distanceToCamera;
    }

    private void ShowShip(bool toShow) {
        //Logger.Log("ShowShip({0}) called.".Inject(toShow));
        if (IsShipShowing != toShow) {
            IsShipShowing = toShow;
            //ShowShipViaRenderer(toShow);
            ShowShipViaMaterialColor(toShow);
        }
    }

    /// <summary>
    /// Controls whether the ship can be seen by controlling the alpha value of the material color. 
    /// <remarks>This approach to hiding the ship works, but it requires use of a shader capable of transparency,
    /// which doesn't show up well with the current material.</remarks>
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> material.alpha = 1.0, otherwise material.alpha = 0.0</param>
    private void ShowShipViaMaterialColor(bool toShow) {
        _renderer.material.color = toShow ? _originalShipColor : _hiddenShipColor;
        //Logger.Log("ShowShipViaMaterialColor({0}) called.".Inject(toShow));
    }

    /// <summary>
    /// Controls whether the ship can be seen via the activation state of the renderer gameobject. 
    /// <remarks>This approach to hiding the ship works, but we lose any benefit of knowing
    /// when the ship is invisible since, when deactivated, OnBecameVisible/Invisible is not called.</remarks>
    /// </summary>
    /// <param name="toShow">if set to <c>true</c> [to activate].</param>
    private void ShowShipViaRenderer(bool toShow) {
        _renderer.transform.gameObject.SetActive(toShow);
        //Logger.Log("ShowShipViaRenderer({0}) called.".Inject(toShow));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

