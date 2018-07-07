// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemTableRowForm.cs
// Form that displays info about a system in a table row.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  Form that displays info about a system in a table row.
/// </summary>
public class SystemTableRowForm : ATableRowForm {

    public override FormID FormID { get { return FormID.SystemsTableRow; } }

    protected override void AssignValueToNameGuiElement() {
        base.AssignValueToNameGuiElement();
        _nameLabel.text = Report.Name != null ? Report.Name : Unknown;
    }

    protected override void AssignValueToOwnerGuiElement() {
        base.AssignValueToOwnerGuiElement();
        _ownerGuiElement.Owner = Report.Owner;
    }

    protected override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as SystemReport;
        _locationGuiElement.SectorID = report.SectorID;
        _locationGuiElement.Location = report.Position;
    }

    protected override void AssignValueToOrganicsGuiElement() {
        base.AssignValueToOrganicsGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Organics);
        _organicsLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
    }

    protected override void AssignValueToParticulatesGuiElement() {
        base.AssignValueToParticulatesGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Particulates);
        _particulatesLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
    }

    protected override void AssignValueToEnergyGuiElement() {
        base.AssignValueToEnergyGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Energy);
        _energyLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        var report = Report as SystemReport;
        _resourcesGuiElement.Resources = report.Resources;
    }


}

