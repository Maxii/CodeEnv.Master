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
public class StarModel : AOwnedItemModel, IStarModel, IDestinationTarget, IShipOrbitable {

    public new StarData Data {
        get { return base.Data as StarData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void InitializeRadiiComponents() {
        var meshRenderer = gameObject.GetComponentInImmediateChildren<Renderer>();
        Radius = meshRenderer.bounds.size.x / 2F;    // half of the (length, width or height, all the same surrounding a sphere)
        (collider as SphereCollider).radius = Radius;
        //D.Log("{0}.Radius set to {1}.", FullName, Radius);
    }

    protected override void OnDataChanged() {
        base.OnDataChanged();
        SetKeepoutZoneRadius();
    }

    private void SetKeepoutZoneRadius() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentInImmediateChildren<SphereCollider>();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.radius = Data.ShipOrbitSlot.InnerRadius;
    }

    protected override void Initialize() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IDestinationTarget Members

    public Vector3 Position { get { return Data.Position; } }

    public virtual bool IsMobile { get { return false; } }

    public SpaceTopography Topography { get { return Data.Topography; } }

    #endregion

    #region IShipOrbitable Members

    public OrbitalSlot ShipOrbitSlot { get { return Data.ShipOrbitSlot; } }

    public void AssumeOrbit(IShipModel ship) {
        IOrbiterForShips orbiter;
        var orbiterTransform = _transform.GetTransformWithInterfaceInChildren<IOrbiterForShips>(out orbiter);
        if (orbiterTransform != null) {
            References.UnitFactory.AttachShipToOrbiter(ship, ref orbiterTransform);
        }
        else {
            References.UnitFactory.AttachShipToOrbiter(gameObject, ship, orbitedObjectIsMobile: false);
        }
    }

    public void LeaveOrbit(IShipModel orbitingShip) {
        IOrbiterForShips orbiter;
        var orbiterTransform = _transform.GetTransformWithInterfaceInChildren<IOrbiterForShips>(out orbiter);
        D.Assert(orbiterTransform != null, "{0}.{1} is not present.".Inject(FullName, typeof(IOrbiterForShips).Name));
        var ship = orbiterTransform.gameObject.GetSafeInterfacesInChildren<IShipModel>().Single(s => s == orbitingShip);
        var parentFleetTransform = ship.Command.Transform.parent;
        ship.Transform.parent = parentFleetTransform;
        // OPTIMIZE remove empty orbiters?
    }

    #endregion

}

