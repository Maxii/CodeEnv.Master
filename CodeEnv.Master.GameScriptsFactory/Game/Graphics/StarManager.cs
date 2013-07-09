// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarManager.cs
// Manages a Star.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System.Collections.Generic;
using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages a Star.
/// </summary>
public class StarManager : AMonoBehaviourBase, ICameraFocusable, IZoomToClosest {

    // Cached references
    private GameEventManager _eventMgr;
    private Transform _transform;
    private SphereCollider _collider;
    private SystemManager _systemMgr;

    // components of the heirarchy that can be disabled or deactivated when not visible
    private IList<MonoBehaviour> scriptsThatCanBeDisabled;
    private IList<GameObject> gosThatCanBeDeactivated;

    void Awake() {
        UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _eventMgr = GameEventManager.Instance;
        _transform = transform;
        _collider = collider as SphereCollider;
        _systemMgr = _transform.parent.gameObject.GetSafeMonoBehaviourComponent<SystemManager>();
        UpdateRate = UpdateFrequency.Continuous;
    }

    void OnHover(bool isOver) {
        if (isOver) {
            //Debug.Log("StarManager.OnHover(true) called.");
            _systemMgr.DisplayCursorHUD();
        }
        else {
            //Debug.Log("StarManager.OnHover(false) called.");
            _systemMgr.ClearCursorHUD();
        }
        _systemMgr.TrackingLabel.IsHighlighted = isOver;
    }

    public void EnableHeirarchy(bool toEnable) {
        if (scriptsThatCanBeDisabled == null) {
            scriptsThatCanBeDisabled = new List<MonoBehaviour>();
            scriptsThatCanBeDisabled.Add(gameObject.GetSafeMonoBehaviourComponent<StarAnimator>());
        }
        foreach (MonoBehaviour s in scriptsThatCanBeDisabled) {
            s.enabled = toEnable;
        }

        if (gosThatCanBeDeactivated == null) {
            gosThatCanBeDeactivated = new List<GameObject>();
            gosThatCanBeDeactivated.Add(gameObject.GetSafeMonoBehaviourComponentInChildren<StarBillboardManager>().gameObject);
        }
        foreach (GameObject go in gosThatCanBeDeactivated) {
            go.SetActive(toEnable);
        }
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
                _minimumCameraViewingDistance = _collider.radius * minimumCameraViewingDistanceMultiplier;
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
                _optimalCameraViewingDistance = _collider.radius * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    public void OnClick() {
        //Debug.Log("{0}.OnClick() called.".Inject(GetType().Name));
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    #endregion

}

