// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAiUnitInteractibleHudForm.cs
// Abstract class for AI-owned UnitForms used by the InteractibleHud.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract class for AI-owned UnitForms used by the InteractibleHud.
/// </summary>
public abstract class AAiUnitInteractibleHudForm : ANonUserItemNameChangeInteractibleHudForm {

    private const string FormationLabelFormat = "Formation: {0}";

    public new AUnitCmdReport Report { get { return base.Report as AUnitCmdReport; } }

    protected sealed override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => Report.Item.UnitName, z => (Report.Item as IUnitCmd).UnitName = z);
    }

    protected sealed override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = Report.UnitOutputs;
    }

    protected override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        _offensiveStrengthGuiElement.Strength = Report.UnitOffensiveStrength;
    }

    protected override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        _defensiveStrengthGuiElement.Strength = Report.UnitDefensiveStrength;
    }

    protected sealed override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroGuiElement.Hero = Report.Hero;
    }

    protected sealed override void AssignValueToFormationGuiElement() {
        base.AssignValueToFormationGuiElement();
        string formationText = Report.Formation != Formation.None ? Report.Formation.GetValueName() : Unknown;
        _formationLabel.text = FormationLabelFormat.Inject(formationText);
    }

}

