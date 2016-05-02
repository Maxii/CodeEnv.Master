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
/// </summary>
public class FleetFormationStation : AMonoBase, IFleetFormationStation, IShipNavigable {

    private const string NameFormat = "{0}.{1}";

    /// <summary>
    /// Indicates whether the assignedShip is on its formation station.
    /// <remarks>The ship is OnStation if its entire CollisionDetectionZone is 
    /// within the FormationStation 'sphere' defined by Radius.
    /// </remarks>
    /// </summary>
    public bool IsOnStation { get { return DistanceToStation + AssignedShip.CollisionDetectionZoneRadius < Radius; } }

    private bool _isLocalOffsetSet;
    private Vector3 _localOffset;
    /// <summary>
    /// The offset of this station from FleetCmd in local space.
    /// </summary>
    public Vector3 LocalOffset {
        get { return _localOffset; }
        set { SetProperty<Vector3>(ref _localOffset, value, "LocalOffset", OnLocalOffsetSet); }
    }

    private IShipItem _assignedShip;
    public IShipItem AssignedShip {
        get { return _assignedShip; }
        set {
            D.Assert(_assignedShip == null);    // OPTIMIZE for now only one assignment
            SetProperty<IShipItem>(ref _assignedShip, value, "AssignedShip");
        }
    }

    public float DistanceToStation { get { return Vector3.Distance(Position, AssignedShip.Position); } }

    public float Radius { get { return TempGameValues.FleetFormationStationRadius; } }

    // Note: FormationStation's facing, as a child of FleetCmd, is always the same as FleetCmd's and Flagship's facing

    protected override void Awake() {
        base.Awake();
        InitializeDebugShowFleetFormationStation();
    }

    private void OnLocalOffsetSet() {
        D.Assert(!_isLocalOffsetSet);
        _isLocalOffsetSet = true;
        transform.localPosition = LocalOffset;
    }

    protected override void Cleanup() {
        CleanupDebugShowFleetFormationStation();
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region Debug Show Formation Station

    private void InitializeDebugShowFleetFormationStation() {
        DebugValues debugValues = DebugValues.Instance;
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
        EnableDebugShowFleetFormationStation(DebugValues.Instance.ShowFleetFormationStations);
    }

    private void CleanupDebugShowFleetFormationStation() {
        var debugValues = DebugValues.Instance;
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

    public string DisplayName { get { return NameFormat.Inject(AssignedShip.DisplayName, GetType().Name); } }

    public string FullName { get { return NameFormat.Inject(AssignedShip.FullName, GetType().Name); } }

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

