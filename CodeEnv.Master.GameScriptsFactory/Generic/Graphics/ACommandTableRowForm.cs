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
        _defensiveStrengthGuiElement.Strength = report.UnitDefensiveStrength;
    }

    protected sealed override void AssignValueToHealthGuiElement() {
        base.AssignValueToHealthGuiElement();
        var report = Report as AUnitCmdReport;
        _healthGuiElement.Health = report.UnitHealth;
        _healthGuiElement.CurrentHitPts = report.UnitCurrentHitPoints;
        _healthGuiElement.MaxHitPts = report.UnitMaxHitPoints;
    }

    protected sealed override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroGuiElement.Hero = (Report as AUnitCmdReport).Hero;
    }

    protected sealed override void AssignValueToLocationGuiElement() {
        base.AssignValueToLocationGuiElement();
        var report = Report as AUnitCmdReport;
        _locationGuiElement.SectorID = report.SectorID;
        _locationGuiElement.Position = report.Position;
    }

    protected sealed override void AssignValueToNetIncomeGuiElement() {
        base.AssignValueToNetIncomeGuiElement();
        var report = Report as AUnitCmdReport;
        _netIncomeGuiElement.Income = report.UnitIncome;
        _netIncomeGuiElement.Expense = report.UnitExpense;
    }

    protected sealed override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        var report = Report as AUnitCmdReport;
        _offensiveStrengthGuiElement.Strength = report.UnitOffensiveStrength;
    }

    protected sealed override void AssignValueToScienceGuiElement() {
        base.AssignValueToScienceGuiElement();
        var report = Report as AUnitCmdReport;
        _scienceLabel.text = report.UnitScience.HasValue ? Constants.FormatFloat_0Dp.Inject(report.UnitScience) : Unknown;
    }

}

