// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasesScreenManager.cs
// Arranges and displays the table-based screen listing of starbases.
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
/// Arranges and displays the table-based screen listing of starbases.
/// </summary>
public class StarbasesScreenManager : ACommandsScreenManager<IStarbaseCmdItem, StarbaseReport> {

    protected override StarbaseReport GetUserReportFor(IStarbaseCmdItem cmd) {
        return cmd.GetUserReport();
    }

    protected override IEnumerable<IStarbaseCmdItem> GetItemsUserIsAwareOf() {
        return GameManager.Instance.GetUserPlayerKnowledge().Starbases;
    }

    protected override bool ConfigureGuiElement(GuiElement guiElement, StarbaseReport report) {
        bool isAlreadyConfigured = base.ConfigureGuiElement(guiElement, report);
        if (!isAlreadyConfigured) {
            switch (guiElement.elementID) {
                case GuiElementID.StrategicResources:
                    isAlreadyConfigured = true;
                    ConfigureStrategicResourcesElement(guiElement, report);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiElement.elementID));
            }
        }
        return isAlreadyConfigured;
    }

    #region Row Element Configurators

    protected override void ConfigureCompositionElement(GuiElement element, StarbaseReport report, IconInfo iconInfo) {
        var compElement = element as StarbaseCompositionGuiElement;
        compElement.IconInfo = iconInfo;
        compElement.Category = report.Category;
    }

    private void ConfigureStrategicResourcesElement(GuiElement element, StarbaseReport report) {
        var resourcesElement = element as ResourcesGuiElement;
        resourcesElement.Resources = report.Resources;
    }

    #endregion

    #region Sorting

    public void SortOnStrategicResources() {
        _table.onCustomSort = CompareStrategicResources;
        _sortDirection = DetermineSortDirection(GuiElementID.StrategicResources);
        _table.repositionNow = true;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

