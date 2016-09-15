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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Formation station for a ship in a Fleet formation.
/// <remarks>6.23.16 This Station is a MonoBehaviour because its' position relative to the Cmd
/// is key to its functionality. As Cmd moves and rotates, this station moves as its child using
/// its' local position value thereby keeping its position continuously accurate. This would be very
/// hard to do using a regular class.</remarks>
/// </summary>
public class FleetFormationStation : AFormationStation, IFleetFormationStation, IShipNavigable {

    private const string NameFormat = "{0}.{1}";

    /// <summary>
    /// Indicates whether the assignedShip is on its formation station.
    /// <remarks>The ship is OnStation if its entire CollisionDetectionZone is 
    /// within the FormationStation 'sphere' defined by Radius.
    /// </remarks>
    /// </summary>
    public bool IsOnStation { get { return DistanceToStation + AssignedShip.CollisionDetectionZoneRadius < Radius; } }

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
        set {
            SetProperty<IShip>(ref _assignedShip, value, "AssignedShip");
        }
    }

    public float DistanceToStation { get { return Vector3.Distance(Position, AssignedShip.Position); } }

    public float Radius { get { return TempGameValues.FleetFormationStationRadius; } }

    // Note: FormationStation's facing, as a child of FleetCmd, is always the same as FleetCmd's and Flagship's facing

    protected override void Validate() {
        base.Validate();
        // TODO
    }

    #region Event and Prop Change Handlers

    private void StationInfoPropChangedHandler() {
        transform.localPosition = StationInfo.LocalOffset;
    }

    private void OnSpawned() {
        //D.Log("{0}.OnSpawned() called.", FullName);
        InitializeDebugShowFleetFormationStation();
    }

    private void OnDespawned() {
        //D.Log("{0}.OnDespawned() called.", FullName);
        StationInfo = default(FormationStationSlotInfo);
        D.Assert(AssignedShip == null);
        CleanupDebugShowFleetFormationStation();
    }

    #endregion

    protected override void Cleanup() {
        CleanupDebugShowFleetFormationStation();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Formation Station

    private void InitializeDebugShowFleetFormationStation() {
        DebugControls debugValues = DebugControls.Instance;
        debugValues.showFleetFormationStationsChanged += ShowDebugFleetFormationStationsChangedEventHandler;
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
            debugValues.showFleetFormationStationsChanged -= ShowDebugFleetFormationStationsChangedEventHandler;
        }
        DrawSphereGizmo drawCntl = gameObject.GetComponent<DrawSphereGizmo>();
        if (drawCntl != null) {
            Destroy(drawCntl);
        }
    }

    #endregion

    #region INavigable Members

    public string DisplayName {
        get {
            string nameText = AssignedShip != null ? AssignedShip.DisplayName : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    public string FullName {
        get {
            string nameText = AssignedShip != null ? AssignedShip.FullName : "NoAssignedShip";
            return NameFormat.Inject(nameText, GetType().Name);
        }
    }

    public bool IsMobile { get { return true; } }

    public Vector3 Position { get { return transform.position; } }

    #endregion

    #region IShipNavigable Members

    public AutoPilotDestinationProxy GetApMoveTgtProxy(Vector3 tgtOffset, float tgtStandoffDistance, Vector3 shipPosition) {
        D.Assert(AssignedShip.CollisionDetectionZoneRadius.ApproxEquals(tgtStandoffDistance));   // its the same ship
        float outerShellRadius = Radius - tgtStandoffDistance;   // entire shipCollisionDetectionZone is within the FormationStation 'sphere'
        float innerShellRadius = Constants.ZeroF;
        return new AutoPilotDestinationProxy(this, tgtOffset, innerShellRadius, outerShellRadius);
    }

    #endregion


}

