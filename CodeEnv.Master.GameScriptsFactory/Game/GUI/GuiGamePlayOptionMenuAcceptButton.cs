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

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


// default namespace

using System;
using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiGamePlayOptionMenuAcceptButton : GuiMenuAcceptButtonBase {

    private bool isCameraRollEnabled;
    private bool isZoomOutOnCursorEnabled;
    private bool isResetOnFocusEnabled;
    private bool isPauseOnLoadEnabled;

    private GameClockSpeed gameSpeedOnLoad;

    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        tooltip = "Click to implement Option changes.";
    }

    protected override void CaptureInitializedState() {
        base.CaptureInitializedState();
        ValidateState();
    }

    protected override void RecordCheckboxState(string checkboxName, bool checkedState) {
        if (checkboxName.Contains("roll")) {
            isCameraRollEnabled = checkedState;
        }
        else if (checkboxName.Contains("zoom")) {
            isZoomOutOnCursorEnabled = checkedState;
        }
        else if (checkboxName.Contains("reset")) {
            isResetOnFocusEnabled = checkedState;
        }
        else if (checkboxName.Contains("pause")) {
            isPauseOnLoadEnabled = checkedState;
        }
        // more checkboxes here
    }

    protected override void RecordPopupListState(string selectionName) {
        GameClockSpeed _gameSpeedOnLoad;
        if (Enums<GameClockSpeed>.TryParse(selectionName, true, out _gameSpeedOnLoad)) {
            //UnityEngine.Debug.Log("GameClockSpeedOnLoad recorded as {0}.".Inject(selectionName));
            gameSpeedOnLoad = _gameSpeedOnLoad;
        }
        // more popupLists here
    }

    protected override void RecordSliderState(float sliderValue) {
        // UNDONE
    }

    protected override void OnCheckboxStateChange(bool state) {
        base.OnCheckboxStateChange(state);
    }

    protected override void OnPopupListSelectionChange(string item) {
        base.OnPopupListSelectionChange(item);
        ValidateState();
    }

    protected override void OnSliderValueChange(float value) {
        base.OnSliderValueChange(value);
    }

    protected override void OnButtonClick(GameObject sender) {
        OptionSettings settings = new OptionSettings();
        settings.IsCameraRollEnabled = isCameraRollEnabled;
        settings.IsPauseOnLoadEnabled = isPauseOnLoadEnabled;
        settings.IsResetOnFocusEnabled = isResetOnFocusEnabled;
        settings.IsZoomOutOnCursorEnabled = isZoomOutOnCursorEnabled;
        settings.GameSpeedOnLoad = gameSpeedOnLoad;
        ValidateState();
        eventMgr.Raise<OptionChangeEvent>(new OptionChangeEvent(this, settings));
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(gameSpeedOnLoad != GameClockSpeed.None);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

