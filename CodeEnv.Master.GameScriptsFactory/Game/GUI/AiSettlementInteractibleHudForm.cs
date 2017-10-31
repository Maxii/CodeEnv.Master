// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AiSettlementInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Unit is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info (and allow name changes) when a AI-owned Unit is selected.
/// </summary>
public class AiSettlementInteractibleHudForm : AAiUnitInteractibleHudForm {

    public override FormID FormID { get { return FormID.AiSettlement; } }

    public new SettlementCmdReport Report { get { return base.Report as SettlementCmdReport; } }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = Report.Resources;
    }

    protected override void AssignValueToPopulationGuiElement() {
        base.AssignValueToPopulationGuiElement();
        _populationGuiElement.Population = Report.Population;
    }

}

