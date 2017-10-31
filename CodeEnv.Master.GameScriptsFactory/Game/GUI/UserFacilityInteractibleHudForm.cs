// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserFacilityInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info and allow changes when a user-owned Item is selected.
/// </summary>
public class UserFacilityInteractibleHudForm : AUserItemInteractibleHudForm {

    public override FormID FormID { get { return FormID.UserFacility; } }

    public new FacilityData ItemData { get { return base.ItemData as FacilityData; } }

    protected override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => ItemData.Name, z => ItemData.Name = z);
    }

    protected override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = ItemData.Outputs;
    }

    protected override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        _offensiveStrengthGuiElement.Strength = ItemData.OffensiveStrength;
    }

    protected override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        _defensiveStrengthGuiElement.Strength = ItemData.DefensiveStrength;
    }

}

