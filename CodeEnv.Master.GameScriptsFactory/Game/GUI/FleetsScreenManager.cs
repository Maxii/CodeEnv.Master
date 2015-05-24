// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetsScreenManager.cs
// Arranges and displays the table-based screen listing of fleets.
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
/// Arranges and displays the table-based screen listing of fleets.
/// </summary>
public class FleetsScreenManager : ACommandsScreenManager<IFleetCmdItem, FleetReport> {

    private static string _speedFormat = Constants.FormatFloat_1DpMax;

    protected override FleetReport GetUserReportFor(IFleetCmdItem cmd) {
        return cmd.GetUserReport();
    }

    protected override IEnumerable<IFleetCmdItem> GetItemsUserIsAwareOf() {
        return GameManager.Instance.GetUserPlayerKnowledge().Fleets;
    }

    protected override bool ConfigureGuiElement(GuiElement guiElement, FleetReport report) {
        bool isAlreadyConfigured = base.ConfigureGuiElement(guiElement, report);
        if (!isAlreadyConfigured) {
            switch (guiElement.elementID) {
                case GuiElementID.SpeedLabel:
                    isAlreadyConfigured = true;
                    ConfigureMaxSpeedElement(guiElement, report);
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(guiElement.elementID));
            }
        }
        return isAlreadyConfigured;
    }

    #region Row Element Configurators

    protected override void ConfigureCompositionElement(GuiElement element, FleetReport report, IconInfo iconInfo) {
        var compElement = element as FleetCompositionGuiElement;
        compElement.IconInfo = iconInfo;
        compElement.Category = report.Category;
    }

    private void ConfigureMaxSpeedElement(GuiElement element, FleetReport report) {
        var speedLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        speedLabel.text = report.UnitFullSpeed.HasValue ? _speedFormat.Inject(report.UnitFullSpeed.Value) : _unknown;
    }

    #endregion

    #region Sorting

    public void SortOnSpeed() {
        _table.onCustomSort = CompareSpeed;
        _sortDirection = DetermineSortDirection(GuiElementID.SpeedLabel);
        _table.repositionNow = true;
    }

    #endregion

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

