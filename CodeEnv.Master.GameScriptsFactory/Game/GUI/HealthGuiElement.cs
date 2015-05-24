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
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Health of an Item.      
/// </summary>
public class HealthGuiElement : GuiElement, IComparable<HealthGuiElement> {

    private static string _unknown = Constants.QuestionMark;

    /// <summary>
    /// Tooltip format. Health percentage followed by CurrentHitPts / MaxHitPts, aka 100% 240/240.
    /// </summary>
    private static string _tooltipFormat = "{0} {1}/{2}";

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool _isCurrentHitPtsSet;
    private float? _currentHitPts;
    public float? CurrentHitPts {
        get { return _currentHitPts; }
        set {
            _currentHitPts = value;
            OnCurrentHitPtsSet();
        }
    }

    private bool _isMaxHitPtsSet;
    private float? _maxHitPts;
    public float? MaxHitPts {
        get { return _maxHitPts; }
        set {
            _maxHitPts = value;
            OnMaxHitPtsSet();
        }
    }

    private bool _isHealthSet;
    private float? _health;
    public float? Health {
        get { return _health; }
        set {
            _health = value;
            OnHealthSet();
        }
    }

    private bool AreAllValuesSet { get { return _isCurrentHitPtsSet && _isMaxHitPtsSet && _isHealthSet; } }

    private UISlider _slider;
    private UISprite _barForeground;

    protected override void Awake() {
        base.Awake();
        Validate();
        _slider = gameObject.GetSafeMonoBehaviourInChildren<UISlider>();
        _barForeground = _slider.gameObject.GetSafeMonoBehaviourInImmediateChildren<UISprite>();
    }

    private void OnCurrentHitPtsSet() {
        _isCurrentHitPtsSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void OnMaxHitPtsSet() {
        _isMaxHitPtsSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void OnHealthSet() {
        if (Health.HasValue) {
            Arguments.ValidateForRange(Health.Value, Constants.ZeroF, Constants.OneF);
        }
        _isHealthSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PopulateElementWidgets() {
        GameColor barColor = GameColor.Clear;   // appears disabled
        string healthTooltip = _unknown;
        if (Health.HasValue) {
            _slider.value = Health.Value;
            healthTooltip = Constants.FormatPercent_0Dp.Inject(Health.Value);
            D.Log("{0} setting slider value to {1:0.#}.", GetType().Name, Health.Value);
            if (Health.Value > GeneralSettings.Instance.InjuredHealthThreshold) {
                barColor = GameColor.Green;
            }
            else if (Health.Value > GeneralSettings.Instance.CriticalHealthThreshold) {
                barColor = GameColor.Yellow;
            }
            else {
                barColor = GameColor.Red;
            }
        }
        D.Log("{0} setting barColor to {1}.", GetType().Name, barColor.GetName());
        _barForeground.color = barColor.ToUnityColor();

        string currentHitPtsTooltip = _unknown;
        if (CurrentHitPts.HasValue) {
            currentHitPtsTooltip = Constants.FormatFloat_0Dp.Inject(CurrentHitPts.Value);
        }

        string maxHitPtsTooltip = _unknown;
        if (MaxHitPts.HasValue) {
            maxHitPtsTooltip = Constants.FormatFloat_0Dp.Inject(MaxHitPts.Value);
        }
        _tooltipContent = _tooltipFormat.Inject(healthTooltip, currentHitPtsTooltip, maxHitPtsTooltip);
    }

    public override void Reset() {
        base.Reset();
        _isCurrentHitPtsSet = false;
        _isHealthSet = false;
        _isMaxHitPtsSet = false;
    }

    private void Validate() {
        if (elementID != GuiElementID.Health) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Health;
        }
    }

    public override string ToString() { return GetType().Name + Constants.Space + TooltipContent; }

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

