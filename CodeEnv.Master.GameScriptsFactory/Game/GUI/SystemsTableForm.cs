// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SystemsTableForm.cs
// ATableForm that displays a table of information about known Systems in a TableWindow. 
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
/// ATableForm that displays a table of information about known Systems in a TableWindow. 
/// </summary>
public class SystemsTableForm : ATableForm {

    public override FormID FormID { get { return FormID.SystemsTable; } }

    protected override FormID RowFormID { get { return FormID.SystemsTableRow; } }

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as SystemItem).UserReport;
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserAIManager.Knowledge.Systems.Cast<AItem>();
    }

    protected override void ResumePreviousSortTopic(GuiElementID sortTopicToResume) {
        switch (sortTopicToResume) {
            case GuiElementID.Name:
                SortOnName();
                break;
            case GuiElementID.Owner:
                SortOnOwner();
                break;
            case GuiElementID.Location:
                SortOnLocation();
                break;
            case GuiElementID.Organics:
                SortOnOrganics();
                break;
            case GuiElementID.Particulates:
                SortOnParticulates();
                break;
            case GuiElementID.Energy:
                SortOnEnergy();
                break;
            case GuiElementID.Resources:
                SortOnResources();
                break;
            case GuiElementID.Food:
            case GuiElementID.Production:
            case GuiElementID.Income:
            case GuiElementID.Expense:
            case GuiElementID.NetIncome:
            case GuiElementID.Science:
            case GuiElementID.Culture:
            case GuiElementID.Speed:
            case GuiElementID.Population:
            case GuiElementID.DefensiveStrength:
            case GuiElementID.OffensiveStrength:
            case GuiElementID.Health:
            case GuiElementID.Hero:
            case GuiElementID.Composition:
            case GuiElementID.Construction:
            case GuiElementID.Approval:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sortTopicToResume.GetValueName()));
        }
    }

    #region Sorting Elements

    public void SortOnOrganics() {
        _table.onCustomSort = CompareOrganics;
        _sortDirection = DetermineSortDirection(GuiElementID.Organics);
        _table.repositionNow = true;
    }

    public void SortOnParticulates() {
        _table.onCustomSort = CompareParticulates;
        _sortDirection = DetermineSortDirection(GuiElementID.Particulates);
        _table.repositionNow = true;
    }

    public void SortOnEnergy() {
        _table.onCustomSort = CompareEnergy;
        _sortDirection = DetermineSortDirection(GuiElementID.Energy);
        _table.repositionNow = true;
    }

    #endregion

}

