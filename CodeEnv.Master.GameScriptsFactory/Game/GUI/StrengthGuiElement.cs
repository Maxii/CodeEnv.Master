// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StrengthGuiElement.cs
// GuiElement handling the display and tooltip content for OffensiveStrength and DefensiveStrength.   
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// GuiElement handling the display and tooltip content for OffensiveStrength and DefensiveStrength.   
/// </summary>
public class StrengthGuiElement : AGuiElement, IComparable<StrengthGuiElement> {

    //[FormerlySerializedAs("widgetsPresent")]
    [Tooltip("The widgets that are present to display the content of this GuiElement.")]
    [SerializeField]
    private WidgetsPresent _widgetsPresent = WidgetsPresent.SumLabel;

    //[FormerlySerializedAs("elementID")]
    [Tooltip("The unique ID of this Strength GuiElement")]
    [SerializeField]
    private GuiElementID _elementID = GuiElementID.None;

    public override GuiElementID ElementID { get { return _elementID; } }

    private bool _isStrengthSet;
    private CombatStrength? _strength;
    public CombatStrength? Strength {
        get { return _strength; }
        set {
            D.Assert(!_isStrengthSet);  // only occurs once between Resets
            _strength = value;
            StrengthPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool AreAllValuesSet { get { return _isStrengthSet; } }

    private UILabel _combinedValueLabel;
    private UILabel _detailValuesLabel;

    protected sealed override void Awake() {
        base.Awake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        var labels = gameObject.GetSafeComponentsInChildren<UILabel>();
        switch (_widgetsPresent) {
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
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
    }

    #region Event and Property Change Handlers

    private void StrengthPropSetHandler() {
        _isStrengthSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    private void PopulateElementWidgets() {
        string combinedValueLabelText = _unknown;
        string detailValuesLabelText = _unknown;
        string tooltipText = _unknown;

        if (Strength.HasValue) {
            combinedValueLabelText = Strength.Value.TotalDeliveryStrength.FormatValue();
            detailValuesLabelText = __GetStrengthDetailText(Strength.Value);
            tooltipText = detailValuesLabelText;
        }

        if (_widgetsPresent != WidgetsPresent.DetailsLabel) {
            _combinedValueLabel.text = combinedValueLabelText;
        }
        if (_widgetsPresent != WidgetsPresent.SumLabel) {
            _detailValuesLabel.text = detailValuesLabelText;
        }
        if (_widgetsPresent == WidgetsPresent.SumLabel) {
            _tooltipContent = tooltipText;
        }
    }

    private string __GetStrengthDetailText(CombatStrength strength) {
        return strength.BeamDeliveryStrength.ToLabel()
            + Constants.NewLine + strength.ProjectileDeliveryStrength.ToLabel()
            + Constants.NewLine + strength.MissileDeliveryStrength.ToLabel();
    }

    public override void Reset() {
        _isStrengthSet = false;
    }

    protected override void Validate() {
        base.Validate();
        D.Assert(ElementID == GuiElementID.OffensiveStrength || ElementID == GuiElementID.DefensiveStrength, ElementID.GetValueName());
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<StrengthGuiElement> Members

    public int CompareTo(StrengthGuiElement other) {
        D.AssertEqual(_elementID, other._elementID);

        int result;
        if (!Strength.HasValue) {
            result = !other.Strength.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other.Strength.HasValue ? Constants.One : Strength.Value.CompareTo(other.Strength.Value);
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

