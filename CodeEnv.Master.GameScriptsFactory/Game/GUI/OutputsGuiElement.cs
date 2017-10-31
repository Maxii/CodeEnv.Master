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
    private const string NetIncomeTooltipFormat = "{0}[{1}] {2}-{3}";   // ()'s cause tooltip to fade out
    private const string OutputValueFormat_Tooltip = Constants.FormatFloat_1DpMax;

#pragma warning disable 0649

    [SerializeField]
    private UIWidget[] _containers;

#pragma warning restore 0649

    [Tooltip("Check to select between showing 'NetIncome' or 'Income and Expense'")]
    [SerializeField]
    private bool _useNetIncome = true;

    private bool _isOutputsPropSet;   // can be default(OutputsYield) if no access to Outputs
    private OutputsYield _outputs;
    public OutputsYield Outputs {
        get { return _outputs; }
        set {
            D.Assert(!_isOutputsPropSet); // occurs only once between Resets
            _outputs = value;
            OutputsPropSetHandler();
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Outputs; } }

    public override bool IsInitialized { get { return _isOutputsPropSet; } }

    /// <summary>
    /// Lookup for OutputID, keyed by the Output container's gameObject. 
    /// Used to show the right OutputID tooltip when the container is hovered over.
    /// </summary>
    private IDictionary<GameObject, OutputID> _outputsIDLookup;
    private float? _totalOutputsYield = null;
    private UILabel _unknownLabel;

    protected override void InitializeValuesAndReferences() {
        _outputsIDLookup = new Dictionary<GameObject, OutputID>(_containers.Length);

        _unknownLabel = gameObject.GetSingleComponentInImmediateChildren<UILabel>(includeInactive: true);
        if (_unknownLabel.gameObject.activeSelf) {   // 10.21.17 If initially inactive, this usage can't result in unknown
            MyEventListener.Get(_unknownLabel.gameObject).onTooltip += UnknownTooltipEventHandler;
            NGUITools.SetActive(_unknownLabel.gameObject, false);
        }

        InitializeContainers();
    }

    private void InitializeContainers() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip += OutputsContainerTooltipEventHandler;
            NGUITools.SetActive(container.gameObject, false);
        }
    }

    #region Event and Property Change Handlers

    private void UnknownTooltipEventHandler(GameObject go, bool show) {
        if (show) {
            TooltipHudWindow.Instance.Show("Outputs unknown");
        }
        else {
            TooltipHudWindow.Instance.Hide();
        }
    }

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

    private void OutputsPropSetHandler() {
        _isOutputsPropSet = true;
        if (IsInitialized) {
            PopulateMemberWidgetValues();
        }
    }

    #endregion

    protected override void PopulateMemberWidgetValues() {
        base.PopulateMemberWidgetValues();

        if (Outputs == default(OutputsYield)) {
            HandleValuesUnknown();
            return;
        }

        OutputID[] outputsToShow = _useNetIncome ? Outputs.OutputsPresent.Except(OutputID.Income, OutputID.Expense).ToArray()
            : Outputs.OutputsPresent.Except(OutputID.NetIncome).ToArray();

        int outputsToShowCount = outputsToShow.Length;
        D.Assert(_containers.Length >= outputsToShowCount);
        float? cumOutputYield = null;
        for (int i = Constants.Zero; i < outputsToShowCount; i++) {
            UIWidget container = _containers[i];
            NGUITools.SetActive(container.gameObject, true);

            UISprite iconSprite = container.gameObject.GetSingleComponentInChildren<UISprite>();
            OutputID outputToShow = outputsToShow[i];
            iconSprite.atlas = outputToShow.GetIconAtlasID().GetAtlas();
            iconSprite.spriteName = outputToShow.GetIconFilename();
            _outputsIDLookup.Add(container.gameObject, outputToShow);

            float? yield = GetOutputYield(outputToShow);
            cumOutputYield.NullableSum(yield);

            GameColor yieldColor = GameColor.White;
            if (yield.HasValue) {
                if (outputToShow == OutputID.Income) {
                    yieldColor = GameColor.Green;
                }
                else if (outputToShow == OutputID.Expense) {
                    yieldColor = GameColor.Red;
                }
                else if (outputToShow == OutputID.NetIncome) {
                    yieldColor = yield.Value < Constants.ZeroF ? GameColor.Red : GameColor.Green;
                }
            }
            string yieldLabelText = yield.HasValue ? OutputValueFormat_Label.Inject(Mathf.RoundToInt(yield.Value)) : Unknown;
            var yieldLabel = container.gameObject.GetSingleComponentInChildren<UILabel>();
            yieldLabel.text = yieldLabelText.SurroundWith(yieldColor);
        }
        _totalOutputsYield = cumOutputYield;
    }

    private void HandleValuesUnknown() {
        NGUITools.SetActive(_unknownLabel.gameObject, true);
    }

    private float? GetOutputYield(OutputID outputToShow) {
        D.AssertNotDefault((int)outputToShow);

        float? outputYield = null;
        if (Outputs.IsPresent(outputToShow)) {
            outputYield = Outputs.GetYield(outputToShow);
        }
        return outputYield;
    }

    private string GetTooltipText(OutputID outputID) {
        string valueText;
        float? outputValue = null;
        switch (outputID) {
            case OutputID.Food:
                D.Assert(Outputs.IsPresent(OutputID.Food));
                outputValue = Outputs.GetYield(OutputID.Food);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.Production:
                D.Assert(Outputs.IsPresent(OutputID.Production));
                outputValue = Outputs.GetYield(OutputID.Production);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.Income:
                D.Assert(Outputs.IsPresent(OutputID.Income));
                outputValue = Outputs.GetYield(OutputID.Income);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.Expense:
                D.Assert(Outputs.IsPresent(OutputID.Expense));
                outputValue = Outputs.GetYield(OutputID.Expense);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.NetIncome:
                D.Assert(Outputs.IsPresent(OutputID.NetIncome));
                outputValue = Outputs.GetYield(OutputID.NetIncome);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.Science:
                D.Assert(Outputs.IsPresent(OutputID.Science));
                outputValue = Outputs.GetYield(OutputID.Science);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.Culture:
                D.Assert(Outputs.IsPresent(OutputID.Culture));
                outputValue = Outputs.GetYield(OutputID.Culture);
                valueText = outputValue.HasValue ? OutputValueFormat_Tooltip.Inject(outputValue.Value) : Unknown;
                break;
            case OutputID.None:
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
        }
        return GeneralTooltipFormat.Inject(outputID.GetValueName(), valueText);
    }

    /// <summary>
    /// Resets this GuiElement instance so it can be reused.
    /// </summary>
    public override void ResetForReuse() {
        _containers.ForAll(container => {
            NGUITools.SetActive(container.gameObject, false);
        });
        NGUITools.SetActive(_unknownLabel.gameObject, false);
        _outputsIDLookup.Clear();
        _totalOutputsYield = null;
        _isOutputsPropSet = false;
    }

    #region Cleanup

    private void Unsubscribe() {
        foreach (var container in _containers) {
            MyEventListener.Get(container.gameObject).onTooltip -= OutputsContainerTooltipEventHandler;
        }
        MyEventListener.Get(_unknownLabel.gameObject).onTooltip -= UnknownTooltipEventHandler;
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
        if (!_totalOutputsYield.HasValue) {
            result = !other._totalOutputsYield.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other._totalOutputsYield.HasValue ? Constants.One : _totalOutputsYield.Value.CompareTo(other._totalOutputsYield.Value);
        }
        return result;
    }

    #endregion

}

