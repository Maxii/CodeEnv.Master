// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UserShipInteractibleHudForm.cs
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
public class UserShipInteractibleHudForm : AUserItemInteractibleHudForm {

    public override FormID FormID { get { return FormID.UserShip; } }

    public new ShipData ItemData { get { return base.ItemData as ShipData; } }

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

    protected override void AssignValueToCombatStanceChangeGuiElement() {
        base.AssignValueToCombatStanceChangeGuiElement();
        _combatStanceChgGuiElement.CombatStanceReference = new Reference<ShipCombatStance>(() => ItemData.CombatStance, z => ItemData.CombatStance = z);
    }

}

