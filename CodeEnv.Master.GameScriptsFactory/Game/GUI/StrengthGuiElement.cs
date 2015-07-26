// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrengthGuiElement.cs
// GuiElement handling the display and tooltip content for OffensiveStrength, DefensiveStrength and Total Strength.  
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for OffensiveStrength, DefensiveStrength and Total Strength.   
/// </summary>
public class StrengthGuiElement : AGuiElement, IComparable<StrengthGuiElement> {

    private static string _totalStrengthTooltipFormat = "Offense:" + Constants.NewLine + "{0}"
                                                            + Constants.NewLine +
                                                        "Defense:" + Constants.NewLine + "{1}";

    [Tooltip("The widgets that are present to display the content of this GuiElement.")]
    public WidgetsPresent widgetsPresent = WidgetsPresent.SumLabel;

    public GuiElementID elementID;

    public override GuiElementID ElementID { get { return elementID; } }

    private bool _isOffensiveStrengthSet = false;
    private CombatStrength? _offensiveStrength;
    public CombatStrength? OffensiveStrength {
        get { return _offensiveStrength; }
        set {
            _offensiveStrength = value;
            OnOffensiveStrengthSet();
        }
    }

    private bool _isDefensiveStrengthSet = false;
    private CombatStrength? _defensiveStrength;
    public CombatStrength? DefensiveStrength {
        get { return _defensiveStrength; }
        set {
            _defensiveStrength = value;
            OnDefensiveStrengthSet();
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool AreAllValuesSet { get { return _isOffensiveStrengthSet && _isDefensiveStrengthSet; } }

    private UILabel _combinedValueLabel;
    private UILabel _detailValuesLabel;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        var labels = gameObject.GetSafeMonoBehavioursInChildren<UILabel>();
        switch (widgetsPresent) {
            case WidgetsPresent.SumLabel:
                _combinedValueLabel = labels.Single();
                NGUITools.AddWidgetCollider(gameObject);
                break;
            case WidgetsPresent.DetailsLabel:
                _detailValuesLabel = labels.Single();
                break;
            case WidgetsPresent.Both:
                _detailValuesLabel = labels.Single(l => l.maxLineCount == 3);
                _combinedValueLabel = labels.Single(l => l != _detailValuesLabel);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(widgetsPresent));
        }
    }

    private void OnOffensiveStrengthSet() {
        D.Assert(elementID != GuiElementID.DefensiveStrength, "Do not use {0}.OffensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetValueName()));
        _isOffensiveStrengthSet = true;
        if (elementID == GuiElementID.OffensiveStrength || AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void OnDefensiveStrengthSet() {
        D.Assert(elementID != GuiElementID.OffensiveStrength, "Do not use {0}.DefensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetValueName()));
        _isDefensiveStrengthSet = true;
        if (elementID == GuiElementID.DefensiveStrength || AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PopulateElementWidgets() {
        string combinedValueLabelText = _unknown;
        string detailValuesLabelText = _unknown;
        string tooltipText = _unknown;
        switch (elementID) {
            case GuiElementID.OffensiveStrength:
                if (OffensiveStrength.HasValue) {
                    combinedValueLabelText = OffensiveStrength.Value.Combined.FormatValue(showZero: true);
                    detailValuesLabelText = OffensiveStrength.Value.ToLabel();
                    tooltipText = OffensiveStrength.Value.ToLabel();
                }
                break;
            case GuiElementID.DefensiveStrength:
                if (DefensiveStrength.HasValue) {
                    combinedValueLabelText = DefensiveStrength.Value.Combined.FormatValue(showZero: true);
                    detailValuesLabelText = DefensiveStrength.Value.ToLabel();
                    tooltipText = DefensiveStrength.Value.ToLabel();
                }
                break;
            case GuiElementID.TotalStrength:
                var totalStrength = OffensiveStrength + DefensiveStrength;
                if (totalStrength.HasValue) {
                    combinedValueLabelText = totalStrength.Value.Combined.FormatValue(showZero: true);
                }
                tooltipText = ConstructTotalStrengthTooltip();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementID));
        }

        if (widgetsPresent != WidgetsPresent.DetailsLabel) {
            _combinedValueLabel.text = combinedValueLabelText;
        }
        if (widgetsPresent != WidgetsPresent.SumLabel) {
            _detailValuesLabel.text = detailValuesLabelText;
        }
        if (widgetsPresent == WidgetsPresent.SumLabel) {
            _tooltipContent = tooltipText;
        }
    }

    private string ConstructTotalStrengthTooltip() {
        string offensiveText = OffensiveStrength.HasValue ? OffensiveStrength.Value.ToLabel() : _unknown;
        string defensiveText = DefensiveStrength.HasValue ? DefensiveStrength.Value.ToLabel() : _unknown;
        return _totalStrengthTooltipFormat.Inject(offensiveText, defensiveText);
    }

    public override void Reset() {
        _isDefensiveStrengthSet = false;
        _isOffensiveStrengthSet = false;
    }

    protected override void Validate() {
        base.Validate();
        D.Assert(ElementID == GuiElementID.OffensiveStrength || ElementID == GuiElementID.DefensiveStrength
        || ElementID == GuiElementID.TotalStrength, "{0} has illegal ElementID: {1}.".Inject(GetType().Name, ElementID.GetValueName()));

        if (ElementID == GuiElementID.TotalStrength) {
            D.Assert(widgetsPresent == WidgetsPresent.SumLabel);
        }
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<StrengthGuiElement> Members

    public int CompareTo(StrengthGuiElement other) {
        D.Assert(elementID == other.elementID);

        CombatStrength? strengthToCompare;
        CombatStrength? otherStrengthToCompare;
        switch (elementID) {
            case GuiElementID.OffensiveStrength:
                strengthToCompare = OffensiveStrength;
                otherStrengthToCompare = other.OffensiveStrength;
                break;
            case GuiElementID.DefensiveStrength:
                strengthToCompare = DefensiveStrength;
                otherStrengthToCompare = other.DefensiveStrength;
                break;
            case GuiElementID.TotalStrength:
                strengthToCompare = OffensiveStrength + DefensiveStrength;
                otherStrengthToCompare = other.OffensiveStrength + other.DefensiveStrength;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementID));
        }

        int result;
        if (!strengthToCompare.HasValue) {
            result = !otherStrengthToCompare.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !otherStrengthToCompare.HasValue ? Constants.One : strengthToCompare.Value.CompareTo(otherStrengthToCompare.Value);
        }
        return result;
    }

    #endregion

    #region Nested Classes

    /// <summary>
    /// Enum that identifies the Widgets that are present in this GuiElement.
    /// </summary>
    public enum WidgetsPresent {

        /// <summary>
        /// A label  for showing a single value to represent this GuiElement.
        /// </summary>
        SumLabel,

        /// <summary>
        /// A label for showing multiple detailed values to represent this GuiElement.
        /// </summary>
        DetailsLabel,

        /// <summary>
        /// Both widgets are present.
        /// </summary>
        Both
    }

    #endregion

}

