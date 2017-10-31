// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: NetIncomeGuiElement.cs
// AGuiElement that represents the Income and Expense of an Item or Empire.
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
/// AGuiElement that represents the Income and Expense of an Item or Empire.
/// </summary>
public class NetIncomeGuiElement : AGuiElement, IComparable<NetIncomeGuiElement> {

    private static readonly string TooltipFormat = "Income: {0}" + Constants.NewLine + "Expense: {1}";

    public override GuiElementID ElementID { get { return GuiElementID.NetIncome; } }

    private bool _isOutputsPropSet = false;
    private OutputsYield _outputs;
    public OutputsYield Outputs {
        get { return _outputs; }
        set {
            D.Assert(!_isOutputsPropSet);
            _outputs = value;
            OutputsPropSetHandler();
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    public override bool IsInitialized { get { return _isOutputsPropSet; } }

    private float? _netIncomeYield = null;
    private UILabel _netIncomeLabel;

    protected override void InitializeValuesAndReferences() {
        _netIncomeLabel = gameObject.GetSingleComponentInChildren<UILabel>();
    }

    #region Event and Property Change Handlers

    private void OutputsPropSetHandler() {
        _isOutputsPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        string incomeTooltipContent = Unknown;
        string expenseTooltipContent = Unknown;
        string labelContent = Unknown;
        GameColor labelContentColor = GameColor.White;

        if (Outputs.IsPresent(OutputID.NetIncome)) {
            // Income and Expense must also be present per OutputsYield validation
            _netIncomeYield = Outputs.GetYield(OutputID.NetIncome);
        }
        else {
            if (Outputs.IsPresent(OutputID.Expense)) {
                float? expenseYield = Outputs.GetYield(OutputID.Expense);
                _netIncomeYield = expenseYield.HasValue ? -expenseYield.Value : (float?)null;
            }
        }

        if (_netIncomeYield.HasValue) {
            labelContentColor = _netIncomeYield.Value < Constants.ZeroF ? GameColor.Red : GameColor.Green;
            labelContent = Constants.FormatFloat_0Dp.Inject(_netIncomeYield.Value);
            incomeTooltipContent = Unknown;
            if (Outputs.IsPresent(OutputID.Income)) {
                float? incomeYield = Outputs.GetYield(OutputID.Income);
                if (incomeYield.HasValue) {
                    incomeTooltipContent = Constants.FormatFloat_0Dp.Inject(incomeYield.Value);
                }
            }

            D.Assert(Outputs.IsPresent(OutputID.Expense));
            float? expenseYield = Outputs.GetYield(OutputID.Expense);
            D.Assert(expenseYield.HasValue);
            expenseTooltipContent = Constants.FormatFloat_0Dp.Inject(expenseYield.Value);
        }
        else {
            labelContent = Unknown;
            incomeTooltipContent = Unknown;
            expenseTooltipContent = Unknown;
        }
        _netIncomeLabel.text = labelContent.SurroundWith(labelContentColor);
        _tooltipContent = TooltipFormat.Inject(incomeTooltipContent, expenseTooltipContent);
    }

    public override void ResetForReuse() {
        _isOutputsPropSet = false;
        _netIncomeYield = null;
    }

    protected override void Cleanup() { }

    #region IComparable<NetIncomeGuiElement> Members

    public int CompareTo(NetIncomeGuiElement other) {
        int result;
        if (!_netIncomeYield.HasValue) {
            if (other._netIncomeYield.HasValue) {
                // unknown value sorts higher than negative netIncome
                result = other._netIncomeYield.Value < Constants.ZeroF ? Constants.One : Constants.MinusOne;
            }
            else {
                result = Constants.Zero;
            }
        }
        else {
            if (!other._netIncomeYield.HasValue) {
                // unknown value sorts higher than negative netIncome
                result = _netIncomeYield.Value < Constants.ZeroF ? Constants.MinusOne : Constants.One;
            }
            else {
                result = _netIncomeYield.Value.CompareTo(other._netIncomeYield.Value);
            }
        }
        return result;
    }

    #endregion

    #region Archive

    // Secondary comparison using Income and Expense if NetIncome not conclusive

    ////public int CompareTo(NetIncomeGuiElement other) {
    ////    int result;

    ////    if (!_netIncome.HasValue) {
    ////        if (other._netIncome.HasValue) {
    ////            // unknown value sorts higher than negative netIncome
    ////            result = other._netIncome.Value < Constants.ZeroF ? Constants.One : Constants.MinusOne;
    ////        }
    ////        else {
    ////            result = Constants.Zero;
    ////        }
    ////    }
    ////    else {
    ////        if (!other._netIncome.HasValue) {
    ////            // unknown value sorts higher than negative netIncome
    ////            result = _netIncome.Value < Constants.ZeroF ? Constants.MinusOne : Constants.One;
    ////        }
    ////        else {
    ////            result = _netIncome.Value.CompareTo(other._netIncome.Value);
    ////        }
    ////    }

    ////    if (result == Constants.Zero) {
    ////        // either both _netIncome = ? or they have the same value so sort on Income
    ////        if (!Income.HasValue) {
    ////            result = !other.Income.HasValue ? Constants.Zero : Constants.MinusOne;
    ////        }
    ////        else {
    ////            result = !other.Income.HasValue ? Constants.One : Income.Value.CompareTo(other.Income.Value);
    ////        }
    ////    }

    ////    if (result == Constants.Zero) {
    ////        // neither netIncome or Income have separated the two, so use Expense 
    ////        // as Expense is always accounted for as a positive value, higher Expense is worse than lower expense
    ////        if (!Expense.HasValue) {
    ////            result = !other.Expense.HasValue ? Constants.Zero : Constants.One;
    ////        }
    ////        else {
    ////            result = !other.Expense.HasValue ? Constants.MinusOne : -Expense.Value.CompareTo(other.Expense.Value);
    ////        }
    ////    }
    ////    return result;
    ////}

    #endregion
}

