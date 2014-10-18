// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemHighlight.cs
// Singleton spherical highlight control and label that tracks the designated IWidgetTrackable target.
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
/// Singleton spherical highlight control and label that tracks the designated IWidgetTrackable target.
/// </summary>
public class SphericalHighlight : AMonoBaseSingleton<SphericalHighlight>, ISphericalHighlight, IDisposable {

    public bool enableTrackingLabel = false;

    public float alphaValueWhenShowing = 0.5F;

    private IWidgetTrackable _target;
    private Renderer _renderer;
    private Transform _meshTransform;
    private float _radius;
    private float _baseSphereRadius;

    private ITrackingWidget _trackingLabel;

    private string _radiusLabelText = "Highlight\nRadius: {0:0.#}";

    protected override void Awake() {
        base.Awake();
        _renderer = gameObject.GetComponentInChildren<Renderer>();
        _meshTransform = _renderer.transform;
        _baseSphereRadius = _renderer.bounds.size.x / 2F;
        //D.Log("{0} base sphere radius = {1}.", GetType().Name, _baseSphereRadius);
        UpdateRate = FrameUpdateFrequency.Normal;
        Show(false);
    }

    public void SetTarget(IWidgetTrackable target, float sphereRadius, WidgetPlacement labelPlacement = WidgetPlacement.Below) {
        _target = target;
        if (enableTrackingLabel && _trackingLabel == null) {
            _trackingLabel = InitializeTrackingLabel();
        }

        if (_trackingLabel != null) {
            if (_trackingLabel.Target != target) {    // eliminates PropertyChanged not changed warning
                _trackingLabel.Target = target;
            }
            _trackingLabel.Placement = labelPlacement;
        }
        UpdatePosition();
        SetRadius(sphereRadius);
    }

    private ITrackingWidget InitializeTrackingLabel() {
        var trackingLabel = TrackingWidgetFactory.Instance.CreateUITrackingLabel(_target);
        trackingLabel.Name = GetType().Name + CommonTerms.Label;
        return trackingLabel;
    }

    public void SetRadius(float sphereRadius) {
        if (Mathf.Approximately(_radius, sphereRadius)) {
            return;
        }
        _radius = sphereRadius;
        float scaleFactor = sphereRadius / _baseSphereRadius;
        _meshTransform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        //D.Log("{0} Radius = {1}.", GetType().Name, _radiusLabel.text);
        if (_trackingLabel != null) {
            _trackingLabel.Set(_radiusLabelText.Inject(sphereRadius));
            _trackingLabel.SetShowDistance(sphereRadius * 2F);
            // highlight sphere radius has changed so label placement offset has changed
            _trackingLabel.RefreshWidgetValues();
        }
    }

    public void Show(bool toShow) {
        enabled = toShow;
        float alpha = toShow ? alphaValueWhenShowing : Constants.ZeroF;
        _renderer.material.SetAlpha(alpha);
        if (_trackingLabel != null) {
            _trackingLabel.Show(toShow);
        }
    }

    protected override void OccasionalUpdate() {
        base.OccasionalUpdate();
        UpdatePosition();
    }

    private void UpdatePosition() {
        _transform.position = _target.Transform.position;
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        Dispose();
    }

    private void Cleanup() {
        // other cleanup here including any tracking Gui2D elements
        UnityUtility.ExecuteIfNotNullOrDestroyed(_trackingLabel, Destroy);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDisposable

    [DoNotSerialize]
    private bool _alreadyDisposed = false;
    protected bool _isDisposing = false;

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
        if (_alreadyDisposed) {
            return;
        }

        _isDisposing = true;
        if (isDisposing) {
            // free managed resources here including unhooking events
            Cleanup();
        }
        // free unmanaged resources here

        _alreadyDisposed = true;
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

