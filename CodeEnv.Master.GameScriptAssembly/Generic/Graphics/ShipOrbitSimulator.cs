// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipOrbitSimulator.cs
// Simulates orbiting around an immobile parent of any ships attached by a fixed joint.
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
/// Simulates orbiting around an immobile parent of any ships attached by a fixed joint.
/// This is also an INavigableTarget which allows it to be used as a destination by a Ship's AutoPilot.
/// </summary>
public class ShipOrbitSimulator : OrbitSimulator, IShipOrbitSimulator, INavigableTarget {

    private const string NameFormat = "{0}.{1}";

    public Rigidbody Rigidbody { get; private set; }

    public new ShipOrbitSlot OrbitSlot {
        protected get { return base.OrbitSlot as ShipOrbitSlot; }
        set { base.OrbitSlot = value; }
    }

    protected override void Awake() {
        base.Awake();
        Rigidbody = UnityUtility.ValidateComponentPresence<Rigidbody>(gameObject);
        Rigidbody.isKinematic = true;
        Rigidbody.useGravity = false;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region INavigableTarget Members

    public string DisplayName { get { return FullName; } }

    public string FullName { get { return NameFormat.Inject(OrbitSlot.OrbitedObject.FullName, GetType().Name); } }

    public Vector3 Position { get { return OrbitSlot.OrbitedObject.Position; } }    // OPTIMIZE

    public bool IsMobile { get { return OrbitSlot.IsOrbitedObjectMobile; } }

    public float Radius { get { return OrbitSlot.OuterRadius; } }

    public Topography Topography { get { return References.SectorGrid.GetSpaceTopography(Position); } }

    public float RadiusAroundTargetContainingKnownObstacles { get { return OrbitSlot.InnerRadius; } }

    public float GetShipArrivalDistance(float shipCollisionDetectionRadius) {
        return OrbitSlot.OuterRadius - shipCollisionDetectionRadius;   // entire shipCollisionDetectionZone is inside the OrbitSlot

    }

    #endregion
}

