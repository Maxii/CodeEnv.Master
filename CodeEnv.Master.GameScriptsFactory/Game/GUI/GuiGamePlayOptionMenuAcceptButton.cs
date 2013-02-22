// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGamePlayOptionMenuAcceptButton.cs
// COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.Common.Unity;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiGamePlayOptionMenuAcceptButton : GuiMenuAcceptButtonBase {

    private bool isCameraRollEnabled;
    private bool isZoomOutOnCursorEnabled;
    private bool isResetOnFocusEnabled;
    private bool isPauseOnLoadEnabled;

    private GameClockSpeed gameSpeedOnLoad;

    protected override void Initialize() {
        base.Initialize();
        tooltip = "Click to implement Option changes.";
    }

    protected override void OnCheckboxStateChange(bool state) {
        string checkboxName = UICheckbox.current.name.ToLower();
        //Debug.Log("Checkbox Named {0} had a state change to {1}.".Inject(checkboxName, state));
        if (checkboxName.Contains("roll")) {
            isCameraRollEnabled = state;
        }
        else if (checkboxName.Contains("zoom")) {
            isZoomOutOnCursorEnabled = state;
        }
        else if (checkboxName.Contains("focus")) {
            isResetOnFocusEnabled = state;
        }
        else if (checkboxName.Contains("pause")) {
            isPauseOnLoadEnabled = state;
        }
    }

    protected override void OnPopupListSelectionChange(string item) {
        // UIPopupList.current gives the reference to the sender, but there is nothing except names to distinguish them
        if (Enums<GameClockSpeed>.TryParse(item, true, out gameSpeedOnLoad)) {
            //Debug.Log("GameClockSpeedOnLoad {0} PopupList change event received by {1}.".Inject(item, typeof(GuiGamePlayOptionMenuAcceptButton).Name));
            return;
        }
        // more popupLists here
    }

    protected override void OnSliderValueChange(float value) { }

    protected override void OnButtonClick(GameObject sender) {
        OptionSettings settings = new OptionSettings();
        settings.IsCameraRollEnabled = isCameraRollEnabled;
        settings.IsPauseOnLoadEnabled = isPauseOnLoadEnabled;
        settings.IsResetOnFocusEnabled = isResetOnFocusEnabled;
        settings.IsZoomOutOnCursorEnabled = isZoomOutOnCursorEnabled;
        settings.GameSpeedOnLoad = gameSpeedOnLoad;
        eventMgr.Raise<OptionChangeEvent>(new OptionChangeEvent(settings));
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

