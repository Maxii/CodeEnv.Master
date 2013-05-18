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

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// Manages the propulsion of a ship.
/// </summary>
public class ShipPropulsionManager : MonoBehaviourBase, IFollow {

    private float minimumCameraApproachDistance;
    public float MinimumCameraApproachDistance {
        get {
            if (minimumCameraApproachDistance == Constants.ZeroF) {
                minimumCameraApproachDistance = minimumCameraApproachFactor * _collider.size.magnitude;
            }
            return minimumCameraApproachDistance;
        }
    }

    private float optimalCameraApproachDistance;
    public float OptimalCameraApproachDistance {
        get {
            if (optimalCameraApproachDistance == Constants.ZeroF) {
                optimalCameraApproachDistance = optimalCameraApproachFactor * _collider.size.magnitude;
            }
            return optimalCameraApproachDistance;
        }
    }

    [SerializeField]
    private float minimumCameraApproachFactor = 5.0F;
    [SerializeField]
    private float optimalCameraApproachFactor = 20.0F;

    private BoxCollider _collider;
    private Rigidbody _rigidbody;
    private GameEventManager _eventMgr;
    private Transform _transform;

    private Vector3 thrustDirection = Vector3.forward;

    void Awake() {
        _transform = transform;
        _rigidbody = _transform.parent.GetComponent<Rigidbody>();
        _collider = _transform.collider as BoxCollider;
        _eventMgr = GameEventManager.Instance;
    }

    void Start() {
        // Keep at a minimum, an empty Start method so that instances receive the OnDestroy event
    }

    void Update() {

    }

    void OnHover() {
        Debug.Log("Ship.OnHover() called.");
    }

    public void OnClick() {
        if (NguiGameInput.IsMiddleMouseButtonClick()) {
            _eventMgr.Raise<FocusSelectedEvent>(new FocusSelectedEvent(this, _transform));
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

}

