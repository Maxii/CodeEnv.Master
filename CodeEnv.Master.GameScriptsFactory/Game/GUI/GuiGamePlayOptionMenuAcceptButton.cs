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

#define DEBUG_LOG
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

    protected override string TooltipContent {
        get { return "Accept Option changes."; }
    }

    private bool _isCameraRollEnabled;
    private bool _isZoomOutOnCursorEnabled;
    private bool _isResetOnFocusEnabled;
    private bool _isPauseOnLoadEnabled;

    private GameClockSpeed _gameSpeedOnLoad;

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
        // TODO more checkboxes here
    }

    protected override void RecordPopupListState(string popupListName, string selectionName) {
        GameClockSpeed gameSpeedOnLoad;
        if (Enums<GameClockSpeed>.TryParse(selectionName, true, out gameSpeedOnLoad)) {
            _gameSpeedOnLoad = gameSpeedOnLoad;
        }
        // TODO more popupLists here
    }

    protected override void OnPopupListSelectionChange() {
        base.OnPopupListSelectionChange();
        ValidateState();
    }

    protected override void OnLeftClick() {
        GamePlayOptionSettings settings = new GamePlayOptionSettings() {
            IsCameraRollEnabled = _isCameraRollEnabled,
            IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled,
            IsResetOnFocusEnabled = _isResetOnFocusEnabled,
            IsPauseOnLoadEnabled = _isPauseOnLoadEnabled,
            GameSpeedOnLoad = _gameSpeedOnLoad
        };
        ValidateState();
        _playerPrefsMgr.RecordGamePlayOptions(settings);
    }

    [Conditional("UNITY_EDITOR")]
    private void ValidateState() {
        D.Assert(_gameSpeedOnLoad != GameClockSpeed.None);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

