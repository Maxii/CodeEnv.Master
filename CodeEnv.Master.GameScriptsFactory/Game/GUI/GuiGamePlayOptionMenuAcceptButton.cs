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
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Accept button script for the GamePlayOptionsMenu.
/// </summary>
public class GuiGamePlayOptionMenuAcceptButton : AGuiMenuAcceptButton {

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

    protected override void RecordCheckboxState(GuiMenuElementID checkboxID, bool checkedState) {
        base.RecordCheckboxState(checkboxID, checkedState);
        switch (checkboxID) {
            case GuiMenuElementID.PauseOnLoadCheckbox:
                _isPauseOnLoadEnabled = checkedState;
                break;
            case GuiMenuElementID.CameraRollCheckbox:
                _isCameraRollEnabled = checkedState;
                break;
            case GuiMenuElementID.ResetOnFocusCheckbox:
                _isResetOnFocusEnabled = checkedState;
                break;
            case GuiMenuElementID.ZoomOutOnCursorCheckbox:
                _isZoomOutOnCursorEnabled = checkedState;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
        }
    }

    protected override void RecordPopupListState(GuiMenuElementID popupListID, string selectionName) {
        base.RecordPopupListState(popupListID, selectionName);
        switch (popupListID) {
            case GuiMenuElementID.GameSpeedOnLoadPopupList:
                _gameSpeedOnLoad = Enums<GameClockSpeed>.Parse(selectionName);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
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

