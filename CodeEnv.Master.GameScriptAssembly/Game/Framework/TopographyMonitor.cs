// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TopographyMonitor.cs
// Monitor attached to regions of space that notifies ships of the SpaceTopography they are entering/exiting.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Monitor attached to regions of space that notifies ships of the SpaceTopography they are entering/exiting.
/// </summary>
public class TopographyMonitor : AMonoBase {

    private ITopographyMonitorable _itemMonitored;
    public ITopographyMonitorable ItemMonitored {
        get { return _itemMonitored; }
        set {
            D.Assert(_itemMonitored == null);   // should only occur 1 time
            SetProperty<ITopographyMonitorable>(ref _itemMonitored, value, "ItemMonitored", OnItemMonitoredChanged);
        }
    }

    /// <summary>
    /// Control for enabling/disabling the monitor's collider.
    /// </summary>
    private bool IsOperational {
        get { return _collider.enabled; }
        set {
            if (_collider.enabled != value) {
                _collider.enabled = value;
                OnIsOperationalChanged();
            }
        }
    }

    public Topography SurroundingTopography { get; set; }   // IMPROVE ItemMonitored should know about their surrounding topology

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        IsOperational = false;  // IsOperational controlled when ItemMonitored added
    }

    void OnTriggerEnter(Collider other) {
        if (other.isTrigger) { return; }
        D.Log("{0}.{1}.OnTriggerEnter() tripped by Collider {2}. Distance from Monitor = {3}.",
        ItemMonitored.FullName, GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
        var enteringShip = other.gameObject.GetInterface<IShipItem>();
        if (enteringShip != null) {
            enteringShip.OnTopographicBoundaryTransition(ItemMonitored.Topography);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.isTrigger) { return; }
        D.Log("{0}.{1}.OnTriggerExit() tripped by Collider {2}. Distance from Monitor = {3}.",
        ItemMonitored.FullName, GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
        var exitingShip = other.gameObject.GetInterface<IShipItem>();
        if (exitingShip != null) {
            exitingShip.OnTopographicBoundaryTransition(SurroundingTopography);
        }
    }

    private void OnItemMonitoredChanged() {
        _collider.radius = ItemMonitored.Radius;
        IsOperational = true;
    }

    private void OnIsOperationalChanged() { }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

