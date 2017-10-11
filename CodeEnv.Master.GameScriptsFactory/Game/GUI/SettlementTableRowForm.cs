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

    protected override void AssignValueToCompositionGuiElement() {
        base.AssignValueToCompositionGuiElement();
        var report = Report as SettlementCmdReport;
        _compositionGuiElement.IconInfo = SettlementIconInfoFactory.Instance.MakeInstance(report);
        (_compositionGuiElement as SettlementCompositionGuiElement).Category = report.Category;
    }

    protected override void AssignValueToApprovalGuiElement() {
        base.AssignValueToApprovalGuiElement();
        var report = Report as SettlementCmdReport;
        _approvalGuiElement.Approval = report.Approval;
    }

    protected override void AssignValueToEnergyGuiElement() {
        base.AssignValueToEnergyGuiElement();
        var report = Report as SettlementCmdReport;
        float? yield = report.Resources.GetYield(ResourceID.Energy);
        _energyLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        ////_energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : Unknown;
    }

    protected override void AssignValueToOrganicsGuiElement() {
        base.AssignValueToOrganicsGuiElement();
        var report = Report as SettlementCmdReport;
        float? yield = report.Resources.GetYield(ResourceID.Organics);
        _organicsLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        ////_organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : Unknown;
    }

    protected override void AssignValueToParticulatesGuiElement() {
        base.AssignValueToParticulatesGuiElement();
        var report = Report as SettlementCmdReport;
        float? yield = report.Resources.GetYield(ResourceID.Particulates);
        _particulatesLabel.text = yield.HasValue ? Constants.FormatFloat_0Dp.Inject(yield.Value) : Unknown;
        ////_particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : Unknown;
    }

    protected override void AssignValueToPopulationGuiElement() {
        base.AssignValueToPopulationGuiElement();
        var report = Report as SettlementCmdReport;
        _populationLabel.text = report.Population.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Population.Value) : Unknown;
    }

    protected override void AssignValueToResourcesGuiElement() {
        base.AssignValueToResourcesGuiElement();
        var report = Report as SettlementCmdReport;
        _resourcesGuiElement.Resources = report.Resources;
    }

    protected override void AssignValueToConstructionGuiElement() {
        base.AssignValueToConstructionGuiElement();
        var report = Report as SettlementCmdReport;
        _constructionGuiElement.Construction = report.CurrentConstruction;
    }

}

