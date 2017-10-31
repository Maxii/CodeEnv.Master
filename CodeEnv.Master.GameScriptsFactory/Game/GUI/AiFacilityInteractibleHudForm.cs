// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiFacilityInteractibleHudForm.cs
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
public class AiFacilityInteractibleHudForm : ANonUserItemNameChangeInteractibleHudForm {

    public override FormID FormID { get { return FormID.AiFacility; } }

    public new FacilityReport Report { get { return base.Report as FacilityReport; } }

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

}

