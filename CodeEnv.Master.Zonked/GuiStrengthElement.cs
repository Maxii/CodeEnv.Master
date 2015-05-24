// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiStrengthElement.cs
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
[Obsolete]
public class GuiStrengthElement : GuiElement, IComparable<GuiStrengthElement> {

    private static string _totalStrengthTooltipFormat = "Offense:" + Constants.NewLine + "{0}"
                                                            + Constants.NewLine +
                                                        "Defense:" + Constants.NewLine + "{1}";

    private static string _labelFormat = Constants.FormatFloat_1DpMax;
    private static string _unknown = Constants.QuestionMark;

    private CombatStrength? _offensiveStrength;
    public CombatStrength? OffensiveStrength {
        get { return _offensiveStrength; }
        set { SetProperty<CombatStrength?>(ref _offensiveStrength, value, "OffensiveStrength", OnOffensiveStrengthChanged); }
    }

    private CombatStrength? _defensiveStrength;
    public CombatStrength? DefensiveStrength {
        get { return _defensiveStrength; }
        set { SetProperty<CombatStrength?>(ref _defensiveStrength, value, "DefensiveStrength", OnDefensiveStrengthChanged); }
    }

    protected override string TooltipContent { get { return GetTooltipContent(); } }

    private UILabel _label;
    private bool _valuesAreReady = false;   // IMPROVE as currently implemented an instance of this class is intended for one-time use

    protected override void Awake() {
        base.Awake();
        Validate();
        _label = gameObject.GetSafeMonoBehaviourInChildren<UILabel>();
    }


    private void OnOffensiveStrengthChanged() {
        D.Assert(elementID != GuiElementID.DefensiveStrength, "Do not use {0}.OffensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetName()));

        if (elementID == GuiElementID.OffensiveStrength || (elementID == GuiElementID.TotalStrength && _valuesAreReady)) {
            PopulateElementWidgets();
        }
        _valuesAreReady = true;
    }

    private void OnDefensiveStrengthChanged() {
        D.Assert(elementID != GuiElementID.OffensiveStrength, "Do not use {0}.DefensiveStrength when using {1}.{2}.".Inject(GetType().Name, typeof(GuiElementID).Name, elementID.GetName()));
        if (elementID == GuiElementID.DefensiveStrength || (elementID == GuiElementID.TotalStrength && _valuesAreReady)) {
            PopulateElementWidgets();
        }
        _valuesAreReady = true;
    }

    private void PopulateElementWidgets() {
        string labelText = string.Empty;
        switch (elementID) {
            case GuiElementID.OffensiveStrength:
                labelText = OffensiveStrength.HasValue ? _labelFormat.Inject(OffensiveStrength.Value.Combined) : _unknown;
                break;
            case GuiElementID.DefensiveStrength:
                labelText = DefensiveStrength.HasValue ? _labelFormat.Inject(DefensiveStrength.Value.Combined) : _unknown;
                break;
            case GuiElementID.TotalStrength:
                CombatStrength? totalStrength = OffensiveStrength + DefensiveStrength;
                labelText = totalStrength.HasValue ? _labelFormat.Inject(totalStrength.Value.Combined) : _unknown;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementID));
        }
        _label.text = labelText;
    }

    private string GetTooltipContent() {
        switch (elementID) {
            case GuiElementID.OffensiveStrength:
                return OffensiveStrength.HasValue ? OffensiveStrength.Value.ToLabel() : _unknown;
            case GuiElementID.DefensiveStrength:
                return DefensiveStrength.HasValue ? DefensiveStrength.Value.ToLabel() : _unknown;
            case GuiElementID.TotalStrength:
                return GetTotalStrengthTooltip();
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementID));
        }
    }

    private string GetTotalStrengthTooltip() {
        string offensiveText = OffensiveStrength.HasValue ? OffensiveStrength.Value.ToLabel() : _unknown;
        string defensiveText = DefensiveStrength.HasValue ? DefensiveStrength.Value.ToLabel() : _unknown;
        return _totalStrengthTooltipFormat.Inject(offensiveText, defensiveText);
    }

    private void Validate() {
        D.Assert(elementID == GuiElementID.OffensiveStrength || elementID == GuiElementID.DefensiveStrength
            || elementID == GuiElementID.TotalStrength, "{0} has illegal ElementID: {1}.".Inject(GetType().Name, elementID.GetName()));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

    #region IComparable<GuiStrengthElement> Members

    public int CompareTo(GuiStrengthElement other) {
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

