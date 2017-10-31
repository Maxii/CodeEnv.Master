// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NonUserSystemInteractibleHudForm.cs
// Form used by the InteractibleHudWindow to display info (and allow name changes) when a nonUser-owned Item is selected.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form used by the InteractibleHudWindow to display info (and allow name changes) when a nonUser-owned Item is selected.
/// </summary>
public class NonUserSystemInteractibleHudForm : ANonUserItemNameChangeInteractibleHudForm {

    public override FormID FormID { get { return FormID.NonUserSystem; } }

    public new SystemReport Report { get { return base.Report as SystemReport; } }

    protected override void AssignValueToNameChangeGuiElement() {
        base.AssignValueToNameChangeGuiElement();
        _nameChgGuiElement.NameReference = new Reference<string>(() => Report.Item.Name, z => (Report.Item as IOwnerItem).Name = z);
    }

    protected override void AssignValueToOutputsGuiElement() {
        base.AssignValueToOutputsGuiElement();
        _outputsGuiElement.Outputs = Report.Outputs;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        _resourcesGuiElement.Resources = Report.Resources;
    }

    protected override void AssignValueToPopulationGuiElement() {
        base.AssignValueToPopulationGuiElement();
        _populationGuiElement.Population = Report.Population;
    }

    protected override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroGuiElement.Hero = Report.Hero;
    }


}

