// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseUnitCreator.cs
// Initialization class that deploys a Starbase at the location of this StarbaseCreator.
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
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Initialization class that deploys a Starbase at the location of this StarbaseCreator. 
/// </summary>
public class StarbaseUnitCreator : AUnitCreator<FacilityModel, FacilityCategory, FacilityData, StarbaseCmdModel, BaseComposition> {

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;
    }

    protected override FacilityData CreateElementData(FacilityCategory elementCategory, string elementInstanceName) {
        FacilityData elementData = new FacilityData(elementCategory, elementInstanceName, maxHitPoints: 50F, mass: 10000F) {   // TODO mass variation
            // optionalParentName gets set when it gets attached to a command
            Strength = new CombatStrength(),
            WeaponRange = UnityEngine.Random.Range(5F, 10F),
            WeaponFireRate = UnityEngine.Random.Range(1F, 3F),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
        };
        return elementData;
    }

    protected override void AddDataToComposition(FacilityData elementData) {
        _composition.Add(elementData);
    }

    protected override IList<FacilityCategory> GetCompositionCategories() {
        return _composition.Categories;
    }

    protected override IList<FacilityData> GetCompositionData(FacilityCategory elementCategory) {
        return _composition.GetData(elementCategory);
    }

    protected override IEnumerable<GameObject> GetElementPrefabs() {
        return RequiredPrefabs.Instance.facilities.Select<FacilityModel, GameObject>(f => f.gameObject);
    }

    protected override FacilityCategory[] GetValidElementCategories() {
        return new FacilityCategory[] { FacilityCategory.Construction, FacilityCategory.Defense, FacilityCategory.Economic, FacilityCategory.Science };
    }

    protected override FacilityCategory[] GetValidHQElementCategories() {
        return new FacilityCategory[] { FacilityCategory.CentralHub };
    }

    protected override GameObject GetCommandPrefab() {
        return RequiredPrefabs.Instance.starbaseCmd.gameObject;
    }

    protected override void InitializeCommandData(IPlayer owner) {
        _command.Data = new StarbaseCmdData(UnitName, 10F) {
            Strength = new CombatStrength(0F, 10F, 0F, 10F, 0F, 10F),  // no offense, strong defense
            Owner = owner
        };
    }

    protected override void MarkHQElement() {
        _elements.Single(e => (e.Data as FacilityData).Category == FacilityCategory.CentralHub).IsHQElement = true;
    }

    protected override void PositionElements() {
        float globeRadius = 1F * (float)Math.Pow(_elements.Count * 0.2F, 0.33F);  // cube root of number of groups of 5 elements
        PositionElementsEquidistantInCircle(globeRadius);

        //if (!PositionElementsRandomlyInSphere(globeRadius)) {
        //    // try again with a larger radius
        //    D.Assert(PositionElementsRandomlyInSphere(globeRadius * 1.5F), "{0} Positioning Error.".Inject(_pieceName));
        //}
    }

    protected override void EnableViews() {
        _elements.ForAll(e => e.gameObject.GetSafeMonoBehaviourComponent<FacilityView>().enabled = true);
        _command.gameObject.GetSafeMonoBehaviourComponent<StarbaseCmdView>().enabled = true;
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel.CurrentCoverage = IntelCoverage.Comprehensive;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

