﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCreator.cs
// Initialization class that deploys a fleet at the location of this FleetCreator. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a fleet at the location of this FleetCreator. The fleet
/// deployed will simply be initialized if already present in the scene. If it is not present, then
/// it will be built and then initialized.
/// </summary>
public class FleetCreator : ACreator<ShipItem, ShipCategory, ShipData, FleetItem, FleetComposition> {

    protected override ShipData CreateElementData(ShipCategory elementCategory, string elementInstanceName, IPlayer owner) {
        float mass = TempGameValues.__GetMass(elementCategory);
        float drag = 0.1F;
        ShipData elementData = new ShipData(elementCategory, elementInstanceName, maxHitPoints: 50F, mass: mass, drag: drag) {   // TODO mass variation
            // optionalParentName gets set when it gets attached to a command
            Strength = new CombatStrength(),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
            Owner = owner,
            MaxTurnRate = UnityEngine.Random.RandomRange(45F, 315F),
            MaxThrust = mass * drag * UnityEngine.Random.Range(2F, 5F) // MaxThrust = Mass * Drag * MaxSpeed;
        };
        return elementData;
    }

    protected override GameObject GetCommandPrefab() {
        return RequiredPrefabs.Instance.fleetCmd.gameObject;
    }

    protected override void AddDataToComposition(ShipData elementData) {
        _composition.Add(elementData);
    }

    protected override IList<ShipCategory> GetCompositionCategories() {
        return _composition.Categories;
    }

    protected override IList<ShipData> GetCompositionData(ShipCategory elementCategory) {
        return _composition.GetData(elementCategory);
    }

    protected override IEnumerable<GameObject> GetElementPrefabs() {
        return RequiredPrefabs.Instance.ships.Select<ShipItem, GameObject>(s => s.gameObject);
    }

    protected override ShipCategory[] GetValidElementCategories() {
        return new ShipCategory[] { ShipCategory.Frigate, ShipCategory.Destroyer, ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override ShipCategory[] GetValidHQElementCategories() {
        return new ShipCategory[] { ShipCategory.Cruiser, ShipCategory.Carrier, ShipCategory.Dreadnaught };
    }

    protected override void AddCommandDataToCommand() {
        _command.Data = new FleetData(_pieceName);
    }

    protected override void MarkHQElement() {
        RandomExtended<ShipItem>.Choice(_elements).IsHQElement = true;
    }

    protected override void PositionElements() {
        float globeRadius = 1F * (float)Math.Pow(_elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements
        PositionElementsEquidistantInCircle(globeRadius);

        //if (!PositionElementsRandomlyInSphere(globeRadius)) {
        //    // try again with a larger radius
        //    D.Assert(PositionElementsRandomlyInSphere(globeRadius * 1.5F), "{0} Positioning Error.".Inject(_pieceName));
        //}
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    }

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<ShipView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<FleetView>().enabled = true;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

