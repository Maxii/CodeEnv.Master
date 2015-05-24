// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementsScreenManager.cs
// Arranges and displays the table-based screen listing of settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
///  Arranges and displays the table-based screen listing of settlements.
/// </summary>
public class SettlementsScreenManager : ACommandsScreenManager<ISettlementCmdItem, SettlementReport> {

    protected override SettlementReport GetUserReportFor(ISettlementCmdItem cmd) {
        return cmd.GetUserReport();
    }

    protected override IEnumerable<ISettlementCmdItem> GetItemsUserIsAwareOf() {
        return GameManager.Instance.GetUserPlayerKnowledge().Settlements;
    }

    protected override bool ConfigureGuiElement(GuiElement guiElement, SettlementReport report) {
        bool isAlreadyConfigured = base.ConfigureGuiElement(guiElement, report);
        if (!isAlreadyConfigured) {
            switch (guiElement.elementID) {
                case GuiElementID.StrategicResources:
                    isAlreadyConfigured = true;
                    ConfigureStrategicResourcesElement(guiElement, report);
                    break;
                case GuiElementID.OrganicsLabel:
                    isAlreadyConfigured = true;
                    ConfigureOrganicsElement(guiElement, report);
                    break;
                case GuiElementID.ParticulatesLabel:
                    isAlreadyConfigured = true;
                    ConfigureParticulatesElement(guiElement, report);
                    break;
                case GuiElementID.EnergyLabel:
                    isAlreadyConfigured = true;
                    ConfigureEnergyElement(guiElement, report);
                    break;
                case GuiElementID.PopulationLabel:
                    isAlreadyConfigured = true;
                    ConfigurePopulationElement(guiElement, report);
                    break;
                case GuiElementID.Approval:
                    isAlreadyConfigured = true;
                    ConfigureApprovalElement(guiElement, report);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiElement.elementID));
            }
        }
        return isAlreadyConfigured;
    }

    #region Row Element Configurators

    protected override void ConfigureCompositionElement(GuiElement element, SettlementReport report, IconInfo iconInfo) {
        var compElement = element as SettlementCompositionGuiElement;
        compElement.IconInfo = iconInfo;
        compElement.Category = report.Category;
    }

    private void ConfigureStrategicResourcesElement(GuiElement element, SettlementReport report) {
        var resourcesElement = element as ResourcesGuiElement;
        resourcesElement.Resources = report.Resources;
    }

    private void ConfigureOrganicsElement(GuiElement element, SettlementReport report) {
        var organicsLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : _unknown;
    }

    private void ConfigureParticulatesElement(GuiElement element, SettlementReport report) {
        var particulatesLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : _unknown;
    }

    private void ConfigureEnergyElement(GuiElement element, SettlementReport report) {
        var energyLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : _unknown;
    }

    private void ConfigurePopulationElement(GuiElement element, SettlementReport report) {
        var popLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        popLabel.text = report.Population.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Population) : _unknown;
    }

    private void ConfigureApprovalElement(GuiElement element, SettlementReport report) {
        var approvalElement = element as ApprovalGuiElement;
        approvalElement.Approval = report.Approval;
    }

    #endregion

    #region Sorting

    public void SortOnStrategicResources() {
        _table.onCustomSort = CompareStrategicResources;
        _sortDirection = DetermineSortDirection(GuiElementID.StrategicResources);
        _table.repositionNow = true;
    }

    public void SortOnOrganics() {
        _table.onCustomSort = CompareOrganics;
        _sortDirection = DetermineSortDirection(GuiElementID.OrganicsLabel);
        _table.repositionNow = true;
    }

    public void SortOnParticulates() {
        _table.onCustomSort = CompareParticulates;
        _sortDirection = DetermineSortDirection(GuiElementID.ParticulatesLabel);
        _table.repositionNow = true;
    }

    public void SortOnEnergy() {
        _table.onCustomSort = CompareEnergy;
        _sortDirection = DetermineSortDirection(GuiElementID.EnergyLabel);
        _table.repositionNow = true;
    }

    public void SortOnPopulation() {
        _table.onCustomSort = ComparePopulation;
        _sortDirection = DetermineSortDirection(GuiElementID.PopulationLabel);
        _table.repositionNow = true;
    }

    public void SortOnApproval() {
        _table.onCustomSort = CompareApproval;
        _sortDirection = DetermineSortDirection(GuiElementID.Approval);
        _table.repositionNow = true;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

