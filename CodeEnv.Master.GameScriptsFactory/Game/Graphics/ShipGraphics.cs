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
using System.Collections;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using Vectrosity;

/// <summary>
/// Handles graphics for a ship.  Assumes location on the same 
/// game object as the ShipCaptain.
/// </summary>
public class ShipGraphics : AGraphics, IDisposable {

    private bool _isShowShip = true; // start true in case game starts close enough to see the ship
    public bool IsShowShip {
        get { return _isShowShip; }
        set { SetProperty<bool>(ref _isShowShip, value, "IsShowShip", OnIsShowShipChanged); }
    }

    public int maxShowDistance;
    private Color _originalMainShipColor;
    private Color _originalSpecularShipColor;
    private Color _hiddenShipColor;
    private Renderer _shipRenderer;

    public float circleScaleFactor = 1.5F;
    private HighlightCircle _circles;
    private VelocityRay _velocityRay;
    private VectorLineFactory _vectorLineFactory;

    // cached references
    private ShipCaptain _shipCaptain;
    private FleetManager _fleetMgr;

    protected override void Awake() {
        base.Awake();
        Target = _transform;
        _shipCaptain = gameObject.GetSafeMonoBehaviourComponent<ShipCaptain>();
        maxAnimateDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipAnimateDistanceFactor * _shipCaptain.Size);
        maxShowDistance = Mathf.RoundToInt(AnimationSettings.Instance.MaxShipShowDistanceFactor * _shipCaptain.Size);
        _fleetMgr = gameObject.GetSafeMonoBehaviourComponentInParents<FleetManager>();
        _vectorLineFactory = VectorLineFactory.Instance;
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
        IsShowShip = toShowShip;
        return distanceToCamera;
    }

    private void OnIsShowShipChanged() {
        AssessHighlighting();
    }

    public void AssessHighlighting() {
        if (!IsShowShip) {
            Highlight(false, Highlights.None);
            return;
        }
        if (_shipCaptain.IsFocus) {
            if (_shipCaptain.IsSelected) {
                Highlight(true, Highlights.SelectedAndFocus);
                return;
            }
            if (_fleetMgr.IsSelected) {
                Highlight(true, Highlights.FocusAndGeneral);
                return;
            }
            Highlight(true, Highlights.Focused);
            return;
        }
        if (_shipCaptain.IsSelected) {
            Highlight(true, Highlights.Selected);
            return;
        }
        if (_fleetMgr.IsSelected) {
            Highlight(true, Highlights.General);
            return;
        }
        Highlight(true, Highlights.None);
    }

    private void Highlight(bool toShowShip, Highlights highlight) {
        if (toShowShip) {
            _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _originalMainShipColor);
            _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _originalSpecularShipColor);
        }
        else {
            _shipRenderer.material.SetColor(UnityConstants.MainMaterialColor, _hiddenShipColor);
            _shipRenderer.material.SetColor(UnityConstants.SpecularMaterialColor, _hiddenShipColor);
        }
        ShowVelocityRay(toShowShip);
        switch (highlight) {
            case Highlights.Focused:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.Selected:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.SelectedAndFocus:
                ShowCircle(true, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            case Highlights.General:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.FocusAndGeneral:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(true, Highlights.Focused);
                ShowCircle(true, Highlights.General);
                break;
            case Highlights.None:
                ShowCircle(false, Highlights.Selected);
                ShowCircle(false, Highlights.Focused);
                ShowCircle(false, Highlights.General);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(highlight));
        }
    }

    private void ShowCircle(bool toShow, Highlights highlight) {
        D.Assert(highlight == Highlights.Focused || highlight == Highlights.Selected || highlight == Highlights.General);
        if (!toShow && _circles == null) {
            return;
        }
        if (_circles == null) {
            float normalizedRadius = Screen.height * circleScaleFactor * _shipCaptain.Size;
            _circles = _vectorLineFactory.MakeInstance("ShipCircles", _shipCaptain.transform, normalizedRadius, isRadiusDynamic: true, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        _circles.ShowCircle(toShow, (int)highlight);
    }

    /// <summary>
    /// Shows a Vectrosity Ray indicating the course and speed of the ship.
    /// </summary>
    private void ShowVelocityRay(bool toShow) {
        if (DebugSettings.Instance.EnableShipVelocityRays) {
            if (!toShow && _velocityRay == null) {
                return;
            }
            if (_velocityRay == null) {
                var speedReference = new Reference<float>(() => _shipCaptain.Data.CurrentSpeed);
                _velocityRay = _vectorLineFactory.MakeInstance("ShipVelocityRay", _shipCaptain.transform, speedReference, GameColor.Gray);
            }
            _velocityRay.ShowRay(toShow);
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_velocityRay != null) {
            Destroy(_velocityRay.gameObject);
            _velocityRay = null;
        }
        if (_circles != null) {
            Destroy(_circles.gameObject);
            _circles = null;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

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

}

