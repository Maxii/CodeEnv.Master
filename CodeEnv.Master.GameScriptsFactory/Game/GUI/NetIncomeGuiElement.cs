﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NetIncomeGuiElement.cs
// GuiElement handling the display and tooltip content for the Income and Expense of a Command.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Income and Expense of a Command.   
/// </summary>
public class NetIncomeGuiElement : AGuiElement, IComparable<NetIncomeGuiElement> {

    private static readonly string TooltipFormat = "Income: {0}" + Constants.NewLine + "Expense: {1}";

    public override GuiElementID ElementID { get { return GuiElementID.NetIncome; } }

    private bool _isIncomeSet;
    private decimal? _income;
    public decimal? Income {
        get { return _income; }
        set {
            D.Assert(!_isIncomeSet);    // only happens once between Resets
            _income = value;
            IncomePropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private bool _isExpenseSet;
    private decimal? _expense;
    public decimal? Expense {
        get { return _expense; }
        set {
            D.Assert(!_isExpenseSet);    // only happens once between Resets
            _expense = value;
            ExpensePropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool AreAllValuesSet { get { return _isIncomeSet && _isExpenseSet; } }

    private decimal? _netIncome;
    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        _label = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void IncomePropSetHandler() {
        _isIncomeSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void ExpensePropSetHandler() {
        _isExpenseSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    private void PopulateElementWidgets() {
        string incomeTooltipContent = Unknown;
        string expenseTooltipContent = Unknown;
        string labelContent = Unknown;

        _netIncome = Income - Expense;  // if either are null, the result is null
        if (_netIncome.HasValue) {
            labelContent = Constants.FormatFloat_0Dp.Inject(_netIncome);
            incomeTooltipContent = Constants.FormatFloat_0Dp.Inject(Income.Value);
            expenseTooltipContent = Constants.FormatFloat_0Dp.Inject(Expense.Value);
        }
        else {
            if (Income.HasValue) {
                D.Assert(!Expense.HasValue);
                incomeTooltipContent = Constants.FormatFloat_0Dp.Inject(Income.Value);
            }
            else if (Expense.HasValue) {
                D.Assert(!Income.HasValue);
                expenseTooltipContent = Constants.FormatFloat_0Dp.Inject(Expense.Value);
            }
        }
        _label.text = labelContent;
        _tooltipContent = TooltipFormat.Inject(incomeTooltipContent, expenseTooltipContent);
    }

    public override void ResetForReuse() {
        _isIncomeSet = false;
        _isExpenseSet = false;
    }

    protected override void Cleanup() { }


    #region IComparable<NetIncomeGuiElement> Members

    public int CompareTo(NetIncomeGuiElement other) {
        int result;

        if (!_netIncome.HasValue) {
            result = !other._netIncome.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other._netIncome.HasValue ? Constants.One : _netIncome.Value.CompareTo(other._netIncome.Value);
        }

        if (result == Constants.Zero) {
            // either both netIncome = ? or its the same value so sort on Income
            if (!Income.HasValue) {
                result = !other.Income.HasValue ? Constants.Zero : Constants.MinusOne;
            }
            else {
                result = !other.Income.HasValue ? Constants.One : Income.Value.CompareTo(other.Income.Value);
            }
        }

        if (result == Constants.Zero) {
            // neither netIncome or Income have separated the two, so use Expense 
            // as Expense is always accounted for as a positive value, higher Expense is worse than lower expense
            if (!Expense.HasValue) {
                result = !other.Expense.HasValue ? Constants.Zero : Constants.One;
            }
            else {
                result = !other.Expense.HasValue ? Constants.MinusOne : -Expense.Value.CompareTo(other.Expense.Value);
            }
        }
        return result;
    }

    #endregion

}

