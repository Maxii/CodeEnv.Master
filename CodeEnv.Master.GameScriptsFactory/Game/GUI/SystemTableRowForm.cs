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

    public override FormID FormID { get { return FormID.SystemTableRow; } }

    protected override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as SystemReport;
        _locationElement.SectorID = report.SectorID;
        _locationElement.Position = report.Position;
    }

    protected override void AssignValueToEnergyGuiElement() {
        base.AssignValueToEnergyGuiElement();
        var report = Report as SystemReport;
        _energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : _unknown;
    }

    protected override void AssignValueToOrganicsGuiElement() {
        base.AssignValueToOrganicsGuiElement();
        var report = Report as SystemReport;
        _organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : _unknown;
    }

    protected override void AssignValueToParticulatesGuiElement() {
        base.AssignValueToParticulatesGuiElement();
        var report = Report as SystemReport;
        _particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : _unknown;
    }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        var report = Report as SystemReport;
        _resourcesElement.Resources = report.Resources;
    }

}

