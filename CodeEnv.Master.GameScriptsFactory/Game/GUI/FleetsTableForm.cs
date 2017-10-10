// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetsTableForm.cs
// ATableForm that displays a table of information about known Fleets in a TableWindow. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// ATableForm that displays a table of information about known Fleets in a TableWindow. 
/// </summary>
public class FleetsTableForm : ACommandsTableForm {

    public override FormID FormID { get { return FormID.FleetsTable; } }

    protected override FormID RowFormID { get { return FormID.FleetTableRow; } }

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as FleetCmdItem).UserReport;
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserAIManager.Knowledge.Fleets.Cast<AItem>();
    }

    protected override void ResumePreviousSortTopic(GuiElementID sortTopicToResume) {
        switch (sortTopicToResume) {
            case GuiElementID.NameLabel:
                SortOnName();
                break;
            case GuiElementID.SpeedLabel:
                SortOnSpeed();
                break;
            case GuiElementID.ScienceLabel:
                SortOnScience();
                break;
            case GuiElementID.CultureLabel:
                SortOnCulture();
                break;
            case GuiElementID.DefensiveStrength:
                SortOnDefensiveStrength();
                break;
            case GuiElementID.OffensiveStrength:
                SortOnOffensiveStrength();
                break;
            case GuiElementID.Health:
                SortOnHealth();
                break;
            case GuiElementID.Owner:
                SortOnOwner();
                break;
            case GuiElementID.Hero:
                SortOnHero();
                break;
            case GuiElementID.Location:
                SortOnLocation();
                break;
            case GuiElementID.Composition:
                SortOnComposition();
                break;
            case GuiElementID.Approval:
                break;
            case GuiElementID.NetIncome:
                SortOnNetIncome();
                break;
            case GuiElementID.OrganicsLabel:
            case GuiElementID.ParticulatesLabel:
            case GuiElementID.EnergyLabel:
            case GuiElementID.PopulationLabel:
            case GuiElementID.Resources:
            case GuiElementID.Construction:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sortTopicToResume.GetValueName()));
        }
    }

    #region Sorting

    public void SortOnSpeed() {
        _table.onCustomSort = CompareSpeed;
        _sortDirection = DetermineSortDirection(GuiElementID.SpeedLabel);
        _table.repositionNow = true;
    }

    #endregion

}

