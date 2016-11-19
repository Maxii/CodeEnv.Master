// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasesTableWindow.cs
// Arranges and displays the table-based window for starbases.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Arranges and displays the table-based window for starbases.
/// </summary>
public class StarbasesTableWindow : ACommandsTableWindow {

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as StarbaseCmdItem).UserReport;
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserAIManager.Knowledge.Starbases.Cast<AItem>();
    }

    #region Sorting

    public void SortOnStrategicResources() {
        _table.onCustomSort = CompareStrategicResources;
        _sortDirection = DetermineSortDirection(GuiElementID.Resources);
        _table.repositionNow = true;
    }

    #endregion

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

