// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemsScreenManager.cs
// Arranges and displays the table-based screen listing of systems. 
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

/// <summary>
/// Arranges and displays the table-based screen listing of systems. 
/// </summary>
public class SystemsScreenManager : ATableScreenManager<ISystemItem, SystemReport> {

    protected override SystemReport GetUserReportFor(ISystemItem system) {
        return system.GetUserReport();
    }

    protected override IEnumerable<ISystemItem> GetItemsUserIsAwareOf() {
        return GameManager.Instance.GetUserPlayerKnowledge().Systems;
    }

    protected override bool ConfigureGuiElement(GuiElement guiElement, SystemReport report) {
        bool isAlreadyConfigured = base.ConfigureGuiElement(guiElement, report);
        if (!isAlreadyConfigured) {
            switch (guiElement.elementID) {
                case GuiElementID.Location:
                    isAlreadyConfigured = true;
                    ConfigureLocationElement(guiElement, report);
                    break;
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
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiElement.elementID));
            }
        }
        return isAlreadyConfigured;
    }

    #region Row Element Configurators

    private void ConfigureLocationElement(GuiElement element, SystemReport report) {
        var locationElement = element as LocationGuiElement;
        locationElement.SectorIndex = report.SectorIndex;
        locationElement.Position = report.Position;
    }

    private void ConfigureStrategicResourcesElement(GuiElement element, SystemReport report) {
        var resourcesElement = element as ResourcesGuiElement;
        resourcesElement.Resources = report.Resources;
    }

    private void ConfigureOrganicsElement(GuiElement element, SystemReport report) {
        var organicsLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        organicsLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Organics)) : _unknown;
    }

    private void ConfigureParticulatesElement(GuiElement element, SystemReport report) {
        var particulatesLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        particulatesLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Particulates)) : _unknown;
    }

    private void ConfigureEnergyElement(GuiElement element, SystemReport report) {
        var energyLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        energyLabel.text = report.Resources.HasValue ? Constants.FormatFloat_0Dp.Inject(report.Resources.Value.GetYield(ResourceID.Energy)) : _unknown;
    }

    #endregion

    #region Sorting Elements

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

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

