// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HealthGuiElement.cs
// GuiElement handling the display and tooltip content for the Health of an Item.      
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Health of an Item.      
/// </summary>
public class HealthGuiElement : AProgressBarGuiElement, IComparable<HealthGuiElement> {

    /// <summary>
    /// Format for the alternative display of values. Health percentage followed by CurrentHitPts / MaxHitPts, aka 100% 240/240.
    /// </summary>
    private static string _detailValuesFormat = "{0} {1}/{2}";

    private bool _isCurrentHitPtsSet;
    private float? _currentHitPts;
    public float? CurrentHitPts {
        get { return _currentHitPts; }
        set {
            D.Assert(!_isCurrentHitPtsSet); // happens only once between Resets
            _currentHitPts = value;
            CurrentHitPtsPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private bool _isMaxHitPtsSet;
    private float? _maxHitPts;
    public float? MaxHitPts {
        get { return _maxHitPts; }
        set {
            D.Assert(!_isMaxHitPtsSet);  // happens only once between Resets
            _maxHitPts = value;
            MaxHitPtsPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    private bool _isHealthSet;
    private float? _health;
    public float? Health {
        get { return _health; }
        set {
            D.Assert(!_isHealthSet);    // happens only once between Resets
            _health = value;
            HealthPropSetHandler();  // SetProperty() only calls handler when changed
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Health; } }

    protected override bool AreAllValuesSet { get { return _isCurrentHitPtsSet && _isMaxHitPtsSet && _isHealthSet; } }

    #region Event and Property Change Handlers

    private void CurrentHitPtsPropSetHandler() {
        _isCurrentHitPtsSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void MaxHitPtsPropSetHandler() {
        _isMaxHitPtsSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void HealthPropSetHandler() {
        if (Health.HasValue) {
            Utility.ValidateForRange(Health.Value, Constants.ZeroF, Constants.OneF);
        }
        _isHealthSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected override void PopulateElementWidgets() {
        if (!Health.HasValue) {
            HandleValuesUnknown();
            return;
        }

        GeneralSettings generalSettings = GeneralSettings.Instance;
        GameColor healthColor;
        float healthValue = Health.Value;
        if (healthValue > generalSettings.HealthThreshold_Damaged) {
            healthColor = GameColor.Green;
        }
        else if (healthValue > generalSettings.HealthThreshold_BadlyDamaged) {
            healthColor = GameColor.Yellow;
        }
        else if (healthValue > generalSettings.HealthThreshold_CriticallyDamaged) {
            healthColor = GameColor.Orange;
        }
        else {
            healthColor = GameColor.Red;
        }

        string currentHitPtsValueText = _unknown;
        if (CurrentHitPts.HasValue) {
            currentHitPtsValueText = Constants.FormatFloat_0Dp.Inject(CurrentHitPts.Value);
        }

        string maxHitPtsValueText = _unknown;
        if (MaxHitPts.HasValue) {
            maxHitPtsValueText = Constants.FormatFloat_0Dp.Inject(MaxHitPts.Value);
        }

        string healthValuePercentText = Constants.FormatPercent_0Dp.Inject(healthValue);
        _detailValuesContent = _detailValuesFormat.Inject(healthValuePercentText, currentHitPtsValueText, maxHitPtsValueText);

        var detailValuesContent_Colored = _detailValuesFormat.Inject(healthValuePercentText.SurroundWith(healthColor),
            currentHitPtsValueText.SurroundWith(healthColor), maxHitPtsValueText.SurroundWith(GameColor.Green));

        switch (_widgetsPresent) {
            case WidgetsPresent.ProgressBar:
                PopulateProgressBarValues(healthValue, healthColor);
                _tooltipContent = detailValuesContent_Colored;
                break;
            case WidgetsPresent.Label:
                _detailValuesLabel.text = detailValuesContent_Colored;
                break;
            case WidgetsPresent.Both:
                PopulateProgressBarValues(healthValue, healthColor);
                _detailValuesLabel.text = detailValuesContent_Colored;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
    }

    public override void Reset() {
        _isCurrentHitPtsSet = false;
        _isHealthSet = false;
        _isMaxHitPtsSet = false;
    }

    protected override void Cleanup() { }

    #region IComparable<HealthGuiElement> Members

    public int CompareTo(HealthGuiElement other) {
        int result;

        if (!CurrentHitPts.HasValue) {
            result = !other.CurrentHitPts.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other.CurrentHitPts.HasValue ? Constants.One : CurrentHitPts.Value.CompareTo(other.CurrentHitPts.Value);
        }

        if (result == Constants.Zero) {
            // CurrentHitPts doesn't help, so sort now on health
            if (!Health.HasValue) {
                result = !other.Health.HasValue ? Constants.Zero : Constants.MinusOne;
            }
            else {
                result = !other.Health.HasValue ? Constants.One : Health.Value.CompareTo(other.Health.Value);
            }
        }

        if (result == Constants.Zero) {
            // CurrentHitPts and Health don't help, so sort now on MaxHitPts
            if (!MaxHitPts.HasValue) {
                result = !other.MaxHitPts.HasValue ? Constants.Zero : Constants.MinusOne;
            }
            else {
                result = !other.MaxHitPts.HasValue ? Constants.One : MaxHitPts.Value.CompareTo(other.MaxHitPts.Value);
            }
        }
        return result;
    }

    #endregion

}

