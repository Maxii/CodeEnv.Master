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

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for TableRowForms that are for Commands.  
/// </summary>
public abstract class ACommandTableRowForm : ATableRowForm {

    protected override void AssignValueToNameGuiElement() {
        var report = Report as AUnitCmdReport;
        _nameLabel.text = report.UnitName != null ? report.UnitName : Unknown;
    }

    protected sealed override void AssignValueToCultureGuiElement() {
        base.AssignValueToCultureGuiElement();
        var report = Report as AUnitCmdReport;
        _cultureLabel.text = report.UnitCulture.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitCulture) : Unknown;
    }

    protected sealed override void AssignValueToDefensiveStrengthGuiElement() {
        base.AssignValueToDefensiveStrengthGuiElement();
        var report = Report as AUnitCmdReport;
        _defensiveStrengthElement.Strength = report.UnitDefensiveStrength;
    }

    protected sealed override void AssignValueToHealthGuiElement() {
        base.AssignValueToHealthGuiElement();
        var report = Report as AUnitCmdReport;
        _healthElement.Health = report.UnitHealth;
        _healthElement.CurrentHitPts = report.UnitCurrentHitPoints;
        _healthElement.MaxHitPts = report.UnitMaxHitPoints;
    }

    protected sealed override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroElement.Hero = (Report as AUnitCmdReport).Hero;
    }

    protected override void AssignValueToProductionGuiElement() {
        base.AssignValueToProductionGuiElement();
        _productionElement.__ProducingName = "None";    // UNDONE
    }

    protected sealed override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as AUnitCmdReport;
        _locationElement.SectorID = report.SectorID;
        _locationElement.Position = report.Position;
    }

    protected sealed override void AssignValueToNetIncomeGuiElement() {
        base.AssignValueToNetIncomeGuiElement();
        var report = Report as AUnitCmdReport;
        _netIncomeElement.Income = report.UnitIncome;
        _netIncomeElement.Expense = report.UnitExpense;
    }

    protected sealed override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        var report = Report as AUnitCmdReport;
        _offensiveStrengthElement.Strength = report.UnitOffensiveStrength;
    }

    protected sealed override void AssignValueToScienceGuiElement() {
        base.AssignValueToScienceGuiElement();
        var report = Report as AUnitCmdReport;
        _scienceLabel.text = report.UnitScience.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitScience) : Unknown;
    }

}

