// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DialogWindow.cs
// Singleton. AGuiWindow that handles dialogs of various types that popup in the center of the screen.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using System;

/// <summary>
/// Singleton. AGuiWindow that handles dialogs of various types that popup in the center of the screen.
/// <remarks>Operates as a modal popup that blocks all other interaction with the screen through the use of a 
/// 'Blocking Background and Collider'. Allows for dedicated forms that contain their own menu controls.</remarks>
/// <remarks>Also automatically handles pausing - RequestsPause onShowBegin, RequestsUnpause onHideComplete.</remarks>
/// </summary>
public class DialogWindow : AHudWindow<DialogWindow> {

    #region Consolidated User Pick Design Support

    private Action<AUnitCmdModuleDesign> _onCmdModDesignPicked;
    private Action<AUnitElementDesign> _onElementDesignPicked;

    /// <summary>
    /// Prompts the user to pick a CmdModuleDesign for a newly created Command.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    /// <param name="text">The text.</param>
    /// <param name="cancelDelegate">The cancel delegate.</param>
    /// <param name="onPicked">Action to execute when picked.</param>
    /// <param name="useUserActionButton">if set to <c>true</c> [use user action button].</param>
    public void HaveUserPickCmdModDesign(FormID formId, string text, EventDelegate cancelDelegate, Action<AUnitCmdModuleDesign> onPicked,
        bool useUserActionButton) {
        D.Assert(!DebugControls.Instance.AiChoosesUserCmdModInitialDesigns);
        // Can't Assert that _onPicked is null as cancelDialog is created in client and has no access to it
        _onCmdModDesignPicked = onPicked;
        EventDelegate acceptDelegate = new EventDelegate(this, "HandleUserPickedCmdModDesign");
        var settings = new APopupDialogForm.DialogSettings(GameManager.Instance.UserPlayer, acceptDelegate, cancelDelegate) {
            Text = text,
            OptionalParameter = null    // indicates design is for a newly created Command
        };

        if (useUserActionButton) {
            UserActionButton.Instance.ShowPickDesignPromptButton(formId, settings);
        }
        else {
            Show(formId, settings);
        }
    }

    /// <summary>
    /// Prompts the user to pick a CmdModuleDesign to refit an existing Command.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    /// <param name="text">The text.</param>
    /// <param name="cancelDelegate">The cancel delegate.</param>
    /// <param name="existingDesign">The existing design that needs to be refit.</param>
    /// <param name="onPicked">Action to execute when picked.</param>
    /// <param name="useUserActionButton">if set to <c>true</c> [use user action button].</param>
    public void HaveUserPickCmdModRefitDesign(FormID formId, string text, EventDelegate cancelDelegate, AUnitCmdModuleDesign existingDesign,
        Action<AUnitCmdModuleDesign> onPicked, bool useUserActionButton) {
        D.Assert(!DebugControls.Instance.AiChoosesUserCmdModRefitDesigns);
        // Can't Assert that _onPicked is null as cancelDialog is created in client and has no access to it
        _onCmdModDesignPicked = onPicked;
        EventDelegate acceptDelegate = new EventDelegate(this, "HandleUserPickedCmdModDesign");
        var settings = new APopupDialogForm.DialogSettings(GameManager.Instance.UserPlayer, acceptDelegate, cancelDelegate) {
            Text = text,
            OptionalParameter = existingDesign // indicates design is for a refit of an existing Command
        };

        if (useUserActionButton) {
            UserActionButton.Instance.ShowPickDesignPromptButton(formId, settings);
        }
        else {
            Show(formId, settings);
        }
    }

    private void HandleUserPickedCmdModDesign(AUnitCmdModuleDesign pickedDesign) {
        Hide();
        _onCmdModDesignPicked(pickedDesign);
        _onCmdModDesignPicked = null;
    }

    /// <summary>
    /// Prompts the user to pick a CentralHubFacilityDesign for a newly created Base.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="cancelDelegate">The cancel delegate.</param>
    /// <param name="onPicked">Action to execute when picked.</param>
    /// <param name="useUserActionButton">if set to <c>true</c> [use user action button].</param>
    public void HaveUserPickCentralHubFacilityDesign(string text, EventDelegate cancelDelegate, Action<AUnitElementDesign> onPicked,
        bool useUserActionButton) {
        D.Assert(!DebugControls.Instance.AiChoosesUserCentralHubInitialDesigns);
        // Can't Assert that _onPicked is null as cancelDialog is created in client and has no access to it
        _onElementDesignPicked = onPicked;
        EventDelegate acceptDelegate = new EventDelegate(this, "HandleUserPickedElementDesign");
        var settings = new APopupDialogForm.DialogSettings(GameManager.Instance.UserPlayer, acceptDelegate, cancelDelegate) {
            Text = text,
            OptionalParameter = null    // indicates design is for a newly created Command
        };

        if (useUserActionButton) {
            UserActionButton.Instance.ShowPickDesignPromptButton(FormID.SelectFacilityDesignDialog, settings);
        }
        else {
            Show(FormID.SelectFacilityDesignDialog, settings);
        }
    }

    /// <summary>
    /// Prompts the user to pick a ElementDesign to refit an existing Element.
    /// </summary>
    /// <param name="formId">The form identifier.</param>
    /// <param name="text">The text.</param>
    /// <param name="cancelDelegate">The cancel delegate.</param>
    /// <param name="existingDesign">The existing design that needs to be refit.</param>
    /// <param name="onPicked">Action to execute when picked.</param>
    /// <param name="useUserActionButton">if set to <c>true</c> [use user action button].</param>
    public void HaveUserPickElementRefitDesign(FormID formId, string text, EventDelegate cancelDelegate, AUnitElementDesign existingDesign,
        Action<AUnitElementDesign> onPicked, bool useUserActionButton) {
        D.Assert(!DebugControls.Instance.AiChoosesUserElementRefitDesigns);
        // Can't Assert that _onPicked is null as cancelDialog is created in client and has no access to it
        _onElementDesignPicked = onPicked;
        EventDelegate acceptDelegate = new EventDelegate(this, "HandleUserPickedElementDesign");
        var settings = new APopupDialogForm.DialogSettings(GameManager.Instance.UserPlayer, acceptDelegate, cancelDelegate) {
            Text = text,
            OptionalParameter = existingDesign // indicates design is for a refit of an existing Element
        };

        if (useUserActionButton) {
            UserActionButton.Instance.ShowPickDesignPromptButton(formId, settings);
        }
        else {
            Show(formId, settings);
        }
    }

    private void HandleUserPickedElementDesign(AUnitElementDesign pickedDesign) {
        Hide();
        _onElementDesignPicked(pickedDesign);
        _onElementDesignPicked = null;
    }

    #endregion

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            // Widget Buttons and/or EnvelopContent move and scale
        }
    }

    public void Show(FormID formID, APopupDialogForm.DialogSettings settings) {
        var form = PrepareForm(formID);
        (form as APopupDialogForm).Settings = settings;
        ShowForm(form);
    }

    protected override void PositionWindow() {
        // do nothing as Window is anchored
    }

    protected override void HandleShowBegin() {
        base.HandleShowBegin();
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    protected override void HandleHideComplete() {
        base.HandleHideComplete();
        // Calls ResetForReuse from base class AGuiWindow
        _gameMgr.RequestPauseStateChange(toPause: false);
    }

}

