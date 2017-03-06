// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetFormationStation.cs
// Formation station for a ship in a Fleet formation.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>
/// Formation station for a ship in a Fleet formation.
/// <remarks>6.23.16 This Station is a MonoBehaviour because its' position relative to the Cmd
/// is key to its functionality. As Cmd moves and rotates, this station moves as its child using
/// its' local position value thereby keeping its position continuously accurate. This would be very
/// hard to do using a regular class.</remarks>
/// <remarks>2.15.17 Added IEquatable to allow pool-generated instances to be used in Dictionary and HashSet.
/// Without it, a reused instance appears to be equal to another reused instance if from the same instance. Probably doesn't matter
/// as only 1 reused instance from an instance can exist at the same time, but...</remarks>
/// </summary>
public class FleetFormationStation : AFormationStation, IFleetFormationStation, IShipNavigable, IEquatable<FleetFormationStation> {

    private const string NameFormat = "{0}.{1}";

    private static int _UniqueIDCount = Constants.One;

    /// <summary>
    /// Indicates whether the assignedShip is completely on its formation station.
    /// <remarks>The ship is OnStation if its entire CollisionDetectionZone is 
    /// within the FormationStation 'sphere' defined by Radius.</remarks>
    /// <remarks>Algorithm boils down to: shipPositionDistanceFromStationCenter &lt; StationRadius -  ShipCollisionDetectionZoneRadius</remarks>
    /// </summary>
    public bool IsOnStation { get { return (Position - AssignedShip.Position).sqrMagnitude < Mathf.Pow(Radius - AssignedShip.CollisionDetectionZoneRadius, 2F); } }

    /// <summary>
    /// The distance away from being completely OnStation.
    /// <remarks>Used for OnStation debugging.</remarks>
    /// </summary>
    public float __DistanceFromOnStation {
        get {
            if (IsOnStation) { return Constants.ZeroF; }
            return Vector3.Distance(Position, AssignedShip.Position) + AssignedShip.CollisionDetectionZoneRadius - Radius;
        }
    }

    /// <summary>
    /// The offset of this station from FleetCmd in local space.
    /// </summary>
    public Vector3 LocalOffset { get { return StationInfo.LocalOffset; } }

    private FormationStationSlotInfo _stationInfo;
    public FormationStationSlotInfo StationInfo {
        get { return _stationInfo; }
        set { SetProperty<FormationStationSlotInfo>(ref _stationInfo, value, "StationInfo", StationInfoPropChangedHandler); }
    }

    private IShip _assignedShip;
    public IShip AssignedShip {
        get { return _assignedShip; }
        set { SetProperty<IShip>(ref _assignedShip, value, "AssignedShip", AssignedShipPropChangedHandler); }
    }

    public float Radius { get { return TempGameValues.FleetFormationStationRadius; } }

    // Note: FormationStation's facing, as a child of FleetCmd, is always the same as FleetCmd's and Flagship's facing

    private int _uniqueID;

    #region Event and Prop Change Handlers

    private void AssignedShipPropChangedHandler() {
        ValidateShipSize();
    }

    private void StationInfoPropChangedHandler() {
        transform.localPosition = StationInfo.LocalOffset;
    }

    private void OnSpawned() {
        //D.Log("{0}.OnSpawned() called.", DebugName);
        D.AssertEqual(Constants.Zero, _uniqueID);
        InitializeDebugShowFleetFormationStation();
        _uniqueID = _UniqueIDCount;
        _UniqueIDCount++;
    }

    private void OnDespawned() {
        //D.Log("{0}.OnDespawned() called.", DebugName);
        StationInfo = default(FormationStationSlotInfo);
        D.AssertNull(AssignedShip);
        CleanupDebugShowFleetFormationStation();
        D.AssertNotEqual(Constants.Zero, _uniqueID);
        _uniqueID = Constants.Zero;
    }

    #endregion

    private void ValidateShipSize() {
        if (AssignedShip != null) {
            if (AssignedShip.CollisionDetectionZoneRadius * 1.5F > Radius) {
                D.Warn("{0}.CollisionDetectionZoneRadius {1:0.00} is too large for {2}.Radius of {3:0.00}.",
                    DebugName, AssignedShip.CollisionDetectionZoneRadius, typeof(FleetFormationStation).Name, Radius);
            }
        }
    }

    protected override void Cleanup() {
        CleanupDebugShowFleetFormationStation();
    }

    #region Object.Equals and GetHashCode Override

    public override bool Equals(object obj) {
        if (!(obj is FleetFormationStation)) { return false; }
        return Equals((FleetFormationStation)obj);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// See "Page 254, C# 4.0 in a Nutshell."
    /// </summary>
    /// <returns>
    /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
    /// </returns>
    public override int GetHashCode() {
        unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
            int hash = base.GetHashCode();
            hash = hash * 31 + _uniqueID.GetHashCode(); // 31 = another prime number
            return hash;
        }
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Formation Station

    private void InitializeDebugShowFleetFormationStation() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showFleetFormationStations += ShowDebugFleetFormationStationsChangedEventHandler;
        if (debugValues.ShowFleetFormationStations) {
            EnableDebugShowFleetFormationStation(true);
        }
    }

    private void EnableDebugShowFleetFormationStation(bool toEnable) {
        DrawSphereGizmo drawCntl = gameObject.AddMissingComponent<DrawSphereGizmo>();
        drawCntl.Radius = Radius;
        drawCntl.Color = Color.white;
        drawCntl.enabled = toEnable;
    }

    private void ShowDebugFleetFormationStationsChangedEventHandler(object sender, EventArgs e) {
        EnableDebugShowFleetFormationStation(DebugControls.Instance.ShowFleetFormationStations);
    }

    private void CleanupDebugShowFleetFormationStation() {
        var debugValues = DebugControls.Instance;
        if (debugValues != null) {
            debugValues.showFleetFormationStations -= ShowDebugFleetFormationStationsChangedEventHandler;
        }
        Profiler.BeginSample("Editor-only GC allocation (GetComponent returns null)", gameObject);
        DrawSphereGizmo drawCntl = gameObject.GetComponent<DrawSphereGizmo>();
        Profiler.EndSample();

        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #region INavigable Members

    public string Name {
        get {
            string nameText = AssignedShip != null ? AssignedShip.Name : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    public string DebugName {
        get {
            string nameText = AssignedShip != null ? AssignedShip.DebugName : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    public bool IsMobile { get { return true; } }

    public Vector3 Position { get { return transform.position; } }

    public bool IsOperational { get { return AssignedShip != null; } }


    #endregion

    #region IShipNavigable Members

    public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        D.Assert(AssignedShip.CollisionDetectionZoneRadius.ApproxEquals(tgtStandoffDistance));   // its the same ship
        float outerShellRadius = Radius - tgtStandoffDistance;   // entire shipCollisionDetectionZone is within the FormationStation 'sphere'
        float innerShellRadius = Constants.ZeroF;
        return new ApMoveDestinationProxy(this, ship, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IEquatable<FleetFormationStation> Members

    public bool Equals(FleetFormationStation other) {
        // if the same instance and _uniqueID are equal, then its the same
        return base.Equals(other) && _uniqueID == other._uniqueID;  // need instance comparison as _uniqueID is 0 in PoolMgr
    }

    #endregion

}

