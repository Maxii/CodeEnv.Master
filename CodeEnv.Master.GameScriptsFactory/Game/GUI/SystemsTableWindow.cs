// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemsTableWindow.cs
// Arranges and displays the table-based window for systems. 
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
/// Arranges and displays the table-based window for systems. 
/// </summary>
public class SystemsTableWindow : ATableWindow {

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as SystemItem).GetUserReport();
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return GameManager.Instance.GetUserPlayerKnowledge().Systems.Cast<AItem>();
    }

    #region Sorting Elements

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

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

