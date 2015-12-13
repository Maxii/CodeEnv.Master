// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FocusableItemControl.cs
// Manages a stationary Celestial Object's interaction with the camera.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a stationary Celestial Object's interaction with the camera.
/// </summary>
public class FocusableItemControl : AMonoBase, ICameraFocusable, IZoomToClosest {

    private GameEventManager _eventMgr;
    protected Collider _collider;

    private Transform _transform;

    void Awake() {
        InitializeOnAwake();
    }

    protected virtual void InitializeOnAwake() {
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _transform = transform;
        _collider = gameObject.GetComponent<Collider>();
        _eventMgr = GameEventManager.Instance;
    }

    protected virtual void OnHover(bool isOver) {
        //TODO
        Logger.Log("{0}.OnHover({1}) called.", gameObject.name, isOver);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsTargetable {
        get { return true; }
    }

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 4.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.bounds.extents.magnitude * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFocusable Members

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 10.0F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.bounds.extents.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    public virtual void OnClick() {
        Logger.Log("{0}.OnClick() called.", gameObject.name);
        if (GameInputHelper.IsMiddleMouseButton()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    public virtual void IsFocus() {
        // does nothing for now
    }

    #endregion

}

