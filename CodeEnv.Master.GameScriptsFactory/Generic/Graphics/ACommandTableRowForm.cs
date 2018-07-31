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

    protected sealed override void AssignValueToOwnerGuiElement() {
        base.AssignValueToOwnerGuiElement();
        _ownerGuiElement.Owner = Report.Owner;
    }

    protected sealed override void AssignValueToHeroGuiElement() {
        base.AssignValueToHeroGuiElement();
        _heroGuiElement.Hero = (Report as AUnitCmdReport).Hero;
    }

    protected sealed override void AssignValueToFoodGuiElement() {
        base.AssignValueToFoodGuiElement();
        var report = Report as AUnitCmdReport;
        _foodLabel.text = GetOutputLabelTextFor(OutputID.Food, report.UnitOutputs);
    }

    protected sealed override void AssignValueToProductionGuiElement() {
        base.AssignValueToProductionGuiElement();
        var report = Report as AUnitCmdReport;
        _prodnLabel.text = GetOutputLabelTextFor(OutputID.Production, report.UnitOutputs);
    }

    protected sealed override void AssignValueToIncomeGuiElement() {
        base.AssignValueToIncomeGuiElement();
        var report = Report as AUnitCmdReport;
        _incomeLabel.text = GetOutputLabelTextFor(OutputID.Income, report.UnitOutputs);
    }
    protected sealed override void AssignValueToExpenseGuiElement() {
        base.AssignValueToExpenseGuiElement();
        var report = Report as AUnitCmdReport;
        _expenseLabel.text = GetOutputLabelTextFor(OutputID.Expense, report.UnitOutputs);
    }

    protected sealed override void AssignValueToNetIncomeGuiElement() {
        base.AssignValueToNetIncomeGuiElement();
        var report = Report as AUnitCmdReport;
        _netIncomeGuiElement.Outputs = report.UnitOutputs;
    }

    protected sealed override void AssignValueToScienceGuiElement() {
        base.AssignValueToScienceGuiElement();
        var report = Report as AUnitCmdReport;
        _scienceLabel.text = GetOutputLabelTextFor(OutputID.Science, report.UnitOutputs);
    }

    protected sealed override void AssignValueToCultureGuiElement() {
        base.AssignValueToCultureGuiElement();
        var report = Report as AUnitCmdReport;
        _cultureLabel.text = GetOutputLabelTextFor(OutputID.Culture, report.UnitOutputs);
    }

    protected sealed override void AssignValueToOffensiveStrengthGuiElement() {
        base.AssignValueToOffensiveStrengthGuiElement();
        var report = Report as AUnitCmdReport;
        _offensiveStrengthGuiElement.Strength = report.UnitOffensiveStrength;
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


    protected string GetOutputLabelTextFor(OutputID outputID, OutputsYield outputs) {
        string labelText = Unknown;
        if (outputs != default(OutputsYield)) {
            if (outputs.IsPresent(outputID)) {
                float? yield = outputs.GetYield(outputID);
                if (yield.HasValue) {
                    labelText = Constants.FormatFloat_0Dp.Inject(yield.Value);
                }
            }
        }
        return labelText;
    }

}

