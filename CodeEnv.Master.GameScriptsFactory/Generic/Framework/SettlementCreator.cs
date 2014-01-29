// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCreator.cs
//  Initialization class that deploys a Settlement that is available for assignment to a System.
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
///  Initialization class that deploys a Settlement that is available for assignment to a System.
///  When assigned, the Settlement relocates to the orbital slot for Settlements held open by the System.
/// </summary>
public class SettlementCreator : ACreator<FacilityItem, FacilityCategory, FacilityData, SettlementItem, BaseComposition> {

    public event Action<SettlementCreator> onCompleted;

    protected override GameState GetCreationGameState() {
        return GameState.DeployingSystems;  // Building can take place anytime? Placing in Systems takes place in DeployingSettlements
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
        return RequiredPrefabs.Instance.settlementCmd.gameObject;
    }

    protected override void AddCommandDataToCommand() {
        _command.Data = new SettlementData(PieceName) {
            Population = 100,
            CapacityUsed = 10,
            ResourcesUsed = new OpeYield(1.3F, 0.5F, 2.4F),
            SpecialResourcesUsed = new XYield(new XYield.XResourceValuePair(XResource.Special_1, 0.2F))
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
        _command.gameObject.GetSafeMonoBehaviourComponent<SettlementView>().enabled = true;
    }

    protected override void __InitializeCommandIntel() {
        // Settlements assume the intel state of their system when assigned
    }

    protected override void OnCompleted() {
        base.OnCompleted();
        var temp = onCompleted;
        if (temp != null) {
            temp(this);
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

