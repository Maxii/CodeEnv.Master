// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UniverseCenterModel.cs
// The Item at the center of the universe.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// The Item at the center of the universe.
/// </summary>
public class UniverseCenterModel : AItemModel, INavigableTarget, IShipOrbitable {

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        D.Assert(Mathfx.Approx(Radius, TempGameValues.UniverseCenterRadius, 1F));    // 50
        collider.isTrigger = false;
        (collider as SphereCollider).radius = Radius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetFirstComponentInImmediateChildrenOnly<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public Topography Topography { get { return Data.Topography; } }

    #endregion

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion


}

