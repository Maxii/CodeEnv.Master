// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FormationStationMonitor.cs
// Monitors whether the assigned ship is within the radius of it's Station in the Formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Monitors whether the assigned ship is within the radius of it's Station in the Formation.
/// </summary>
public class FormationStationMonitor : AMonitor, INavigableTarget {

    public bool IsOnStation { get; private set; }   // OPTIMIZE Eliminate collider and just test for distance to ship < stationRadius?

    public float StationRadius { get; private set; }

    private Vector3 _stationOffset;
    /// <summary>
    /// The Vector3 offset of this formation station from the HQ Element.
    /// </summary>
    public Vector3 StationOffset {
        get { return _stationOffset; }
        set { SetProperty<Vector3>(ref _stationOffset, value, "StationOffset", OnStationOffsetChanged); }
    }

    private IShipItem _assignedShip;
    public IShipItem AssignedShip {
        get { return _assignedShip; }
        set { SetProperty<IShipItem>(ref _assignedShip, value, "AssignedShip", OnAssignedShipChanged); }
    }

    /// <summary>
    /// The vector from the currently assigned ship to the station.
    /// </summary>
    public Vector3 VectorToStation { get { return Position - AssignedShip.Position; } }

    protected override void OnTriggerEnter(Collider other) {
        base.OnTriggerEnter(other);
        if (other.isTrigger) { return; }
        D.Log("{0}.{1} OnTriggerEnter() tripped by Collider {2}.", FullName, GetType().Name, other.name);
        var arrivingShip = other.gameObject.GetInterface<IShipItem>();
        if (arrivingShip != null) {
            if (arrivingShip == AssignedShip) {
                IsOnStation = true;
            }
        }
    }

    protected override void OnTriggerExit(Collider other) {
        base.OnTriggerExit(other);
        if (other.isTrigger) { return; }
        D.Log("{0}.{1} OnTriggerExit() tripped by Collider {2}.", FullName, GetType().Name, other.name);
        var departingShip = other.gameObject.GetInterface<IShipItem>();
        if (departingShip != null) {
            if (departingShip == AssignedShip) {
                IsOnStation = false;
            }
        }
    }

    private void OnAssignedShipChanged() {
        if (AssignedShip != null) {
            StationRadius = AssignedShip.Radius * 5F;
            D.Log("Radius of {0} assigned to {1} set to {2:0.0000}.", GetType().Name, AssignedShip.FullName, StationRadius);
            _collider.radius = StationRadius;
            // Note: OnTriggerEnter appears to detect ship is onStation once the collider is enabled even if already inside
            // Unfortunately, that detection has a small delay (collider init?) so this is needed to fill the gap
            IsOnStation = IsShipAlreadyOnStation;
            IsOperational = true;
        }
        else {
            IsOperational = false;
            IsOnStation = false;
        }
    }

    // Note: no need to detect when the AssignedShip dies as FleetCmd will set it
    // to null when it is removed from the fleet, whether through death or some other reason

    private void OnStationOffsetChanged() {
        // setting local position doesn't work as the resultant world position is modified by the rotation of FleetCmd
        _transform.position += StationOffset;
        // UNCLEAR when an FST changes its offset (location), does OnTriggerEnter/Exit detect it?
    }

    protected override void OnIsOperationalChanged() { }

    /// <summary>
    /// Manually detects whether the ship is on station by seeing whether the ship's
    /// position is inside the collider bounds.
    /// </summary>
    private bool IsShipAlreadyOnStation {
        get { return _collider.bounds.Contains(AssignedShip.Position); }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public string DisplayName { get { return FullName; } }

    public string FullName {
        get {
            string msg = AssignedShip == null ? " (unassigned)" : string.Format(" ({0})", AssignedShip.FullName);
            return GetType().Name + msg;
        }
    }

    public Vector3 Position { get { return _transform.position; } }

    public bool IsMobile { get { return true; } }

    public float Radius { get { return Constants.ZeroF; } }

    public Topography Topography { get { return References.SectorGrid.GetSpaceTopography(Position); } }

    #endregion

}

