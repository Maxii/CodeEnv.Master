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
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for OffensiveStrength, DefensiveStrength and Total Strength.  
/// </summary>
public class StrengthGuiElement : GuiElement, IComparable<StrengthGuiElement> {

    private static string _unknown = Constants.QuestionMark;
    private static string _labelFormat = Constants.FormatFloat_1DpMax;
    private static string _totalStrengthTooltipFormat = "Offense:" + Constants.NewLine + "{0}"
                                                            + Constants.NewLine +
                                                        "Defense:" + Constants.NewLine + "{1}";

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

    private UILabel _label;

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }

    private void OnOffensiveStrengthSet() {
        D.Assert(elementID != GuiElementID.DefensiveStrength, "Do not use {0}.OffensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetName()));
        _isOffensiveStrengthSet = true;

        if (elementID == GuiElementID.OffensiveStrength || AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void OnDefensiveStrengthSet() {
        D.Assert(elementID != GuiElementID.OffensiveStrength, "Do not use {0}.DefensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetName()));
        _isDefensiveStrengthSet = true;
        if (elementID == GuiElementID.DefensiveStrength || AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PopulateElementWidgets() {
        string labelText = string.Empty;
        string tooltip = string.Empty;
        switch (elementID) {
            case GuiElementID.OffensiveStrength:
                labelText = OffensiveStrength.HasValue ? _labelFormat.Inject(OffensiveStrength.Value.Combined) : _unknown;
                tooltip = OffensiveStrength.HasValue ? OffensiveStrength.Value.ToLabel() : _unknown;
                break;
            case GuiElementID.DefensiveStrength:
                labelText = DefensiveStrength.HasValue ? _labelFormat.Inject(DefensiveStrength.Value.Combined) : _unknown;
                tooltip = DefensiveStrength.HasValue ? DefensiveStrength.Value.ToLabel() : _unknown;
                break;
            case GuiElementID.TotalStrength:
                CombatStrength? totalStrength = OffensiveStrength + DefensiveStrength;
                labelText = totalStrength.HasValue ? _labelFormat.Inject(totalStrength.Value.Combined) : _unknown;
                tooltip = GetTotalStrengthTooltip();
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementID));
        }
        _label.text = labelText;
        _tooltipContent = tooltip;
    }

    private string GetTotalStrengthTooltip() {
        string offensiveText = OffensiveStrength.HasValue ? OffensiveStrength.Value.ToLabel() : _unknown;
        string defensiveText = DefensiveStrength.HasValue ? DefensiveStrength.Value.ToLabel() : _unknown;
        return _totalStrengthTooltipFormat.Inject(offensiveText, defensiveText);
    }

    public override void Reset() {
        base.Reset();
        _isDefensiveStrengthSet = false;
        _isOffensiveStrengthSet = false;
    }

    private void Validate() {
        D.Assert(elementID == GuiElementID.OffensiveStrength || elementID == GuiElementID.DefensiveStrength
            || elementID == GuiElementID.TotalStrength, "{0} has illegal ElementID: {1}.".Inject(GetType().Name, elementID.GetName()));
    }

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

}

