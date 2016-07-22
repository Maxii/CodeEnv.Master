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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// GuiElement handling the display and tooltip content for the Approval in a Command.       
/// </summary>
public class ApprovalGuiElement : AProgressBarGuiElement, IComparable<ApprovalGuiElement> {

    /// <summary>
    /// Format for the alternative display of values. Approval percentage aka 100%.
    /// </summary>
    private static string _detailValuesFormat = "{0}";

    private bool _isApprovalSet;
    private float? _approval;
    public float? Approval {
        get { return _approval; }
        set {
            D.Assert(!_isApprovalSet); // should only happen once between Resets
            _approval = value;
            ApprovalPropSetHandler();   // SetProperty() only calls handler when changed
        }
    }

    public override GuiElementID ElementID { get { return GuiElementID.Approval; } }

    protected override bool AreAllValuesSet { get { return _isApprovalSet; } }

    #region Event and Property Change Handlers

    private void ApprovalPropSetHandler() {
        if (Approval.HasValue) {
            Utility.ValidateForRange(Approval.Value, Constants.ZeroPercent, Constants.OneHundredPercent);
        }
        _isApprovalSet = true;
        if (AreAllValuesSet) {
            PopulateElementWidgets();
        }
    }

    #endregion

    protected override void PopulateElementWidgets() {
        if (!Approval.HasValue) {
            HandleValuesUnknown();
            return;
        }

        float approvalValue = Approval.Value;
        GameColor approvalColor;
        //D.Log("{0} setting slider value to {1:0.#}.", GetType().Name, Approval.Value);
        if (approvalValue > GeneralSettings.Instance.ContentApprovalThreshold) {
            approvalColor = GameColor.Green;    // Happy
        }
        else if (approvalValue > GeneralSettings.Instance.UnhappyApprovalThreshold) {
            approvalColor = GameColor.White;    // Content
        }
        else if (approvalValue > GeneralSettings.Instance.RevoltApprovalThreshold) {
            approvalColor = GameColor.Yellow;   // Unhappy
        }
        else {
            approvalColor = GameColor.Red;  // Revolt
        }

        string approvalValuePercentText = Constants.FormatPercent_0Dp.Inject(approvalValue);
        _detailValuesContent = _detailValuesFormat.Inject(approvalValuePercentText);
        var detailValuesContent_Colored = _detailValuesFormat.Inject(approvalValuePercentText.SurroundWith(approvalColor));

        switch (_widgetsPresent) {
            case WidgetsPresent.ProgressBar:
                PopulateProgressBarValues(approvalValue, approvalColor);
                _tooltipContent = detailValuesContent_Colored;
                break;
            case WidgetsPresent.Label:
                _detailValuesLabel.text = detailValuesContent_Colored;
                break;
            case WidgetsPresent.Both:
                PopulateProgressBarValues(approvalValue, approvalColor);
                _detailValuesLabel.text = detailValuesContent_Colored;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(_widgetsPresent));
        }
    }

    public override void Reset() {
        _isApprovalSet = false;
    }

    protected override void Cleanup() { }

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

