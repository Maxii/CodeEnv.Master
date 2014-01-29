// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCreator.cs
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
public class StarbaseCreator : ACreator<FacilityItem, FacilityCategory, FacilityData, StarbaseItem, BaseComposition> {

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;
    }

    protected override FacilityData CreateElementData(FacilityCategory elementCategory, string elementInstanceName, IPlayer owner) {
        FacilityData elementData = new FacilityData(elementCategory, elementInstanceName, maxHitPoints: 50F, mass: 10000F) {   // TODO mass variation
            // optionalParentName gets set when it gets attached to a command
            Strength = new CombatStrength(),
            CurrentHitPoints = UnityEngine.Random.Range(25F, 50F),
            Owner = owner,
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
        return RequiredPrefabs.Instance.facilities.Select<FacilityItem, GameObject>(f => f.gameObject);
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

    protected override void AddCommandDataToCommand() {
        _command.Data = new StarbaseData(PieceName);
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
        _command.gameObject.GetSafeMonoBehaviourComponent<StarbaseView>().enabled = true;
    }

    protected override void __InitializeCommandIntel() {
        _command.gameObject.GetSafeInterface<ICommandViewable>().PlayerIntel = new Intel(IntelScope.Comprehensive, IntelSource.InfoNet);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

