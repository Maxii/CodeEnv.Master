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
using System.Linq;
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
[System.Obsolete]
public class ShipGraphics : AGraphics, IDisposable {

    private bool _isDetectable = true; // FIXME if starts false, it doesn't get updated right away...
    /// <summary>
    /// Gets or sets a value indicating whether the object this graphics script
    /// is associated with is detectable by the human player. 
    /// eg. a fleet the human player has no intel about is not detectable.
    /// </summary>
    public bool IsDetectable {
        get { return _isDetectable; }
        set { SetProperty<bool>(ref _isDetectable, value, "IsDetectable", OnIsDetectableChanged); }
    }

    public int maxShowDistance;
    private Color _originalMainShipColor;
    private Color _originalSpecularShipColor;
    private Color _hiddenShipColor;
    private Renderer _shipRenderer;
    private bool _toShowShipBasedOnDistance;

    public float circleScaleFactor = 1.5F;
    private HighlightCircle _circles;
    private VelocityRay _velocityRay;

    // cached references
    private ShipCaptain _shipCaptain;
    private FleetUnitCreator _fleetMgr;

    protected override void Awake() {
        base.Awake();
        Target = _transform;
        _shipCaptain = gameObject.GetSafeMonoBehaviour<ShipCaptain>();
        maxAnimateDistance = Mathf.RoundToInt(GraphicsSettings.Instance.MaxShipAnimateDistanceFactor * _shipCaptain.Size);
        maxShowDistance = Mathf.RoundToInt(GraphicsSettings.Instance.MaxShipShowDistanceFactor * _shipCaptain.Size);
        _fleetMgr = gameObject.GetSafeFirstMonoBehaviourInParents<FleetUnitCreator>();
        InitializeHighlighting();
    }

    private void InitializeHighlighting() {
        _shipRenderer = gameObject.GetComponentInChildren<Renderer>();
        _originalMainShipColor = _shipRenderer.material.GetColor(UnityConstants.MaterialColor_Main);
        _originalSpecularShipColor = _shipRenderer.material.GetColor(UnityConstants.MaterialColor_Specular);
        _hiddenShipColor = GameColor.Clear.ToUnityColor();
    }

    protected override void RegisterComponentsToDisable() {
        disableComponentOnCameraDistance = new Component[1] {
            gameObject.GetComponentInChildren<Animation>()
        };
    }

    private void OnIsDetectableChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, _shipCaptain.PlayerIntelLevel != IntelLevel.Nil);
        AssessHighlighting();
    }

    protected override void OnIsVisibleChanged() {
        EnableBasedOnDiscernible(InCameraLOS, IsDetectable);
        EnableBasedOnDistanceToCamera(InCameraLOS, _shipCaptain.PlayerIntelLevel != IntelLevel.Nil);
        AssessHighlighting();
    }

    protected override int EnableBasedOnDistanceToCamera(params bool[] conditions) {
        bool condition = conditions.All<bool>(c => c == true);
        int distanceToCamera = base.EnableBasedOnDistanceToCamera(condition);
        _toShowShipBasedOnDistance = false;
        if (condition) {
            if (distanceToCamera == Constants.Zero) {
                distanceToCamera = Target.DistanceToCameraInt();
            }
            if (distanceToCamera < maxShowDistance) {
                _toShowShipBasedOnDistance = true;
            }
        }
        AssessDetectability();
        return distanceToCamera;
    }

    public void AssessDetectability() {
        IsDetectable = _shipCaptain.PlayerIntelLevel != IntelLevel.Nil && _toShowShipBasedOnDistance;
    }

    public override void AssessHighlighting() {
        D.Log("{0}.AssessHighligting(). Ship.IsDescernible = {4}, Ship.IsFocus = {1}, Ship.IsSelected = {2}, Fleet.IsSelected = {3}.",
            _shipCaptain.Data.Name, _shipCaptain.IsFocus, _shipCaptain.IsSelected, _fleetMgr.IsSelected, IsDetectable && InCameraLOS);
        if (!IsDetectable || !InCameraLOS) {
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
            _shipRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _originalMainShipColor);
            _shipRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _originalSpecularShipColor);
            //TODO audio on goes here
        }
        else {
            _shipRenderer.material.SetColor(UnityConstants.MaterialColor_Main, _hiddenShipColor);
            _shipRenderer.material.SetColor(UnityConstants.MaterialColor_Specular, _hiddenShipColor);
            //TODO audio off goes here
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
            _circles = new HighlightCircle("ShipCircles", _shipCaptain.transform, normalizedRadius, parent: DynamicObjectsFolder.Folder,
                isRadiusDynamic: true, maxCircles: 3);
            _circles.Colors = new GameColor[3] { UnityDebugConstants.FocusedColor, UnityDebugConstants.SelectedColor, UnityDebugConstants.GeneralHighlightColor };
            _circles.Widths = new float[3] { 2F, 2F, 1F };
        }
        if (toShow) {
            D.Log("Ship {1} attempting to show circle {0}.", highlight.GetValueName(), _shipCaptain.Data.Name);
            if (!_circles.IsShowing) {
                StartCoroutine(_circles.ShowCircles((int)highlight));
            }
            else {
                _circles.AddCircle((int)highlight);
            }
        }
        else if (_circles.IsShowing) {
            _circles.RemoveCircle((int)highlight);
        }
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
                _velocityRay = new VelocityRay("ShipVelocityRay", _shipCaptain.transform, speedReference, parent: DynamicObjectsFolder.Folder,
                    width: 1F, color: GameColor.Gray);
            }
            if (toShow) {
                if (!_velocityRay.IsShowing) {
                    StartCoroutine(_velocityRay.Show());
                }
            }
            else if (_velocityRay.IsShowing) {
                _velocityRay.Hide();
            }
        }
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        if (_velocityRay != null) {
            _velocityRay.Dispose();
            _velocityRay = null;
        }
        if (_circles != null) {
            _circles.Dispose();
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

