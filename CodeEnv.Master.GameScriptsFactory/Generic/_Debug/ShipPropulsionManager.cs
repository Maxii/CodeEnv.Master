// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipPropulsionManager.cs
// Manages the propulsion of a ship.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System.Text;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Manages the propulsion of a ship.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ShipPropulsionManager : AMonoBehaviourBase, ICameraFollowable {

    private BoxCollider _collider;
    private Rigidbody _rigidbody;
    private GameEventManager _eventMgr;
    private Transform _transform;
    private IGuiCursorHud _cursorHud;

    private StringBuilder hudMsg;

    private Vector3 thrustDirection = Vector3.forward;

    void Awake() {
        _transform = transform;
        // this approach allows this script to be located with the mesh or on the parent
        _rigidbody = gameObject.GetComponentInChildren<Rigidbody>();
        if (_rigidbody == null) {
            _rigidbody = _transform.parent.rigidbody;
            if (_rigidbody == null) {
                Debug.LogError("Can not find Rigidbody. Destroying {0}.".Inject(this.GetType().Name));
                Destroy(gameObject);
            }
        }
        _collider = gameObject.GetComponentInChildren<BoxCollider>();
        _eventMgr = GameEventManager.Instance;
        _cursorHud = GuiCursorHud.Instance;
    }

    void Start() {
        hudMsg = ConstructMsgForHud();
    }

    private StringBuilder ConstructMsgForHud() {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Borg ship to Cursor HUD.");
        sb.Append("Resistance is futile!");
        return sb;
    }

    void OnHover(bool isOver) {
        //Debug.Log("Ship.OnHover() called.");
        if (isOver) {
            _cursorHud.Set(hudMsg);
        }
        else {
            _cursorHud.Clear();
        }
    }

    void OnDoubleClick() {
        thrustDirection = -thrustDirection;
    }

    void FixedUpdate() {
        _rigidbody.AddRelativeForce(thrustDirection * 1000);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region ICameraFocusable Members

    public void OnClick() {
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
        }
    }

    [SerializeField]
    private float optimalCameraViewingDistanceMultiplier = 20.0F;

    private float _optimalCameraViewingDistance;
    public float OptimalCameraViewingDistance {
        get {
            if (_optimalCameraViewingDistance == Constants.ZeroF) {
                _optimalCameraViewingDistance = _collider.size.magnitude * optimalCameraViewingDistanceMultiplier;
            }
            return _optimalCameraViewingDistance;
        }
    }

    #endregion

    #region ICameraTargetable Members

    [SerializeField]
    private float minimumCameraViewingDistanceMultiplier = 5.0F;

    private float _minimumCameraViewingDistance;
    public float MinimumCameraViewingDistance {
        get {
            if (_minimumCameraViewingDistance == Constants.ZeroF) {
                _minimumCameraViewingDistance = _collider.size.magnitude * minimumCameraViewingDistanceMultiplier;
            }
            return _minimumCameraViewingDistance;
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

