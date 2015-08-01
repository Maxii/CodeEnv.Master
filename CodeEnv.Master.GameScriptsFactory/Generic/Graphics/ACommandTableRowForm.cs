// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ACommandTableRowForm.cs
// Abstract base class for TableRowForms that are for Commands.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for TableRowForms that are for Commands.  
/// </summary>
public abstract class ACommandTableRowForm : ATableRowForm {

    protected override void AssignValueToNameGuiElement() {
        var report = Report as ACmdReport;
        _nameLabel.text = report.ParentName != null ? report.ParentName : _unknown;
    }

    protected sealed override void AssignValueToCultureGuiElement() {
        base.AssignValueToCultureGuiElement();
        var report = Report as ACmdReport;
        _cultureLabel.text = report.UnitCulture.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitCulture) : _unknown;
    }

    protected sealed override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        var report = Report as ACmdReport;
        _defensiveStrengthElement.Strength = report.UnitDefensiveStrength;
    }

    protected sealed override void AssignValueToHealthGuiElement() {
        base.AssignValueToHealthGuiElement();
        var report = Report as ACmdReport;
        _healthElement.Health = report.UnitHealth;
        _healthElement.CurrentHitPts = report.UnitCurrentHitPoints;
        _healthElement.MaxHitPts = report.UnitMaxHitPoints;
    }

    protected sealed override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroElement.__HeroName = "None";   // UNDONE
    }

    protected override void AssignValueToProductionGuiElement() {
        base.AssignValueToProductionGuiElement();
        _productionElement.__ProducingName = "None";    // UNDONE
    }

    protected sealed override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as ACmdReport;
        _locationElement.SectorIndex = report.SectorIndex;
        _locationElement.Position = report.Position;
    }

    protected sealed override void AssignValueToNetIncomeGuiElement() {
        base.AssignValueToNetIncomeGuiElement();
        var report = Report as ACmdReport;
        _netIncomeElement.Income = report.UnitIncome;
        _netIncomeElement.Expense = report.UnitExpense;
    }

    protected sealed override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        var report = Report as ACmdReport;
        _offensiveStrengthElement.Strength = report.UnitOffensiveStrength;
    }

    protected sealed override void AssignValueToScienceGuiElement() {
        base.AssignValueToScienceGuiElement();
        var report = Report as ACmdReport;
        _scienceLabel.text = report.UnitScience.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitScience) : _unknown;
    }

}

