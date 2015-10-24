// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementsTableWindow.cs
// Arranges and displays the table-based window for settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
///  Arranges and displays the table-based window for settlements.
/// </summary>
public class SettlementsTableWindow : ACommandsTableWindow {

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as SettlementCmdItem).GetUserReport();
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserPlayerKnowledge.Settlements.Cast<AItem>();
    }

    #region Sorting

    public void SortOnStrategicResources() {
        _table.onCustomSort = CompareStrategicResources;
        _sortDirection = DetermineSortDirection(GuiElementID.Resources);
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

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

