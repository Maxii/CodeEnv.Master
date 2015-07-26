// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AUnitBaseCmdModel.cs
// Abstract base class for a UnitBaseCmdModel, an object that commands Facilities. 
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
/// Abstract base class for a UnitBaseCmdModel, an object that commands Facilities. 
/// </summary>
public abstract class AUnitBaseCmdModel : AUnitCommandModel, IShipOrbitable {

    protected override void InitializeRadiiComponents() {
        base.InitializeRadiiComponents();
        // the radius of a BaseCommand is fixed to include all of its elements
        Radius = TempGameValues.BaseRadius;
        InitializeShipOrbitSlot();
        InitializeKeepoutZone();
    }

    private void InitializeShipOrbitSlot() {
        float innerOrbitRadius = Radius * TempGameValues.KeepoutRadiusMultiplier;
        float outerOrbitRadius = innerOrbitRadius + TempGameValues.DefaultShipOrbitSlotDepth;
        ShipOrbitSlot = new ShipOrbitSlot(innerOrbitRadius, outerOrbitRadius, this);
    }

    private void InitializeKeepoutZone() {
        SphereCollider keepoutZoneCollider = gameObject.GetComponentsInImmediateChildrenOnly<SphereCollider>().Where(c => c.isTrigger).Single();
        D.Assert(keepoutZoneCollider.gameObject.layer == (int)Layers.CelestialObjectKeepout);
        keepoutZoneCollider.isTrigger = true;
        keepoutZoneCollider.radius = ShipOrbitSlot.InnerRadius;
    }

    public override void AddElement(AUnitElementModel element) {
        base.AddElement(element);

        element.Command = this;
        if (HQElement != null) {
            _formationGenerator.RegenerateFormation();    // Bases simply regenerate the formation when adding an element
        }
    }

    #region IShipOrbitable Members

    public ShipOrbitSlot ShipOrbitSlot { get; private set; }

    #endregion


}

