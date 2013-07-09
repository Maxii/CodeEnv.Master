// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetIconManager.cs
// Manages the Collider events associated with the FleetIcon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the Collider events associated with the FleetIcon.
/// </summary>
public class FleetIconManager : AMonoBehaviourBase, ICameraFollowable, IZoomToClosest {

    private FleetManager _fleetMgr;
    private Transform _transform;
    private GameEventManager _eventMgr;

    void Awake() {
        UnityUtility.ValidateComponentPresence<Collider>(gameObject);
        _transform = transform;
        _fleetMgr = _transform.parent.parent.gameObject.GetSafeMonoBehaviourComponent<FleetManager>();
        _eventMgr = GameEventManager.Instance;
    }

    void OnHover(bool isOver) {
        //Debug.Log("FleetIconManager.OnHover() called.");
        if (isOver) {
            // highlight guiTrackingLabel
            _fleetMgr.UpdateSpeed();
            _fleetMgr.DisplayCursorHUD();
        }
        else {
            _fleetMgr.ClearCursorHUD();
        }
    }

    void OnDoubleClick() {
        _fleetMgr.ChangeFleetHeading(_transform.right);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraTargetable Members

    public bool IsTargetable {
        get { return true; }
    }

    [SerializeField]
    private float _minimumCameraViewingDistance = 5.0F;
    public float MinimumCameraViewingDistance {
        get {
            return _minimumCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFocusable Members

    public void OnClick() {
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    [SerializeField]
    private float _optimalCameraViewingDistance = 10.0F;
    public float OptimalCameraViewingDistance {
        get {
            return _optimalCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraFollowable Members

    [SerializeField]
    private float cameraFollowDistanceDampener = 2.0F;
    public float CameraFollowDistanceDampener {
        get { return cameraFollowDistanceDampener; }
    }

    [SerializeField]
    private float cameraFollowRotationDampener = 1.0F;
    public float CameraFollowRotationDampener {
        get { return cameraFollowRotationDampener; }
    }

    #endregion

}

