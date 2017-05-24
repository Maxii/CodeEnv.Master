// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementTableRowForm.cs
// Form that displays info about a settlement in a table row.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Form that displays info about a settlement in a table row.
/// </summary>
public class SettlementTableRowForm : ACommandTableRowForm {

    public override FormID FormID { get { return FormID.SettlementTableRow; } }

    private SettlementCompositionGuiElement _compositionElement;

    protected override void InitializeCompositionGuiElement(AGuiElement e) {
        base.InitializeCompositionGuiElement(e);
        _compositionElement = e as SettlementCompositionGuiElement;
    }

    protected override void AssignValueToApprovalGuiElement() {
        base.AssignValueToApprovalGuiElement();
        var report = Report as SettlementCmdReport;
        _approvalElement.Approval = report.Approval;
    }

    protected override void AssignValueToCompositionGuiElement() {
        base.AssignValueToCompositionGuiElement();
        var report = Report as SettlementCmdReport;
        _compositionElement.IconInfo = SettlementIconInfoFactory.Instance.MakeInstance(report);
        _compositionElement.Category = report.Category;
    }

    protected override void AssignValueToEnergyGuiElement() {
        base.AssignValueToEnergyGuiElement();
        var report = Report as SettlementCmdReport;
        _energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : _unknown;
    }

    protected override void AssignValueToOrganicsGuiElement() {
        base.AssignValueToOrganicsGuiElement();
        var report = Report as SettlementCmdReport;
        _organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : _unknown;
    }

    protected override void AssignValueToParticulatesGuiElement() {
        base.AssignValueToParticulatesGuiElement();
        var report = Report as SettlementCmdReport;
        _particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : _unknown;
    }

    protected override void AssignValueToPopulationGuiElement() {
        base.AssignValueToPopulationGuiElement();
        var report = Report as SettlementCmdReport;
        _populationLabel.text = report.Population.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Population.Value) : _unknown;
    }

    protected override void AssignValueToStrategicResourcesGuiElement() {
        base.AssignValueToStrategicResourcesGuiElement();
        var report = Report as SettlementCmdReport;
        _resourcesElement.Resources = report.Resources;
    }

}

