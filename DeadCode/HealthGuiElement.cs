// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: HealthGuiElement.cs
// GuiElement handling the display and tooltip content for the Health of a Command.     
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// GuiElement handling the display and tooltip content for the Health of a Command.     
/// </summary>
[Obsolete]
public class HealthGuiElement : GuiElement, IComparable<HealthGuiElement> {

    /// <summary>
    /// Tooltip format when values are unknown, aka ?% ?/?.
    /// </summary>
    private static string _unknownTooltipFormat = Constants.QuestionMark + Constants.PercentSign +
        Constants.Space + Constants.QuestionMark + Constants.ForwardSlash + Constants.QuestionMark;

    /// <summary>
    /// Tooltip format when values are known. Health percentage followed by CurrentHitPts / MaxHitPts, aka 100% 240/240.
    /// </summary>
    private static string _tooltipFormat = Constants.FormatPercent_0Dp + Constants.Space + "{1:0.}" + Constants.ForwardSlash + "{2:0.}";

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private float? _currentHitPts;
    private float? _maxHitPts;
    private float? _health;

    private bool _fieldsHaveValue;
    private UISlider _slider;
    private UISprite _barForeground;

    protected override void Awake() {
        base.Awake();
        Validate();
        _slider = gameObject.GetSafeFirstMonoBehaviourInChildren<UISlider>();
        _barForeground = _slider.gameObject.GetSafeMonoBehaviourInImmediateChildren<UISprite>();
    }

    public void SetValues(float? health, float? currentHitPts, float? maxHitPts) {
        // make sure either all have value, or all don't. No mixing
        Arguments.Validate((health.HasValue == currentHitPts.HasValue) && (health.HasValue == maxHitPts.HasValue));
        if (health.HasValue) {
            Arguments.ValidateForRange(health.Value, Constants.ZeroF, Constants.OneF);
            _fieldsHaveValue = true;
        }
        _health = health;
        _currentHitPts = currentHitPts;
        _maxHitPts = maxHitPts;

        PopulateElementWidgets();
    }

    private void PopulateElementWidgets() {
        string tooltip;
        GameColor barColor;
        if (_fieldsHaveValue) {
            _slider.value = _health.Value;
            D.Log("{0} setting slider value to {1:0.#}.", GetType().Name, _health.Value);
            tooltip = _tooltipFormat.Inject(_health.Value, _currentHitPts.Value, _maxHitPts.Value);

            if (_health.Value > GeneralSettings.Instance.InjuredHealthThreshold) {
                barColor = GameColor.Green;
            }
            else if (_health.Value > GeneralSettings.Instance.CriticalHealthThreshold) {
                barColor = GameColor.Yellow;
            }
            else {
                barColor = GameColor.Red;
            }
        }
        else {
            barColor = GameColor.Clear;
            tooltip = _unknownTooltipFormat;
        }
        D.Log("{0} setting barColor to {1}.", GetType().Name, barColor.GetValueName());
        _barForeground.color = barColor.ToUnityColor();
        _tooltipContent = tooltip;
    }

    private void Validate() {
        if (elementID != GuiElementID.Health) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetValueName());
            elementID = GuiElementID.Health;
        }
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<GuiHealthElement> Members

    public int CompareTo(HealthGuiElement other) {
        int result;
        if (!_fieldsHaveValue) {
            result = !other._fieldsHaveValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other._fieldsHaveValue ? Constants.One : _health.Value.CompareTo(other._health.Value);
            if (result == Constants.Zero) {
                // health % is the same, so sort now on remaining hit pts   // IMPROVE hitPts first, then %?
                result = _currentHitPts.Value.CompareTo(other._currentHitPts.Value);
            }
        }
        return result;
    }

    #endregion

}

