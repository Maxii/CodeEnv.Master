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
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Handles graphics for a ship.  Assumes location on the same 
/// game object as the ShipCaptain.
/// </summary>
public class ShipGraphics : AGraphics {

    private bool _isShipShowing = true; // start true in case game starts close enough to see the ship
    public bool IsShipShowing {
        get { return _isShipShowing; }
        set { SetProperty<bool>(ref _isShipShowing, value, "IsShipShowing", OnIsShipShowingChanged); }
    }

    public int maxShowDistance;

    private Color _originalMainShipColor;
    private Color _originalSpecularShipColor;
    private Color _hiddenShipColor;
    private Renderer _shipRenderer;

    private ShipCaptain _shipCaptain;

    protected override void Awake() {
        base.Awake();
        maxAnimateDistance = maxAnimateDistance == Constants.Zero ? AnimationSettings.Instance.MaxShipAnimateDistance : maxAnimateDistance;
        maxShowDistance = AnimationSettings.Instance.MaxShipShowDistance;
        Target = transform;
        _shipCaptain = gameObject.GetSafeMonoBehaviourComponent<ShipCaptain>();
        InitializeHighlighting();
    }

    private void InitializeHighlighting() {
        _shipRenderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMainShipColor = _shipRenderer.material.GetColor(UnityConstants.MainMaterialColor);
        _originalSpecularShipColor = _shipRenderer.material.GetColor(UnityConstants.SpecularMaterialColor);
        _hiddenShipColor = GameColor.Clear.ToUnityColor();
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
        IsShipShowing = toShowShip;
        return distanceToCamera;
    }

    private void OnIsShipShowingChanged() {
        ChangeHighlighting();
    }

    public void ChangeHighlighting() {
        if (!IsVisible) {
            Highlight(false);
            return;
        }
        if (_shipCaptain.IsFocus) {
            if (_shipCaptain.IsSelected) {
                Highlight(true, Highlights.Both);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        if (_shipCaptain.IsSelected) {
            Highlight(true, Highlights.Selected);
            return;
        }
        Highlight(true, Highlights.None);
    }

    private void Highlight(bool toShow, Highlights highlight = Highlights.None) {
        if (!toShow) {
            _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _hiddenShipColor);
            _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _hiddenShipColor);
            return;
        }
        switch (highlight) {
            case Highlights.Focused:
                _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsFocusedColor);
                _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, UnityDebugConstants.IsFocusedColor);
                break;
            case Highlights.Selected:
                _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsSelectedColor);
                _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, UnityDebugConstants.IsSelectedColor);
                break;
            case Highlights.Both:
                _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, UnityDebugConstants.IsFocusAndSelectedColor);
                _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, UnityDebugConstants.IsFocusAndSelectedColor);
                break;
            case Highlights.None:
                _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _originalMainShipColor);
                _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _originalSpecularShipColor);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
        // Note: The approach above to hiding the ship works by controlling the alpha value of the material color. It requires use
        // of a shader capable of transparency. The alternative approach is to deactivate the renderer gameobject, but we lose
        // any benefit of knowing when the ship is invisible since, when deactivated, OnBecameVisible/Invisible is not called.
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

