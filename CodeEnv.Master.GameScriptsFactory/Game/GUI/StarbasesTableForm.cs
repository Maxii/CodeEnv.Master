﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbasesTableForm.cs
// ATableForm that displays a table of information about known Starbases in a TableWindow. 
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
/// ATableForm that displays a table of information about known Starbases in a TableWindow. 
/// </summary>
public class StarbasesTableForm : ACommandsTableForm {

    public override FormID FormID { get { return FormID.StarbasesTable; } }

    protected override FormID RowFormID { get { return FormID.StarbaseTableRow; } }

    protected override AItemReport GetUserReportFor(AItem item) {
        return (item as StarbaseCmdItem).UserReport;
    }

    protected override IEnumerable<AItem> GetItemsUserIsAwareOf() {
        return _gameMgr.UserAIManager.Knowledge.Starbases.Cast<AItem>();
    }

    protected override void ResumePreviousSortTopic(GuiElementID sortTopicToResume) {
        switch (sortTopicToResume) {
            case GuiElementID.Name:
                SortOnName();
                break;
            case GuiElementID.Food:
                SortOnFood();
                break;
            case GuiElementID.Production:
                SortOnProduction();
                break;
            case GuiElementID.Income:
                SortOnIncome();
                break;
            case GuiElementID.Expense:
                SortOnExpense();
                break;
            case GuiElementID.NetIncome:
                SortOnNetIncome();
                break;
            case GuiElementID.Science:
                SortOnScience();
                break;
            case GuiElementID.Culture:
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
            case GuiElementID.Resources:
                SortOnResources();
                break;
            case GuiElementID.Composition:
                SortOnComposition();
                break;
            case GuiElementID.Construction:
                SortOnConstruction();
                break;
            case GuiElementID.Approval:
            case GuiElementID.Organics:
            case GuiElementID.Particulates:
            case GuiElementID.Energy:
            case GuiElementID.Population:
            case GuiElementID.Speed:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(sortTopicToResume.GetValueName()));
        }
    }

    #region Sorting

    public void SortOnConstruction() {
        _table.onCustomSort = CompareConstruction;
        _sortDirection = DetermineSortDirection(GuiElementID.Construction);
        _table.repositionNow = true;
    }

    #endregion

}

