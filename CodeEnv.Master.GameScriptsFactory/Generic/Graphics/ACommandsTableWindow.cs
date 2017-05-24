// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandsTableWindow.cs
// Abstract base class for all TableWindows for Commands. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for all TableWindows for Commands.
/// </summary>
public abstract class ACommandsTableWindow : ATableWindow {

    #region Sorting Elements

    public void SortOnHero() {
        _table.onCustomSort = CompareHero;
        _sortDirection = DetermineSortDirection(GuiElementID.Hero);
        _table.repositionNow = true;
    }

    public void SortOnComposition() {
        _table.onCustomSort = CompareComposition;
        _sortDirection = DetermineSortDirection(GuiElementID.Composition);
        _table.repositionNow = true;
    }

    public void SortOnHealth() {
        _table.onCustomSort = CompareHealth;
        _sortDirection = DetermineSortDirection(GuiElementID.Health);
        _table.repositionNow = true;
    }

    public void SortOnDefensiveStrength() {
        _table.onCustomSort = CompareDefensiveStrength;
        _sortDirection = DetermineSortDirection(GuiElementID.DefensiveStrength);
        _table.repositionNow = true;
    }

    public void SortOnOffensiveStrength() {
        _table.onCustomSort = CompareOffensiveStrength;
        _sortDirection = DetermineSortDirection(GuiElementID.OffensiveStrength);
        _table.repositionNow = true;
    }

    public void SortOnScience() {
        _table.onCustomSort = CompareScience;
        _sortDirection = DetermineSortDirection(GuiElementID.ScienceLabel);
        _table.repositionNow = true;
    }

    public void SortOnCulture() {
        _table.onCustomSort = CompareCulture;
        _sortDirection = DetermineSortDirection(GuiElementID.CultureLabel);
        _table.repositionNow = true;
    }

    public void SortOnNetIncome() {
        _table.onCustomSort = CompareNetIncome;
        _sortDirection = DetermineSortDirection(GuiElementID.NetIncome);
        _table.repositionNow = true;
    }

    #endregion

}

