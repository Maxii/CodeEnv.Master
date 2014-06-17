// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TopographyMonitor.cs
// Notifies ships of the SpaceTopography they are entering/exiting.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Notifies ships of the SpaceTopography they are entering/exiting.
/// </summary>
public class TopographyMonitor : AMonoBase {

    public SpaceTopography Topography { get; set; }

    public SpaceTopography SurroundingTopography { get; set; }

    private float _topographyRadius;
    public float TopographyRadius {
        get { return _topographyRadius; }
        set { SetProperty<float>(ref _topographyRadius, value, "TopographyRadius", OnRadiusChanged); }
    }

    private SphereCollider _collider;

    protected override void Awake() {
        base.Awake();
        _collider = UnityUtility.ValidateComponentPresence<SphereCollider>(gameObject);
        _collider.isTrigger = true;
        enabled = false;
        // creators enable this notifier when the objects they create (systems, nebulas, etc.) become operational 
    }

    void OnTriggerEnter(Collider other) {
        if (enabled) {
            if (other.isTrigger) { return; }
            //D.Log("{0}.OnTriggerEnter() tripped by Collider {1}. Distance from Monitor = {2}.",
            //GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
            IShipModel enteringShip = other.gameObject.GetInterface<IShipModel>();
            if (enteringShip != null) {
                enteringShip.OnTopographicBoundaryTransition(Topography);
            }
        }
    }

    void OnTriggerExit(Collider other) {
        if (enabled) {
            if (other.isTrigger) { return; }
            //D.Log("{0}.OnTriggerExit() tripped by Collider {1}. Distance from Monitor = {2}.",
            //GetType().Name, other.name, Vector3.Magnitude(other.transform.position - _transform.position));
            IShipModel exitingShip = other.gameObject.GetInterface<IShipModel>();
            if (exitingShip != null) {
                exitingShip.OnTopographicBoundaryTransition(SurroundingTopography);
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

    private void OnRadiusChanged() {
        // UNCLEAR if enter/exit works automatically when radius changes
        _collider.radius = TopographyRadius;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

