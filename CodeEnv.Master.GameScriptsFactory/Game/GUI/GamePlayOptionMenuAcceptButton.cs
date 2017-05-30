// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GamePlayOptionMenuAcceptButton.cs
// Accept button for the GamePlayOptionsMenu.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Accept button for the GamePlayOptionsMenu.
/// </summary>
public class GamePlayOptionMenuAcceptButton : AGuiMenuAcceptButton {

    protected override IList<KeyCode> ValidKeys { get { return new List<KeyCode>() { KeyCode.Return }; } }

    protected override string TooltipContent { get { return "Accept Option changes."; } }

    private bool _isCameraRollEnabled;
    private bool _isZoomOutOnCursorEnabled;
    private bool _isResetOnFocusEnabled;
    private bool _isPauseOnLoadEnabled;
    private GameSpeed _gameSpeedOnLoad;

    protected override void RecordCheckboxState(GuiElementID checkboxID, bool isChecked) {
        base.RecordCheckboxState(checkboxID, isChecked);
        switch (checkboxID) {
            case GuiElementID.PauseOnLoadCheckbox:
                _isPauseOnLoadEnabled = isChecked;
                break;
            case GuiElementID.CameraRollCheckbox:
                _isCameraRollEnabled = isChecked;
                break;
            case GuiElementID.ResetOnFocusCheckbox:
                _isResetOnFocusEnabled = isChecked;
                break;
            case GuiElementID.ZoomOutOnCursorCheckbox:
                _isZoomOutOnCursorEnabled = isChecked;
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(checkboxID));
        }
    }

    protected override void RecordPopupListState(GuiElementID popupListID, string selection, string convertedSelection) {
        base.RecordPopupListState(popupListID, selection, convertedSelection);
        switch (popupListID) {
            case GuiElementID.GameSpeedOnLoadPopupList:
                _gameSpeedOnLoad = Enums<GameSpeed>.Parse(selection);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    #region Event and Property Change Handlers

    protected override void HandleValidClick() {
        base.HandleValidClick();
        GamePlayOptionSettings settings = new GamePlayOptionSettings() {
            IsCameraRollEnabled = _isCameraRollEnabled,
            IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled,
            IsResetOnFocusEnabled = _isResetOnFocusEnabled,
            IsPauseOnLoadEnabled = _isPauseOnLoadEnabled,
            GameSpeedOnLoad = _gameSpeedOnLoad
        };
        _playerPrefsMgr.RecordGamePlayOptions(settings);
    }

    #endregion

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.AssertNotDefault((int)_gameSpeedOnLoad);
    }

    protected override void Cleanup() { }


}

