// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlanetModel.cs
// The data-holding class for all planets in the game. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// The data-holding class for all planets in the game. 
/// </summary>
public class PlanetModel : APlanetoidModel, IPlanetModel /*, IShipOrbitable*/ {

    public new PlanetData Data {
        get { return base.Data as PlanetData; }
        set { base.Data = value; }
    }

    protected override void Awake() {
        base.Awake();
        Subscribe();
    }

    protected override void Initialize() {
        base.Initialize();
        float orbitalRadius = Data.SystemOrbitSlot.MeanRadius;
        Data.OrbitalSpeed = gameObject.GetSafeMonoBehaviourComponentInParents<Orbiter>().GetSpeedOfBodyInOrbit(orbitalRadius);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    //#region IShipOrbitable Members

    //public void AssumeOrbit(IShipModel ship) {
    //    var shipOrbit = gameObject.GetComponentInImmediateChildren<ShipOrbit>();
    //    if (shipOrbit == null) {
    //        UnitFactory.Instance.MakeShipOrbitInstance(gameObject, ship);
    //    }
    //    else {
    //        UnitFactory.Instance.AttachShipToShipOrbit(ship, ref shipOrbit);
    //    }
    //}

    //public void LeaveOrbit(IShipModel orbitingShip) {
    //    var shipOrbit = gameObject.GetComponentInImmediateChildren<ShipOrbit>();
    //    D.Assert(shipOrbit != null, "{0}.{1} is not present.".Inject(FullName, typeof(ShipOrbit).Name));
    //    var ship = shipOrbit.gameObject.GetSafeInterfacesInChildren<IShipModel>().Single(s => s == orbitingShip);
    //    var parentFleetTransform = ship.Command.Transform.parent;
    //    ship.Transform.parent = parentFleetTransform;
    //}

    //#endregion
}

