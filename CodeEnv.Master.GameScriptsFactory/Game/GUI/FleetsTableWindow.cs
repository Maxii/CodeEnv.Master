// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetsTableWindow.cs
// Arranges and displays the table-based window for fleets.
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
/// Arranges and displays the table-based window for fleets.
/// </summary>
public class FleetsTableWindow : ACommandsTableWindow {

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as FleetCmdItem).UserReport;
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserAIManager.Knowledge.Fleets.Cast<AItem>();
    }

    #region Sorting

    public void SortOnSpeed() {
        _table.onCustomSort = CompareSpeed;
        _sortDirection = DetermineSortDirection(GuiElementID.SpeedLabel);
        _table.repositionNow = true;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

