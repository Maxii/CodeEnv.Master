// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiShipInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Element is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Element is selected.
/// </summary>
public class AiShipInteractibleHudForm : ANonUserItemNameChangeInteractibleHudForm {

    private const string CombatStanceLabelFormat = "Stance: {0}";

    public override FormID FormID { get { return FormID.AiShip; } }

    public new ShipReport Report { get { return base.Report as ShipReport; } }

    protected override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => Report.Item.Name, z => (Report.Item as IShip).Name = z);
    }

    protected override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = Report.Outputs;
    }

    protected override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        _offensiveStrengthGuiElement.Strength = Report.OffensiveStrength;
    }

    protected override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        _defensiveStrengthGuiElement.Strength = Report.DefensiveStrength;
    }

    protected override void AssignValueToCombatStanceGuiElement() {
        base.AssignValueToCombatStanceGuiElement();
        string stanceText = Report.CombatStance != ShipCombatStance.None ? Report.CombatStance.GetValueName() : Unknown;
        _combatStanceLabel.text = CombatStanceLabelFormat.Inject(stanceText);
    }

}

