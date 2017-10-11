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

    protected override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as SystemReport;
        _locationGuiElement.SectorID = report.SectorID;
        _locationGuiElement.Position = report.Position;
    }

    protected override void AssignValueToEnergyGuiElement() {
        base.AssignValueToEnergyGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Energy);
        _energyLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        ////_energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : Unknown;
    }

    protected override void AssignValueToOrganicsGuiElement() {
        base.AssignValueToOrganicsGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Organics);
        _organicsLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        //// _organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : Unknown;
    }

    protected override void AssignValueToParticulatesGuiElement() {
        base.AssignValueToParticulatesGuiElement();
        var report = Report as SystemReport;
        float? yield = report.Resources.GetYield(ResourceID.Particulates);
        _particulatesLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        ////_particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : Unknown;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        var report = Report as SystemReport;
        _resourcesGuiElement.Resources = report.Resources;
    }


}

