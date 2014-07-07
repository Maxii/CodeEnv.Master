// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStation.cs
// Tracks whether the assigned ship is within the radius of it's Station in the Formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Tracks whether the assigned ship is within the radius of it's Station in the Formation.
/// </summary>
public class FormationStation : AMonoBase, IFormationStation, IDestinationTarget {

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        enabled = false;
        // stations control enabled themselves when AssignedShip changes
    }

    void OnTriggerEnter(Collider other) {
        if (enabled) {
            if (other.isTrigger) { return; }
            //D.Log("OnTriggerEnter({0}) called.", other.name);
            IShipModel arrivingShip = other.gameObject.GetInterface<IShipModel>();
            if (arrivingShip != null) {
                if (arrivingShip == AssignedShip) {
                    OnShipOnStation(true);
                }
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (enabled) {
            if (other.isTrigger) { return; }
            //D.Log("{0}.OnTriggerExit() called by Collider {1}.", GetType().Name, other.name);
            IShipModel departingShip = other.gameObject.GetInterface<IShipModel>();
            if (departingShip != null) {
                if (departingShip == AssignedShip) {
                    OnShipOnStation(false);
                }
            }
        }
    }

    protected override void OnEnable() {
        base.OnEnable();
        _collider.enabled = true;
    }

    protected override void OnDisable() {
        base.OnDisable();
        _collider.enabled = false;
    }

    private void OnAssignedShipChanged() {
        if (AssignedShip != null) {
            StationRadius = AssignedShip.Radius * 5F;
            //D.Log("{0}.StationRadius set to {1:0.0000}.", AssignedShip.FullName, StationRadius);
            _collider.radius = StationRadius;
            // Note: OnTriggerEnter appears to detect ship is onStation once the collider is enabled even if already inside
            // Unfortunately, that detection has a small delay (collider init?) so this is needed to fill the gap
            if (IsShipAlreadyOnStation) {
                //D.Log("{0} is already OnStation.", AssignedShip.FullName);
                OnShipOnStation(true);
            }
            enabled = true;
        }
        else {
            enabled = false;
            OnShipOnStation(false);
        }
    }

    // Note: no need to detect when the AssignedShip dies as FleetCmd will set it
    // to null when it is removed from the fleet, whether through death or some other reason

    private void OnStationOffsetChanged() {
        // setting local position doesn't work as the resultant world position is modified by the rotation of FleetCmd
        _transform.position += StationOffset;
        // UNCLEAR when an FST changes its offset (location), does OnTriggerEnter/Exit detect it?
    }

    protected void OnShipOnStation(bool isOnStation) {
        IsOnStation = isOnStation;
        if (AssignedShip != null) {
            AssignedShip.OnShipOnStation(isOnStation);
        }
    }

    /// <summary>
    /// Manually detects whether the ship is on station by seeing whether the ship's
    /// position is inside the collider bounds.
    /// </summary>
    /// <value>
    /// <c>true</c> if this ship is already on station; otherwise, <c>false</c>.
    /// </value>
    private bool IsShipAlreadyOnStation {
        get {
            //D.Log("FormationStation at {0} with Radius {1}, Assigned Ship {2} at {3}.", _transform.position, StationRadius, AssignedShip.FullName, AssignedShip.Data.Position);
            return _collider.bounds.Contains(AssignedShip.Data.Position);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IFormationStation Members

    public bool IsOnStation { get; private set; }

    public float StationRadius { get; private set; }

    private Vector3 _stationOffset;
    /// <summary>
    /// The Vector3 offset of this station of the formation from the HQ Element.
    /// </summary>
    public Vector3 StationOffset {
        get { return _stationOffset; }
        set { SetProperty<Vector3>(ref _stationOffset, value, "StationOffset", OnStationOffsetChanged); }
    }

    private IShipModel _assignedShip;
    public IShipModel AssignedShip {
        get { return _assignedShip; }
        set { SetProperty<IShipModel>(ref _assignedShip, value, "AssignedShip", OnAssignedShipChanged); }
    }

    #endregion

    #region IDestinationTarget Members

    public string FullName {
        get {
            string msg = AssignedShip == null ? " (unassigned)" : string.Format(" ({0})", AssignedShip.FullName);
            return GetType().Name + msg;
        }
    }

    public Vector3 Position {
        get { return _transform.position; }
    }

    public bool IsMobile { get { return true; } }

    public float Radius { get { return Constants.ZeroF; } }

    public SpaceTopography Topography { get { return Universe.Instance.GetSpaceTopography(Position); } }

    #endregion

}

