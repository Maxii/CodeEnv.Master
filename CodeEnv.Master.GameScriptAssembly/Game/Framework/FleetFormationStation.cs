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
public class FleetFormationStation : AFormationStation, IFleetFormationStation, IShipNavigableDestination, IFacilityRepairCapable,
    IEquatable<FleetFormationStation> {

    private const string NameFormat = "{0}.{1}";

    private static int _UniqueIDCount = Constants.One;

    public string DebugName {
        get {
            string nameText = AssignedShip != null ? AssignedShip.DebugName : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    /// <summary>
    /// Indicates whether the assignedShip is completely on its formation station.
    /// <remarks>The ship is OnStation if its entire CollisionDetectionZone is 
    /// within the FormationStation 'sphere' defined by Radius.</remarks>
    /// </summary>
    public bool IsOnStation {
        get { return Vector3.SqrMagnitude(Position - AssignedShip.Position).IsLessThan(_maxStationToOnStationDistanceSqrd); }
    }

    /// <summary>
    /// The distance to being completely OnStation.
    /// <remarks>Used for OnStation debugging.</remarks>
    /// </summary>
    public float __DistanceToOnStation {
        get {
            float distanceFromOnStation = Vector3.Distance(Position, AssignedShip.Position) - _maxStationToOnStationDistance;
            if (distanceFromOnStation < Constants.ZeroF) {
                // ship is completely contained within the sphere with radius _maxStationToOnStationDistance
                distanceFromOnStation = Constants.ZeroF;
            }
            return distanceFromOnStation;
        }
    }

    /// <summary>
    /// The offset of this station from FleetCmd in (FleetCmds?) local space.
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
        set { SetProperty<IShip>(ref _assignedShip, value, "AssignedShip", AssignedShipPropChangedHandler, __AssignedShipPropChangingHandler); }
    }

    private Player Owner { get { return AssignedShip != null ? AssignedShip.Owner : TempGameValues.NoPlayer; } }

    public float Radius { get { return TempGameValues.FleetFormationStationRadius; } }

    private bool ShowDebugLog { get { return AssignedShip != null ? AssignedShip.ShowDebugLog : false; } }

    // Note: FormationStation's facing, as a child of FleetCmd, is always the same as FleetCmd's and Flagship's facing

    private float _maxStationToOnStationDistanceSqrd;
    /// <summary>
    /// The maximum distance from the station's center to a point within the station's radius that qualifies
    /// the ship as being 'OnStation'. Effectively the StationRadius - the ship's CollisionDetectionZoneRadius.
    /// </summary>
    private float _maxStationToOnStationDistance;
    private int _uniqueID;

    #region Event and Prop Change Handlers

    private void __AssignedShipPropChangingHandler(IShip incomingShip) {
        if (incomingShip == null) {
            D.Log(ShowDebugLog, "{0}.AssignedShip is changing to null in Frame {1}.", DebugName, Time.frameCount);
        }
    }

    private void AssignedShipPropChangedHandler() {
        __ValidateShipSize();
        CalcMaxStationToOnStationDistanceValues();
    }

    private void StationInfoPropChangedHandler() {
        transform.localPosition = StationInfo.LocalOffset;
    }

    private void OnSpawned() {
        //D.Log(ShowDebugLog, "{0}.OnSpawned() called.", DebugName);
        D.AssertEqual(Constants.Zero, _uniqueID);
        D.AssertEqual(Constants.Zero, _maxStationToOnStationDistance);
        D.AssertEqual(Constants.Zero, _maxStationToOnStationDistanceSqrd);
        InitializeDebugShowFleetFormationStation();
        _uniqueID = _UniqueIDCount;
        _UniqueIDCount++;
    }

    private void OnDespawned() {
        //D.Log(ShowDebugLog, "{0}.OnDespawned() called.", DebugName);
        StationInfo = default(FormationStationSlotInfo);
        D.AssertNull(AssignedShip);
        CleanupDebugShowFleetFormationStation();
        D.AssertNotEqual(Constants.Zero, _uniqueID);
        _uniqueID = Constants.Zero;
        _maxStationToOnStationDistance = Constants.ZeroF;
        _maxStationToOnStationDistanceSqrd = Constants.ZeroF;
    }

    #endregion

    /// <summary>
    /// Returns <c>true</c> if AssignedShip is still making progress toward this station, <c>false</c>
    /// if progress is no longer being made as the ship has arrived OnStation. If still making
    /// progress, direction and distance to the station are valid.
    /// </summary>
    /// <param name="onStationDirection">The direction to being OnStation.</param>
    /// <param name="onStationDistance">The distance to being OnStation.</param>
    /// <returns></returns>
    public bool TryCheckProgressTowardStation(out Vector3 onStationDirection, out float onStationDistance) {
        D.AssertNotNull(AssignedShip, "{0}: AssignedShip is null. Frame {1}.".Inject(DebugName, Time.frameCount));
        onStationDirection = Vector3.zero;
        onStationDistance = Constants.ZeroF;
        var vectorToOnStation = Position - AssignedShip.Position;
        float distanceToStationSqrd = Vector3.SqrMagnitude(vectorToOnStation);
        if (distanceToStationSqrd.IsLessThan(_maxStationToOnStationDistanceSqrd)) {
            D.Assert(IsOnStation);
            // ship has arrived
            return false;
        }

        onStationDirection = vectorToOnStation.normalized;
        onStationDistance = Mathf.Sqrt(distanceToStationSqrd) - _maxStationToOnStationDistance;
        if (onStationDistance.IsGreaterThan(Constants.ZeroF)) {
            // Effectively means onStationDistance > 0.0001 FloatEqualityPrecision
            // 3.28.17 saw onStationDistance = -5E-05, aka -0.00005 which failed D.Assert(onStationDistance > Constants.ZeroF);
            return true;
        }
        // ship has arrived
        return false;
    }

    private void CalcMaxStationToOnStationDistanceValues() {
        if (AssignedShip != null) {
            _maxStationToOnStationDistance = Radius - AssignedShip.CollisionDetectionZoneRadius;
            _maxStationToOnStationDistanceSqrd = _maxStationToOnStationDistance * _maxStationToOnStationDistance;
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
        return DebugName;
    }

    #region Debug

    [System.Diagnostics.Conditional("DEBUG")]
    private void __ValidateShipSize() {
        if (AssignedShip != null) {
            if (AssignedShip.CollisionDetectionZoneRadius * 1.5F > Radius) {
                D.Warn("{0}.CollisionDetectionZoneRadius {1:0.00} is too large for {2}.Radius of {3:0.00}.",
                    DebugName, AssignedShip.CollisionDetectionZoneRadius, typeof(FleetFormationStation).Name, Radius);
            }
        }
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

    #endregion

    #region IEquatable<FleetFormationStation> Members

    public bool Equals(FleetFormationStation other) {
        // if the same instance and _uniqueID are equal, then its the same
        return base.Equals(other) && _uniqueID == other._uniqueID;  // need instance comparison as _uniqueID is 0 in PoolMgr
    }

    #endregion

    #region INavigableDestination Members

    public string Name {
        get {
            string nameText = AssignedShip != null ? AssignedShip.Name : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    public bool IsMobile { get { return true; } }

    public Vector3 Position { get { return transform.position; } }

    public bool IsOperational { get { return AssignedShip != null; } }

    #endregion

    #region IShipNavigableDestination Members

    public ApMoveDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, IShip ship) {
        D.Assert(AssignedShip.CollisionDetectionZoneRadius.ApproxEquals(tgtStandoffDistance));   // its the same ship
        D.AssertApproxEqual(tgtStandoffDistance, ship.CollisionDetectionZoneRadius);
        float outerShellRadius = Radius - tgtStandoffDistance;   // entire shipCollisionDetectionZone is within the FormationStation 'sphere'
        float innerShellRadius = Constants.ZeroF;
        return new ApMoveFormationStationProxy(this, ship, innerShellRadius, outerShellRadius);
    }

    #endregion

    #region IRepairCapable Members

    /// <summary>
    /// Indicates whether the player is currently allowed to repair at this item.
    /// A player is always allowed to repair items if the player doesn't know who, if anyone, is the owner.
    /// A player is not allowed to repair at the item if the player knows who owns the item and they are enemies.
    /// <remarks>This implementation simply tests whether the player is the Owner of this FormationStation.</remarks>
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns></returns>
    public bool IsRepairingAllowedBy(Player player) {
        return Owner == player;
    }

    /// <summary>
    /// Gets the repair capacity available for this Unit's CmdModule in hitPts per day.
    /// </summary>
    /// <param name="unitCmd">The unit command module.</param>
    /// <param name="hqElement">The HQElement.</param>
    /// <param name="cmdOwner">The command owner.</param>
    /// <returns></returns>
    public float GetAvailableRepairCapacityFor(IUnitCmd_Ltd unitCmd, IUnitElement_Ltd hqElement, Player cmdOwner) {
        D.AssertEqual(Owner, cmdOwner);
        float basicValue = TempGameValues.RepairCapacityBaseline_FormationStation_CmdModule;
        return basicValue;
    }

    #endregion

    #region IShipRepairCapable Members

    public float GetAvailableRepairCapacityFor(IShip_Ltd ship, Player elementOwner) {
        D.AssertEqual(Owner, elementOwner);
        float basicValue = TempGameValues.RepairCapacityBaseline_FormationStation_Element;
        return basicValue;
    }

    #endregion

    #region IFacilityRepairCapable Members

    public float GetAvailableRepairCapacityFor(IFacility_Ltd facility, Player elementOwner) {
        D.AssertEqual(Owner, elementOwner);
        float basicValue = TempGameValues.RepairCapacityBaseline_FormationStation_Element;
        return basicValue;
    }

    #endregion

}

