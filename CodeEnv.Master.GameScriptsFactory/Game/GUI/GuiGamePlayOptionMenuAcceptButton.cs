// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GuiGamePlayOptionMenuAcceptButton.cs
// Accept button script for the GamePlayOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using System.Diagnostics;
using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Accept button script for the GamePlayOptionsMenu.
/// </summary>
public class GuiGamePlayOptionMenuAcceptButton : AGuiMenuAcceptButtonBase {

    private bool _isCameraRollEnabled;
    private bool _isZoomOutOnCursorEnabled;
    private bool _isResetOnFocusEnabled;
    private bool _isPauseOnLoadEnabled;

    private GameClockSpeed _gameSpeedOnLoad;

    protected override void Awake() {
        base.Awake();
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
        else if (checkboxName.Contains("focus")) {
            _isResetOnFocusEnabled = checkedState;
        }
        else if (checkboxName.Contains("pause")) {
            _isPauseOnLoadEnabled = checkedState;
        }
        else {
            D.Error("Name of Checkbox {0} not found.", checkboxName);
        }
        // more checkboxes here
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        GameClockSpeed gameSpeedOnLoad;
        if (Enums<GameClockSpeed>.TryParse(selectionName, true, out gameSpeedOnLoad)) {
            //UnityEngine.Logger.Log("GameClockSpeedOnLoad recorded as {0}.".Inject(selectionName));
            _gameSpeedOnLoad = gameSpeedOnLoad;
        }
        // more popupLists here
    }

    protected override void OnPopupListSelectionChange(string item) {
        base.OnPopupListSelectionChange(item);
        ValidateState();
    }

    protected override void OnLeftClick() {
        GamePlayOptionSettings settings = new GamePlayOptionSettings();
        settings.IsCameraRollEnabled = _isCameraRollEnabled;
        settings.IsPauseOnLoadEnabled = _isPauseOnLoadEnabled;
        settings.IsResetOnFocusEnabled = _isResetOnFocusEnabled;
        settings.IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled;
        settings.GameSpeedOnLoad = _gameSpeedOnLoad;
        ValidateState();
        _eventMgr.Raise<GamePlayOptionsAcceptedEvent>(new GamePlayOptionsAcceptedEvent(this, settings));
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(_gameSpeedOnLoad != GameClockSpeed.None);
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

