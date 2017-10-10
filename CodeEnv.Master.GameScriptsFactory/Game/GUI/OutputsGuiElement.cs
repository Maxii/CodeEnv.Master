// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: OutputsGuiElement.cs
// AGuiElement that represents the Outputs associated with a Unit Element, Command or Empire.
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
using UnityEngine;

/// <summary>
/// AGuiElement that represents the Outputs associated with a Unit Element, Command or Empire. Handles unknown individual output values.
/// </summary>
public class OutputsGuiElement : AGuiElement {

    private const string OutputValueFormat_Label = Constants.FormatInt_1DMin;

    private const string GeneralTooltipFormat = "{0}[{1}]";

    private const string NetIncomeTooltipFormat = "{0}[{1}] ({2}-{3})";

    private const string OutputValueFormat_Tooltip = Constants.FormatFloat_1DpMax;

    private static IList<OutputsID> OutputsToShow_UseNetIncome = new OutputsID[] {
        OutputsID.Food, OutputsID.Prodn, OutputsID.NetIncome, OutputsID.Science, OutputsID.Culture
    };

    private static IList<OutputsID> OutputsToShow_UseIncomeExpense = new OutputsID[] {
        OutputsID.Food, OutputsID.Prodn, OutputsID.Income, OutputsID.Expense, OutputsID.Science, OutputsID.Culture
    };


#pragma warning disable 0649

    [SerializeField]
    private UIWidget[] _containers;

    [Tooltip("Check to select between showing 'NetIncome' or 'Income and Expense'")]
    [SerializeField]
    private bool _useNetIncome = true;

#pragma warning restore 0649

    private bool _isFoodPropSet;   // can be a null if unknown
    private float? _food;
    public float? Food {
        get { return _food; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _food = value;
            FoodPropSetHandler();
        }
    }

    private bool _isProdnPropSet;   // can be a null if unknown
    private float? _production;
    public float? Production {
        get { return _production; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _production = value;
            ProductionPropSetHandler();
        }
    }

    private bool _isIncomePropSet;   // can be a null if unknown
    private float? _income;
    public float? Income {
        get { return _income; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _income = value;
            IncomePropSetHandler();
        }
    }

    private bool _isExpensePropSet;   // can be a null if unknown
    private float? _expense;
    public float? Expense {
        get { return _expense; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _expense = value;
            ExpensePropSetHandler();
        }
    }

    private bool _isSciencePropSet;   // can be a null if unknown
    private float? _science;
    public float? Science {
        get { return _science; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _science = value;
            SciencePropSetHandler();
        }
    }

    private bool _isCulturePropSet;   // can be a null if unknown
    private float? _culture;
    public float? Culture {
        get { return _culture; }
        set {
            D.Assert(!_isFoodPropSet); // occurs only once between Resets
            _culture = value;
            CulturePropSetHandler();
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Outputs; } }

    public override bool IsInitialized {
        get { return _isCulturePropSet && _isExpensePropSet && _isFoodPropSet && _isIncomePropSet && _isProdnPropSet && _isSciencePropSet; }
    }


    /// <summary>
    /// Lookup for ResourceIDs, keyed by the Resource container's gameObject. 
    /// Used to show the right ResourceID tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, OutputsID> _outputsIDLookup;
    private float _totalOutputYield = Constants.ZeroF;

    protected override void InitializeValuesAndReferences() {
        _outputsIDLookup = new Dictionary<GameObject, OutputsID>(_containers.Length);
        InitializeContainers();
    }

    private void InitializeContainers() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip += OutputsContainerTooltipEventHandler;
            NGUITools.SetActive(container.gameObject, false);
        }
    }

    #region Event and Property Change Handlers

    private void OutputsContainerTooltipEventHandler(GameObject containerGo, bool show) {
        if (show) {
            var outputsID = _outputsIDLookup[containerGo];
            string tooltipText = GetTooltipText(outputsID);
            TooltipHudWindow.Instance.Show(tooltipText);
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }


    private void FoodPropSetHandler() {
        _isFoodPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void ProductionPropSetHandler() {
        _isProdnPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void IncomePropSetHandler() {
        _isIncomePropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void ExpensePropSetHandler() {
        _isExpensePropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void SciencePropSetHandler() {
        _isSciencePropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    private void CulturePropSetHandler() {
        _isCulturePropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();
        IList<OutputsID> outputsToShow = _useNetIncome ? OutputsToShow_UseNetIncome : OutputsToShow_UseIncomeExpense;
        int outputsToShowCount = outputsToShow.Count;
        D.Assert(_containers.Length >= outputsToShowCount);
        float cumOutputYield = Constants.ZeroF;
        for (int i = Constants.Zero; i < outputsToShowCount; i++) {
            UIWidget container = _containers[i];
            NGUITools.SetActive(container.gameObject, true);

            UISprite iconSprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
            var outputID = outputsToShow[i];
            iconSprite.atlas = outputID.GetIconAtlasID().GetAtlas();
            iconSprite.spriteName = outputID.GetIconFilename();
            _outputsIDLookup.Add(container.gameObject, outputID);

            float yield = GetOutputYield(outputID);
            cumOutputYield += yield;
            var yieldLabel = container.gameObject.GetSingleComponentInChildren<UILabel>();
            yieldLabel.text = OutputValueFormat_Label.Inject(Mathf.RoundToInt(yield));
        }
        _totalOutputYield = cumOutputYield;
        D.Assert(_totalOutputYield >= Constants.ZeroF);
    }

    private float GetOutputYield(OutputsID outputID) {
        float outputYield;
        switch (outputID) {
            case OutputsID.Food:
                outputYield = Food.HasValue ? Food.Value : Constants.ZeroF;
                break;
            case OutputsID.Prodn:
                outputYield = Production.HasValue ? Production.Value : Constants.ZeroF;
                break;
            case OutputsID.Income:
                outputYield = Income.HasValue ? Income.Value : Constants.ZeroF;
                break;
            case OutputsID.Expense:
                outputYield = Expense.HasValue ? Expense.Value : Constants.ZeroF;
                break;
            case OutputsID.NetIncome:
                if (Income.HasValue) {
                    outputYield = Expense.HasValue ? Income.Value - Expense.Value : Income.Value;
                }
                else {
                    outputYield = Expense.HasValue ? -Expense.Value : Constants.ZeroF;
                }
                break;
            case OutputsID.Science:
                outputYield = Science.HasValue ? Science.Value : Constants.ZeroF;
                break;
            case OutputsID.Culture:
                outputYield = Culture.HasValue ? Culture.Value : Constants.ZeroF;
                break;
            case OutputsID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
        }
        return outputYield;
    }

    private string GetTooltipText(OutputsID outputsID) {
        string valueText;
        switch (outputsID) {
            case OutputsID.Food:
                valueText = Food.HasValue ? OutputValueFormat_Tooltip.Inject(Food) : Unknown;
                break;
            case OutputsID.Prodn:
                valueText = Production.HasValue ? OutputValueFormat_Tooltip.Inject(Production) : Unknown;
                break;
            case OutputsID.Income:
                valueText = Income.HasValue ? OutputValueFormat_Tooltip.Inject(Income) : Unknown;
                break;
            case OutputsID.Expense:
                valueText = Expense.HasValue ? OutputValueFormat_Tooltip.Inject(Expense) : Unknown;
                break;
            case OutputsID.NetIncome:
                return GetNetIncomeText();
            case OutputsID.Science:
                valueText = Science.HasValue ? OutputValueFormat_Tooltip.Inject(Science) : Unknown;
                break;
            case OutputsID.Culture:
                valueText = Culture.HasValue ? OutputValueFormat_Tooltip.Inject(Culture) : Unknown;
                break;
            case OutputsID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputsID));
        }
        return GeneralTooltipFormat.Inject(outputsID.GetValueName(), valueText);
    }

    private string GetNetIncomeText() {
        string valueText;
        string incomeText;
        string expenseText;
        if (Income.HasValue) {
            incomeText = OutputValueFormat_Tooltip.Inject(Income);
            if (Expense.HasValue) {
                valueText = OutputValueFormat_Tooltip.Inject(Income - Expense);
                expenseText = OutputValueFormat_Tooltip.Inject(Income);
            }
            else {
                valueText = OutputValueFormat_Tooltip.Inject(Income);
                expenseText = Unknown;
            }
        }
        else {
            incomeText = Unknown;
            if (Expense.HasValue) {
                valueText = OutputValueFormat_Tooltip.Inject(-Expense);
                expenseText = OutputValueFormat_Tooltip.Inject(Expense);
            }
            else {
                valueText = Unknown;
                expenseText = Unknown;
            }
        }
        return NetIncomeTooltipFormat.Inject(OutputsID.NetIncome.GetValueName(), valueText, incomeText, expenseText);
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void ResetForReuse() {
        _containers.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        _outputsIDLookup.Clear();
        _isFoodPropSet = false;
        _isProdnPropSet = false;
        _isIncomePropSet = false;
        _isExpensePropSet = false;
        _isSciencePropSet = false;
        _isCulturePropSet = false;
        _totalOutputYield = Constants.ZeroF;
    }

    #region Cleanup

    private void Unsubscribe() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip -= OutputsContainerTooltipEventHandler;
        }
    }

    protected override void Cleanup() {
        Unsubscribe();
    }

    #endregion

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        Utility.ValidateNotNullOrEmpty<UIWidget>(_containers);
        foreach (var container in _containers) {
            D.AssertNotNull(container);
        }
    }

    #endregion

    #region IComparable<OutputsGuiElement> Members

    public int CompareTo(OutputsGuiElement other) {
        int result;
        if (_totalOutputYield == Constants.ZeroF) {
            result = other._totalOutputYield == Constants.ZeroF ? Constants.Zero : Constants.MinusOne;
        }
        else if (Food == null) {
            // an unknown yield (Resources == null) sorts higher than a yield that is Zero
            result = other.Food == null ? Constants.Zero : (other._totalOutputYield == Constants.ZeroF) ? Constants.One : Constants.MinusOne;
        }
        else {
            result = (other._totalOutputYield == Constants.ZeroF || other.Food == null) ? Constants.One : _totalOutputYield.CompareTo(other._totalOutputYield);
        }
        return result;
    }

    #endregion

}

