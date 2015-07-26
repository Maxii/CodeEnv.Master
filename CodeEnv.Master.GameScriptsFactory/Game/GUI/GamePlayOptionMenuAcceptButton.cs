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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;

/// <summary>
/// Accept button for the GamePlayOptionsMenu.
/// </summary>
public class GamePlayOptionMenuAcceptButton : AGuiMenuAcceptButton {

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

    protected override void RecordPopupListState(GuiElementID popupListID, string selection) {
        base.RecordPopupListState(popupListID, selection);
        switch (popupListID) {
            case GuiElementID.GameSpeedOnLoadPopupList:
                _gameSpeedOnLoad = Enums<GameSpeed>.Parse(selection);
                break;
            default:
                throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(popupListID));
        }
    }

    protected override void OnLeftClick() {
        base.OnLeftClick();
        GamePlayOptionSettings settings = new GamePlayOptionSettings() {
            IsCameraRollEnabled = _isCameraRollEnabled,
            IsZoomOutOnCursorEnabled = _isZoomOutOnCursorEnabled,
            IsResetOnFocusEnabled = _isResetOnFocusEnabled,
            IsPauseOnLoadEnabled = _isPauseOnLoadEnabled,
            GameSpeedOnLoad = _gameSpeedOnLoad
        };
        _playerPrefsMgr.RecordGamePlayOptions(settings);
    }

    protected override void ValidateStateOnCapture() {
        base.ValidateStateOnCapture();
        D.Assert(_gameSpeedOnLoad != GameSpeed.None);
    }

    protected override void Cleanup() { }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}

