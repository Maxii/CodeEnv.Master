// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ApprovalGuiElement.cs
// GuiElement handling the display and tooltip content for the Approval in a Command.       
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
/// GuiElement handling the display and tooltip content for the Approval in a Command.       
/// </summary>
public class ApprovalGuiElement : GuiElement, IComparable<ApprovalGuiElement> {

    private static string _unknown = Constants.QuestionMark;

    /// <summary>
    /// Tooltip format. Approval percentage aka 100%.
    /// </summary>
    private static string _tooltipFormat = "{0}";

    private string _tooltipContent;
    protected override string TooltipContent { get { return _tooltipContent; } }

    private bool _isApprovalSet;
    private float? _approval;
    public float? Approval {
        get { return _approval; }
        set {
            _approval = value;
            OnApprovalSet();
        }
    }

    private bool AreAllValuesSet { get { return _isApprovalSet; } }

    private UISlider _slider;
    private UISprite _barForeground;

    protected override void Awake() {
        base.Awake();
        Validate();
        _slider = gameObject.GetSafeMonoBehaviourInChildren<UISlider>();
        _barForeground = _slider.gameObject.GetSafeMonoBehaviourInImmediateChildren<UISprite>();
    }

    private void OnApprovalSet() {
        if (Approval.HasValue) {
            Arguments.ValidateForRange(Approval.Value, Constants.ZeroF, Constants.OneF);
        }
        _isApprovalSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    private void PopulateElementWidgets() {
        GameColor barColor = GameColor.Clear;   // appears disabled
        string approvalTooltip = _unknown;
        if (Approval.HasValue) {
            _slider.value = Approval.Value;
            approvalTooltip = Constants.FormatPercent_0Dp.Inject(Approval.Value);
            D.Log("{0} setting slider value to {1:0.#}.", GetType().Name, Approval.Value);
            if (Approval.Value > GeneralSettings.Instance.ContentApprovalThreshold) {
                barColor = GameColor.Green;
            }
            else if (Approval.Value > GeneralSettings.Instance.UnhappyApprovalThreshold) {
                barColor = GameColor.White;
            }
            else if (Approval.Value > GeneralSettings.Instance.RevoltApprovalThreshold) {
                barColor = GameColor.Yellow;
            }
            else {
                barColor = GameColor.Red;
            }
        }
        D.Log("{0} setting barColor to {1}.", GetType().Name, barColor.GetName());
        _barForeground.color = barColor.ToUnityColor();
        _tooltipContent = _tooltipFormat.Inject(approvalTooltip);
    }

    public override void Reset() {
        base.Reset();
        _isApprovalSet = false;
    }

    private void Validate() {
        if (elementID != GuiElementID.Approval) {
            D.Warn("{0}.ID = {1}. Fixing...", GetType().Name, elementID.GetName());
            elementID = GuiElementID.Approval;
        }
    }

    public override string ToString() { return GetType().Name + Constants.Space + TooltipContent; }

    #region IComparable<ApprovalGuiElement> Members

    public int CompareTo(ApprovalGuiElement other) {
        int result;
        if (!Approval.HasValue) {
            result = !other.Approval.HasValue ? Constants.Zero : Constants.MinusOne;
        }
        else {
            result = !other.Approval.HasValue ? Constants.One : Approval.Value.CompareTo(other.Approval.Value);
        }
        return result;
    }

    #endregion

}

