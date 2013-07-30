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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// COMMENT 
/// </summary>
public class GuiGamePlayOptionMenuAcceptButton : AGuiMenuAcceptButtonBase {

    private bool _isCameraRollEnabled;
    private bool _isZoomOutOnCursorEnabled;
    private bool _isResetOnFocusEnabled;
    private bool _isPauseOnLoadEnabled;

    private GameClockSpeed _gameSpeedOnLoad;

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
            _isCameraRollEnabled = checkedState;
        }
        else if (checkboxName.Contains("zoom")) {
            _isZoomOutOnCursorEnabled = checkedState;
        }
        else if (checkboxName.Contains("reset")) {
            _isResetOnFocusEnabled = checkedState;
        }
        else if (checkboxName.Contains("pause")) {
            _isPauseOnLoadEnabled = checkedState;
        }
        // more checkboxes here
    }

    protected override void RecordPopupListState(string selectionName) {
        GameClockSpeed gameSpeedOnLoad;
        if (Enums<GameClockSpeed>.TryParse(selectionName, true, out gameSpeedOnLoad)) {
            //UnityEngine.Logger.Log("GameClockSpeedOnLoad recorded as {0}.".Inject(selectionName));
            _gameSpeedOnLoad = gameSpeedOnLoad;
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
        settings.IsCameraRollEnabled = _isCameraRollEnabled;
        settings.IsPauseOnLoadEnabled = _isPauseOnLoadEnabled;
        settings.IsResetOnFocusEnabled = _isResetOnFocusEnabled;
        settings.IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled;
        settings.GameSpeedOnLoad = _gameSpeedOnLoad;
        ValidateState();
        eventMgr.Raise<GamePlayOptionsAcceptedEvent>(new GamePlayOptionsAcceptedEvent(this, settings));
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(_gameSpeedOnLoad != GameClockSpeed.None);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

