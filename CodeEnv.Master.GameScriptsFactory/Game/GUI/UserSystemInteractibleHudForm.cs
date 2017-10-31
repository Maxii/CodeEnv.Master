// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserSystemInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class UserSystemInteractibleHudForm : AUserItemInteractibleHudForm {

    public override FormID FormID { get { return FormID.UserSystem; } }

    public new SystemData ItemData { get { return base.ItemData as SystemData; } }

    protected override void HandleItemDataPropSet() {
        base.HandleItemDataPropSet();
        D.AssertNotNull(ItemData.SettlementData);   // if user system, must have a user settlement
    }

    protected override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => ItemData.Name, z => ItemData.Name = z);
    }

    protected override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = ItemData.SettlementData.UnitOutputs;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = ItemData.Resources;
    }

    protected override void AssignValueToPopulationGuiElement() {
        base.AssignValueToPopulationGuiElement();
        _populationGuiElement.Population = ItemData.SettlementData.Population;
    }

    protected override sealed void AssignValueToHeroChangeGuiElement() {
        base.AssignValueToHeroChangeGuiElement();
        _heroChgGuiElement.HeroDataReference = new Reference<Hero>(() => ItemData.SettlementData.Hero, z => ItemData.SettlementData.Hero = z);
    }


}

