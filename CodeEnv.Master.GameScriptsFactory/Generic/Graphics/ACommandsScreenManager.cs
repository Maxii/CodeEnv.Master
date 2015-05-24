// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandsScreenManager.cs
// Abstract base class for all Command Screen Managers. 
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
using UnityEngine;

/// <summary>
/// Abstract base class for all Table Screen Managers for Commands.
/// </summary>
/// <typeparam name="CmdType">The type of Command.</typeparam>
/// <typeparam name="CmdReportType">The type of Command Report.</typeparam>
public abstract class ACommandsScreenManager<CmdType, CmdReportType> : ATableScreenManager<CmdType, CmdReportType>
    where CmdType : IUnitCmdItem
    where CmdReportType : ACmdReport {

    protected override bool ConfigureGuiElement(GuiElement guiElement, CmdReportType report) {
        bool isAlreadyConfigured = base.ConfigureGuiElement(guiElement, report);
        if (!isAlreadyConfigured) {
            switch (guiElement.elementID) {
                case GuiElementID.Location:
                    isAlreadyConfigured = true;
                    ConfigureLocationElement(guiElement, report);
                    break;
                case GuiElementID.Hero:
                    isAlreadyConfigured = true;
                    ConfigureHeroElement(guiElement, report);
                    break;
                case GuiElementID.Composition:
                    isAlreadyConfigured = true;
                    IconInfo cmdIconInfo = ((CmdType)report.Item).IconInfo;
                    //D.Log("{0} acquired IconInfo {1} from {2}.", GetType().Name, cmdIconInfo, cmd.FullName);
                    D.Assert(cmdIconInfo != default(IconInfo));    // a Cmd we are aware of should never have a 'null' iconInfo
                    ConfigureCompositionElement(guiElement, report, cmdIconInfo);
                    break;
                case GuiElementID.Health:
                    isAlreadyConfigured = true;
                    ConfigureHealthElement(guiElement, report);
                    break;
                case GuiElementID.OffensiveStrength:
                    isAlreadyConfigured = true;
                    ConfigureOffensiveStrengthElement(guiElement, report);
                    break;
                case GuiElementID.DefensiveStrength:
                    isAlreadyConfigured = true;
                    ConfigureDefensiveStrengthElement(guiElement, report);
                    break;
                case GuiElementID.TotalStrength:
                    isAlreadyConfigured = true;
                    ConfigureTotalStrengthElement(guiElement, report);
                    break;
                case GuiElementID.ScienceLabel:
                    isAlreadyConfigured = true;
                    ConfigureScienceElement(guiElement, report);
                    break;
                case GuiElementID.CultureLabel:
                    isAlreadyConfigured = true;
                    ConfigureCultureElement(guiElement, report);
                    break;
                case GuiElementID.NetIncome:
                    isAlreadyConfigured = true;
                    ConfigureNetIncomeElement(guiElement, report);
                    break;
            }
        }
        return isAlreadyConfigured;
    }

    #region Row Element Configurators

    private void ConfigureLocationElement(GuiElement element, CmdReportType report) {
        var locationElement = element as LocationGuiElement;
        locationElement.SectorIndex = report.SectorIndex;
        locationElement.Position = report.Position;
    }

    private void ConfigureHeroElement(GuiElement element, CmdReportType report) {
        var heroElement = element as HeroGuiElement;
        heroElement.__HeroName = "None";    // = report.Hero;
    }

    protected abstract void ConfigureCompositionElement(GuiElement element, CmdReportType report, IconInfo iconInfo);

    private void ConfigureHealthElement(GuiElement element, CmdReportType report) {
        //D.Log("Configuring HealthGuiElement for {0}. Cmd/HQ IntelCoverage = {1}, UnitCurrentHitPts = {2}, UnitMaxHitPts = {3}, Health = {4}.",
        //    report.ParentName, report.IntelCoverage.GetName(), report.UnitCurrentHitPoints, report.UnitMaxHitPoints, report.UnitHealth);
        var healthElement = element as HealthGuiElement;
        healthElement.Health = report.UnitHealth;
        healthElement.CurrentHitPts = report.UnitCurrentHitPoints;
        healthElement.MaxHitPts = report.UnitMaxHitPoints;
    }

    private void ConfigureOffensiveStrengthElement(GuiElement element, CmdReportType report) {
        var strengthElement = element as StrengthGuiElement;
        strengthElement.OffensiveStrength = report.UnitOffensiveStrength;
    }

    private void ConfigureDefensiveStrengthElement(GuiElement element, CmdReportType report) {
        var strengthElement = element as StrengthGuiElement;
        strengthElement.DefensiveStrength = report.UnitDefensiveStrength;
    }

    private void ConfigureTotalStrengthElement(GuiElement element, CmdReportType report) {
        var strengthElement = element as StrengthGuiElement;
        strengthElement.OffensiveStrength = report.UnitOffensiveStrength;
        strengthElement.DefensiveStrength = report.UnitDefensiveStrength;
    }

    private void ConfigureScienceElement(GuiElement element, CmdReportType report) {
        var scienceLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        scienceLabel.text = report.UnitScience.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitScience) : _unknown;
    }

    private void ConfigureCultureElement(GuiElement element, CmdReportType report) {
        var cultureLabel = element.gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
        cultureLabel.text = report.UnitCulture.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitCulture) : _unknown;
    }

    private void ConfigureNetIncomeElement(GuiElement element, CmdReportType report) {
        var netIncomeElement = element as NetIncomeGuiElement;
        netIncomeElement.Income = report.UnitIncome;
        netIncomeElement.Expense = report.UnitExpense;
    }

    #endregion

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

    public void SortOnTotalStrength() {
        _table.onCustomSort = CompareTotalStrength;
        _sortDirection = DetermineSortDirection(GuiElementID.TotalStrength);
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

