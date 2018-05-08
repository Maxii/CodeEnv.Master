// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SelectDesignScreenDialogForm.cs
// APopupDialogForm that allows selection of a DesignScreen to show.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System;
using UnityEngine;

/// <summary>
/// APopupDialogForm that allows selection of a DesignScreen to show.
/// <remarks>An experiment and example of how to use Ngui's EventDelegate. 
/// See DesignScreensManager Archive for usage.</remarks>
/// </summary>
[Obsolete("Not currently used")]
public class SelectDesignScreenDialogForm : APopupDialogForm {

    private const int ScreenChoiceCheckboxGroupNumber = 1;

    [SerializeField]
    private UIButton _acceptButton = null;

    [SerializeField]
    private UIButton _cancelButton = null;

    public override FormID FormID { get { return FormID.SelectDesignScreenDialog; } }

    protected override void InitializeValuesAndReferences() { }

    protected override void AssignValuesToMembers() {
        base.AssignValuesToMembers();
        var screenChoiceCheckboxes = GetComponentsInChildren<UIToggle>();
        D.AssertEqual(5, screenChoiceCheckboxes.Length);
        foreach (var checkbox in screenChoiceCheckboxes) {
            checkbox.group = ScreenChoiceCheckboxGroupNumber;
        }
    }

    protected override void InitializeMenuControls() {
        D.Assert(Settings.ShowAcceptButton);
        D.Assert(Settings.ShowCancelButton);

        D.AssertNotNull(Settings.AcceptButtonDelegate);
        EventDelegate.Set(_acceptButton.onClick, HandleAcceptButtonClicked);

        // The CancelButtonDelegate is optional. If not used, the buttons themselves or this class will need to Hide the window.
        D.AssertNotNull(Settings.CancelButtonDelegate);
        EventDelegate.Set(_cancelButton.onClick, Settings.CancelButtonDelegate);
    }

    private void HandleAcceptButtonClicked() {
        UIToggle selectedCheckbox = UIToggle.GetActiveToggle(ScreenChoiceCheckboxGroupNumber);
        GuiElementID chosenCheckboxID = selectedCheckbox.GetComponent<GuiElement>().ElementID;

        EventDelegate acceptButtonDelegate = Settings.AcceptButtonDelegate;

        D.AssertNotNull(acceptButtonDelegate.parameters);   // parameters will be null unless the Delegate's method expects a parameter

        var parameter = new EventDelegate.Parameter(chosenCheckboxID);
        acceptButtonDelegate.parameters.SetValue(parameter, 0);
        acceptButtonDelegate.Execute();
    }

    protected override void UnsubscribeFromMenuControls() {
        EventDelegate.Remove(_acceptButton.onClick, Settings.AcceptButtonDelegate);
        if (Settings.CancelButtonDelegate != null) {
            EventDelegate.Remove(_cancelButton.onClick, Settings.CancelButtonDelegate);
        }
    }

    protected override void ResetForReuse_Internal() {
        base.ResetForReuse_Internal();
        // nothing to reset beside menu control subscriptions
    }

    protected override void Cleanup() {
        base.Cleanup();
        // nothing to cleanup beside menu control subscriptions
    }

    #region Debug

    protected override void __ValidateOnAwake() {
        base.__ValidateOnAwake();
        D.AssertNotNull(_acceptButton);
        D.AssertNotNull(_cancelButton);
    }

    #endregion

}

