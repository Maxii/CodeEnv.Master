// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarItem.cs
// The data-holding class for all Stars in the game.
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
/// The data-holding class for all Stars in the game.
/// </summary>
public class StarModel : AOwnedItemModel, INavigableTarget, IShipOrbitable {

    public StarCategory category;

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderer = gameObject.GetFirstComponentInImmediateChildrenOnly<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        collider.isTrigger = false;
        (collider as SphereCollider).radius = Radius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
        //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.ShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetFirstComponentInImmediateChildrenOnly<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.ShipTransitBan);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    protected override void Initialize() {
        D.Assert(category == Data.Category);
    }

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

